import fs from 'node:fs/promises';
import crypto from 'node:crypto';
import {createRequire} from 'node:module';
const sharp=createRequire(import.meta.url)(process.env.SHARP_MODULE||'sharp');
const prepared='assets/prepared/relics-nov12',runtime='FrierenMod/images/relics';
const sources=JSON.parse(await fs.readFile('assets/author_adjusted/20261112/relics/sources.json','utf8')).images;
const generated=JSON.parse(await fs.readFile('docs/generated_assets.json','utf8'));
const authored=JSON.parse(await fs.readFile('docs/assets.json','utf8'));
const ids={SacredSnowCrystal:'exec-ea04e2a1-1e9c-4989-a102-6026f871300d.png',Skycleaver:'exec-f3c5ef90-450a-4a4a-a7f2-d8056ab07c3a.png',RottingSageSkull:'exec-8781d2cc-2531-4b37-9a60-e498967361b4.png',ManaSource:'exec-2cd9f55d-50b8-4b5e-9dbf-42368a779b49.png'};
const hash=data=>crypto.createHash('sha256').update(data).digest('hex');
function put(items,key,value){const n=items.findIndex(x=>x[key]===value[key]);if(n<0)items.push(value);else items[n]=value;}
async function outline(image){const alpha=await sharp(image).extractChannel('alpha').dilate(2).raw().toBuffer();const rgba=Buffer.alloc(128*128*4,255);for(let i=0;i<alpha.length;i++)rgba[i*4+3]=alpha[i];return sharp(rgba,{raw:{width:128,height:128,channels:4}}).png().toBuffer();}
for(const name of ['SacredSnowCrystal','Skycleaver','WeatheredHelmet','RottingSageSkull','ManaSource']) {
    let input;
    if(name==='WeatheredHelmet') {
        const entry=sources.find(r=>r.rid==='rId11'),m=await sharp(entry.sourceFile).metadata(),c=entry.crop;
        const left=Math.round(m.width*(+c.l||0)/100000),top=Math.round(m.height*(+c.t||0)/100000);
        const width=m.width-left-Math.round(m.width*(+c.r||0)/100000),height=m.height-top-Math.round(m.height*(+c.b||0)/100000);
        input=await sharp(entry.sourceFile).extract({left,top,width,height}).png().toBuffer();
    } else input=await fs.readFile(`${prepared}/${name}.png`);
    const png=await sharp(input).trim().resize(112,112,{fit:'contain',background:{r:0,g:0,b:0,alpha:0}}).extend({top:8,bottom:8,left:8,right:8,background:{r:0,g:0,b:0,alpha:0}}).png().toBuffer();
    if(name==='ManaSource') {
        const target='FrierenMod/images/enchantments/ManaSource.png';await fs.writeFile(target,png);
        put(generated.assets,'key',{key:name,date:'2026-11-12',target,sha256:hash(png),tool:'built-in image_gen',source:`${ids[name]}; retained at ${prepared}/${name}.png`,prompt_file:`${prepared}/prompts.json`,prompt_key:`prompts.${name}`,transparent_background:true,processing:'tools/prepare-nov12-relic-assets.mjs; original Word crop, alpha cutout, uniform fit 128x128'});
        continue;
    }
    for(const [suffix,data] of [['',png],['Outline',await outline(png)]]) {
        const target=`${runtime}/${name}${suffix}.png`;await fs.writeFile(target,data);
        await fs.writeFile(`${runtime}/${name}${suffix}.tres`,`[gd_resource type="AtlasTexture" load_steps=2 format=3]\n\n[ext_resource type="Texture2D" path="res://${target}" id="1"]\n\n[resource]\natlas = ExtResource("1")\nregion = Rect2(0, 0, 128, 128)\nfilter_clip = true\n`);
        if(name==='WeatheredHelmet')put(authored,'file',{file:target,source:'assets/author_adjusted/20261112/relics/WeatheredHelmet.png',source_kind:'author_provided',modified:true,processing:'tools/prepare-nov12-relic-assets.mjs; preserve author Word crop and source alpha, uniform scale; native white outline',sha256:hash(data)});
        else put(generated.assets,'key',{key:name+suffix,date:'2026-11-12',target,sha256:hash(data),tool:'built-in image_gen',source:`${ids[name]}; retained at ${prepared}/${name}.png`,prompt_file:`${prepared}/prompts.json`,prompt_key:`prompts.${name}`,transparent_background:true,processing:'tools/prepare-nov12-relic-assets.mjs; alpha cutout, uniform fit 128x128; outline from alpha'});
    }
}
await fs.writeFile('docs/assets.json',JSON.stringify(authored,null,2)+'\n');
await fs.writeFile('docs/generated_assets.json',JSON.stringify(generated,null,2)+'\n');
console.log('NOV12_RELIC_ASSETS_OK');
