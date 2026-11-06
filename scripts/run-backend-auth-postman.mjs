import { createRequire } from 'node:module'
import { randomUUID,createHash } from 'node:crypto'
import { readFile, readdir, stat, mkdir, writeFile } from 'node:fs/promises'
import { resolve } from 'node:path'
import { execFileSync } from 'node:child_process'
const root = resolve(import.meta.dirname, '..')
const require = createRequire(resolve(root,'tools/backend-qa/package.json'))
const newman = require('newman')
const target = execFileSync('sqlcmd',['-S',String.raw`lpc:.\MSSQLSERVER01`,'-d','MeChat','-E','-C','-b','-h','-1','-W','-Q',"SET NOCOUNT ON; SELECT CONVERT(nvarchar(128),SERVERPROPERTY('InstanceName'));SELECT DB_NAME();SELECT CONVERT(nvarchar(20),DATABASEPROPERTYEX(DB_NAME(),'Status'));"],{encoding:'utf8'}).trim().split(/\r?\n/).map(s=>s.trim()).filter(Boolean)
if (target.join('|')!=='MSSQLSERVER01|MeChat|ONLINE') throw new Error('Unexpected SQL target/state')
const suffix=randomUUID().replaceAll('-','').slice(0,12)
const vars={baseUrl:'http://localhost:5080',email:`qa-auth-${suffix}@example.invalid`,username:`qa${suffix}`,password:`Synthetic-${randomUUID()}!Aa1`,nextPassword:`Changed-${randomUUID()}!Aa1`,resetPassword:`Reset-${randomUUID()}!Aa1`}
const collection=JSON.parse(await readFile(resolve(root,'docs/postman/MeChat.Auth.postman_collection.json'),'utf8'))
const mailDir=resolve(root,'backend/MeChat.Api/private-uploads/dev-mail')
async function pickup(purpose) {
 const names=await readdir(mailDir)
 const files=await Promise.all(names.filter(n=>n.endsWith('.json')).map(async name=>({name,time:(await stat(resolve(mailDir,name))).mtimeMs})))
 files.sort((a,b)=>b.time-a.time)
 for(const file of files){ const mail=JSON.parse(await readFile(resolve(mailDir,file.name),'utf8'));if(mail.email===vars.email&&mail.purpose===purpose)return mail.token }
 throw new Error('Synthetic development mail not found')
}
const sourceHashes={}
for(const path of ['backend/MeChat.Api/Features/Auth/AuthService.cs','backend/MeChat.Api/Features/Auth/AuthController.cs','backend/MeChat.Api/Program.cs','backend/MeChat.Api/Data/DatabaseTarget.cs'])sourceHashes[path]=createHash('sha256').update(await readFile(resolve(root,path))).digest('hex')
const toolingLockHash=createHash('sha256').update(await readFile(resolve(root,'tools/backend-qa/package-lock.json'))).digest('hex')
const results=[];let assertions=0
for(const item of collection.item){
 await new Promise(resolve=>setTimeout(resolve,2400))
 if(item.name==='Verify email')vars.verificationToken=await pickup('verify-email')
 if(item.name==='Reset password')vars.resetToken=await pickup('reset-password')
 const summary=await new Promise((resolve,reject)=>newman.run({collection:{info:collection.info,item:[item]},environment:{name:'Synthetic private auth',values:Object.entries(vars).map(([key,value])=>({key,value,enabled:true,type:'secret'}))},reporters:[],timeoutRequest:15000},(error,summary)=>error?reject(error):resolve(summary)))
 const failed=summary.run.failures.length
 assertions+=summary.run.stats.assertions.total
 for(const value of summary.environment.values.all())if(value.enabled!==false)vars[value.key]=value.value
 results.push({name:item.name,status:failed?'FAIL':'PASS',httpStatus:summary.run.executions[0]?.response?.code,assertions:summary.run.stats.assertions.total})
 console.log(`${failed?'FAIL':'PASS'}: ${item.name}`)
 if(failed)break
}
const out=resolve(root,'backend/MeChat.Api/private-uploads/qa-auth-summary-2026-11-05.json')
await mkdir(resolve(root,'backend/MeChat.Api/private-uploads'),{recursive:true})
const passed=results.length===collection.item.length&&results.every(r=>r.status==='PASS')
await writeFile(out,JSON.stringify({date:new Date().toISOString(),type:'REAL NEWMAN + API + SQL',sourceHashes,toolingLockHash,status:passed?'PASS':'FAIL',requests:results.length,assertions,plannedRequests:collection.item.length,results},null,2))
console.log(`${passed?'PASS':'FAIL'}: ${results.length}/${collection.item.length} requests, ${assertions} assertions. Summary contains no credentials.`)
if(!passed)process.exitCode=1
