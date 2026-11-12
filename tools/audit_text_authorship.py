"""只读核对游戏文本来源，输出审阅清单；不改本地化或作者文档。"""
from pathlib import Path
from zipfile import ZipFile
from io import BytesIO
from collections import Counter
from lxml import etree as E
import json,re,subprocess,difflib

ROOT=Path(__file__).resolve().parents[1]
def norm(s):
    s=re.sub(r'\[[^\]]*\]|#[rybgpw]|\bNL\b|[~@]','',s)
    return re.sub(r'[\s，,。.!！?？“”"‘’：:；;…·]','',s)
def strings(v):
    if isinstance(v,str):yield v
    elif isinstance(v,list):
        for x in v:yield from strings(x)
    elif isinstance(v,dict):
        for x in v.values():yield from strings(x)
def doc_paragraphs(raw):
    with ZipFile(BytesIO(raw)) as z:d=E.fromstring(z.read('word/document.xml'))
    for p in d.findall('.//{http://schemas.openxmlformats.org/wordprocessingml/2006/main}p'):
        s=''.join(p.itertext())
        # 旧文档的代理回写进度不能充当作者提供原句的证据。
        if s.startswith(('更新于','2026-','现有7个事件','事件／营地：','三幕共用曲池。')):continue
        yield re.split(r'0\.\d+\.\d+\s*(?:已|补|作者)',s)[0]

old={}
one=ROOT.parent/'FrierenMod_STS1_source code/src/main/resources/FrierenModResources/localization'
for p in (one/'ZHS').glob('*.json'):
    for value in strings(json.loads(p.read_text(encoding='utf-8-sig'))):
        if norm(value):old.setdefault(norm(value),p.relative_to(ROOT.parent).as_posix())
author=[]
for name in ['事件todo.docx','确定做的新东西.docx','第二季新东西.docx']:
    p=ROOT.parent/name
    if p.exists():author.extend((norm(s),'../'+name) for s in doc_paragraphs(p.read_bytes()) if len(norm(s))>=8)
for revision in ['ece596a','272ec2b','8172e84']:
    try:raw=subprocess.check_output(['git','show',revision+':TODOs.docx'],cwd=ROOT,stderr=subprocess.DEVNULL)
    except subprocess.CalledProcessError:continue
    author.extend((norm(s),revision+':TODOs.docx') for s in doc_paragraphs(raw) if len(norm(s))>=8)

# 确认属于代理补写的具体部分。混用一代原句时只将新增部分列为原创。
priority={
 'relics':{
  'FRIERENMOD-HOLY_EMBLEM.flavor':('代理补写','整句；一代圣徽的风味原文并非此句。'),
  'FRIERENMOD-UPGRADED_HOLY_EMBLEM.flavor':('代理补写','沿用代理为圣徽补写的同一句。'),
  'FRIERENMOD-HAITAA_WAND.flavor':('代理补写／旧兼容项','重复遗物名称的占位句；当前遗物已废弃。'),
  'FRIERENMOD-KRAFT_SERPENT_RING.flavor':('代理补写','二代兼容遗物的简短收尾；奖励玩法有作者要求，但这句不等于经作者逐字确认。'),
  'FRIERENMOD-AURA_COMPANION.flavor':('待确认来源','ece596a落实作者改稿时出现；没有找到这句的独立原稿或逐句确认，不能仅凭提交判定作者确认。'),
 },
 'events':{
  'FRIERENMOD-READING_GRIMOIRE_EVENT.pages.BOOK_1.description':('部分代理补写','前句“你感到体内流动的魔力正在发生变化”来自一代；新增“抽牌槽的因子已经装载。还有两本魔导书没有检查。”'),
  'FRIERENMOD-READING_GRIMOIRE_EVENT.pages.BOOK_2.description':('部分代理补写','前句来自一代；新增“手牌槽的因子也已装载。现在，只剩下最后一本。”'),
  'FRIERENMOD-READING_GRIMOIRE_EVENT.pages.TOOLS.description':('部分代理补写','魔力具象化来自一代；新增“三本魔导书都检查完了，旅行箱里还有一些魔法道具。”'),
  'FRIERENMOD-READING_GRIMOIRE_EVENT.pages.DONE.description':('代理改写','为拆分开局流程重写的结束句；两个工具由作者指定，旁白措辞由代理写。'),
  'FRIERENMOD-FOOD_EVENT.pages.FEAST.description':('代理改写','替换了一代贵族餐厅、狼吞虎咽、生命真谛的结束段；没有找到作者要求替换此段旁白。'),
  'FRIERENMOD-SEALING_ORE_MINE.pages.PLAY.description':('代理补写','作者规定移除卡并按费用获得魔力；未提供本分支结束旁白。'),
  'FRIERENMOD-SEALING_ORE_MINE.pages.GREED.description':('代理补写','作者规定获得封魔矿与商人出售；未提供本分支结束旁白。'),
 },
 'characters':{
  'FRIERENMOD-FRIEREN.description':('代理适配改写','用于二代角色选择页；一代角色介绍并非此两句，未找到逐句确认。'),
  'FRIERENMOD-FRIEREN.eventDeathPrevention':('代理补写','二代事件免死提示。'),
  'FRIERENMOD-FRIEREN.unlockText':('代理补写','二代角色解锁提示。'),
 },
 'monsters':{
  'FRIERENMOD-AURA.taunt':('待确认来源','二代阿乌拉对话；没有找到此句的一代原文或独立作者原稿。')
 }
}
legacy=['SUPPLIES','ROAD','CAREFUL','ESCORT','TEA','COPPER','THRIFT','SHELTER']
for key in legacy:
    priority.setdefault('frieren',{})['COMMISSION.REQUEST.'+key]=('代理补写／已停用委托','当前不再新生成；旧档或兼容本地化仍留有此段。')
for key in ['PROP.ElvenHourglass','PROP.PhoenixQuill','PROP.MysticBox']:
    priority.setdefault('frieren',{})[key]=('代理旧设计／已退出新获取','砂漏、羽笔仅兼容旧库存；魔术箱已废弃且禁用。本次不重新启用。')

known_json_edits={'FRIERENMOD-HOLLOW_KNIGHT_ADVENTURE.pages.INITIAL.description',
 'FRIERENMOD-HOLLOW_KNIGHT_ADVENTURE.pages.RADIANCE.description',
 'FRIERENMOD-HOLLOW_KNIGHT_ADVENTURE.pages.SILKSONG.description',
 'FRIERENMOD-HOLLOW_KNIGHT_ADVENTURE.pages.INITIAL.options.SILKSONG.title',
 'FRIERENMOD-KNIGHT_SECRET.flavor'}
records=[]
tables={}
for p in sorted((ROOT/'FrierenMod/localization/zhs').glob('*.json')):
    table=p.stem;j=json.loads(p.read_text(encoding='utf-8'));tables[table]=j
    lines=p.read_text(encoding='utf-8').splitlines()
    for key,value in j.items():
        n=norm(value);source='';status='未找到逐字来源／确认';note='可能为代理适配、原稿改写或未收录的作者修订，不能把所有未匹配项都判为代理原创或未经授权。'
        if not value:status='留空，无新增风味文字';note=''
        elif n in old:status='一代原文匹配';source=old[n];note='忽略颜色、动画标签、空白和标点差异；匹配不代表原作设定考据。'
        elif len(n)>=8 and (match:=next(((s,path) for s,path in author if n in s),None)):
            status='现有作者需求文档有对应';source=match[1];note='仅说明存在文字依据，不将文档中的代理进度说明当批准。'
        if key.startswith('BACK.80'):
            status='作者本次填写并明确要求实装';source='../开发资料/outputs/slot-backs-final-20261109/二代新增卡槽背景-解锁条件填写表.xlsx';note='8019仅登记文字，待作者新增事件。'
        if key in known_json_edits:
            status='作者工作区JSON修订';source='本次开始前作者已修改的JSON；病毒误改按2026-11-10回复恢复';note='保留作者改稿，不归为本次代理自创。'
        if table=='frieren' and key.startswith(('COMMISSION.NAME.','COMMISSION.REQUEST.')) and key.rsplit('.',1)[1] in ['HUNT','GRAPES','STATUE','WEDDING','FESTIVAL','SUMMER','LOST','TUTOR','CARGO','EXHIBITION','LIBRARY','LESSON','VARIETY','GRANNY','NAMUSAN','STARK','GUIDE']:
            status='作者指定沿用当前frieren.json';source='../开发资料/docs/AGENT_ADDITIONS_REVIEW.md：“这些名字和文本照我在frieren.json里的写”';note='这是采用当前文案的依据；不冒充每句都有独立逐字审阅记录。'
        if (override:=priority.get(table,{}).get(key)):
            status,note=override;source='当前源码、一代原文、作者文档与Git差异对照'
        if table=='events' and 'EVENT_ART_' in key:
            status='代理撰写的临时预览界面';note='用户授权制作事件图和特效预览；仅控制台预览，不进入正式随机事件。'
        line=next(i for i,s in enumerate(lines,1) if json.dumps(key,ensure_ascii=False) in s)
        records.append(dict(table=table,key=key,text=value,status=status,evidence=source,note=note,
            language='zhs',file=p.relative_to(ROOT).as_posix(),line=line,authorDecision=''))

english=[]
old_eng={norm(v):p.relative_to(ROOT.parent).as_posix() for p in (one/'ENG').glob('*.json')
    for v in strings(json.loads(p.read_text(encoding='utf-8-sig'))) if norm(v)}
for p in sorted((ROOT/'FrierenMod/localization/eng').glob('*.json')):
    lines=p.read_text(encoding='utf-8').splitlines()
    for key,value in json.loads(p.read_text(encoding='utf-8')).items():
        status='一代英文原文匹配' if norm(value) in old_eng else '英文翻译／适配，未找到逐句确认'
        if not value:status='留空，无新增风味文字'
        english.append(dict(language='eng',table=p.stem,key=key,text=value,status=status,
            evidence=old_eng.get(norm(value),''),note='中文获准不代表本句英文已经逐字验收。',
            file=p.relative_to(ROOT).as_posix(),line=next(i for i,s in enumerate(lines,1) if json.dumps(key) in s),authorDecision=''))

out=ROOT/'../开发资料/docs/inventory/text_authorship.jsonl'
out.write_text(''.join(json.dumps(r,ensure_ascii=False)+'\n' for r in [*records,*english]),encoding='utf-8')
def cell(x):return x.replace('|','\\|').replace('\n','<br>')
def row(r):
    link=f"[{r['table']}:{r['line']}](../{r['file']}#L{r['line']})"
    return '| '+link+' | '+cell(r['key'])+' | '+cell(r['text'])+' | '+cell(r['status']+'：'+r['note'])+' |  |'
report=['# 代理文本来源核对','',
 '2026年11月10日，基于0.12.13工作区、一代完整中文本地化、作者现有及历史Word、作者本次背景解锁表和Git记录核对。本文只列来源与复核状态，没有改删游戏文本。',
 '', '本次能明确找到的代理旁白和占位句主要集中在开局拆页、美食结束页、矿山分支结束页、少数遗物风味文字与二代角色介绍。完整逐键索引见 [文本来源索引](inventory/text_authorship.jsonl)，共'+str(len(records))+'条中文与'+str(len(english))+'条英文文本，含原文、文件行号、判断理由和留空的作者处理意见字段。索引中的“未找到逐句确认”不等于“玩法未经授权”。',
 '', '## 当前正式内容中优先核对的句子','',
 '| 位置 | 文本键 | 当前全文 | 来源判断 | 作者处理意见 |','|---|---|---|---|---|']
report += [row(r) for r in records if r['key'] in priority.get(r['table'],{}) and '旧' not in r['status'] and '停用' not in r['status']]
report += ['', '## 旧设计及兼容文本','',
 '精灵的时砂漏、不死鸟的羽笔都是之前代理自拟的魔具名称和效果；0.12.12已退出新获取，旧库存仍兼容。神秘魔术箱已按作者要求废弃且禁用。以下旧委托不再新生成，当前文字仍可能存在于旧档或本地化中。', '',
 '| 位置 | 文本键 | 当前全文 | 来源判断 | 作者处理意见 |','|---|---|---|---|---|']
report += [row(r) for r in records if r['key'] in priority.get(r['table'],{}) and ('旧' in r['status'] or '停用' in r['status'])]
report += ['', '## 当前委托对白','',
 '作者最新核对表明确要求沿用frieren.json中的名称和文本，因此本批按原句保留。这里仍列全当前17项对白，方便逐条看；不因曾提交过就自动标成“逐句已验收”。', '',
 '| 位置 | 文本键 | 当前全文 | 来源判断 | 作者处理意见 |','|---|---|---|---|---|']
report += [row(r) for r in records if r['table']=='frieren' and r['key'].startswith('COMMISSION.REQUEST.') and r['status']=='作者指定沿用当前frieren.json']
report += ['', '## 不应误算为代理原创的部分','',
 '- 美食、克拉伏特、动物井、宝箱怪的大多数叙事来自一代。旧版~、@、#颜色标记转换成二代标签后，不能仅因字符串不同就算代理重写。美食FEAST结束页的替换已单独列出。',
 '- 开局“体内流动的魔力正在发生变化”“部分魔力似乎具象化”也来自一代；上表仅将后来追加的槽位进度旁白列为代理补写。',
 '- 矿山开场描述可追溯到ece596a保存的作者Word原稿；“所有商人会疯抢”“魔法使最大的天敌”等不是本次凭空补出的。两个分支结束旁白仍需核对。',
 '- 空洞骑士正文来自作者事件Word；本次工作区的颜色、部分句子及小骑士倒下的修订来自作者JSON。误改“病毒”按作者回复恢复红色。',
 '- 小骑士遗物“用尊严换来的最终奥义，其强大毋庸置疑。”为作者工作区改稿，保留。',
 '- 二代8001～8023的23组成就中文名与条件均来自作者填写表；8019由作者说明待新增事件，当前保持锁定。',
 '', '## 效果说明与界面用语','',
 '卡牌、能力、因子、魔具、奖励、选牌提示的大量文字由代理按已授权效果整理成二代格式；有些与作者原稿不逐字相同。这一类没有一概判为擅自设计。索引逐条列出尚无逐字原稿匹配的内容，可以按table与status筛选。原版选牌提示的复用不增加新设定；代码内硬编码中文另核查后仅有日志、报错和筛选用“魔法”，未找到遗漏的硬编码剧情段落。',
 '', '## 英文文本','',
 '英文分为一代英文移植与代理翻译／二代适配。中文获准不代表英文每句话均已被作者审阅；没有查到整套英文逐句验收的记录。本次23组背景名称的英文也是代理翻译。英文原文保留在eng各表，可在中文定稿后逐表核对；本次没有为了消除审计项而改写英文剧情。',
 '', '## 临时预览文本','',
 '12张插画标题、上一张／下一张／离开以及“普通／淡入／飞入／省略号”等说明由代理为用户要求的预览写成，只用于控制台测试事件，不在正式随机池。它们是界面演示文本，不应冒充作者正式剧情。',
 '', '## 来源判断统计','', '| 判断 | 中文键数 |','|---|---:|']
for status,n in Counter(r['status'] for r in records).most_common():report.append('| '+status+' | '+str(n)+' |')
report += ['', '没有把未找到本地批准记录解释为“用户从未批准”。聊天批准可能未逐句持久化；可疑处保留当前内容，由作者在上表或索引的authorDecision中填写意见。已有整体玩法核对文档仍见 [代理新增内容核对](AGENT_ADDITIONS_REVIEW.md)。','']
(ROOT/'../开发资料/docs/AGENT_TEXT_REVIEW.md').write_text('\n'.join(report),encoding='utf-8')
print('TEXT_AUDIT_OK:',len(records),'Chinese entries;',len([r for r in records if r['key'] in priority.get(r['table'],{})]),'priority/legacy entries. No game localization was changed.')
