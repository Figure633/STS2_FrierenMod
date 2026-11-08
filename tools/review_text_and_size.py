"""只读核对二代中文增补文本及实际打包体积；不改写游戏文本和原始资料。"""
from pathlib import Path
from collections import Counter,defaultdict
import json,re,difflib
from datetime import datetime, timezone, timedelta
ROOT=Path(__file__).resolve().parents[1]
VERSION=json.loads((ROOT/'FrierenMod.json').read_text('utf-8-sig'))['version']
DATE=datetime.now(timezone(timedelta(hours=8))).strftime('%Y-%m-%d')
OLD=ROOT.parent/'FrierenMod_STS1_source code/src/main/resources/FrierenModResources/localization/ZHS'
OUT=ROOT/'artifacts/text-and-size-review'
OUT.mkdir(parents=True,exist_ok=True)
def flatten(value,key=''):
 if isinstance(value,str):yield key,value
 elif isinstance(value,dict):
  for k,v in value.items():yield from flatten(v,f'{key}.{k}' if key else k)
 elif isinstance(value,list):
  for i,v in enumerate(value):yield from flatten(v,f'{key}[{i}]')
def norm(s):
 s=re.sub(r'\[[^\]]*\]|#[rgbyp]|[~@*]|\bNL\b','',s)
 return re.sub(r'[\s。，、：；！？,.!?:;“”"「」（）()\-]+','',s)
def ident(s):return re.sub('[^a-z0-9]','',s.lower().replace('frierenmod',''))
legacy=[]
for p in sorted(OLD.glob('*.json')):
 for key,text in flatten(json.loads(p.read_text('utf-8-sig'))):
  if text:legacy.append({'source':p.name,'key':key,'text':text,'norm':norm(text)})
exact={v['norm'] for v in legacy}
rows=[];total=0;unchanged=0
for p in sorted((ROOT/'FrierenMod/localization/zhs').glob('*.json')):
 for key,text in flatten(json.loads(p.read_text('utf-8-sig'))):
  total+=1;n=norm(text)
  if not n or n in exact:unchanged+=1;continue
  model=ident(key.split('.')[0]);pool=[v for v in legacy if ident(v['key'].split('.')[0])==model]
  if not pool:pool=[v for v in legacy if v['source'].lower()==p.name.lower()]
  if not pool:pool=legacy
  candidate=max(pool,key=lambda v:difflib.SequenceMatcher(None,n,v['norm'],autojunk=False).ratio()) if pool else None
  score=difflib.SequenceMatcher(None,n,candidate['norm'],autojunk=False).ratio() if candidate else 0
  # 相似度低时保留最近候选以供人审，不能据此声称有对应的一代原文。
  added=[]
  if candidate and score>=.35:
   for op,_,_,j1,j2 in difflib.SequenceMatcher(None,candidate['norm'],n,autojunk=False).get_opcodes():
    if op in ('insert','replace') and j2>j1:added.append(n[j1:j2])
  rows.append({'table':p.name,'key':key,'current':text,'legacy':candidate if score>=.35 else None,'similarity':round(score,3),'extraFragments':added,'status':'改写或增补待审' if score>=.35 else '未找到近似一代原文'})
(OUT/'text-differences.json').write_text(json.dumps({'total':total,'unchangedIgnoringFormat':unchanged,'differences':rows},ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
def cell(s):return s.replace('|','\\|').replace('\n','<br>')
lines=['# 二代中文增补文本核对清单','',f'核对日期：{DATE}。当前版本：{VERSION}。扫描 {total} 条中文本地化，{unchanged} 条与完整一代原文一致（忽略格式、空白和标点），其余 {len(rows)} 条全部列出。','',
'这是可复核的差异清单，不能仅凭措辞确认是谁写的或是否由 AI 生成。作者已确认的二代规则、新内容、原生界面所需提示和未经确认的扩写都会出现在差异中；本次不自动删除。近似一代文案用于对照，不等于已确认的映射。所有字段保留在同目录 JSON。','',
'优先检查事件、教程、按钮的补充限制说明，再检查卡牌数值或玩法变化。用户点名的“需要可以升级的牌”见事件中的禁用条件提示。','']
for table in ['events.json','frieren.json']+[p for p in sorted(set(v['table'] for v in rows)) if p not in ['events.json','frieren.json']]:
 group=[v for v in rows if v['table']==table]
 if not group:continue
 lines += [f'## {table}（{len(group)} 条）','','| ID | 二代当前全文 | 一代对照原文 | 归一化新增/替换片段 |','|---|---|---|---|']
 for v in group:
  old=v['legacy'];oldtext=(old['text']+' ['+old['source']+' / '+old['key']+']') if old else '未找到近似原文'
  lines.append('| '+' | '.join(cell(s) for s in [v['key'],v['current'],oldtext,' / '.join(v['extraFragments']) or '整段新增或需人工对照'])+' |')
# 本地化之外的硬编码中文也列入候选，避免遗漏界面文案和规则说明。
lines += ['','## 代码与场景中的中文字符串候选','','这里还包含异常、日志和开发诊断文字；仅列出候选，不把它们都当成玩家可见文案。注释排除，具体是否显示须按所在调用判断。','','| 来源 | 字符串 |','|---|---|']
code_count=0
for base in ['Scripts','Core','FrierenMod/scenes']:
 for p in sorted((ROOT/base).rglob('*')):
  if p.suffix not in ['.cs','.gd','.tscn']:continue
  content=p.read_text('utf-8-sig'); content=re.sub(r'/\*.*?\*/',lambda m:'\n'*m[0].count('\n'),content,flags=re.S)
  for number,line in enumerate(content.splitlines(),1):
   if line.lstrip().startswith('//') or line.lstrip().startswith('#'):continue
   for match in re.finditer(r'"(?:\\.|[^"\\])*"',line):
    text=match[0][1:-1]
    if re.search('[\u4e00-\u9fff]',text) and norm(text) not in exact:
     lines.append(f'| {p.relative_to(ROOT).as_posix()}:{number} | {cell(text)} |');code_count+=1
lines.insert(6,f'另列本地化之外中文字符串候选 {code_count} 处，包含诊断字符串，供按来源确认。')
(OUT/'text-differences.md').write_text('\n'.join(lines)+'\n',encoding='utf-8')
# 按 export_pack.gd 实际边界统计，原始图片和 Godot 导入副本分别计量。
files={p for p in (ROOT/'FrierenMod').rglob('*') if p.is_file() and 'fonts' not in p.relative_to(ROOT/'FrierenMod').parts}
for p in list(files):
 if p.suffix=='.import':
  for dest in re.findall(r'"(res://\.godot/imported/[^\"]+)"',p.read_text('utf-8-sig')):
   f=ROOT/dest.removeprefix('res://')
   if f.is_file():files.add(f)
groups=Counter();sizes=defaultdict(list)
for p in files:
 rel=p.relative_to(ROOT).as_posix();size=p.stat().st_size
 if rel.startswith('.godot/imported/'):
  group='字体导入副本' if p.suffix=='.fontdata' else '音频导入副本' if p.suffix in ['.oggvorbisstr','.mp3str'] else '纹理及其他Godot导入副本'
 elif rel.startswith('FrierenMod/audio/bgm/'):group='20首BGM原音频'
 elif rel.startswith('FrierenMod/audio/'):group='其他原音频'
 elif rel.startswith('FrierenMod/images/slot_backs/'):group='卡槽背景原图'
 elif rel.startswith('FrierenMod/images/'):group='其余原图片和图层'
 elif rel.startswith('FrierenMod/fonts/'):group='原字体'
 else:group='本地化、场景和其他资源'
 groups[group]+=size;sizes[group].append((size,rel))
size_lines=['# 模组体积核对','', f'{DATE}，{VERSION}。MiB = 1,048,576 字节。仅统计发行内容，不把工作区、原始资料、Git历史和测试输出当成安装大小。','', '| 发行文件 | MiB |','|---|---:|']
release=ROOT/'artifacts/FrierenMod'
for p in sorted(release.iterdir()):
 if p.is_file():size_lines.append(f'| {p.name} | {p.stat().st_size/1048576:.2f} |')
size_lines += ['','| 打包资源分类 | MiB |','|---|---:|']
for key,value in groups.most_common():size_lines.append(f'| {key} | {value/1048576:.2f} |')
size_lines += ['','0.10.4起成就悬停直接复用二代字体。四份一代字体仅保留为仓库资料，原字体约39.29MiB及导入副本约34.95MiB均不再打入发行包。','',
'其余 `FrierenMod/` 资源原文件和 `.import` 指向的 Godot 导入副本仍一起打包。图片原 PNG 与导入纹理、音频原文件与导入流会同时占体积。PCK 是打包容器，不会自动把所有文件再做一次通用压缩。','',
'可以进一步按引用排除未使用资源、停止重复装入可省略的原文件，但要同时验证 Godot 导入路径、直接 FileAccess 读取和纹理/音频运行时加载；本批先统计，不擅自删原图、音频或作者资料。','', '| 最大单项（含导入副本） | MiB |','|---|---:|']
for size,rel in sorted((x for values in sizes.values() for x in values),reverse=True)[:30]:size_lines.append(f'| {rel} | {size/1048576:.2f} |')
(OUT/'package-size.md').write_text('\n'.join(size_lines)+'\n',encoding='utf-8')
print(f'LOCALIZATION total={total} unchanged={unchanged} differences={len(rows)} code_candidates={code_count}')
print('RESOURCE_MIB',round(sum(groups.values())/1048576,2));print(groups)
