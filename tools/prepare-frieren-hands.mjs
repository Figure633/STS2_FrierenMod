// AI 原图保留在 assets/hand_sources；只等比缩放和对齐，不绘制新细节。
import fs from 'node:fs/promises';
import crypto from 'node:crypto';
import {createRequire} from 'node:module';
const require=createRequire(import.meta.url), sharp=require(process.env.SHARP_MODULE||'sharp');
const source='assets/hand_sources/v2',target='FrierenMod/images/character';
await fs.mkdir(target,{recursive:true});
// 新版手型更小；指向的指尖仍落在原生点击位置，其他手势以袖口对齐。
const origins={point:-63,rock:-67,paper:-67,scissors:-67};
const ids={point:'exec-46c985cd-47a7-4d90-8b69-26508f8a225d.png',rock:'exec-f7c4773c-d70b-40d2-a2dd-f58a0c0bead5.png',paper:'exec-b2479866-071b-4b5d-a79a-1911c952553c.png',scissors:'exec-1d37007d-5dc8-4480-8dfc-bebdfbfc9ec9.png'};
const provenance=JSON.parse(await fs.readFile('docs/generated_assets.json','utf8'));
for(const [pose,dy] of Object.entries(origins)) {
    const resized=await sharp(`${source}/${pose}.png`).resize({height:1280}).png().toBuffer();
    const output=`${target}/hand_${pose}.png`;
    await sharp({create:{width:480,height:1360,channels:4,background:{r:0,g:0,b:0,alpha:0}}})
        .composite([{input:resized,left:20,top:90+dy}]).png().toBuffer()
        .then(buffer=>sharp(buffer).extract({left:30,top:90,width:422,height:1200}).png().toFile(output));
    const entry={key:`frieren_hand_${pose}`,date:'2026-11-12',prompt_file:`${source}/prompts.json`,prompt_key:`poses.${pose}`,
        referenced_image_paths:[pose==='point'?'assets/hand_sources/point.png':`${source}/point.png`,pose==='point'?'assets/hand_sources/references/frieren-official-back.png':`assets/hand_sources/${pose}.png`],
        transparent_background:true,source:`built-in image_gen ${ids[pose]}; original retained at ${source}/${pose}.png`,
        reference_url:'https://frieren-anime.jp/character/chara_group1/1-1/',
        processing:'tools/prepare-frieren-hands.mjs; uniform scale, wrist alignment, 422x1200 transparent canvas',
        target:output,sha256:crypto.createHash('sha256').update(await fs.readFile(output)).digest('hex')};
    const index=provenance.assets.findIndex(item=>item.key===entry.key);
    if(index<0)provenance.assets.push(entry); else provenance.assets[index]=entry;
    console.log(`${pose}: ${output} (422x1200)`);
}
await fs.writeFile('docs/generated_assets.json',JSON.stringify(provenance,null,2)+'\n');
