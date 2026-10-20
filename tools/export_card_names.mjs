// 从已核定的索引生成四列表；不再从一代覆盖作者选定的名字。
import fs from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { createRequire } from 'node:module';
import assert from 'node:assert/strict';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const temp = path.join(root, 'artifacts/card-names');
const req = createRequire(path.join(temp, 'runtime.cjs'));
const { Workbook, SpreadsheetFile, FileBlob } = await import(pathToFileURL(req.resolve('@oai/artifact-tool')).href);
const records = (await fs.readFile(path.join(root, 'docs/inventory/card_names.jsonl'), 'utf8')).trim().split('\n').map(JSON.parse);
const output = path.join(root, '卡名对照表.xlsx');
const rows = records.map(r => [r.model, r.chinese, r.english, r.descriptionChinese]);
const headers = ['ID', '二代中文名', '二代英文名', '卡面效果（中文）'];
assert.equal(new Set(records.map(r => r.model)).size, records.length);
assert(rows.every(row => row.every(v => typeof v === 'string' && v.trim())));
for (const [language, field] of [['zhs', 'chinese'], ['eng', 'english']]) {
  const loc = JSON.parse(await fs.readFile(path.join(root, `FrierenMod/localization/${language}/cards.json`), 'utf8'));
  for (const r of records) assert.equal(loc[r.currentId + '.title'], r[field], `索引未与游戏名称同步：${r.model}`);
}
// 初次从审核表转为四列时显式传入 --from-reviewed；日常禁止覆盖尚未回写的人工修改。
if (!process.argv.includes('--from-reviewed')) {
  const old = await SpreadsheetFile.importXlsx(await FileBlob.load(output));
  const sheet = old.worksheets.getItemAt(0);
  assert.deepEqual(sheet.getRange('A1:D1').values[0], headers, '旧表格式不同，不能自动覆盖');
  assert.deepEqual(sheet.getRange(`A2:D${rows.length + 1}`).values, rows, '表中有尚未回写索引的人工修改，停止覆盖');
}

const wb = Workbook.create();
const sheet = wb.worksheets.add('卡名对照');
sheet.showGridLines = false;
sheet.tabColor = '#243B53';
const end = records.length + 1;
sheet.getRange(`A1:D${end}`).values = [headers, ...rows];
sheet.getRange(`A1:D${end}`).format.font = { name: 'Microsoft YaHei', size: 11, color: '#253044' };
sheet.getRange(`A1:D${end}`).format.verticalAlignment = 'center';
sheet.getRange(`A2:D${end}`).format.wrapText = true;
const table = sheet.tables.add(`A1:D${end}`, true, 'CardNames');
table.style = 'TableStyleMedium2';
table.showFilterButton = true;
sheet.getRange('A1:D1').format = {
  fill: '#243B53', font: { name: 'Microsoft YaHei', size: 11, bold: true, color: '#FFFFFF' },
  rowHeight: 30, horizontalAlignment: 'center', verticalAlignment: 'center',
  borders: { insideVertical: { color: '#FFFFFF', style: 'thin' } }
};
const widths = [28, 34, 34, 88];
widths.forEach((w, i) => sheet.getRange(`${String.fromCharCode(65 + i)}1:${String.fromCharCode(65 + i)}${end}`).format.columnWidth = w);
const visualLength = s => [...s].reduce((n, c) => n + (c.codePointAt(0) > 255 ? 2 : 1), 0);
rows.forEach((row, i) => {
  const lines = Math.max(...row.map((text, j) => text.split('\n').reduce((n, line) => n + Math.max(1, Math.ceil(visualLength(line) / (widths[j] * .92))), 0)));
  sheet.getRange(`A${i + 2}:D${i + 2}`).format.rowHeight = Math.max(42, lines * 16 + 10);
});
sheet.freezePanes.freezeRows(1);
wb.recalculate();
console.log((await wb.inspect({ kind: 'table', range: '卡名对照!A1:C8', tableMaxRows: 8, tableMaxCols: 3, maxChars: 1800 })).ndjson);
await fs.mkdir(temp, {recursive: true});
for (const [range, filename] of [['A1:D7', 'names-final-top.png'], ['A38:D43', 'names-final-middle.png'], ['A123:D131', 'names-final-end.png']]) {
  const preview = await wb.render({sheetName: sheet.name, range, scale: 1.2, format: 'png'});
  await fs.writeFile(path.join(temp, filename), new Uint8Array(await preview.arrayBuffer()));
}
const staged = path.join(temp, '卡名对照表.xlsx');
await (await SpreadsheetFile.exportXlsx(wb)).save(staged);
await fs.copyFile(staged, output);
console.log(`已导出 ${records.length} 行四列表。`);
