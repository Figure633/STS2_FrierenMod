// 原图只复制；头像从作者图裁切，升级圣徽用作者画在Outline中的版本。
import fs from 'node:fs/promises';
import path from 'node:path';
import { createRequire } from 'node:module';
const require = createRequire(import.meta.url), sharp = require(process.env.SHARP_MODULE || 'sharp');
const sourceDir = '../开发资料/assets/author_adjusted/20261111';
const ancientDir = 'FrierenMod/images/ancients', relicDir = 'FrierenMod/images/relics';
await fs.mkdir(ancientDir, { recursive: true });
await fs.copyFile(`${sourceDir}/Serie.png`, `${ancientDir}/serie.png`);
// 圆形头像只用于地图、历史和对话图标，不裁切事件全图。
const portrait = await sharp(`${sourceDir}/Serie.png`).extract({ left: 385, top: 42, width: 185, height: 185 }).resize(128, 128).ensureAlpha()
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
const upgraded = await fs.readFile(`${sourceDir}/UpgradedHolyEmblem.png`);
await fs.writeFile(`${relicDir}/UpgradedHolyEmblem.png`, upgraded);
await fs.writeFile(`${relicDir}/UpgradedHolyEmblemOutline.png`, await outline(upgraded, 128));
// 扩大Atlas到整个128画布，保留作者伸出原68×68区域的全部金色光线。
for (const name of ['UpgradedHolyEmblem', 'UpgradedHolyEmblemOutline'])
  await fs.writeFile(`${relicDir}/${name}.tres`, `[gd_resource type="AtlasTexture" load_steps=2 format=3]\n\n[ext_resource type="Texture2D" path="res://${relicDir}/${name}.png" id="1"]\n\n[resource]\natlas = ExtResource("1")\nregion = Rect2(0, 0, 128, 128)\nfilter_clip = true\n`);
console.log('SERIE_ASSETS_OK: original=928x524; event=keep_aspect; upgraded_emblem=author_image');
