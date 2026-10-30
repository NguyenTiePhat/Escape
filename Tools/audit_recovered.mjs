import fs from 'node:fs';
import path from 'node:path';
const root=path.resolve('RecoveredProjects/TheGhost_Original/ExportedProject/Assets');
const files=[];
function walk(dir){for(const e of fs.readdirSync(dir,{withFileTypes:true})){const p=path.join(dir,e.name);if(e.isDirectory())walk(p);else files.push(p);}}
walk(root);
const guids=new Map();
for(const file of files.filter(f=>f.endsWith('.meta'))){const m=fs.readFileSync(file,'utf8').match(/^guid: ([a-f0-9]{32})/m);if(m)guids.set(m[1],file.slice(0,-5));}
const scene=path.join(root,'Scenes','SchoolLevel.unity');
const seen=new Set(),pending=[scene],missing=new Map();
const readable=new Set(['.unity','.prefab','.mat','.asset','.controller','.overrideController','.anim','.renderTexture','.lighting','.physicMaterial']);
while(pending.length){const file=pending.pop();if(seen.has(file))continue;seen.add(file);if(!readable.has(path.extname(file)))continue;
 const content=fs.readFileSync(file,'utf8');
 for(const match of content.matchAll(/guid: ([a-f0-9]{32})/g)){const guid=match[1];if(/^0{16}/.test(guid))continue;const target=guids.get(guid);if(target&&fs.existsSync(target)){if(!seen.has(target))pending.push(target);}else {if(!missing.has(guid))missing.set(guid,[]);const list=missing.get(guid);if(list.length<3)list.push(path.relative(root,file));}}
}
const counts={};for(const file of seen){const ext=path.extname(file);counts[ext]=(counts[ext]||0)+1;}
const report={scene:'SchoolLevel',assetFiles:files.filter(f=>!f.endsWith('.meta')).length,dependencyFiles:seen.size,dependencyTypes:counts,unresolvedGUIDs:[...missing].map(([guid,referencedBy])=>({guid,referencedBy})),dependencies:[...seen].map(f=>path.relative(root,f)).sort()};
fs.writeFileSync('Preview/school-dependency-audit.json',JSON.stringify(report,null,2));
console.log(JSON.stringify({assetFiles:report.assetFiles,dependencyFiles:seen.size,dependencyTypes:counts,unresolvedGUIDs:report.unresolvedGUIDs},null,2));
