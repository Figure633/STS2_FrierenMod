// AI 原图保留在 assets/hand_sources；只等比缩放和对齐，不绘制新细节。
import fs from 'node:fs/promises';
import crypto from 'node:crypto';
import {createRequire} from 'node:module';
const require=createRequire(import.meta.url), sharp=require(process.env.SHARP_MODULE||'sharp');
const source='assets/hand_sources',target='FrierenMod/images/character';
await fs.mkdir(target,{recursive:true});
const origins={point:0,rock:4,paper:4,scissors:-8};
const ids={point:'exec-e3125018-128a-4c1d-beda-8b2d5c6719cc.png',rock:'exec-ffacb8f3-3e70-4497-be2c-1c51f47553c5.png',paper:'exec-8c133565-182e-4aaa-987f-2b37c69a73c8.png',scissors:'exec-75bb6790-3816-4920-899f-72876abcc9c1.png'};
const provenance=JSON.parse(await fs.readFile('docs/generated_assets.json','utf8'));
for(const [pose,dy] of Object.entries(origins)) {
    const resized=await sharp(`${source}/${pose}.png`).resize({height:1210}).png().toBuffer();
    const output=`${target}/hand_${pose}.png`;
    await sharp({create:{width:440,height:1240,channels:4,background:{r:0,g:0,b:0,alpha:0}}})
        .composite([{input:resized,left:8,top:16+dy}]).png().toBuffer()
        .then(buffer=>sharp(buffer).extract({left:4,top:16,width:422,height:1200}).png().toFile(output));
    const entry={key:`frieren_hand_${pose}`,date:'2026-11-12',prompt_file:`${source}/prompts.json`,prompt_key:`poses.${pose}`,
        referenced_image_paths:[pose==='point'?`${source}/references/frieren-official-back.png`:`${source}/point.png`, `external: native STS2 Ironclad ${pose} hand pose (reference only)`],
        transparent_background:true,source:`built-in image_gen ${ids[pose]}; original retained at ${source}/${pose}.png`,
        reference_url:'https://frieren-anime.jp/character/chara_group1/1-1/',
        processing:'tools/prepare-frieren-hands.mjs; uniform scale, wrist alignment, 422x1200 transparent canvas',
        target:output,sha256:crypto.createHash('sha256').update(await fs.readFile(output)).digest('hex')};
    const index=provenance.assets.findIndex(item=>item.key===entry.key);
    if(index<0)provenance.assets.push(entry); else provenance.assets[index]=entry;
    console.log(`${pose}: ${output} (422x1200)`);
}
await fs.writeFile('docs/generated_assets.json',JSON.stringify(provenance,null,2)+'\n');
