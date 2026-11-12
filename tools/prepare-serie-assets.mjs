// 原图只复制；头像从作者图裁切，升级圣徽用作者画在Outline中的版本。
import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import { createRequire } from 'node:module';
const require = createRequire(import.meta.url), sharp = require(process.env.SHARP_MODULE || 'sharp');
const sourceDir = '../开发资料/assets/author_adjusted/20261111';
const ancientDir = 'FrierenMod/images/ancients', relicDir = 'FrierenMod/images/relics';
await fs.mkdir(ancientDir, { recursive: true });
if (process.argv.includes('--faceless')) {
  const source = '../开发资料/assets/prepared/serie-map-faceless/serie-faceless-preview.png';
  const generated = JSON.parse(await fs.readFile('tools/data/generated_assets.json', 'utf8'));
  let authored = JSON.parse(await fs.readFile('tools/data/assets.json', 'utf8'));
  for (const [name, size] of [['node', 128], ['history', 64]]) {
    const png = await sharp(source).resize(size, size, { fit: 'contain', background: '#00000000' }).png().toBuffer();
    for (const [suffix, data] of [['', png], ['_outline', await outline(png, size)]]) {
      const target = `${ancientDir}/serie_${name}${suffix}.png`;
      await fs.writeFile(target, data);
      authored = authored.filter(item => item.file !== target);
      generated.assets = generated.assets.filter(item => item.target !== target);
      generated.assets.push({key: `serie_${name}${suffix}`, date: '2026-11-13', target,
        sha256: crypto.createHash('sha256').update(data).digest('hex'), tool: 'built-in image_gen',
        source, prompt_file: '../开发资料/assets/prepared/serie-map-faceless/prompt.json', prompt_key: 'prompt',
        transparent_background: true, processing: `tools/prepare-serie-assets.mjs --faceless; approved full silhouette uniformly resized to ${size}x${size}; white outline from alpha`});
    }
  }
  await fs.writeFile('tools/data/assets.json', JSON.stringify(authored, null, 2) + '\n');
  await fs.writeFile('tools/data/generated_assets.json', JSON.stringify(generated, null, 2) + '\n');
  console.log('SERIE_FACELESS_OK: node=128x128 history=64x64; approved silhouette preserved');
  process.exit(0);
}
const avatarOnly = process.argv.includes('--avatar-only');
if (!avatarOnly) await fs.copyFile(`${sourceDir}/Serie.png`, `${ancientDir}/serie.png`);
// 圆形头像只用于地图、历史和对话图标，不裁切事件全图。
const portrait = await sharp(`${sourceDir}/Serie.png`).extract({ left: 372, top: 42, width: 185, height: 185 }).resize(128, 128).ensureAlpha()
  .composite([{ input: Buffer.from('<svg width="128" height="128"><circle cx="64" cy="64" r="60" fill="white"/></svg>'), blend: 'dest-in' }]).png().toBuffer();
async function outline(png, size) {
  const alpha = await sharp(png).extractChannel('alpha').dilate(2).raw().toBuffer(), out = Buffer.alloc(size * size * 4, 255);
  for (let i = 0; i < alpha.length; i++) out[i * 4 + 3] = alpha[i];
  return sharp(out, { raw: { width: size, height: size, channels: 4 } }).png().toBuffer();
}
for (const [name, size] of [['node', 128], ['history', 64]]) {
  const png = await sharp(portrait).resize(size, size).png().toBuffer();
  await fs.writeFile(`${ancientDir}/serie_${name}.png`, png);
  await fs.writeFile(`${ancientDir}/serie_${name}_outline.png`, await outline(png, size));
}
if (!avatarOnly) {
const upgraded = await fs.readFile(`${sourceDir}/UpgradedHolyEmblem.png`);
await fs.writeFile(`${relicDir}/UpgradedHolyEmblem.png`, upgraded);
await fs.writeFile(`${relicDir}/UpgradedHolyEmblemOutline.png`, await outline(upgraded, 128));
// 扩大Atlas到整个128画布，保留作者伸出原68×68区域的全部金色光线。
for (const name of ['UpgradedHolyEmblem', 'UpgradedHolyEmblemOutline'])
  await fs.writeFile(`${relicDir}/${name}.tres`, `[gd_resource type="AtlasTexture" load_steps=2 format=3]\n\n[ext_resource type="Texture2D" path="res://${relicDir}/${name}.png" id="1"]\n\n[resource]\natlas = ExtResource("1")\nregion = Rect2(0, 0, 128, 128)\nfilter_clip = true\n`);
}
console.log('SERIE_ASSETS_OK: original=928x524; event=keep_aspect; upgraded_emblem=author_image');
