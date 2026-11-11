// 作者原图按 Word 的 srcRect 裁剪；龙珠使用可编辑的 UI 矢量图标，不生成插画。
import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import { createRequire } from 'node:module';
const require=createRequire(import.meta.url),sharp=require(process.env.SHARP_MODULE||'sharp');
const sources='assets/author_adjusted/20261111/events';
await fs.mkdir(sources,{recursive:true}); await fs.writeFile(path.join(sources,'.gdignore'),'');
const extracted=process.argv[2]||'artifacts/new-event-art';
const manifest={sourceDocument:'../事件todo.docx',assets:[]};
async function author(input,name,crop={}) {
  const original=await fs.readFile(path.join(extracted,input));
  const saved=path.join(sources,input);
  let previous;try{previous=await fs.readFile(saved);}catch(e){if(e.code!=='ENOENT')throw e;}
  if(previous&&!previous.equals(original))throw Error('作者原图已存在且不同：'+saved);
  if(!previous)await fs.writeFile(saved,original);
  const m=await sharp(original).metadata();
  const left=Math.round(m.width*(crop.l||0)/100000),top=Math.round(m.height*(crop.t||0)/100000);
  const width=m.width-left-Math.round(m.width*(crop.r||0)/100000),height=m.height-top-Math.round(m.height*(crop.b||0)/100000);
  await sharp(original).extract({left,top,width,height}).png().toFile('FrierenMod/images/events/'+name+'.png');
  manifest.assets.push({name,source:saved,sha256:crypto.createHash('sha256').update(original).digest('hex'),wordSrcRect:crop,output:[width,height]});
}
await author('p2-image1.png','SnowCrystalLand');
await author('p5-image2.png','SnowCrystalTouched',{l:2190,t:3232,r:2192,b:10830});
await author('p86-image14.jpeg','SummonDragon');
await author('p102-image15.png','DebtCollector',{t:5044,b:5112});
const kameSource=path.join(sources,'Kamehameha-reference.png');
try{await fs.access(kameSource);}catch{await fs.copyFile('../png/龟派气功！.png',kameSource);}
const kameBytes=await fs.readFile(kameSource);
await sharp(kameBytes).resize(250,190,{fit:'cover'}).png().toFile('FrierenMod/images/cards/Kamehameha.png');
manifest.assets.push({name:'Kamehameha',source:kameSource,sha256:crypto.createHash('sha256').update(kameBytes).digest('hex'),output:[250,190]});
for(const file of ['SenzuBean.png','SenzuBeanOutline.png']) await fs.copyFile('assets/prepared/event-potions/senzu/'+file,'FrierenMod/images/potions/'+file);
// 1—7 星的离散图形，每一颗都能在魔法书和奖励预览中辨认。
const positions={1:[[125,95]],2:[[107,78],[143,112]],3:[[125,67],[98,112],[152,112]],4:[[103,73],[147,73],[103,117],[147,117]],5:[[103,72],[147,72],[125,95],[103,118],[147,118]],6:[[125,61],[96,78],[154,78],[96,112],[154,112],[125,129]],7:[[125,95],[125,57],[92,76],[158,76],[92,114],[158,114],[125,133]]};
const svgDir='assets/prepared/dragon-balls';await fs.mkdir(svgDir,{recursive:true});await fs.writeFile(path.join(svgDir,'.gdignore'),'');
function star(x,y){return Array.from({length:10},(_,i)=>{const a=-Math.PI/2+i*Math.PI/5,r=i%2?4.4:10;return `${(x+Math.cos(a)*r).toFixed(2)},${(y+Math.sin(a)*r).toFixed(2)}`;}).join(' ');}
for(let n=1;n<=7;n++){
  const svg=`<svg xmlns="http://www.w3.org/2000/svg" width="250" height="190" viewBox="0 0 250 190"><defs><radialGradient id="g" cx=".32" cy=".25" r=".8"><stop stop-color="#fff2a4"/><stop offset=".4" stop-color="#ffc14a"/><stop offset=".8" stop-color="#ed7a14"/><stop offset="1" stop-color="#9e320b"/></radialGradient></defs><circle cx="125" cy="95" r="82" fill="url(#g)" stroke="#6b2b13" stroke-width="4"/><ellipse cx="91" cy="49" rx="24" ry="12" transform="rotate(-35 91 49)" fill="#fff9d7" opacity=".8"/>${positions[n].map(([x,y])=>`<polygon points="${star(x,y)}" fill="#c72e1b"/>`).join('')}</svg>`;
  await fs.writeFile(path.join(svgDir,`DragonBall${n}.svg`),svg);
  await sharp(Buffer.from(svg)).png().toFile(`FrierenMod/images/cards/DragonBall${n}.png`);
}
await fs.writeFile('FrierenMod/images/enchantments/LoadedDice.svg','<svg xmlns="http://www.w3.org/2000/svg" width="64" height="64" viewBox="0 0 64 64"><rect x="8" y="8" width="48" height="48" rx="9" fill="#e8d9ac" stroke="#725832" stroke-width="4"/><g fill="#7f341e"><circle cx="20" cy="20" r="4"/><circle cx="44" cy="20" r="4"/><circle cx="20" cy="44" r="4"/><circle cx="44" cy="44" r="4"/><circle cx="32" cy="32" r="4"/></g></svg>');
await fs.writeFile(path.join(sources,'manifest.json'),JSON.stringify(manifest,null,2)+'\n');
console.log('完成作者事件图、仙豆运行资源、龟派气功及七颗龙珠图标。');
