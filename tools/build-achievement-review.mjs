import fs from 'node:fs/promises';
import path from 'node:path';
import { createRequire } from 'node:module';
import { pathToFileURL } from 'node:url';

const root = process.cwd();
const work = path.join(root, 'artifacts/achievement-review');
const require = createRequire(pathToFileURL(path.join(work, 'resolver.cjs')));
const { Workbook, SpreadsheetFile } = await import(pathToFileURL(require.resolve('@oai/artifact-tool')));
const sharp = require('sharp');
const rows = JSON.parse(await fs.readFile(path.join(work, 'records.json'), 'utf8'));
const output = path.join(root, 'outputs/achievement-review-20261024');
await fs.mkdir(output, { recursive: true });
const outputFile = path.join(output, '成就与因子卡背对照表.xlsx');
if (await fs.stat(outputFile).then(() => true, () => false))
  throw new Error('对照表已存在，可能包含作者建议；禁止重新生成覆盖，请明确指定另一个输出文件。');
const wb = Workbook.create();
const sheet = wb.worksheets.add('成就与卡背');
const pending = wb.worksheets.add('待决定');
const font = 'Microsoft YaHei';
const navy = '#34445B';
const orange = '#FFF0D2';
const red = '#FFE4E0';
const green = '#EAF3EC';

function styleSheet(s, last, columns, title, subtitle) {
  s.showGridLines = false;
  s.getRange(`A1:${columns.at(-1)[0]}${last}`).format.font = { name: font, size: 11, color: '#273347' };
  s.getRange('A2').values = [[title]];
  s.getRange('A2').format.font = { name: font, size: 16, bold: true, color: navy };
  s.getRange('A3').values = [[subtitle]];
  s.getRange('A3').format.font = { name: font, size: 10, color: '#657184' };
  s.getRange('A4').values = [['黄色单元格供你填写决定；解锁条件尚未改动。图片为一代实际奖励卡背，已嵌入文件。']];
  s.getRange('A4').format.font = { name: font, size: 10, color: '#657184' };
  s.getRange('A2:I2').format.rowHeight = 27;
  s.getRange('A3:I4').format.rowHeight = 22;
  for (const [col, width] of columns) s.getRange(`${col}1:${col}${last}`).format.columnWidthPx = width;
  s.getRange(`A6:${columns.at(-1)[0]}6`).format = {
    fill: navy, font: { name: font, size: 11, bold: true, color: '#FFFFFF' },
    horizontalAlignment: 'center', verticalAlignment: 'center', rowHeight: 32, wrapText: true,
  };
  s.getRange(`A7:${columns.at(-1)[0]}${last}`).format.verticalAlignment = 'center';
  s.getRange(`A7:${columns.at(-1)[0]}${last}`).format.wrapText = true;
  s.getRange(`A7:${columns.at(-1)[0]}${last}`).format.rowHeightPx = 182;
  s.freezePanes.freezeRows(6);
  s.freezePanes.freezeColumns(2);
}

styleSheet(sheet, rows.length + 6,
  [['A', 74], ['B', 205], ['C', 136], ['D', 260], ['E', 350], ['F', 172], ['G', 365], ['H', 240], ['I', 340]],
  '芙莉莲成就与因子卡背对照表',
  '104张原图｜55项直接暂缺，2项受牵连，2项规则冲突｜核对日期：2026-10-24');
sheet.getRange('A6:I6').values = [['ID', '中文名', '成就图片／奖励卡背', '解锁条件（一代原说明）', '实际判定（一代源码）', '二代适配情况', '差异、暂缺原因及补充', '你的决定（可填写）', '一代代码位置']];
sheet.getRange(`A7:I${rows.length + 6}`).values = rows.map(r => [r.id, r.title, '', r.description, r.actual, r.status, r.note, '', r.source]);
sheet.getRange(`A7:A${rows.length + 6}`).setNumberFormat('@');
sheet.getRange(`H7:H${rows.length + 6}`).format.fill = '#FFF9DB';
sheet.getRange(`H7:H${rows.length + 6}`).dataValidation = { rule: { type: 'list', values: ['沿用原条件', '暂不可达，后续补齐', '改为二代条件（请补充）', '按说明判定', '按源码判定', '保留现行二代判定'] } };
const thumbs = new Map();
async function picture(s, record, row) {
  if (!thumbs.has(record.id)) {
    const data = await sharp(record.image).resize({ height: 168 }).png().toBuffer();
    thumbs.set(record.id, 'data:image/png;base64,' + data.toString('base64'));
  }
  s.images.add({ dataUrl: thumbs.get(record.id), anchor: {
    from: { row: row - 1, col: 2, rowOffsetPx: 7, colOffsetPx: 13 }, extent: { widthPx: 110, heightPx: 168 },
  } });
}
for (let i = 0; i < rows.length; i++) {
  const r = rows[i], n = i + 7;
  sheet.getRange(`A${n}:G${n}`).format.fill = i % 2 ? '#F5F7FA' : '#FFFFFF';
  sheet.getRange(`F${n}`).format.fill = r.status.startsWith('内容暂缺') ? red : r.status.includes('待决定') ? orange : green;
  sheet.getRange(`A${n}:I${n}`).format.borders = { bottom: { style: 'thin', color: '#DFE4EA' } };
  sheet.getRange(`A${n}`).format.horizontalAlignment = 'center';
  await picture(sheet, r, n);
}
sheet.tables.add(`A6:I${rows.length + 6}`, true, 'AchievementBacks');
sheet.tabColor = navy;

const byId = Object.fromEntries(rows.map(r => [r.id, r]));
const decisions = [
  ['1001–1018（18张）', '一场愉快的冒险', '1001', 'A0及以上击败腐化之心，随机解锁1张本组未收藏卡背。', '二代没有腐化之心；需要指定新的胜利奖励入口，或保留暂不可达。', '内容暂缺'],
  ['2001–2018（18张）', '一场华丽的冒险', '2001', 'A10及以上击败腐化之心，随机解锁1张本组未收藏卡背。', '二代没有腐化之心；进阶10存在，但需指定奖励入口。', '内容暂缺'],
  ['3001–3018（18张）', '一场史诗的冒险', '3001', '恰好A20击败腐化之心，随机解锁1张本组未收藏卡背。', '二代没有腐化之心，当前最高进阶10；奖励入口及门槛均需决定。', '内容暂缺'],
  ['0009（1张）', byId['0009'].title, '0009', byId['0009'].description, '二代没有一代的被拣选者（Chosen）；不能直接复现指定敌人的战败条件。', '内容暂缺'],
  ['9001（1张）', byId['9001'].title, '9001', byId['9001'].description, byId['9001'].note, '间接受限'],
  ['9002（1张）', byId['9002'].title, '9002', byId['9002'].description, byId['9002'].note, '间接受限'],
  ['5010（1张）', byId['5010'].title, '5010', byId['5010'].description, byId['5010'].note, '规则冲突'],
  ['7002（1张）', byId['7002'].title, '7002', byId['7002'].description, byId['7002'].note, '规则冲突'],
];
styleSheet(pending, decisions.length + 6,
  [['A', 180], ['B', 205], ['C', 136], ['D', 300], ['E', 425], ['F', 120], ['G', 300]],
  '需要你决定的成就适配',
  '8组问题；ID范围内的每张图片与逐项条件，可在“成就与卡背”表中筛选查看。');
pending.getRange('A6:G6').values = [['ID／范围', '中文名', '代表图片', '一代解锁条件', '暂缺内容／冲突说明', '问题类型', '你的决定（可填写）']];
pending.getRange(`A7:G${decisions.length + 6}`).values = decisions.map(r => [r[0], r[1], '', r[3], r[4], r[5], '']);
pending.getRange(`G7:G${decisions.length + 6}`).format.fill = '#FFF9DB';
for (let i = 0; i < decisions.length; i++) {
  const r = i + 7;
  pending.getRange(`A${r}:F${r}`).format.fill = i % 2 ? '#F5F7FA' : '#FFFFFF';
  pending.getRange(`F${r}`).format.fill = i < 4 ? red : orange;
  pending.getRange(`A${r}:G${r}`).format.borders = { bottom: { style: 'thin', color: '#DFE4EA' } };
  await picture(pending, byId[decisions[i][2]], r);
}
pending.tables.add(`A6:G${decisions.length + 6}`, true, 'PendingAchievementDecisions');
pending.tabColor = '#B97829';
wb.recalculate();
console.log((await wb.inspect({ kind: 'sheet,table', maxChars: 1800, tableMaxRows: 2, tableMaxCols: 4 })).ndjson);
await (await SpreadsheetFile.exportXlsx(wb)).save(outputFile);
for (const [sheetName, range, file] of [
  ['成就与卡背', 'A1:F9', 'inventory-preview.png'],
  ['待决定', 'A6:G8', 'pending-preview.png'],
  ['成就与卡背', 'A101:G103', 'rules-preview.png'],
]) {
  const preview = await wb.render({ sheetName, range, format: 'png', scale: 1 });
  await fs.writeFile(path.join(work, file), new Uint8Array(await preview.arrayBuffer()));
}
console.log('WORKBOOK_OK records=104 embedded_pictures=112');
