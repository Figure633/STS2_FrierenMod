// 作者11月9日委托素材：原画直接复制；赌徒金币仅使用生成蒙版，RGB来自作者截图。
import fs from 'node:fs/promises';
import path from 'node:path';
import {createRequire} from 'node:module';
const require=createRequire(import.meta.url),sharp=require(process.env.SHARP_MODULE||'sharp');
const source=path.resolve('../png');
await fs.mkdir('FrierenMod/images/commissions',{recursive:true});
for(const [target,file] of Object.entries({Hunt1:'强化精英1',Hunt2:'强化精英2',Hunt3:'强化精英3',Cargo:'委托图2',Tutor:'招募资深魔法使家教',Exhibition:'压轴节目委托',Library:'传说魔法专栏',Lesson:'魔法学校的示范课',Variety:'魔术师委托',Granny:'顽固婆婆的委托',Namusan:'委托南无三',Stark:'修塔尔克的请求',Guide:'冒险者小队委托'}))
    await fs.copyFile(path.join(source,file+'.png'),`FrierenMod/images/commissions/${target}.png`);
for(const [id,file] of [['MagicExamVial','魔法考试小瓶'],['MagicPot','魔法陶罐']])await fs.copyFile(path.join(source,file+'.png'),`FrierenMod/images/cards/${id}.png`);
await fs.copyFile(path.join(source,'魔具店会员卡.png'),'FrierenMod/images/relics/MagicShopMembership.png');
const maskFile=process.argv[2];
if(!maskFile)throw Error('请传入赌徒金币的透明抠图蒙版PNG路径');
const original=await sharp(path.join(source,'赌徒金币（遗物，要抠图）.png')).ensureAlpha().raw().toBuffer();
const mask=await sharp(maskFile).resize(500,380).ensureAlpha().extractChannel('alpha').raw().toBuffer();
for(let p=0;p<mask.length;p++)original[p*4+3]=mask[p];
await sharp(original,{raw:{width:500,height:380,channels:4}}).png().toFile('FrierenMod/images/relics/GamblerCoin.png');
for(const id of ['MagicShopMembership','GamblerCoin']){
    const file=`FrierenMod/images/relics/${id}.png`;
    const {data,info}=await sharp(file).ensureAlpha().raw().toBuffer({resolveWithObject:true});
    let left=info.width,top=info.height,right=-1,bottom=-1;
    for(let y=0;y<info.height;y++)for(let x=0;x<info.width;x++){let p=(y*info.width+x)*4;if(data[p+3]>8){left=Math.min(left,x);top=Math.min(top,y);right=Math.max(right,x);bottom=Math.max(bottom,y);}data[p]=data[p+1]=data[p+2]=255;}
    await sharp(data,{raw:{width:info.width,height:info.height,channels:4}}).png().toFile(`FrierenMod/images/relics/${id}Outline.png`);
    for(const suffix of ['','Outline'])await fs.writeFile(`FrierenMod/images/relics/${id}${suffix}.tres`,`[gd_resource type="AtlasTexture" load_steps=2 format=3]\n\n[ext_resource type="Texture2D" path="res://FrierenMod/images/relics/${id}${suffix}.png" id="1"]\n\n[resource]\natlas = ExtResource("1")\nregion = Rect2(${left}, ${top}, ${right-left+1}, ${bottom-top+1})\nfilter_clip = true\n`);
}
console.log('AUTHOR_REVIEW_ART_OK: 13张独立委托图、2张魔具图、2张遗物；赌徒金币原RGB保留');
