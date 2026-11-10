// 用Artifact Tool更新指定单元格并预览；最终只合并这些单元格，保留作者原包其他内容。
import fs from 'node:fs/promises';
import path from 'node:path';
import {createRequire} from 'node:module';
import {pathToFileURL,fileURLToPath} from 'node:url';
import {spawnSync} from 'node:child_process';
import assert from 'node:assert/strict';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const planPath=path.resolve(process.argv[2]);
const plan=JSON.parse(await fs.readFile(planPath,'utf8'));
const temp=path.dirname(planPath);
const req=createRequire(path.join(temp,'runtime.cjs'));
const {SpreadsheetFile,FileBlob}=await import(pathToFileURL(req.resolve('@oai/artifact-tool')).href);
const book=await SpreadsheetFile.importXlsx(await FileBlob.load(path.join(root,plan.workbook)));
const sheet=book.worksheets.getItemAt(0);
for(const [row,source] of Object.entries(plan.templateRows??{}))sheet.getRange(`A${row}:G${row}`).copyFrom(sheet.getRange(`A${source}:G${source}`),'all');
for(const [cell,value] of Object.entries(plan.cells))sheet.getRange(cell).values=[[value]];
for(const [row,height] of Object.entries(plan.heights))sheet.getRange(`A${row}:K${row}`).format.rowHeight=height;
book.recalculate();
for(const [cell,value] of Object.entries(plan.cells))assert.deepEqual(sheet.getRange(cell).values,[[value]],cell);
console.log((await book.inspect({kind:'table',range:'卡名对照!A151:G152',tableMaxRows:2,tableMaxCols:7,maxChars:1200})).ndjson);
for(const [range,file] of [['A151:G152','new-rares'],['A174:G175','new-magic']]){
 const preview=await book.render({sheetName:sheet.name,range,scale:1,format:'png'});
 await fs.writeFile(path.join(temp,file+'.png'),new Uint8Array(await preview.arrayBuffer()));
}
await (await SpreadsheetFile.exportXlsx(book)).save(path.join(temp,'authored.xlsx'));
const result=spawnSync(process.env.ARTIFACT_PYTHON,[path.join(root,'tools/merge_workbook_cells.py'),planPath,path.join(temp,'authored.xlsx')],{cwd:root,encoding:'utf8',env:{...process.env,PYTHONIOENCODING:'utf-8'}});
process.stdout.write(result.stdout??'');process.stderr.write(result.stderr??'');
assert.equal(result.status,0,'原包合并失败');
