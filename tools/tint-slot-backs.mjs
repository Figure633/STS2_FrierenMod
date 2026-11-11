// 按作者PSD的灰色正片叠底图层处理二代23张背景；输入固定取已选原稿，避免重复叠加。
// node tools/tint-slot-backs.mjs [--apply]
import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import { createRequire } from 'node:module';
import { psdReader } from './lib/psd-rgba.mjs';
const require = createRequire(import.meta.url);
const sharp = require(process.env.SHARP_MODULE || 'sharp');
const root = process.cwd(), apply = process.argv.includes('--apply');
const output = path.join(root, 'outputs/slot-backs-multiply-20261110');
const inventoryPath = path.join(root, 'docs/inventory/sts2_slot_backs.json');
const inventory = JSON.parse(await fs.readFile(inventoryPath, 'utf8'));
const originalDir = 'outputs/slot-backs-final-20261109/已选成品';
const reference = path.resolve(root, '../png/8009-1_0000_图层 1.psd');
const ref = psdReader(await fs.readFile(reference));
const gray = ref.layers.find(l => l.visible && l.blend === 'mul ');
const W = 301, H = 461, N = W * H;
if (ref.width !== W || ref.height !== H || !gray || gray.width !== W || gray.height !== H || gray.left || gray.top)
  throw Error('参考须为301×461、覆盖整张画布的正片叠底图层');
const color = [...gray.rgba.subarray(0, 3)], opacity = gray.opacity;
for (let p = 0; p < gray.rgba.length; p += 4)
  if (gray.rgba[p + 3] !== 255 || color.some((v, c) => gray.rgba[p + c] !== v)) throw Error('参考灰色层不是均匀不透明填充');
if (inventory.images.length !== 23) throw Error('清单应为23张');
const hash = b => crypto.createHash('sha256').update(b).digest('hex');
if (apply) for (const item of inventory.images) {
  const current = await fs.readFile(path.join(root, item.asset));
  if (hash(current) !== item.sha256) throw Error(`${item.id}游戏资源已另行修改，请先核对来源`);
}
const rel = p => path.relative(root, p).replaceAll('\\', '/');
const u16 = n => { const b = Buffer.alloc(2); b.writeUInt16BE(n & 65535); return b; };
const u32 = n => { const b = Buffer.alloc(4); b.writeUInt32BE(n); return b; };
const str = s => Buffer.from(s, 'ascii');
const sized = b => Buffer.concat([u32(b.length), b]);
// PSD原生两层：下层为未改色成品，上层为作者灰色层（剪贴到原图，保留圆角透明度）。
function writePSD(base, merged, id) {
  const planes = rgba => [-1, 0, 1, 2].map(c => {
    const b = Buffer.alloc(N + 2); // RAW compression=0
    for (let i = 0; i < N; i++) b[i + 2] = rgba[i * 4 + (c === -1 ? 3 : c)];
    return b;
  });
  const record = (name, blend, alpha, clip, channels) => {
    const ascii = str(name), pascal = Buffer.alloc(Math.ceil((ascii.length + 1) / 4) * 4);
    pascal[0] = ascii.length; ascii.copy(pascal, 1);
    const extra = Buffer.concat([u32(0), u32(0), pascal]);
    return Buffer.concat([u32(0), u32(0), u32(H), u32(W), u16(4),
      ...channels.flatMap((b, i) => [u16([-1, 0, 1, 2][i]), u32(b.length)]),
      str('8BIM'), str(blend), Buffer.from([alpha, clip, 0, 0]), sized(extra)]);
  };
  const top = planes(gray.rgba), bottom = planes(base);
  let info = Buffer.concat([u16(-2), record('Gray #65656D - Multiply 15%', 'mul ', opacity, 1, top),
    record(id + ' - Original', 'norm', 255, 0, bottom), ...top, ...bottom]);
  if (info.length % 2) info = Buffer.concat([info, Buffer.alloc(1)]);
  const mergedPlanes = planes(merged);
  return Buffer.concat([str('8BPS'), u16(1), Buffer.alloc(6), u16(4), u32(H), u32(W), u16(8), u16(3),
    u32(0), u32(0), sized(Buffer.concat([sized(info), u32(0)])), u16(0),
    ...[1, 2, 3, 0].map(i => mergedPlanes[i].subarray(2))]);
}
for (const dir of ['', 'PNG', 'PSD']) await fs.mkdir(path.join(output, dir), { recursive: true });
await fs.writeFile(path.join(output, '.gdignore'), '');
const records = [], beforeAfter = [];
for (const item of inventory.images) {
  const source = path.join(root, originalDir, item.file), original = await fs.readFile(source);
  const { data: base, info } = await sharp(original).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  if (info.width !== W || info.height !== H || info.channels !== 4) throw Error(`${item.id}尺寸不正确`);
  const result = Buffer.from(base);
  for (let i = 0; i < N; i++) {
    if (!base[i * 4 + 3]) continue;
    for (let c = 0; c < 3; c++) result[i * 4 + c] = Math.round(base[i * 4 + c] * (1 - opacity / 255 + opacity / 255 * color[c] / 255));
  }
  const png = await sharp(result, { raw: { width: W, height: H, channels: 4 } }).png().toBuffer();
  const pngPath = path.join(output, 'PNG', item.file), psdPath = path.join(output, 'PSD', item.file.replace(/\.png$/, '.psd'));
  await fs.writeFile(pngPath, png);
  const psd = writePSD(base, result, item.id);
  // 回读PSD，核对两个图层及合成预览，防止另存时丢失参数或透明度。
  const checkPSD = psdReader(psd);
  if (!checkPSD.merged.equals(result) || !checkPSD.layers[1].rgba.equals(base) ||
      checkPSD.layers[0].blend !== 'mul ' || checkPSD.layers[0].opacity !== opacity || checkPSD.layers[0].clipping !== 1)
    throw Error(`${item.id} PSD回读不一致`);
  await fs.writeFile(psdPath, psd);
  const decoded = await sharp(png).ensureAlpha().raw().toBuffer();
  if (!decoded.equals(result)) throw Error(`${item.id} PNG回读不一致`);
  for (let i = 0; i < N; i++) if (decoded[i * 4 + 3] !== base[i * 4 + 3]) throw Error(`${item.id}透明度改变`);
  if (apply) {
    // 仅覆盖23张二代资源；原稿目录及一代104张均不修改。
    await fs.writeFile(path.join(root, item.asset), png);
    item.originalSource = rel(source);
    item.source = rel(pngPath);
    item.sha256 = hash(png);
    item.colorAdjustment = { reference: rel(reference), rgb: color, blend: 'multiply', opacityByte: opacity, opacity: opacity / 255, psd: rel(psdPath) };
  }
  records.push({ id: item.id, name: item.name, file: item.file, original: rel(source), originalSha256: hash(original), png: rel(pngPath), psd: rel(psdPath), sha256: hash(png) });
  beforeAfter.push({ item, original, png });
}
const esc = s => s.replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('"', '&quot;');
const svg = (text, width, height = 34) => Buffer.from(`<svg width="${width}" height="${height}"><text x="${width / 2}" y="24" text-anchor="middle" fill="#eee6dc" font-size="15" font-family="Microsoft YaHei,Arial,sans-serif">${esc(text)}</text></svg>`);
async function contact(items, filename, cols, compare) {
  const cw = compare ? 646 : 325, ch = 520, composites = [];
  for (let i = 0; i < items.length; i++) {
    const { item, original, png } = items[i], left = i % cols * cw, top = Math.floor(i / cols) * ch;
    if (compare) composites.push({ input: original, left: left + 12, top: top + 42 });
    composites.push({ input: png, left: left + (compare ? 333 : 12), top: top + 42 });
    composites.push({ input: svg(`${item.id} · ${item.name}${compare ? '　原图 / 叠加后' : ''}`, cw), left, top });
  }
  await sharp({ create: { width: cols * cw, height: Math.ceil(items.length / cols) * ch, channels: 4, background: '#202630' } }).composite(composites).png().toFile(path.join(output, filename));
}
await contact(beforeAfter, '23张-叠加后总览.png', 4, false);
await contact(beforeAfter.filter(x => ['8001', '8009', '8013', '8022'].includes(x.item.id)), '修改前后对照.png', 2, true);
await fs.writeFile(path.join(output, 'index.html'), `<!doctype html><html lang="zh-CN"><meta charset="utf-8"><title>23张卡槽背景 · 灰色正片叠底</title><style>body{background:#202630;color:#eee6dc;font:16px system-ui;margin:24px}main{display:grid;grid-template-columns:repeat(auto-fit,minmax(640px,1fr));gap:20px}article{background:#292f39;padding:16px}h2{font-size:17px}.pair{display:flex;gap:16px}figure{margin:0;text-align:center}img{width:301px;height:461px}a{color:#ddc693}</style><h1>23张卡槽背景 · 灰色正片叠底</h1><p>作者参考 #65656D · 不透明度38/255（约15%） · 原图与叠加后对照。PNG保留透明圆角，PSD保留原图层和可调灰色剪贴图层。</p><main>${beforeAfter.map(({ item }) => `<article><h2>${item.id} · ${esc(item.name)}</h2><div class="pair"><figure><img src="../slot-backs-final-20261109/已选成品/${encodeURIComponent(item.file)}"><figcaption>原图</figcaption></figure><figure><img src="PNG/${encodeURIComponent(item.file)}"><figcaption>叠加后 · <a href="PSD/${encodeURIComponent(item.file.replace(/\.png$/, '.psd'))}">PSD</a></figcaption></figure></div></article>`).join('')}</main></html>`);
await fs.writeFile(path.join(output, 'manifest.json'), JSON.stringify({ reference: rel(reference), rgb: color, opacityByte: opacity, blend: 'multiply', width: W, height: H, images: records }, null, 2) + '\n');
if (apply) {
  await fs.writeFile(inventoryPath, JSON.stringify(inventory, null, 2) + '\n');
  const assetsPath = path.join(root, 'docs/assets.json');
  const assets = JSON.parse(await fs.readFile(assetsPath, 'utf8'));
  for (const asset of assets) {
    const item = inventory.images.find(i => i.asset === asset.file);
    if (!item) continue;
    asset.source = item.source;
    asset.sha256 = item.sha256;
    asset.modified = true;
    asset.processing = `${item.processing}；作者灰色RGB(${color})、正片叠底、不透明度${opacity}/255`;
  }
  await fs.writeFile(assetsPath, JSON.stringify(assets, null, 2) + '\n');
}
console.log(`MULTIPLY_OK count=${records.length} rgb=${color} opacity=${opacity}/255 alpha=unchanged layeredPSD=verified applied=${apply}`);
