from pathlib import Path
import json,hashlib,xml.etree.ElementTree as ET
root=Path(__file__).resolve().parents[1]
def read(path):return json.loads((root/path).read_text(encoding='utf-8-sig'))
def digest(path):return hashlib.sha256((root/path).read_bytes()).hexdigest()
manifest=read('docs/backend-acceptance.json')
fresh=all(digest(path)==value for field in ['sourceSnapshot','testSnapshot'] for path,value in manifest[field].items())
audit_hash=digest('docs/source-audit.json')
ns={'t':'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
trx=ET.parse(root/'backend/MeChat.Tests/TestResults/backend-qa-final-2026-11-05.trx')
counters=trx.find('.//t:Counters',ns).attrib
passed=[row.attrib['testName'] for row in trx.findall('.//t:UnitTestResult',ns) if row.attrib.get('outcome')=='Passed']
regression=int(counters['failed'])==0 and int(counters['passed'])==int(counters['total']) and int(counters['total'])>=65
sources_newer=any((root/path).stat().st_mtime>(root/'backend/MeChat.Tests/TestResults/backend-qa-final-2026-11-05.trx').stat().st_mtime for path in manifest['testSnapshot'])
regression=regression and not sources_newer
try:
 auth=read('backend/MeChat.Api/private-uploads/qa-auth-summary-2026-11-05.json')
 auth_ok=auth['status']=='PASS' and auth['requests']==auth['plannedRequests'] and all(digest(path)==value for path,value in auth['sourceHashes'].items()) and auth.get('toolingLockHash')==digest('tools/backend-qa/package-lock.json')
except (OSError,KeyError):auth_ok=False
try:
 session=read('backend/MeChat.Api/private-uploads/qa-session-realtime-2026-11-05.json');status=read('backend/MeChat.Api/private-uploads/qa-status-realtime-2026-11-05.json')
 realtime_ok=all(proof['status']=='PASS' and proof['sourceAuditHash']==audit_hash for proof in [session,status])
except (OSError,KeyError):realtime_ok=False
tooling=read('backend/MeChat.Api/private-uploads/qa-tooling-audit-patched-2026-11-05.json')['metadata']['vulnerabilities']
rows=[];states={}
for key,row in manifest['requirements'].items():
 state=row['backendAssessment']
 if state=='Verified':
  proof=fresh and regression and all(any(suite in name for name in passed) for suite in row['requiredSuites'])
  if row['group']=='authentication':proof=proof and auth_ok
  if row['group']=='status':proof=proof and realtime_ok
  if not proof:state='Partial'
 states[key]=state
 evidence=row['evidence']+'; '+', '.join(row['requiredSuites'])
 remaining=row['remaining'] if state==row['backendAssessment'] else 'Current source/test proof missing or stale; rerun before accepting.'
 rows.append(f"| {row['id']} | {row['requirement']} | {state} | {evidence} | {remaining} |")
verified=sum(s=='Verified' for s in states.values());na=sum(s=='Not Applicable' for s in states.values())
state={'date':'2026-11-05','scope':'Backend only','status':'PARTIAL / BLOCKED','sourceAuditHash':audit_hash,'manifestHash':digest('docs/backend-acceptance.json'),'sourceFresh':fresh,'regressionPassed':regression,'xunit':counters,'authPostmanCurrent':auth_ok,'realtimeCurrent':realtime_ok,'verified':verified,'applicable':125-na,'partial':125-na-verified,'notApplicable':na,'toolingFindings':tooling,'requirements':states}
(root/'docs/backend-acceptance-state.json').write_text(json.dumps(state,indent=2)+'\n',encoding='utf-8')
text=f"""# Backend acceptance inventory - 2026-11-05

Status: **PARTIAL / BLOCKED**. Backend scope only, separate from Feature Complete, frontend acceptance and Project Completed.

All125 requirements individually reviewed for Backend applicability using the manual mapping in backend-acceptance.json. Current named Backend gates: **{verified}/{125-na} Verified**, {125-na-verified} Partial, {na} UI-only Not Applicable (Q001/Q014). Partial means code exists and some cases pass, but the whole Backend rule is not accepted. It does not mean a missing feature.

Current source/test snapshot: {fresh}; full SQL-backed regression current: {regression} ({counters['passed']}/{counters['total']} passed); Auth Postman source+tooling-lock proof current: {auth_ok}; real session/status SignalR proof current: {realtime_ok}. Gate status is not inferred from file presence or test counts: explicit manually selected requirements also require named passing suites and a matching source/test snapshot. See backend-acceptance.json for the individual Backend source conclusions copied from the source audit.

## Global open gates

- QA toolchain: after compatible leaf patches, npm audit still reports{tooling['total']} affected dependency entries ({tooling['high']} high/{tooling['moderate']} moderate/{tooling['critical']} critical). Newman6.2.3 is current per registry; latest Postman collection5.3.1 still pins Faker5.5.3. Safe audit fix does not resolve these; no force downgrade or unsupported major override was used. This is a tooling finding, not an ASP.NET runtime CVE. Trusted repository collections and synthetic local targets only; no clean security signoff.
- Complete remaining positive/negative/authorization/conflict/429 and boundary cases listed per row. 113 protected controller routes actually reject anonymous requests; that does not prove all business permissions.
- Broader transport/flood/load and role/invite races; current bounded races are passing, not a throughput certification.
- Retention SQL cutoff/hold tests and fake-storage crash/retry pass. Isolated SQL tombstone/binding and evidence/audit-batch crash/hold harnesses remain. Never enable purge/delete against current data to satisfy this gate.

## Evidence

- Full xUnit: backend/MeChat.Tests/TestResults/backend-qa-final-2026-11-05.trx; earlier failure/retry TRX files retained for Auth/profile/retention defects.
- Actual Auth collection43requests/52assertions: ignored private-uploads/qa-auth-summary-2026-11-05.json, sanitized with source hashes and QA package-lock hash.
- Actual Newman broad acceptance58requests/67assertions: private-uploads/newman-backend-qa-2026-11-05.json (contains credentials; do not publish raw).
- Real session SignalR8checks: private-uploads/qa-session-realtime-2026-11-05.json; status/multi-device/ACK/Seen/unread proof: qa-status-realtime-2026-11-05.json. Unauthorized401 reconnect messages are expected negative tests.
- Actual anonymous controller inventory: private-uploads/qa-authenticated-route-inventory-2026-11-05.txt (113 routes).
- Storage failure tests use fake storage only. SQL tests add new synthetic rows; no migration/reset/purge or existing-file deletion.

## Q001-Q125 Backend gates

| ID | Requirement | Backend gate | Executed evidence / scope | Remaining |
|---|---|---|---|---|
"""+'\n'.join(rows)+"\n\nGenerated by scripts/generate-backend-acceptance.py from manual requirement mapping and actual local evidence. Build/tests alone never imply Backend Acceptance Complete.\n"
(root/'docs/backend-acceptance.md').write_text(text,encoding='utf-8')
print(f'Backend gates: {verified}/{125-na} Verified, {125-na-verified} Partial, {na} N/A; sourceFresh={fresh}; regression={regression}; auth={auth_ok}; realtime={realtime_ok}')
