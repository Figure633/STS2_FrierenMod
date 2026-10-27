using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

// 只读取元数据，不加载游戏模块初始化器，不调用 Godot、不接触存档。
// 同一 DLL 的 sts2 成员引用必须能在两套真实程序集上按完整签名解析。
if(args.Length!=3) throw new ArgumentException("参数：模组DLL 游戏data目录 BaseLib目录");
var modPath=Path.GetFullPath(args[0]);var gamePath=Path.GetFullPath(args[1]);
var paths=((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
    .Concat(Directory.GetFiles(gamePath,"*.dll")).Concat(Directory.GetFiles(Path.GetFullPath(args[2]),"*.dll"))
    .Append(modPath).GroupBy(Path.GetFileName,StringComparer.OrdinalIgnoreCase).Select(g=>g.First());
using var context=new MetadataLoadContext(new PathAssemblyResolver(paths),"System.Private.CoreLib");
var game=context.LoadFromAssemblyPath(Path.Combine(gamePath,"sts2.dll"));
var mod=context.LoadFromAssemblyPath(modPath);
using var pe=new PEReader(File.OpenRead(modPath));var reader=pe.GetMetadataReader();var provider=new Names();
int count=0;var failures=new List<string>();
const BindingFlags flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance|BindingFlags.FlattenHierarchy;
foreach(var handle in reader.MemberReferences) {
    var reference=reader.GetMemberReference(handle);
    if(reference.Parent.Kind!=HandleKind.TypeReference) continue;
    var typeRef=(TypeReferenceHandle)reference.Parent;
    if(AssemblyName(reader,typeRef)!="sts2")continue;
    count++;
    var typeName=Names.TypeName(reader,typeRef);var name=reader.GetString(reference.Name);
    var type=game.GetType(typeName);
    if(type==null){failures.Add("类型不存在："+typeName);continue;}
    if(reference.GetKind()==MemberReferenceKind.Field) {
        string expected=reference.DecodeFieldSignature(provider,null);
        if(!type.GetFields(flags).Any(f=>f.Name==name && Names.For(f.FieldType)==expected))
            failures.Add($"字段不存在：{typeName}.{name} : {expected}");
    }
    else {
        var signature=reference.DecodeMethodSignature(provider,null);
        IEnumerable<MethodBase> candidates=name==".ctor"?type.GetConstructors(flags):type.GetMethods(flags).Where(m=>m.Name==name);
        bool found=candidates.Any(m=>
            (m is MethodInfo method?Names.For(method.ReturnType):"System.Void")==signature.ReturnType &&
            m.GetParameters().Select(p=>Names.For(p.ParameterType)).SequenceEqual(signature.ParameterTypes));
        if(!found) failures.Add($"方法不存在：{typeName}.{name}({string.Join(",",signature.ParameterTypes)}) : {signature.ReturnType}");
    }
}
int overrides=0;
foreach(var type in mod.GetTypes().Where(t=>!t.IsInterface))foreach(var method in type.GetMethods(flags|BindingFlags.DeclaredOnly)) {
    if(!method.IsVirtual || (method.Attributes&MethodAttributes.NewSlot)!=0)continue;
    overrides++;bool found=false;
    for(var parent=type.BaseType;parent!=null && !found;parent=parent.BaseType)
        found=parent.GetMethods(flags|BindingFlags.DeclaredOnly).Any(m=>m.IsVirtual && m.Name==method.Name &&
            Names.For(m.ReturnType)==Names.For(method.ReturnType) &&
            m.GetParameters().Select(p=>Names.For(p.ParameterType)).SequenceEqual(method.GetParameters().Select(p=>Names.For(p.ParameterType))));
    if(!found)failures.Add("覆盖的基类虚方法不存在："+type.FullName+"."+method.Name);
}
foreach(string failure in failures.Distinct())Console.WriteLine(failure);
if(failures.Count>0)throw new Exception($"{failures.Count} 个不兼容成员引用");
Console.WriteLine($"MEMBER_REFS_OK: {count} refs + {overrides} overrides; {gamePath}");

static string AssemblyName(MetadataReader r,TypeReferenceHandle handle) {
    var scope=r.GetTypeReference(handle).ResolutionScope;
    return scope.Kind switch {
        HandleKind.AssemblyReference=>r.GetString(r.GetAssemblyReference((AssemblyReferenceHandle)scope).Name),
        HandleKind.TypeReference=>AssemblyName(r,(TypeReferenceHandle)scope),_=>""
    };
}

sealed class Names : ISignatureTypeProvider<string,object?>
{
    public static string For(Type t) {
        if(t.IsGenericParameter)return (t.DeclaringMethod==null?"!":"!!")+t.GenericParameterPosition;
        if(t.IsByRef)return For(t.GetElementType()!)+"&";
        if(t.IsPointer)return For(t.GetElementType()!)+"*";
        if(t.IsArray)return For(t.GetElementType()!)+"["+new string(',',t.GetArrayRank()-1)+"]";
        if(t.IsGenericType)return t.GetGenericTypeDefinition().FullName+"<"+string.Join(",",t.GetGenericArguments().Select(For))+">";
        return t.FullName!;
    }
    public static string TypeName(MetadataReader r,TypeReferenceHandle h) {
        var t=r.GetTypeReference(h);var name=r.GetString(t.Name);
        return t.ResolutionScope.Kind==HandleKind.TypeReference?TypeName(r,(TypeReferenceHandle)t.ResolutionScope)+"+"+name:
            (r.GetString(t.Namespace) is {Length:>0} ns?ns+".":"")+name;
    }
    public string GetTypeFromReference(MetadataReader r,TypeReferenceHandle h,byte k)=>TypeName(r,h);
    public string GetTypeFromDefinition(MetadataReader r,TypeDefinitionHandle h,byte k) {
        var t=r.GetTypeDefinition(h);return r.GetString(t.Namespace)+"."+r.GetString(t.Name);
    }
    public string GetTypeFromSpecification(MetadataReader r,object? c,TypeSpecificationHandle h,byte k)=>r.GetTypeSpecification(h).DecodeSignature(this,c);
    public string GetArrayType(string t,ArrayShape s)=>t+"["+new string(',',s.Rank-1)+"]";
    public string GetSZArrayType(string t)=>t+"[]";
    public string GetByReferenceType(string t)=>t+"&";
    public string GetPointerType(string t)=>t+"*";
    public string GetPinnedType(string t)=>t;
    public string GetModifiedType(string m,string t,bool required)=>t;
    public string GetGenericInstantiation(string t,ImmutableArray<string> args)=>t+"<"+string.Join(",",args)+">";
    public string GetGenericMethodParameter(object? c,int i)=>"!!"+i;
    public string GetGenericTypeParameter(object? c,int i)=>"!"+i;
    public string GetFunctionPointerType(MethodSignature<string> s)=>"fnptr";
    public string GetPrimitiveType(PrimitiveTypeCode c)=>"System."+(c switch {PrimitiveTypeCode.Void=>"Void",PrimitiveTypeCode.Boolean=>"Boolean",PrimitiveTypeCode.Char=>"Char",PrimitiveTypeCode.SByte=>"SByte",PrimitiveTypeCode.Byte=>"Byte",PrimitiveTypeCode.Int16=>"Int16",PrimitiveTypeCode.UInt16=>"UInt16",PrimitiveTypeCode.Int32=>"Int32",PrimitiveTypeCode.UInt32=>"UInt32",PrimitiveTypeCode.Int64=>"Int64",PrimitiveTypeCode.UInt64=>"UInt64",PrimitiveTypeCode.Single=>"Single",PrimitiveTypeCode.Double=>"Double",PrimitiveTypeCode.String=>"String",PrimitiveTypeCode.IntPtr=>"IntPtr",PrimitiveTypeCode.UIntPtr=>"UIntPtr",PrimitiveTypeCode.Object=>"Object",PrimitiveTypeCode.TypedReference=>"TypedReference",_=>throw new ArgumentException(c.ToString())});
}
