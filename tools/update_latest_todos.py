"""局部回写作者TODO，仅修改文档XML，其他包部件原字节保留。"""
from pathlib import Path
from zipfile import ZipFile
from io import BytesIO
from copy import deepcopy
from lxml import etree as E
import hashlib,os

root=Path(__file__).resolve().parents[1]
file=root/'TODOs.docx'
assert not (root/'~$TODOs.docx').exists(),'作者Word仍在使用，停止回写'
raw=file.read_bytes();fingerprint=hashlib.sha256(raw).hexdigest()
temp=root/'artifacts/author-todo-docx';temp.mkdir(parents=True,exist_ok=True)
if not (temp/'before.docx').exists():(temp/'before.docx').write_bytes(raw)
ns={'w':'http://schemas.openxmlformats.org/wordprocessingml/2006/main'}
w='{'+ns['w']+'}'
with ZipFile(BytesIO(raw)) as package:
    document=E.fromstring(package.read('word/document.xml'))
    paragraphs=document.findall('.//w:body/w:p',ns)
    def text(p):return ''.join(t.text or '' for t in p.findall('.//w:t',ns))
    for p in paragraphs:
        if text(p).strip() in {'事件主图：','选择贪选项的图','选择这是什么玩的图：'}:
            properties=p.find('w:pPr',ns)
            if properties is None:properties=E.Element(w+'pPr');p.insert(0,properties)
            if properties.find('w:keepNext',ns) is None:E.SubElement(properties,w+'keepNext')
    def run(p,value):
        r=E.SubElement(p,w+'r')
        existing=p.find('w:r/w:rPr',ns)
        if existing is not None:r.append(deepcopy(existing))
        t=E.SubElement(r,w+'t');t.set('{http://www.w3.org/XML/1998/namespace}space','preserve');t.text=value
    header=next(p for p in paragraphs if text(p).startswith('更新于 '))
    saved=deepcopy(header.find('w:r/w:rPr',ns))
    for item in list(header):
        if item.tag!=w+'pPr':header.remove(item)
    r=E.SubElement(header,w+'r')
    if saved is not None:r.append(saved)
    E.SubElement(r,w+'t').text='更新于 2026年11月10日 当前模组版本 0.12.13（已实装事项仍待游戏实测验收）'
    notes={
        '把贵重的商品改为和原版藏宝图一样的任务牌':' 0.12.13 已修复 待游戏实测：使用原生任务类型、任务稀有度及藏宝图同款边框，保留逐卡运费、复制与交货规则。',
        '乌恩之形能力显示回0':' 0.12.13 已修复 待游戏实测：通用预览使用按层数说明，实战按当前层数显示；咏唱后治疗并移除能力。',
        'Foodevent改成玩家必须小于等于65%血才能遇到。':' 0.12.13 已修复 待游戏实测：生命不高于65%且持有至少75金币时可出现。',
        '冒险物语：动物井：':' 0.12.13 补齐幕数条件 待游戏实测：仅第一、二幕出现，三个既有分支规则保持。',
        '下面几个图如果不合适自己裁剪':' 0.12.13 已修复 待游戏实测：矿山主图、贪、玩三个页面使用本处Word图片，按作者裁剪并去掉播放器边缘；横图整幅展示。',
    }
    for prefix,note in notes.items():
        matches=[p for p in paragraphs if text(p).startswith(prefix)]
        assert len(matches)==1,(prefix,len(matches))
        if note not in text(matches[0]):run(matches[0],note)
    body=document.find('w:body',ns)
    marker='0.12.12 已修复 待游戏实测：按作者核对表重做委托与报酬，加入独立插图、考试小瓶、魔法陶罐、魔具店会员卡及涅奥赌徒金币。第三种三星专属魔具仍待作者设计。'
    if not any(text(p)==marker for p in paragraphs):
        heading=next(p for p in paragraphs if text(p)=='需要尽快解决的问题')
        p=E.Element(w+'p');style=paragraphs[2].find('w:pPr',ns)
        if style is not None:p.append(deepcopy(style))
        run(p,marker);body.insert(list(body).index(heading)+1,p)
    output=temp/'updated.docx'
    xml=E.tostring(document,encoding='UTF-8',xml_declaration=True,standalone=True)
    with ZipFile(output,'w') as target:
        for info in package.infolist():target.writestr(info,xml if info.filename=='word/document.xml' else package.read(info.filename))
assert hashlib.sha256(file.read_bytes()).hexdigest()==fingerprint,'作者在回写期间更新了Word，停止覆盖'
assert not (root/'~$TODOs.docx').exists(),'作者重新打开了Word，停止覆盖'
os.replace(output,file)
with ZipFile(BytesIO(raw)) as before,ZipFile(file) as after:
    assert set(before.namelist())==set(after.namelist())
    assert all(before.read(name)==after.read(name) for name in before.namelist() if name!='word/document.xml')
print('TODO_UPDATED: 仅更新版本、五个对应条目和本批委托进度；其余文档部件原字节保留。')
