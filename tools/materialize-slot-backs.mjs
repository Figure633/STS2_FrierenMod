/**
 * 给 PSD 套图成品合成统一的卡片材质：生成式透明倒角素材 + 保持原画的浅浮雕处理。
 * node tools/materialize-slot-backs.mjs <base-preview-dir> <output-dir> [--candidates]
 * sharp 可通过 SHARP_MODULE 指定；不会修改输入图片或游戏资源。
 */
import fs from 'node:fs/promises';
import path from 'node:path';
import { createRequire } from 'node:module';
const require = createRequire(import.meta.url);
const sharp = require(process.env.SHARP_MODULE || 'sharp');
const [inputArg, outputArg, option] = process.argv.slice(2);
if (!inputArg || !outputArg) throw Error('需要套图预览目录和独立输出目录');
const input = path.resolve(inputArg), output = path.resolve(outputArg);
if (input === output) throw Error('输出必须另存，不能覆盖输入');
const manifest = JSON.parse(await fs.readFile(path.join(input, 'manifest.json'), 'utf8'));
const W = 301, H = 461;
if (manifest.width !== W || manifest.height !== H) throw Error('输入尺寸错误');
const png = data => sharp(data, { raw: { width: W, height: H, channels: 4 } }).png().toBuffer();
const sourceRim = path.join(output, '制作素材', '倒角材质-生成原稿.png');
const rim = await sharp(sourceRim).resize(W, H).ensureAlpha().raw().toBuffer();
const clamp = (n, lo = 0, hi = 255) => Math.min(hi, Math.max(lo, n));
const profiles = {
  '轻': { relief: .28, limit: 14, rim: .60, saturation: .93 },
  '适中': { relief: .48, limit: 24, rim: .92, saturation: .88 },
  '重': { relief: .82, limit: 38, rim: 1.15, saturation: .77 }
};
function edgeDistance(x, y) {
  const radius = 21;
  const qx = Math.abs(x + .5 - W / 2) - (W / 2 - radius);
  const qy = Math.abs(y + .5 - H / 2) - (H / 2 - radius);
  return radius - Math.hypot(Math.max(qx, 0), Math.max(qy, 0)) - Math.min(Math.max(qx, qy), 0);
}
function softLight(base, light) {
  const b = base / 255, s = light / 255;
  if (s <= .5) return (b - (1 - 2 * s) * b * (1 - b)) * 255;
  const d = b <= .25 ? ((16 * b - 12) * b + 4) * b : Math.sqrt(b);
  return (b + (2 * s - 1) * (d - b)) * 255;
}
async function materialize(file, profile, gold) {
  const { data, info } = await sharp(file).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  if (info.width !== W || info.height !== H) throw Error(`尺寸错误: ${file}`);
  const blurred = await sharp(data, { raw: { width: W, height: H, channels: 4 } }).blur(.5).raw().toBuffer();
  const luminance = new Float32Array(W * H);
  for (let i = 0; i < luminance.length; i++) luminance[i] = .2126 * blurred[i * 4] + .7152 * blurred[i * 4 + 1] + .0722 * blurred[i * 4 + 2];
  const out = Buffer.from(data);
  for (let y = 0; y < H; y++) for (let x = 0; x < W; x++) {
    const index = y * W + x, p = index * 4;
    if (!data[p + 3]) continue;
    const lum = .2126 * data[p] + .7152 * data[p + 1] + .0722 * data[p + 2];
    const a = luminance[Math.max(0, y - 1) * W + Math.max(0, x - 1)];
    const b = luminance[Math.min(H - 1, y + 1) * W + Math.min(W - 1, x + 1)];
    const ornament = gold && (y < 32 || y >= 430);
    const bump = clamp((a - b) * profile.relief * (ornament ? 1.2 : 1), -profile.limit, profile.limit);
    // 只在现有轮廓上产生一对亮暗边；平坦肤色区域没有浮雕偏移。
    const saturation = ornament ? profile.saturation * .72 : profile.saturation;
    const distance = edgeDistance(x, y);
    // 材质限制在约 8px 的边带，避免素材内缘杂点进入主体。
    const fade = clamp((8.7 - distance) / 1.6, 0, 1);
    const alpha = rim[p + 3] / 255 * profile.rim * fade;
    // 凹入的内缘是一道细折线，不能将整个外圈处理成模糊光晕。
    const innerEdge = Math.exp(-(((distance - 7.5) / .5) ** 2));
    const nearestLeft = x < W / 2 && x <= y && x <= H - 1 - y;
    const nearestTop = y < H / 2 && y <= x && y <= W - 1 - x;
    const edgeLight = nearestLeft || nearestTop ? 8 : -8;
    for (let c = 0; c < 3; c++) {
      let value = clamp(lum + (data[p + c] - lum) * saturation + bump);
      if (alpha > .01) {
        value += (softLight(value, rim[p + c]) - value) * Math.min(1, alpha);
        value += (rim[p + c] - value) * Math.min(.13, alpha * .13);
      }
      value += innerEdge * edgeLight * profile.rim;
      out[p + c] = Math.round(clamp(value));
    }
  }
  return out;
}
const escape = s => s.replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('"', '&quot;');
async function contact(items, file, columns = 5, title = '') {
  const cellW = 329, cellH = 505, header = title ? 52 : 0, rows = Math.ceil(items.length / columns), layers = [];
  if (title) layers.push({ input: Buffer.from(`<svg width="${cellW * columns}" height="52"><text x="18" y="34" fill="#e7e1d4" font-size="21" font-family="Microsoft YaHei,Arial,sans-serif">${escape(title)}</text></svg>`), left: 0, top: 0 });
  for (let i = 0; i < items.length; i++) {
    const item = items[i], left = i % columns * cellW, top = Math.floor(i / columns) * cellH + header;
    layers.push({ input: item.input, left: left + 14, top: top + 8 });
    layers.push({ input: Buffer.from(`<svg width="${cellW}" height="32"><text x="${cellW / 2}" y="23" fill="#e7e1d4" font-size="14" font-family="Microsoft YaHei,Arial,sans-serif" text-anchor="middle">${escape(item.label)}</text></svg>`), left, top: top + 472 });
  }
  await sharp({ create: { width: columns * cellW, height: rows * cellH + header, channels: 4, background: '#202630' } }).composite(layers).png().toFile(file);
}
await fs.mkdir(path.join(output, '制作对照'), { recursive: true });
const representatives = [0, 3, 12, 17];
if (option === '--candidates') {
  const items = [];
  for (const index of representatives) {
    const entry = manifest.images[index], file = path.join(input, '基础圆角', entry.file);
    items.push({ input: file, label: `${entry.number} · 原套图` });
    for (const [name, profile] of Object.entries(profiles)) {
      const result = await png(await materialize(file, profile, false));
      const destination = path.join(output, '制作对照', `${String(entry.number).padStart(2, '0')}-${name}.png`);
      await fs.writeFile(destination, result);
      items.push({ input: result, label: `${entry.number} · ${name}` });
    }
  }
  await contact(items, path.join(output, '制作对照', '强度比较.png'), 4, '原套图 / 轻 / 适中 / 重');
  console.log('MATERIAL_CANDIDATES_OK: 4组 × 4列');
  process.exit(0);
}

const selected = profiles['适中'];
const fittedRim = Buffer.from(rim);
for (let y = 0; y < H; y++) for (let x = 0; x < W; x++) {
  const p = (y * W + x) * 4;
  fittedRim[p + 3] = Math.round(rim[p + 3] * Math.min(1, selected.rim) * clamp((8.7 - edgeDistance(x, y)) / 1.6, 0, 1));
}
await fs.writeFile(path.join(output, '制作素材', '倒角材质-裁定边带.png'), await png(fittedRim));
let checked = 0;
for (const directory of ['基础圆角', '金色花纹']) {
  await fs.mkdir(path.join(output, directory), { recursive: true });
  const items = [];
  for (const entry of manifest.images) {
    const before = path.join(input, directory, entry.file);
    const result = await materialize(before, selected, directory === '金色花纹');
    const originalAlpha = await sharp(before).ensureAlpha().raw().toBuffer();
    for (let p = 3; p < result.length; p += 4) if (result[p] !== originalAlpha[p]) throw Error(`${entry.file} 透明轮廓改变`);
    const final = path.join(output, directory, entry.file);
    await fs.writeFile(final, await png(result));
    items.push({ input: final, label: `${String(entry.number).padStart(2, '0')} · ${entry.name}` });
    checked++;
  }
  await contact(items, path.join(output, `${directory}-最终总览.png`), 5, `${directory} · 浅浮雕与倒角最终版`);
}
const beforeAfter = [];
for (const index of representatives) {
  const entry = manifest.images[index];
  for (const directory of ['基础圆角', '金色花纹']) {
    beforeAfter.push({ input: path.join(input, directory, entry.file), label: `${entry.number} · ${directory} · 修改前` });
    beforeAfter.push({ input: path.join(output, directory, entry.file), label: `${entry.number} · ${directory} · 最终版` });
  }
}
await contact(beforeAfter, path.join(output, '修改前后对照.png'), 4, '基础版：修改前 / 最终版　　花纹版：修改前 / 最终版');
const referenceDir = path.dirname(manifest.basicTemplate);
await contact([
  { input: path.join(referenceDir, '图图', '9001.png'), label: '旧图参考 · 彩色浅浮雕' },
  { input: path.join(output, '基础圆角', manifest.images[0].file), label: '最终版 · 人像' },
  { input: path.join(output, '基础圆角', manifest.images[3].file), label: '最终版 · 花冠' },
  { input: path.join(output, '基础圆角', manifest.images[13].file), label: '最终版 · 冬日' },
  { input: path.join(referenceDir, '图图', '0001.png'), label: '旧图参考 · 金色花纹' },
  { input: path.join(output, '金色花纹', manifest.images[3].file), label: '最终版 · 花冠' },
  { input: path.join(output, '金色花纹', manifest.images[12].file), label: '最终版 · 雪原' },
  { input: path.join(output, '金色花纹', manifest.images[15].file), label: '最终版 · 菲伦' }
], path.join(output, '旧图参考对照.png'), 4, '每行第一张为旧图参考，其余为本次最终成品');
await fs.writeFile(path.join(output, 'manifest.json'), JSON.stringify({ ...manifest, note: '最终材质版：原画和裁切保持；浅浮雕叠加生成式中性透明倒角素材；尚未替换游戏资源。', material: { profile: selected, rimWidth: 8.7, sourceRim: '制作素材/倒角材质-生成原稿.png', alphaPreserved: true } }, null, 2));
const cards = manifest.images.map(entry => `<article><h2>${String(entry.number).padStart(2, '0')} · ${escape(entry.name)}</h2><div class="pair">${['金色花纹', '基础圆角'].map(d => `<figure><img src="${encodeURI(`${d}/${entry.file}`)}"><figcaption>${d}</figcaption></figure>`).join('')}</div></article>`).join('');
await fs.writeFile(path.join(output, 'index.html'), `<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width"><title>卡槽背景最终版</title><style>*{box-sizing:border-box}body{margin:0;padding:30px;background:#161b24;color:#eee6d8;font-family:system-ui,sans-serif}header{max-width:1300px;margin:0 auto 24px}h1{font-size:26px}p{color:#b5beca;line-height:1.7}main{max-width:1500px;margin:auto;display:grid;grid-template-columns:repeat(auto-fit,minmax(320px,1fr));gap:20px}article{background:#242b36;padding:18px;border-radius:12px}h2{font-size:16px;font-weight:500;margin:0 0 16px}.pair{display:flex;gap:12px}figure{margin:0;width:50%;text-align:center}img{width:100%;max-width:301px}figcaption{color:#b5beca;font-size:12px;margin-top:8px}a{color:#e2c998}</style><header><h1>卡槽背景 · 最终材质版</h1><p>19张原画，两种模板，共38张301×461透明PNG。补充统一的边缘倒角、内侧亮暗线和浅浮雕，保留原画构图与透明轮廓。</p><p><a href="${encodeURI('修改前后对照.png')}">修改前后对照</a> · <a href="${encodeURI('基础圆角-最终总览.png')}">基础版总览</a> · <a href="${encodeURI('金色花纹-最终总览.png')}">花纹版总览</a></p></header><main>${cards}</main></html>`);
console.log(`SLOT_BACK_MATERIAL_OK: ${checked} 张 301×461，全部透明轮廓逐像素一致`);
