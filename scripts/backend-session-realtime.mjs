import assert from 'node:assert/strict'
import { createRequire } from 'node:module'
import { randomUUID,createHash } from 'node:crypto'
import { readFile,readdir,stat,writeFile,mkdir } from 'node:fs/promises'
import { resolve } from 'node:path'
import { execFileSync } from 'node:child_process'
const root=resolve(import.meta.dirname,'..'),base='http://localhost:5080'
const require=createRequire(resolve(root,'frontend/package.json'))
const {HubConnectionBuilder,LogLevel,HubConnectionState}=require('@microsoft/signalr')
const checks=[],hubs=[];let error=null
const sourceAuditHash=createHash('sha256').update(await readFile(resolve(root,'docs/source-audit.json'))).digest('hex')
const pause=ms=>new Promise(r=>setTimeout(r,ms))
async function until(check,name){for(let i=0;i<100;i++){if(await check()){checks.push({name,status:'PASS'});return}await pause(100)}throw new Error(name+' timeout')}
async function api(path,method='GET',body,actor,status=200){const response=await fetch(base+'/api'+path,{method,headers:{...(body?{'Content-Type':'application/json'}:{}),...(actor?{Authorization:`Bearer ${actor.token}`}:{})},body:body?JSON.stringify(body):undefined});assert.equal(response.status,status,method+' '+path);return response.headers.get('content-type')?.includes('json')?response.json():null}
async function actor(){const suffix=randomUUID().replaceAll('-','').slice(0,12);const value={email:`session-qa-${suffix}@example.invalid`,username:`sq${suffix}`,password:`Synthetic-${randomUUID()}!Aa1`};await api('/auth/register','POST',value,null,201);const dir=resolve(root,'backend/MeChat.Api/private-uploads/dev-mail');const names=await readdir(dir);const files=await Promise.all(names.filter(n=>n.endsWith('.json')).map(async name=>({name,time:(await stat(resolve(dir,name))).mtimeMs})));files.sort((a,b)=>b.time-a.time);let token;for(const file of files){const mail=JSON.parse(await readFile(resolve(dir,file.name),'utf8'));if(mail.email===value.email&&mail.purpose==='verify-email'){token=mail.token;break}}assert.ok(token);await api('/auth/verify-email','POST',{token},null,204);Object.assign(value,await login(value,'first'));return value}
async function login(value,device){const data=await api('/auth/login','POST',{identifier:value.email,password:value.password,deviceName:device});return {token:data.accessToken,id:data.userId}}
function hub(actor){const value=new HubConnectionBuilder().withUrl(base+'/hubs/chat',{accessTokenFactory:()=>actor.token}).configureLogging(LogLevel.Error).build();hubs.push(value);return value}
try{
 const a=await actor(),b=await actor();await api('/friends/requests','POST',{targetUserId:b.id},a,201);await api('/friends/requests','POST',{targetUserId:a.id},b)
 const conversation=await api('/conversations/private','POST',{targetUserId:b.id},a)
 const a2={...a,...await login(a,'second')};const a1hub=hub(a),a2hub=hub(a2),bhub=hub(b);const received1=[],received2=[]
 a1hub.on('MessageCreated',event=>{received1.push(event)});a2hub.on('MessageCreated',event=>{received2.push(event)})
 await Promise.all([a1hub.start(),a2hub.start(),bhub.start()]);await Promise.all([a1hub.invoke('JoinConversation',conversation.id),a2hub.invoke('JoinConversation',conversation.id),bhub.invoke('JoinConversation',conversation.id)])
 const send=(text,id=randomUUID())=>api(`/conversations/${conversation.id}/messages`,'POST',{clientMessageId:id,content:text},b)
 const first=await send('Two-device session fixture');await until(()=>received1.some(e=>e.messageId===first.id)&&received2.some(e=>e.messageId===first.id),'Both account devices receive authorized event')
 const sessions=await api('/auth/sessions','GET',null,a);const second=sessions.find(s=>s.deviceName==='second');assert.ok(second)
 await api('/auth/sessions/'+second.id,'DELETE',null,a,204);await until(()=>a2hub.state===HubConnectionState.Disconnected,'Explicit session revoke closes the old socket')
 await api('/users/me','GET',null,a2,401);await assert.rejects(()=>a2hub.start())
 const oldCount=received2.length;const secondMessage=await send('Only current device fixture');await until(()=>received1.some(e=>e.messageId===secondMessage.id),'Current socket survives another-device revoke');assert.equal(received2.length,oldCount)
 const a3={...a,...await login(a,'third')};const a3hub=hub(a3);await a3hub.start();const next=`Changed-${randomUUID()}!Aa1`
 await api('/auth/change-password','POST',{currentPassword:a.password,newPassword:next},a,204);a.password=next
 await until(()=>a3hub.state===HubConnectionState.Disconnected,'Password change closes other-device socket after commit');await api('/users/me','GET',null,a3,401);assert.equal(a1hub.state,HubConnectionState.Connected)
 await a1hub.stop();const offlineId=randomUUID();const offline=await send('Offline catchup fixture',offlineId);const duplicate=await send('Ambiguous retry',offlineId);assert.equal(duplicate.id,offline.id)
 await api(`/messages/${secondMessage.id}`,'PATCH',{content:'Edited while recipient offline'},b);await api(`/messages/${first.id}/recall`,'POST',null,b,204)
 await a1hub.start();await a1hub.invoke('JoinConversation',conversation.id);const history=await api(`/conversations/${conversation.id}/messages`,'GET',null,a)
 assert.equal(history.items.filter(m=>m.id===offline.id).length,1);assert.equal(history.items.find(m=>m.id===first.id).content,null);assert.equal(history.items.find(m=>m.id===secondMessage.id).content,'Edited while recipient offline')
 checks.push({name:'Reconnect REST recovers edit/Recall and canonical retry without duplicate',status:'PASS'})
 await a1hub.stop();await api('/blocks','POST',{targetUserId:a.id},b,204);await a1hub.start();await assert.rejects(()=>a1hub.invoke('JoinConversation',conversation.id));await api(`/conversations/${conversation.id}/messages`,'GET',null,a,404)
 checks.push({name:'Permission revoked while offline denies old subscription and REST recovery',status:'PASS'})
 const admin=await actor();assert.match(admin.id,/^[0-9a-f-]{36}$/i);assert.match(admin.email,/^session-qa-[0-9a-f]{12}@example\.invalid$/)
 const sql=`SET NOCOUNT ON;IF DB_NAME()<>N'MeChat' OR ISNULL(CONVERT(nvarchar(128),SERVERPROPERTY('InstanceName')),N'')<>N'MSSQLSERVER01' THROW 51000,'Wrong target',1;UPDATE dbo.Users SET SystemRole=N'Admin' WHERE Id='${admin.id}' AND Email=N'${admin.email}' AND SystemRole=N'User';IF @@ROWCOUNT<>1 THROW 51001,'Synthetic mismatch',1;`
 execFileSync('sqlcmd',['-S',String.raw`lpc:.\MSSQLSERVER01`,'-d','MeChat','-E','-C','-b','-Q',sql],{stdio:['ignore','pipe','pipe']});Object.assign(admin,await login(admin,'test-admin'))
 await api(`/admin/users/${a.id}/suspend`,'POST',{reason:'Synthetic session revocation acceptance'},admin,204);await until(()=>a1hub.state===HubConnectionState.Disconnected,'Admin suspension aborts current socket');await api('/users/me','GET',null,a,401);await api('/auth/login','POST',{identifier:a.email,password:a.password},null,401);await assert.rejects(()=>a1hub.start())
 checks.push({name:'Suspended session cannot reconnect or use normal REST/login',status:'PASS'})
}catch(failure){error=String(failure.message).slice(0,500)}finally{await Promise.all(hubs.map(h=>h.stop().catch(()=>{})));const dir=resolve(root,'backend/MeChat.Api/private-uploads');await mkdir(dir,{recursive:true});await writeFile(resolve(dir,'qa-session-realtime-2026-11-05.json'),JSON.stringify({date:new Date().toISOString(),sourceAuditHash,type:'REAL SQL + REST + SIGNALR',status:error?'FAIL':'PASS',checks,error},null,2))}
if(error){console.error('FAIL: '+error);process.exitCode=1}else console.log(`PASS: ${checks.length} real session/reconnect/permission checks`)
