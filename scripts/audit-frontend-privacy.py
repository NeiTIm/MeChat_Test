"""Conservative repository scan: reports categories/paths, never matched values."""
import json
import re
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
paths = subprocess.check_output(['git', 'ls-files', '--cached', '--others', '--exclude-standard'], cwd=ROOT, text=True).splitlines()
findings = []
checked = 0
for name in paths:
    path = ROOT / name
    if not path.is_file():
        continue
    sensitive_path = any(p in ('/' + name.lower()) for p in ['/private-uploads/', '/dev-mail/', '/.auth/'])
    env_path = path.name.startswith('.env') and not path.name.endswith('.example')
    if sensitive_path or env_path or path.name in ('storageState.json', 'cookies.json', 'auth-state.json'):
        findings.append({'category': 'sensitive-path', 'path': name})
        continue
    if path.suffix not in ('.js', '.jsx', '.json', '.md', '.py', '.html', '.css', '.yml', '.yaml', '.txt', '.cs'):
        continue
    if path.stat().st_size > 2_000_000:
        continue
    text = path.read_text(encoding='utf-8-sig', errors='replace')
    checked += 1
    if re.search(r'eyJ[A-Za-z0-9_-]{15,}\.[A-Za-z0-9_-]{20,}\.[A-Za-z0-9_-]{20,}', text):
        findings.append({'category': 'jwt-shaped-value', 'path': name})
    if re.search(r'-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----', text):
        findings.append({'category': 'private-key', 'path': name})
    if name.startswith('frontend/src/') and re.search(r'console\.(?:log|debug|info|warn|error)\s*\(', text):
        findings.append({'category': 'production-console-review', 'path': name})
result = {'date': '2026-11-07', 'scope': 'tracked and nonignored text; value-free conservative scan, not exhaustive secret detection or private data certification', 'checkedTextFiles': checked, 'findings': findings,
          'privateWorkbookIgnored': subprocess.run(['git', 'check-ignore', '-q', 'docs/frontend/MeChat_Danh_sach_cai_thien_va_kiem_thu_v1.xlsx'], cwd=ROOT).returncode == 0}
(ROOT / 'docs/frontend/improvement-privacy-audit.json').write_text(json.dumps(result, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
print(json.dumps(result, ensure_ascii=False))
raise SystemExit(bool(findings))
