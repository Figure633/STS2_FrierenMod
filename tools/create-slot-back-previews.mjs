/**
 * 将作者截图直接放进原始 PSD 模板。仅裁切、缩放与原图层合成，不生成或重画原画。
 * node tools/create-slot-back-previews.mjs <manifest.json> <output-directory>
 * 依赖 sharp；也可用 SHARP_MODULE 指定安装目录。PSD 限 8 位 RGB、普通混合图层。
 */
import fs from 'node:fs/promises';
import path from 'node:path';
import { createRequire } from 'node:module';
const require = createRequire(import.meta.url);
const sharp = require(process.env.SHARP_MODULE || 'sharp');

function psdReader(buffer) {
  let position = 0;
  const bytes = n => { const b = buffer.subarray(position, position + n); position += n; return b; };
  const u16 = () => bytes(2).readUInt16BE();
  const i16 = () => bytes(2).readInt16BE();
  const u32 = () => bytes(4).readUInt32BE();
  const i32 = () => bytes(4).readInt32BE();
  if (bytes(4).toString() !== '8BPS' || u16() !== 1) throw Error('只支持 PSD v1');
  bytes(6);
  const channelCount = u16(), height = u32(), width = u32(), depth = u16(), mode = u16();
  if (depth !== 8 || mode !== 3) throw Error('只支持 8 位 RGB PSD');
  bytes(u32()); bytes(u32());
  const sectionLength = u32(), sectionStart = position;
  const infoLength = u32(), infoStart = position;
  const count = Math.abs(i16());
  const layers = [];
  for (let k = 0; k < count; k++) {
    const top = i32(), left = i32(), bottom = i32(), right = i32(), channels = [];
    for (let n = u16(); n > 0; n--) channels.push({ id: i16(), length: u32() });
    if (bytes(4).toString() !== '8BIM') throw Error('图层签名无效');
    const blend = bytes(4).toString(), opacity = bytes(1)[0], clipping = bytes(1)[0], flags = bytes(1)[0];
    bytes(1);
    const extraLength = u32(), end = position + extraLength;
    bytes(u32()); bytes(u32());
    const nameLength = bytes(1)[0];
    let name = bytes(nameLength).toString('latin1');
    bytes((4 - (nameLength + 1) % 4) % 4);
    while (position + 12 <= end) {
      bytes(4); const key = bytes(4).toString(), len = u32(), data = bytes(len); bytes(len % 2);
      if (key === 'luni') {
        name = '';
        for (let c = 0; c < data.readUInt32BE(); c++) name += String.fromCharCode(data.readUInt16BE(4 + c * 2));
      }
    }
    position = end;
    layers.push({ name, top, left, width: right - left, height: bottom - top, channels, blend, opacity, clipping, visible: !(flags & 2) });
  }
  function decode(data, w, h, planes = 1) {
    const compression = data.readUInt16BE();
    if (compression === 0) return data.subarray(2, 2 + w * h * planes);
    if (compression !== 1) throw Error(`PSD 压缩方式 ${compression} 暂不支持`);
    const rows = h * planes, output = Buffer.alloc(w * rows);
    let cursor = 2 + rows * 2;
    for (let y = 0; y < rows; y++) {
      const end = cursor + data.readUInt16BE(2 + y * 2);
      let target = y * w;
      while (cursor < end) {
        const n = data.readInt8(cursor++);
        if (n >= 0) { data.copy(output, target, cursor, cursor + n + 1); target += n + 1; cursor += n + 1; }
        else if (n !== -128) { const value = data[cursor++]; output.fill(value, target, target + 1 - n); target += 1 - n; }
      }
      if (target !== (y + 1) * w) throw Error('PSD RLE 行长度错误');
    }
    return output;
  }
  function rgba(planes, w, h) {
    const size = w * h, out = Buffer.alloc(size * 4);
    for (let p = 0; p < size; p++) {
      for (let c = 0; c < 3; c++) out[p * 4 + c] = planes[c]?.[p] ?? 0;
      out[p * 4 + 3] = planes[-1]?.[p] ?? 255;
    }
    return out;
  }
  for (const layer of layers) {
    const planes = {};
    for (const channel of layer.channels) planes[channel.id] = decode(bytes(channel.length), layer.width, layer.height);
    layer.rgba = rgba(planes, layer.width, layer.height);
  }
  position = sectionStart + sectionLength;
  const mergedData = decode(bytes(buffer.length - position), width, height, channelCount), planes = {}, size = width * height;
  for (let c = 0; c < Math.min(channelCount, 4); c++) planes[c === 3 ? -1 : c] = mergedData.subarray(c * size, (c + 1) * size);
  return { width, height, layers, merged: rgba(planes, width, height) };
}

const [manifestFile, destination] = process.argv.slice(2);
if (!manifestFile || !destination) throw Error('用法: node create-slot-back-previews.mjs manifest.json output-directory');
const manifest = JSON.parse(await fs.readFile(manifestFile, 'utf8'));
const output = path.resolve(destination);
await fs.mkdir(output, { recursive: true });
await fs.writeFile(path.join(output, '.gdignore'), '');
for (const dir of ['金色花纹', '基础圆角', '原图', '模板图层']) await fs.mkdir(path.join(output, dir), { recursive: true });
const basic = psdReader(await fs.readFile(manifest.basicTemplate));
const decorated = psdReader(await fs.readFile(manifest.decoratedTemplate));
const { width: W, height: H } = basic;
if (W !== 301 || H !== 461 || decorated.width !== W || decorated.height !== H) throw Error('模板尺寸必须为 301×461');
const nativeMask = Buffer.alloc(W * H * 4), outline = Buffer.from(basic.merged);
for (let i = 0; i < W * H; i++) {
  nativeMask.fill(255, i * 4, i * 4 + 3); nativeMask[i * 4 + 3] = basic.merged[i * 4 + 3];
  outline[i * 4 + 3] = Math.round(outline[i * 4 + 3] * (255 - outline[i * 4]) / 255);
  outline.fill(0, i * 4, i * 4 + 3);
}
const png = (data, width = W, height = H) => sharp(data, { raw: { width, height, channels: 4 } }).png().toBuffer();
const maskPNG = await png(nativeMask), outlinePNG = await png(outline);
const ornaments = [];
for (const layer of decorated.layers) {
  if (!layer.visible || layer.name === '矩形 1') continue;
  if (layer.blend !== 'norm' || layer.opacity !== 255 || layer.clipping) throw Error('遇到未支持的图层混合方式');
  const left = Math.max(0, layer.left), top = Math.max(0, layer.top);
  const w = Math.min(W, layer.left + layer.width) - left, h = Math.min(H, layer.top + layer.height) - top;
  const layerPNG = await sharp(layer.rgba, { raw: { width: layer.width, height: layer.height, channels: 4 } }).extract({ left: left - layer.left, top: top - layer.top, width: w, height: h }).png().toBuffer();
  ornaments.push({ input: layerPNG, left, top });
  await fs.writeFile(path.join(output, '模板图层', `${layer.name}.png`), layerPNG);
}
await fs.writeFile(path.join(output, '模板图层', '圆角蒙版.png'), maskPNG);
await fs.writeFile(path.join(output, '模板图层', '原生描边.png'), outlinePNG);
const records = [];
for (let index = 0; index < manifest.images.length; index++) {
  const entry = manifest.images[index], id = String(index + 1).padStart(2, '0');
  const source = await fs.readFile(entry.source), originalMeta = await sharp(source).metadata();
  const region = entry.sourceRect ?? { left: 0, top: 0, width: originalMeta.width, height: originalMeta.height };
  if (!['left', 'top', 'width', 'height'].every(k => Number.isInteger(region[k])) ||
      region.left < 0 || region.top < 0 || region.width < 1 || region.height < 1 ||
      region.left + region.width > originalMeta.width || region.top + region.height > originalMeta.height)
    throw Error(`${entry.name} 原图裁切区域无效`);
  if (entry.fit && !['cover', 'contain'].includes(entry.fit)) throw Error(`${entry.name} fit 必须为 cover 或 contain`);
  let preparation = sharp(source).extract(region);
  if (entry.background) preparation = preparation.flatten({ background: entry.background });
  const prepared = await preparation.png().toBuffer(), meta = await sharp(prepared).metadata();
  const file = `${id}-${entry.name}.png`;
  const preservedSource = path.join(output, '原图', file);
  if (path.resolve(entry.source) !== preservedSource) await fs.copyFile(entry.source, preservedSource);
  const record = { number: index + 1, name: entry.name, file, source: entry.source, focus: entry.focus ?? [.5, .5],
    originalWidth: originalMeta.width, originalHeight: originalMeta.height, sourceRect: region,
    fit: entry.fit ?? 'cover', background: entry.background ?? null, crops: {} };
  for (const [directory, top, innerHeight, frame] of [['基础圆角', 0, H, []], ['金色花纹', 32, 398, ornaments]]) {
    const ratio = W / innerHeight;
    let cropWidth = meta.width, cropHeight = meta.height;
    if (meta.width / meta.height > ratio) cropWidth = meta.height * ratio;
    else cropHeight = meta.width / ratio;
    const cx = entry.focus?.[0] ?? .5, cy = entry.focus?.[1] ?? .5;
    const left = Math.round(Math.max(0, Math.min(meta.width - cropWidth, meta.width * cx - cropWidth / 2)));
    const y = Math.round(Math.max(0, Math.min(meta.height - cropHeight, meta.height * cy - cropHeight / 2)));
    const crop = { left, top: y, width: Math.min(Math.round(cropWidth), meta.width - left), height: Math.min(Math.round(cropHeight), meta.height - y) };
    const art = entry.fit === 'contain'
      ? await sharp(prepared).resize(W, innerHeight, { fit: 'contain', background: entry.background ?? '#ffffff' }).png().toBuffer()
      : await sharp(prepared).extract(crop).resize(W, innerHeight, { fit: 'cover' }).png().toBuffer();
    // 扩展图片上下边缘到花纹背后；原生图层的透明缝隙不会露出白底。
    const canvas = await sharp(art).extend({ top, bottom: H - innerHeight - top, left: 0, right: 0, extendWith: 'copy' }).png().toBuffer();
    const framed = await sharp(canvas).composite([{ input: outlinePNG }, ...frame]).png().toBuffer();
    const final = await sharp(framed).composite([{ input: maskPNG, blend: 'dest-in' }]).png().toBuffer();
    await fs.writeFile(path.join(output, directory, file), final);
    const check = await sharp(final).ensureAlpha().raw().toBuffer();
    for (const pixel of [0, W - 1, (H - 1) * W, H * W - 1]) if (check[pixel * 4 + 3] !== 0) throw Error(`${file} 圆角外不透明`);
    record.crops[directory] = entry.fit === 'contain'
      ? { left: region.left, top: region.top, width: region.width, height: region.height, fit: 'contain' }
      : { ...crop, left: crop.left + region.left, top: crop.top + region.top, fit: 'cover' };
  }
  records.push(record);
}
await fs.writeFile(path.join(output, 'manifest.json'), JSON.stringify({ note: '直接套用原始 PSD 图层，未使用生成式重画，未替换游戏内资源。', basicTemplate: manifest.basicTemplate, decoratedTemplate: manifest.decoratedTemplate, width: W, height: H, images: records }, null, 2));
const escape = s => s.replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('"', '&quot;');
const cards = records.map(r => `<article><h2>${String(r.number).padStart(2, '0')} · ${escape(r.name)}</h2><div class="pair"><figure><img src="${encodeURI(`金色花纹/${r.file}`)}"><figcaption>模板·改</figcaption></figure><figure><img src="${encodeURI(`基础圆角/${r.file}`)}"><figcaption>模板真</figcaption></figure></div><details><summary>查看原图</summary><img class="source" src="${encodeURI(`原图/${r.file}`)}"></details></article>`).join('\n');
await fs.writeFile(path.join(output, 'index.html'), `<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width"><title>卡槽背景预览 · ${records.length}组</title><style>*{box-sizing:border-box}body{margin:0;padding:32px;background:#161b24;color:#eae4d9;font-family:system-ui,sans-serif}header{max-width:1250px;margin:auto auto 24px}h1{font-size:27px;font-weight:600}p,figcaption,summary{color:#b6bbc5}main{max-width:1500px;margin:auto;display:grid;grid-template-columns:repeat(auto-fit,minmax(320px,1fr));gap:22px}article{background:#242b36;padding:18px;border-radius:14px}h2{font-size:16px;margin:0 0 16px}.pair{display:flex;gap:12px;justify-content:center}figure{margin:0;width:50%;text-align:center}figure img{width:100%;max-width:301px;height:auto;display:block;margin:auto}figcaption{font-size:12px;margin-top:9px}summary{cursor:pointer;font-size:13px;margin-top:16px}.source{width:100%;margin-top:12px}a{color:#ddc693}</style><header><h1>卡槽背景 · 模板对照预览</h1><p>${records.length} 张原图 × 2 种模板，共 ${records.length * 2} 张透明 PNG，单张 301×461。左为金色花纹，右为基础圆角。点击“查看原图”对照裁切。</p><p>保持原画比例，逐张调整主体位置；直接使用原 PSD 轮廓及装饰图层。此批仅供预览，未指定成就，也未替换游戏资源。</p></header><main>${cards}</main></html>`);
// 总览采用成品原尺寸；标签与背景只用于预览，不写入单张图片。
for (const directory of ['金色花纹', '基础圆角']) {
  const cols = manifest.columns ?? 5;
  if (!Number.isInteger(cols) || cols < 1 || cols > 8) throw Error('总览列数必须是1至8的整数');
  const rows = Math.ceil(records.length / cols), cellW = 329, cellH = 509;
  const composite = [];
  for (let i = 0; i < records.length; i++) {
    const x = i % cols * cellW, y = Math.floor(i / cols) * cellH, r = records[i];
    composite.push({ input: path.join(output, directory, r.file), left: x + 14, top: y + 12 });
    const text = `<svg width="${cellW}" height="32"><text x="${cellW / 2}" y="23" fill="#e6e0d5" font-family="Microsoft YaHei,Arial,sans-serif" font-size="15" text-anchor="middle">${String(r.number).padStart(2, '0')} · ${escape(r.name)}</text></svg>`;
    composite.push({ input: Buffer.from(text), left: x, top: y + H + 17 });
  }
  await sharp({ create: { width: cellW * cols, height: cellH * rows, channels: 4, background: '#202630' } }).composite(composite).png().toFile(path.join(output, `${directory}-总览.png`));
}
console.log(`SLOT_BACK_PREVIEW_OK: ${records.length} 张原图，${records.length * 2} 张 301×461 RGBA 成品；${output}`);
