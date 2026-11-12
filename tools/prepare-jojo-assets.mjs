// 按作者要求，从现有苍月草插画裁出前景花朵；只裁剪和缩放，不生成或重绘。
import fs from 'node:fs/promises';
import crypto from 'node:crypto';
import { createRequire } from 'node:module';
const require = createRequire(import.meta.url), sharp = require(process.env.SHARP_MODULE || 'sharp');
const source = 'FrierenMod/images/cards/BlueMoonWeed.png';
const target = 'FrierenMod/images/relics/GhostInBloom.png';
const sourceRect = { left: 170, top: 235, width: 210, height: 145 };
// 遮罩只保留原图花瓣像素；透明画布留出原生悬停轮廓的空间。
const mask = Buffer.from('<svg width="210" height="145"><path fill="white" d="M0 38 Q22 12 43 18 Q64 17 72 57 Q62 38 74 31 Q89 22 96 29 Q108 37 115 35 Q138 35 143 50 Q149 63 129 78 Q147 62 163 66 Q181 62 186 81 L196 81 Q199 77 198 86 L191 100 L175 103 L168 102 L163 111 Q151 102 131 105 Q115 111 104 118 Q72 131 54 115 Q44 108 30 108 Q9 102 0 106 L0 85 Q10 79 0 70 Z"/></svg>');
const flower = await sharp(source).extract(sourceRect).ensureAlpha().composite([{ input: mask, blend: 'dest-in' }]).png().toBuffer();
const png = await sharp(flower).resize(112, 112, { fit: 'contain', background: '#00000000' })
  .extend({ top: 8, bottom: 8, left: 8, right: 8, background: '#00000000' }).png().toBuffer();
await fs.writeFile(target, png);
const alpha = await sharp(png).extractChannel('alpha').dilate(2).raw().toBuffer(), out = Buffer.alloc(128 * 128 * 4, 255);
for (let i = 0; i < alpha.length; i++) out[i * 4 + 3] = alpha[i];
await sharp(out, { raw: { width: 128, height: 128, channels: 4 } }).png().toFile('FrierenMod/images/relics/GhostInBloomOutline.png');
for (const name of ['GhostInBloom', 'GhostInBloomOutline'])
  await fs.writeFile(`FrierenMod/images/relics/${name}.tres`, `[gd_resource type="AtlasTexture" load_steps=2 format=3]\n\n[ext_resource type="Texture2D" path="res://FrierenMod/images/relics/${name}.png" id="1"]\n\n[resource]\natlas = ExtResource("1")\nregion = Rect2(0, 0, 128, 128)\nfilter_clip = true\n`);
const path = 'tools/data/assets.json', records = JSON.parse(await fs.readFile(path, 'utf8'));
for (const file of [target, 'FrierenMod/images/relics/GhostInBloomOutline.png', 'FrierenMod/images/enchantments/HamonOverdrive.svg']) {
  const record = { file, source: file.endsWith('.svg') ? 'tools/native SVG; original concentric ripple glyph' : source,
    sha256: crypto.createHash('sha256').update(await fs.readFile(file)).digest('hex'), modified: !file.endsWith('.svg'),
    processing: file.endsWith('.svg') ? '原生矢量金色波纹符号' : '按作者要求裁出苍月草原图前景花朵，花瓣轮廓遮罩去背景，等比缩放并置于128×128透明画布；轮廓由Alpha外扩2px生成，无生成或重绘',
    ...(file.endsWith('.svg') ? {} : { source_kind: 'derived_from_author_artwork', source_rect: sourceRect }) };
  const index = records.findIndex(r => r.file === file); if (index < 0) records.push(record); else records[index] = record;
}
await fs.writeFile(path, JSON.stringify(records, null, 2) + '\n');
const generatedPath = 'tools/data/generated_assets.json', generated = JSON.parse(await fs.readFile(generatedPath, 'utf8'));
generated.assets = generated.assets.filter(r => r.key !== 'GhostInBloom');
await fs.writeFile(generatedPath, JSON.stringify(generated, null, 2) + '\n');
console.log('JOJO_ASSETS_OK: cropped author BlueMoonWeed art, transparent 128x128 relic + outline; no generated artwork');
