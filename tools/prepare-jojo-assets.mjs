// 将内置image_gen的透明原稿另存后，生成原生遗物图标与轮廓；不修改事件原海报。
import fs from 'node:fs/promises';
import crypto from 'node:crypto';
import { createRequire } from 'node:module';
const require = createRequire(import.meta.url), sharp = require(process.env.SHARP_MODULE || 'sharp');
const source = 'assets/generated/jojo-20261111/GhostInBloom-source.png';
const target = 'FrierenMod/images/relics/GhostInBloom.png';
const original = await sharp(source).metadata();
if (!original.hasAlpha) throw Error('透明图标原稿必须带Alpha');
const png = await sharp(source).resize(128, 128, { fit: 'contain', background: '#00000000' }).png().toBuffer();
await fs.writeFile(target, png);
const alpha = await sharp(png).extractChannel('alpha').dilate(2).raw().toBuffer(), out = Buffer.alloc(128 * 128 * 4, 255);
for (let i = 0; i < alpha.length; i++) out[i * 4 + 3] = alpha[i];
await sharp(out, { raw: { width: 128, height: 128, channels: 4 } }).png().toFile('FrierenMod/images/relics/GhostInBloomOutline.png');
for (const name of ['GhostInBloom', 'GhostInBloomOutline'])
  await fs.writeFile(`FrierenMod/images/relics/${name}.tres`, `[gd_resource type="AtlasTexture" load_steps=2 format=3]\n\n[ext_resource type="Texture2D" path="res://FrierenMod/images/relics/${name}.png" id="1"]\n\n[resource]\natlas = ExtResource("1")\nregion = Rect2(0, 0, 128, 128)\nfilter_clip = true\n`);
const path = 'docs/assets.json', records = JSON.parse(await fs.readFile(path, 'utf8'));
for (const file of [target, 'FrierenMod/images/relics/GhostInBloomOutline.png', 'FrierenMod/images/enchantments/HamonOverdrive.svg']) {
  const record = { file, source: file.endsWith('.svg') ? 'tools/native SVG; original concentric ripple glyph' : source,
    sha256: crypto.createHash('sha256').update(await fs.readFile(file)).digest('hex'), modified: !file.endsWith('.svg'),
    processing: file.endsWith('.svg') ? '原生矢量金色波纹符号' : '内置image_gen透明花幽灵原稿等比缩至128×128；轮廓由Alpha外扩2px生成' };
  const index = records.findIndex(r => r.file === file); if (index < 0) records.push(record); else records[index] = record;
}
await fs.writeFile(path, JSON.stringify(records, null, 2) + '\n');
const generatedPath = 'docs/generated_assets.json', generated = JSON.parse(await fs.readFile(generatedPath, 'utf8'));
const entry = { key: 'GhostInBloom', date: '2026-11-11', tool: 'built-in image_gen',
  prompt: await fs.readFile('assets/generated/jojo-20261111/prompt.txt', 'utf8'), transparent_background: true,
  source, target, sha256: crypto.createHash('sha256').update(png).digest('hex'), status: '新事件遗物图标；待作者游戏验收' };
generated.assets = generated.assets.filter(r => r.key !== entry.key); generated.assets.push(entry);
await fs.writeFile(generatedPath, JSON.stringify(generated, null, 2) + '\n');
console.log('JOJO_ASSETS_OK: transparent 128x128 relic + outline; original poster preserved');
