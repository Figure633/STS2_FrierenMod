// 从作者截图中抠取仙豆原像素，准备事件专属药水素材；不生成、不重画。
import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import { createRequire } from 'node:module';
const require = createRequire(import.meta.url), sharp = require(process.env.SHARP_MODULE || 'sharp');
const source = '../开发资料/assets/author_adjusted/20261111/senzu/SenzuBean-reference.png';
const output = '../开发资料/assets/prepared/event-potions/senzu';
await fs.mkdir(path.dirname(source), { recursive: true });
await fs.mkdir(output, { recursive: true });
await fs.writeFile(path.join(path.dirname(source), '.gdignore'), '');
await fs.writeFile(path.join(output, '.gdignore'), '');
if (process.argv[2]) {
  const original = await fs.readFile(process.argv[2]);
  let existing;
  try { existing = await fs.readFile(source); } catch (error) { if (error.code !== 'ENOENT') throw error; }
  if (existing && !existing.equals(original)) throw Error('已有作者原图不同，请勿覆盖。');
  if (!existing) await fs.writeFile(source, original);
}
const region = { left: 418, top: 330, width: 110, height: 120 };
const { data, info } = await sharp(source).extract(region).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
const width = info.width, height = info.height, size = width * height;
const candidate = new Uint8Array(size), mask = new Uint8Array(size);
for (let i = 0; i < size; i++) {
  const [r, g, b] = data.subarray(i * 4, i * 4 + 3);
  candidate[i] = g - r > 3 && g - b > 12 || Math.max(r, g, b) < 145 && g >= r - 16 && b <= g + 8 ? 1 : 0;
}
// 种子位于豆身内部；只保留与豆身连通的绿色和原有黑边，不保留掌心阴影。
const queue = [55 * width + 55];
if (!candidate[queue[0]]) throw Error('仙豆内部种子没有匹配到原图绿色。');
mask[queue[0]] = 1;
for (let head = 0; head < queue.length; head++) {
  const i = queue[head], x = i % width, y = Math.floor(i / width);
  for (let dy = -1; dy <= 1; dy++) for (let dx = -1; dx <= 1; dx++) {
    const nx = x + dx, ny = y + dy;
    if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;
    const j = ny * width + nx;
    if (candidate[j] && !mask[j]) { mask[j] = 1; queue.push(j); }
  }
}
// 填补轮廓内部的小空洞，RGB始终来自截图。
const outside = new Uint8Array(size), fill = [];
for (let i = 0; i < size; i++) {
  const x = i % width, y = Math.floor(i / width);
  if ((x === 0 || y === 0 || x === width - 1 || y === height - 1) && !mask[i]) { outside[i] = 1; fill.push(i); }
}
for (let head = 0; head < fill.length; head++) {
  const i = fill[head], x = i % width, y = Math.floor(i / width);
  for (const [dx, dy] of [[-1, 0], [1, 0], [0, -1], [0, 1]]) {
    const nx = x + dx, ny = y + dy;
    if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;
    const j = ny * width + nx;
    if (!mask[j] && !outside[j]) { outside[j] = 1; fill.push(j); }
  }
}
let left = width, top = height, right = 0, bottom = 0, pixels = 0;
for (let i = 0; i < size; i++) {
  const opaque = !outside[i], x = i % width, y = Math.floor(i / width);
  data[i * 4 + 3] = opaque ? 255 : 0;
  if (!opaque) data.fill(0, i * 4, i * 4 + 3);
  if (opaque) { pixels++; left = Math.min(left, x); top = Math.min(top, y); right = Math.max(right, x + 1); bottom = Math.max(bottom, y + 1); }
}
if (pixels < 3000 || pixels > 10000 || left === 0 || top === 0 || right === width || bottom === height)
  throw Error('仙豆抠图边界异常，请重新核对原图。');
const bounds = { left: left - 2, top: top - 2, width: right - left + 4, height: bottom - top + 4 };
const cutout = await sharp(data, { raw: { width, height, channels: 4 } }).extract(bounds).png().toBuffer();
await fs.writeFile(`${output}/SenzuBean-cutout.png`, cutout);
const icon = await sharp(cutout).resize(72, 72, { fit: 'contain', background: '#00000000' })
  .extend({ top: 4, bottom: 4, left: 4, right: 4, background: '#00000000' }).png().toBuffer();
await fs.writeFile(`${output}/SenzuBean.png`, icon);
const alpha = await sharp(icon).extractChannel('alpha').raw().toBuffer();
const outline = Buffer.alloc(80 * 80 * 4, 255);
// 直接对Alpha作2像素最大值扩展，避免图像操作作用到RGB而让轮廓错位。
for (let y = 0; y < 80; y++) for (let x = 0; x < 80; x++) {
  let opacity = 0;
  for (let dy = -2; dy <= 2; dy++) for (let dx = -2; dx <= 2; dx++) {
    const nx = x + dx, ny = y + dy;
    if (nx >= 0 && ny >= 0 && nx < 80 && ny < 80) opacity = Math.max(opacity, alpha[ny * 80 + nx]);
  }
  outline[(y * 80 + x) * 4 + 3] = opacity;
}
for (let i = 0; i < alpha.length; i++) if (outline[i * 4 + 3] < alpha[i]) throw Error('悬停轮廓未完整覆盖图标。');
await sharp(outline, { raw: { width: 80, height: 80, channels: 4 } }).png().toFile(`${output}/SenzuBeanOutline.png`);
const files = [];
for (const name of ['SenzuBean-cutout.png', 'SenzuBean.png', 'SenzuBeanOutline.png']) {
  const file = `${output}/${name}`, bytes = await fs.readFile(file), metadata = await sharp(bytes).metadata();
  files.push({ file, width: metadata.width, height: metadata.height, sha256: crypto.createHash('sha256').update(bytes).digest('hex') });
}
await fs.writeFile(`${output}/manifest.json`, JSON.stringify({
  source, source_sha256: crypto.createHash('sha256').update(await fs.readFile(source)).digest('hex'),
  source_region: region, crop_bounds_in_region: bounds, opaque_source_pixels: pixels,
  processing: '从作者截图连通抠出仙豆与原有黑边，去除手掌及背景；原尺寸主体RGB不变，外部区域RGBA清零；等比适配80×80药水栏图标，Alpha外扩2px制作原生悬停轮廓。无生成、重绘或主体改色。',
  intended_use: '事件专属药水；素材准备，不注册普通掉落、商店或随机药水池。效果及事件方案待作者定稿后接入。',
  files
}, null, 2) + '\n');
console.log(`SENZU_ASSETS_OK: original pixels=${pixels}; cutout=${bounds.width}x${bounds.height}; potion icon+outline=80x80; artwork only`);
