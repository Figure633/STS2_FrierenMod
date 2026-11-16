using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using BaseLib.Patches.Saves;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace FrierenMod.Scripts.Shared.Compatibility;

// 某些模组在初始化期间查询并冻结 Run JSON 结构；此时 BaseLib 的
// SavedSpireField 尚未完成注册，原生缓存会永久缺少扩展存档属性。
// 注册结束后用公开的 options 复制接口重建元数据，保留现有 resolver、
// modifier 和存档格式，不另建存档系统，也不改变网络字段注册顺序。
internal static class SaveMetadataCompatibility
{
    private static readonly object Gate = new();
    private static bool _registrationCompleted;
    private static bool _checked;
    private static JsonSerializerOptions? _recoveredOptions;

    [HarmonyPatch(typeof(ModelDb), nameof(ModelDb.InitIds))]
    private static class RegistrationFinished
    {
        [HarmonyPostfix]
        private static void Postfix() => _registrationCompleted = true;
    }

    [HarmonyPatch(typeof(JsonSerializationUtility), nameof(JsonSerializationUtility.Options), MethodType.Getter)]
    private static class RecoverMissingMetadata
    {
        [HarmonyPostfix]
        private static void Postfix(ref JsonSerializerOptions __result)
        {
            if (!_registrationCompleted) return;
            lock (Gate) {
                if (!_checked) {
                    // 比较 BaseLib 实际注册的属性，不依赖特定版本的字段列表。
                    var cached = __result.GetTypeInfo(typeof(SerializablePlayer));
                    var names = cached.Properties.Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
                    bool missing = ExtendedSaveHandlers<Player, SerializablePlayer>
                        .CreateExtendedProperties(__result).Any(p => !names.Contains(p.Name));
                    if (missing) {
                        // .NET 会让配置相同的 options 副本共享旧缓存；独立 resolver
                        // 包装保证重新生成，同时完整调用原有的第三方 modifier。
                        _recoveredOptions = new JsonSerializerOptions(__result) {
                            TypeInfoResolver = __result.TypeInfoResolver!.WithAddedModifier(_ => { })
                        };
                        Log.Warn("FrierenMod: 检测到提前缓存的存档结构，已重建 JSON 元数据以保留 BaseLib 扩展存档。");
                    }
                    _checked = true;
                }
                if (_recoveredOptions != null) __result = _recoveredOptions;
            }
        }
    }
}
