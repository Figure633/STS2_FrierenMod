"""合并Artifact Tool写出的指定单元格，保留原XLSX图片、批注、样式、其他单元格。"""
from pathlib import Path
from zipfile import ZipFile
from io import BytesIO
from copy import deepcopy
from lxml import etree as E
import json,sys,hashlib,os
root=Path(__file__).resolve().parents[1]
plan=json.loads(Path(sys.argv[1]).read_text(encoding='utf-8'))
file=root/plan['workbook'];lock=file.with_name('~$'+file.name)
assert not lock.exists(),'作者仍打开表格，停止覆盖'
raw=file.read_bytes();assert hashlib.sha256(raw).hexdigest()==plan['sourceHash'],'作者已更新表格，需重新读取'
temp=Path(sys.argv[1]).parent
if not (temp/'before.xlsx').exists():(temp/'before.xlsx').write_bytes(raw)
ns={'s':'http://schemas.openxmlformats.org/spreadsheetml/2006/main'};s='{'+ns['s']+'}'
def read_cells(xml,strings):
 out={}
 for c in xml.findall('.//s:sheetData/s:row/s:c',ns):
  value=c.find('s:v',ns)
  if c.get('t')=='s':v=strings[int(value.text)]
  elif c.get('t')=='inlineStr':v=''.join(c.find('s:is',ns).itertext())
  elif value is None:v=None
  elif c.get('t') in ['str','e','b']:v=value.text
  else:v=float(value.text) if '.' in value.text else int(value.text)
  out[c.get('r')]=v
 return out
def shared(z):
 if 'xl/sharedStrings.xml' not in z.namelist():return []
 return [''.join(x.itertext()) for x in E.fromstring(z.read('xl/sharedStrings.xml'))]
with ZipFile(BytesIO(raw)) as before,ZipFile(sys.argv[2]) as staged:
 xml=E.fromstring(before.read('xl/worksheets/sheet1.xml'));authored=read_cells(E.fromstring(staged.read('xl/worksheets/sheet1.xml')),shared(staged))
 original=read_cells(xml,shared(before));rows={int(r.get('r')):r for r in xml.findall('s:sheetData/s:row',ns)};data=xml.find('s:sheetData',ns)
 for cell,value in plan['cells'].items():
  assert authored.get(cell)==value or value=='' and authored.get(cell) is None,(cell,authored.get(cell),value)
  number=int(''.join(c for c in cell if c.isdigit()))
  if number not in rows:
   row=deepcopy(rows[max(rows)]);row.set('r',str(number))
   for c in row:
    c.set('r',''.join(ch for ch in c.get('r') if ch.isalpha())+str(number))
    for node in list(c):c.remove(node)
    c.attrib.pop('t',None)
   rows[number]=row;data.append(row)
  row=rows[number];c=next((c for c in row if c.get('r')==cell),None)
  if c is None:
   c=E.SubElement(row,s+'c',r=cell)
   column=''.join(ch for ch in cell if ch.isalpha())
   for index in sorted((i for i in rows if i<number),reverse=True):
    template=next((x for x in rows[index] if x.get('r')==column+str(index) and x.get('s') is not None),None)
    if template is not None:c.set('s',template.get('s'));break
  for node in list(c):c.remove(node)
  if isinstance(value,str):
   c.set('t','inlineStr');text=E.SubElement(E.SubElement(c,s+'is'),s+'t');text.set('{http://www.w3.org/XML/1998/namespace}space','preserve');text.text=value
  else:
   c.attrib.pop('t',None);E.SubElement(c,s+'v').text=str(value)
  if cell in plan.get('styleSources',{}):
   source=xml.find(".//s:c[@r='"+plan['styleSources'][cell]+"']",ns)
   if source is not None and source.get('s') is not None:c.set('s',source.get('s'))
 for row,height in plan['heights'].items():rows[int(row)].set('ht',str(height));rows[int(row)].set('customHeight','1')
 def column_order(cell):
  value=0
  for char in ''.join(ch for ch in cell.get('r') if ch.isalpha()):value=value*26+ord(char)-64
  return value
 for row in rows.values():row[:]=sorted(row,key=column_order)
 xml.find('s:dimension',ns).set('ref','A1:L'+str(max(rows)))
 changes={'xl/worksheets/sheet1.xml':E.tostring(xml,xml_declaration=True,encoding='UTF-8',standalone=True)}
 table='xl/tables/table1.xml';t=E.fromstring(before.read(table));t.set('ref','A1:K'+str(max(rows)))
 if t.find('s:autoFilter',ns) is not None:t.find('s:autoFilter',ns).set('ref',t.get('ref'))
 changes[table]=E.tostring(t,xml_declaration=True,encoding='UTF-8',standalone=True)
 output=temp/'merged.xlsx'
 with ZipFile(output,'w') as target:
  for item in before.infolist():target.writestr(deepcopy(item),changes.get(item.filename,before.read(item.filename)))
 with ZipFile(output) as after:
  assert before.namelist()==after.namelist()
  assert all(before.read(n)==after.read(n) for n in before.namelist() if n not in changes),'非目标部件改变'
  actual=read_cells(E.fromstring(after.read('xl/worksheets/sheet1.xml')),shared(after))
  assert all(actual.get(k)==v for k,v in original.items() if k not in plan['cells']),'其他单元格改变'
  assert all(actual[k]==v for k,v in plan['cells'].items())
assert not lock.exists() and hashlib.sha256(file.read_bytes()).hexdigest()==plan['sourceHash'],'作者重新编辑了表格，停止覆盖'
os.replace(output,file)
print('WORKBOOK_SYNC_OK:',len(plan['cells']),'cells; pictures, comments and unrelated package parts unchanged.')
