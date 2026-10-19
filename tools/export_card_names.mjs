// 只导出名称登记，不改游戏本地化。使用 Codex 配套的 artifact-tool。
import fs from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { createRequire } from 'node:module';
import assert from 'node:assert/strict';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const temp = path.join(root, 'artifacts/card-names');
const source = path.resolve(process.env.FRIEREN_STS1_SOURCE || path.join(root, '../reference/FrierenMod_STS1'));
const runtimeRequire = createRequire(path.join(temp, 'runtime.cjs'));
const { Workbook, SpreadsheetFile } = await import(pathToFileURL(runtimeRequire.resolve('@oai/artifact-tool')).href);
const json = async p => JSON.parse((await fs.readFile(p, 'utf8')).replace(/^\uFEFF/, ''));
const oldLoc = path.join(source, 'src/main/resources/FrierenModResources/localization');
const zhs = await json(path.join(oldLoc, 'ZHS/cards.json'));
const eng = await json(path.join(oldLoc, 'ENG/cards.json'));
const currentZhs = await json(path.join(root, 'FrierenMod/localization/zhs/cards.json'));
const currentEng = await json(path.join(root, 'FrierenMod/localization/eng/cards.json'));
const normalize = s => s.replaceAll('_', '').toLowerCase();
const slug = s => s.replace(/([A-Z]+)([A-Z][a-z])/g, '$1_$2').replace(/([a-z\d])([A-Z])/g, '$1_$2').toUpperCase();
const entries = Object.entries(zhs).filter(([, v]) => typeof v.NAME === 'string');
const byName = new Map();
for (const [id] of entries) {
  const key = normalize(id.split(':').at(-1));
  assert(!byName.has(key), `一代名称键冲突 ${id}`);
  byName.set(key, id);
}
async function filesIn(dir, extension) {
  const files = [];
  for (const d of await fs.readdir(dir, { withFileTypes: true })) {
    const p = path.join(dir, d.name);
    if (d.isDirectory()) files.push(...await filesIn(p, extension));
    else if (p.endsWith(extension)) files.push(p);
  }
  return files.sort();
}
const oldFiles = await filesIn(path.join(source, 'src/main/java/FrierenMod/cards'), '.java');
const oldClasses = new Map();
for (const file of oldFiles) {
  const key = normalize(path.basename(file, '.java'));
  const list = oldClasses.get(key) || [];
  list.push(path.relative(path.join(source, 'src/main/java/FrierenMod/cards'), file).replaceAll('\\', '/'));
  oldClasses.set(key, list);
}
const category = {
  Basic: '基础牌', Common: '普通牌', Uncommon: '罕见牌', Rare: '稀有牌',
  Token: '临时或兼容牌', Factors: '魔法因子', MagicProps: '魔具', Choices: '选择预览'
};
const current = [];
for (const file of await filesIn(path.join(root, 'Scripts/Frieren/Cards'), '.cs')) {
  const text = await fs.readFile(file, 'utf8');
  const match = text.match(/public sealed class (\w+)/);
  if (!match) continue;
  const model = match[1];
  const key = `FRIERENMOD-${slug(model)}`;
  assert(currentZhs[key + '.title'], `缺少二代中文标题 ${model} ${key}`);
  assert(currentEng[key + '.title'], `缺少二代英文标题 ${model} ${key}`);
  const oldId = byName.get(normalize(model)) || '';
  const canonicalZhs = oldId ? zhs[oldId].NAME : currentZhs[key + '.title'];
  const canonicalEng = oldId ? (eng[oldId]?.NAME || '') : currentEng[key + '.title'];
  const group = path.relative(path.join(root, 'Scripts/Frieren/Cards'), file).split(path.sep)[0];
  current.push({
    model, chinese: canonicalZhs, english: canonicalEng,
    currentChinese: currentZhs[key + '.title'], currentEnglish: currentEng[key + '.title'],
    chineseStatus: oldId ? (canonicalZhs === currentZhs[key + '.title'] ? '一致' : '需对齐') : '无一代同 ID 待确认',
    englishStatus: oldId ? (!canonicalEng ? '一代英文缺失' : canonicalEng === currentEng[key + '.title'] ? '一致' : '差异 后期处理') : '无一代同 ID 待确认',
    category: category[group] || group, oldId, currentId: key,
    sourceFile: path.relative(root, file).replaceAll('\\', '/')
  });
}
current.sort((a, b) => a.model.localeCompare(b.model, 'en'));
const titles = Object.keys(currentZhs).filter(k => k.endsWith('.title'));
assert.equal(current.length, titles.length, '二代标题与具体卡模型数量不一致');
assert.equal(new Set(current.map(c => c.currentId)).size, current.length, '二代 ID 重复');
const baby = current.find(c => c.model === 'BabySleeping');
assert.equal(baby.chinese, '婴儿般的睡眠');
assert.equal(baby.english, 'Sleeping Like a Baby');
const allOldIds = [...new Set([...entries.map(([id]) => id), ...Object.keys(eng).filter(id => eng[id].NAME)])].sort();
const oldRows = allOldIds.map(id => {
  const linked = current.filter(c => c.oldId === id);
  return [id, zhs[id]?.NAME || '', eng[id]?.NAME || '', linked.map(c => c.model).join(', '),
    linked.map(c => c.currentId).join(', '),
    (oldClasses.get(normalize(id.split(':').at(-1))) || []).join(', ') || '未找到同名 Java 文件',
    !zhs[id]?.NAME ? '中文缺失' : !eng[id]?.NAME ? '英文缺失' : '双语原文'];
});
const currentRows = current.map(c => [c.model, c.chinese, c.english, c.currentChinese, c.currentEnglish,
  c.chineseStatus, c.englishStatus, c.category, c.oldId, c.currentId]);
const wb = Workbook.create();
const sheet = wb.worksheets.add('二代卡名对照');
const original = wb.worksheets.add('一代全部卡名');
function setup(s, title, note, headers, rows, widths, tableName) {
  s.showGridLines = false;
  const last = String.fromCharCode(64 + headers.length);
  const end = rows.length + 6;
  const used = s.getRange(`A1:${last}${end}`);
  used.format.font = { name: 'Microsoft YaHei', size: 10, color: '#253044' };
  used.format.verticalAlignment = 'center';
  used.format.rowHeight = 28;
  s.getRange('A2').values = [[title]];
  s.getRange('A2').format.font = { name: 'Microsoft YaHei', size: 15, bold: true, color: '#243B53' };
  s.getRange('A2').format.rowHeight = 32;
  s.getRange('A3').values = [[note]];
  s.getRange('A4').values = [['来源：一代 ZHS/cards.json 与 ENG/cards.json；二代现名来自当前 zhs/cards.json 与 eng/cards.json。']];
  s.getRange(`A3:${last}4`).format.font = { name: 'Microsoft YaHei', size: 10, color: '#526577' };
  s.getRange(`A6:${last}6`).values = [headers];
  s.getRange(`A7:${last}${end}`).values = rows;
  s.getRange(`A7:${last}${end}`).format.wrapText = true;
  s.getRange(`A7:${last}${end}`).format.rowHeight = 54;
  const table = s.tables.add(`A6:${last}${end}`, true, tableName);
  table.style = 'TableStyleMedium2';
  table.showFilterButton = true;
  s.getRange(`A6:${last}6`).format = {
    fill: '#243B53', font: { name: 'Microsoft YaHei', size: 10, bold: true, color: '#FFFFFF' },
    horizontalAlignment: 'center', verticalAlignment: 'center', rowHeight: 32,
    borders: { insideVertical: { style: 'thin', color: '#FFFFFF' } }
  };
  widths.forEach((width, i) => s.getRange(`${String.fromCharCode(65 + i)}1:${String.fromCharCode(65 + i)}${end}`).format.columnWidth = width);
  s.freezePanes.freezeRows(6);
  s.tabColor = '#243B53';
}
const chineseDiff = current.filter(c => c.chineseStatus === '需对齐').length;
setup(sheet, '芙莉莲模组卡名对照',
  `更新于 2026-10-19。${current.length} 个二代卡模型，${chineseDiff} 个中文名需对齐。前三列用于查名；无一代对应的条目暂保留二代现名。`,
  ['代码 ID', '中文名 一代优先', '英文名 一代优先', '二代当前中文名', '二代当前英文名', '中文核对', '英文核对', '类别', '一代本地化 ID', '二代本地化 ID'],
  currentRows, [28, 34, 40, 34, 40, 24, 25, 18, 43, 46], 'CurrentCardNames');
setup(original, '一代全部卡名',
  `${oldRows.length} 个本地化名称条目，包含菲伦、因子、魔具、临时与未移植条目；名称条目不等于有效奖励池卡数。`,
  ['一代本地化 ID', '中文原名', '英文原名', '二代代码 ID', '二代本地化 ID', '一代源码 cards 下路径', '原文状态'],
  oldRows, [45, 36, 40, 28, 46, 52, 18], 'OriginalCardNames');
sheet.getRange(`F7:F${current.length + 6}`).conditionalFormats.add('containsText', {
  text: '需对齐', format: { fill: '#FFF0D4', font: { color: '#925C0A', bold: true } }
});
sheet.getRange(`F7:G${current.length + 6}`).conditionalFormats.add('containsText', {
  text: '待确认', format: { fill: '#FFF0D4', font: { color: '#925C0A' } }
});
wb.recalculate();
console.log((await wb.inspect({ kind: 'table', range: '二代卡名对照!A7:C15', tableMaxRows: 9, tableMaxCols: 3, maxChars: 3000 })).ndjson);
console.log((await wb.inspect({ kind: 'match', searchTerm: '#REF!|#DIV/0!|#VALUE!|#NAME\\?|#NUM!|#SPILL!', options: { useRegex: true, maxResults: 10 }, maxChars: 1200 })).ndjson);
await fs.mkdir(temp, { recursive: true });
for (const [s, range, name] of [[sheet, 'A1:F15', 'current'], [original, 'A1:D15', 'original']]) {
  const preview = await wb.render({ sheetName: s.name, range, scale: 1.5, format: 'png' });
  await fs.writeFile(path.join(temp, name + '.png'), new Uint8Array(await preview.arrayBuffer()));
}
const output = path.join(root, '卡名对照表.xlsx');
const stagedOutput = path.join(temp, '卡名对照表.xlsx');
await (await SpreadsheetFile.exportXlsx(wb)).save(stagedOutput);
await fs.copyFile(stagedOutput, output);
// JSON 每行一个记录，代理可通过 rg 直接按 ID/中英文检索，不依赖 Excel。
await fs.writeFile(path.join(root, 'docs/inventory/card_names.jsonl'), current.map(c => JSON.stringify(c)).join('\n') + '\n', 'utf8');
await fs.writeFile(path.join(temp, 'expected.json'), JSON.stringify({ currentRows, oldRows, chineseDiff }), 'utf8');
console.log(JSON.stringify({ output, models: current.length, sourceNames: oldRows.length, chineseDifferences: chineseDiff,
  englishDifferences: current.filter(c => c.englishStatus === '差异 后期处理').length,
  unmatched: current.filter(c => !c.oldId).map(c => c.model), baby }, null, 2));
