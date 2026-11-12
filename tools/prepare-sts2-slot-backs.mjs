// 按作者已选样式收集二代新增背景；不重画、不改变已完成图片。
import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import { createRequire } from 'node:module';
const require = createRequire(import.meta.url);
const sharp = require(process.env.SHARP_MODULE || 'sharp');
const root = process.cwd();
const output = path.join(root, '../开发资料/outputs/slot-backs-final-20261109');
const previous = JSON.parse(await fs.readFile(path.join(output, 'manifest.json'), 'utf8'));
const basic = new Set([1, 2, 11, 12]);
const uploaded = [
  ['cf33b327-ff8e-4912-b7a2-9b6842d324ec', '芙莉莲·水光', 'Frieren · Waterlight'],
  ['37a7232e-b41e-43ca-813c-ee0909eba64b', '辛美尔·晴空', 'Himmel · Clear Sky'],
  ['95adce0f-13ca-4741-a386-aaa71dc1131d', '芙莉莲·花冠', 'Frieren · Flower Crown'],
  ['507b4368-0637-499a-8439-e1ac2905aea6', '芙莉莲·温泉', 'Frieren · Hot Spring'],
  ['65d0c8a7-606c-4582-8484-b93270088405', '芙莉莲·得意', 'Frieren · Pleased'],
  ['a35c8929-6404-4a4b-bbe3-e44936ace1df', '弗兰梅', 'Flamme'],
  ['385b350b-9949-408e-ae0c-20dce60ce437', '冰晶原野', 'Crystal Fields'],
  ['38466be3-107b-4c71-83a6-7d372a17ebbe', '芙莉莲·冬日', 'Frieren · Winter'],
  ['3eddae61-1824-425b-b081-9590d01c5a1c', '菲伦·花冠', 'Fern · Flower Crown'],
  ['f5de301d-5304-4ae4-843c-834b4d824b31', '修塔尔克·魔族装扮', 'Stark · Demon Disguise'],
  ['b8b581ba-2046-40c5-90df-7eddf4250c45', '菲伦·整理头发', 'Fern · Fixing Her Hair'],
];
const english = ['Ice Dance', 'Frieren in a Painting', 'Black-Winged Duo', 'A Toast', 'Stark', 'Frieren', 'Fern', 'Heiter', 'Himmel', 'Eisen', "The Hero's Party", 'The Golden Land'];
const items = previous.images.map((r, i) => ({
  id: String(8001 + i), name: r.name, englishName: english[i], batch: '本次新图', batchNumber: r.number,
  style: basic.has(r.number) ? '基础圆角' : '金色花纹',
  source: path.join(output, basic.has(r.number) ? '基础圆角' : '金色花纹', r.file),
  processing: '原PSD套图，复用2026-10-29适中档浅浮雕、8px倒角及内缘折线；未重画人物',
}));
for (let i = 0; i < uploaded.length; i++) {
  const [uuid, name, englishName] = uploaded[i];
  items.push({ id: String(8013 + i), name, englishName, batch: '之前成品', batchNumber: i + 1, style: '原成品圆角',
    source: `C:/Users/a1362/AppData/Local/Temp/codex-clipboard-${uuid}.png`, processing: '作者提供的301×461成品，原字节复制' });
}
await fs.mkdir(path.join(output, '已选成品'), { recursive: true });
const records = [];
for (let i = 0; i < items.length; i++) {
  const item = items[i], data = await fs.readFile(item.source), meta = await sharp(data).metadata();
  if (meta.width !== 301 || meta.height !== 461 || !meta.hasAlpha) throw Error(`${item.name} 不是301×461 RGBA`);
  const asset = `FrierenMod/images/slot_backs/${item.id}.png`;
  await fs.writeFile(path.join(root, asset), data);
  const file = `${String(i + 1).padStart(2, '0')}-${item.name}.png`;
  await fs.writeFile(path.join(output, '已选成品', file), data);
  records.push({ ...item, number: i + 1, file, asset, source: path.relative(root, item.source).replaceAll('\\', '/'), sha256: crypto.createHash('sha256').update(data).digest('hex') });
}
await fs.writeFile(path.join(root, '../开发资料/docs/inventory/sts2_slot_backs.json'), JSON.stringify({
  note: '二代新增23张；名称为画面标识，正式成就名称和解锁条件由作者在填写表中指定。尚未接入自动解锁。',
  unlockTable: '../开发资料/outputs/slot-backs-final-20261109/二代新增卡槽背景-解锁条件填写表.xlsx',
  images: records,
}, null, 2));
const esc = s => s.replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('"', '&quot;');
const cards = records.map(r => `<article><h2>${r.number} · ${esc(r.name)}</h2><img src="${encodeURI(`已选成品/${r.file}`)}"><p>ID ${r.id} · ${r.style}</p></article>`).join('');
await fs.writeFile(path.join(output, '已选背景.html'), `<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width"><title>二代新增卡槽背景</title><style>body{margin:0;padding:24px;background:#202630;color:#eee6d8;font-family:system-ui,sans-serif}h1{font-size:24px}main{display:grid;grid-template-columns:repeat(auto-fit,minmax(240px,1fr));gap:24px}article{padding:16px;background:#292f39;border-radius:12px;text-align:center}h2{font-size:16px;font-weight:500}img{width:100%;max-width:301px}p{font-size:13px;color:#b8c0cc}</style><h1>二代新增卡槽背景 · 23张</h1><p>本次新图1、2、11、12使用基础圆角，其他使用金色花纹，均已补回浅浮雕和倒角；之前11张成品保持原样。</p><main>${cards}</main></html>`);
async function contact(selected, filename, cols) {
  const cw = 329, ch = 508, layers = [];
  for (let i = 0; i < selected.length; i++) {
    const r = selected[i], left = i % cols * cw, top = Math.floor(i / cols) * ch;
    layers.push({ input: path.join(root, r.asset), left: left + 14, top: top + 8 });
    layers.push({ input: Buffer.from(`<svg width="329" height="32"><text x="164" y="23" text-anchor="middle" fill="#eae4d9" font-size="14" font-family="Microsoft YaHei,Arial,sans-serif">${r.number} · ${esc(r.name)}</text></svg>`), left, top: top + 474 });
  }
  await sharp({ create: { width: cols * cw, height: Math.ceil(selected.length / cols) * ch, channels: 4, background: '#202630' } }).composite(layers).png().toFile(path.join(output, filename));
}
await contact(records.slice(0, 12), '本次12张-已选浮雕版总览.png', 4);
await contact(records, '二代新增23张-总览.png', 4);
console.log('STS2_SLOT_BACKS_PREPARED: 23张，前12张按作者选型使用浅浮雕成品，后11张原字节保留');
