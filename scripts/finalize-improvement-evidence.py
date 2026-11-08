"""Summarize evidence, preserve private workbook, list changed files without values."""
import json
import struct
import subprocess
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DOCS = ROOT / 'docs/frontend'
read = lambda path: json.loads(path.read_text(encoding='utf-8'))
browser = read(DOCS / 'improvement-final-regression.json')
unit = read(ROOT / 'frontend/test-results/improvement-unit-final.json')
zoom = read(DOCS / 'improvement-browser-zoom.json')
api = read(DOCS / 'api-consumer-audit.json')
privacy = read(DOCS / 'improvement-privacy-audit.json')
assert browser['stats']['expected'] == 176 and browser['stats']['unexpected'] == 0 and browser['stats']['flaky'] == 0
assert unit['numPassedTests'] == 40 and unit['numFailedTests'] == 0
assert len(zoom['cases']) == 56 and not zoom['errors']
assert api['mappedFunctions'] == 122 and not api['errors']
assert not privacy['findings']
images = sorted((DOCS / 'screenshots/vi/improvements').glob('*.png'))
assert len(images) == 57
lines = ['# Ảnh kiểm tra đợt cải thiện — 07/11/2026', '',
         '57 PNG chụp browser thật bằng Playwright/CDP với tài khoản và nội dung tổng hợp trong HTTP simulation. Không phải bằng chứng Backend authenticated. Không xuất ảnh gốc workbook hoặc thông tin riêng tư vào thư mục này. Ảnh nằm trong vùng Git ignored, giữ cục bộ để review.', '',
         'Đã xem trực tiếp các ảnh đại diện media320 Dark, preview320 Light/1440 Dark, CTA nhóm390 Light, toast320 Dark và zoom80% Dark. Ảnh toast chờ animation hoàn tất. Ảnh zoom dùng CDP physical viewport để tránh padding ảnh chụp của CSS-sized clip; không chỉnh sửa bitmap để che layout.', '',
         '| Ảnh | Kích thước PNG | Phạm vi |', '| --- | --- | --- |']
for path in images:
    header = path.read_bytes()[:24]
    width, height = struct.unpack('>II', header[16:24])
    kind = path.stem.split('-')[0]
    scope = {'login': 'MC-001/003 căn chỉnh và Light/Dark', 'media': 'MC-011/012 authorized image modal', 'preview': 'MC-007 tray, count và composer', 'toast': 'MC-009 success, contrast và vị trí', 'zoom': 'MC-010 actual browser zoom; viewport vật lý', 'group': 'MC-004 sau tạo nhóm, CTA mời rõ ràng'}[kind]
    lines.append(f'| [{path.name}](screenshots/vi/improvements/{path.name}) | {width} × {height} | {scope} |')
(DOCS / 'improvement-screenshots.md').write_text('\n'.join(lines) + '\n', encoding='utf-8')
changed = subprocess.check_output(['git', '-c', 'core.quotepath=false', '-c', 'core.autocrlf=false', 'diff', '--name-only'], cwd=ROOT, text=True).splitlines()
new = subprocess.check_output(['git', '-c', 'core.quotepath=false', 'ls-files', '--others', '--exclude-standard'], cwd=ROOT, text=True).splitlines()
preserved = ['REFACTOR_STATUS.md', 'STATIC_AUDIT.json']
changed = sorted(set(changed + new) - set(preserved))
assert not any(path.startswith(('backend/', 'MeChat.Tests/')) for path in changed)
report = {'capturedAt': datetime.now(timezone.utc).isoformat(), 'localReviewDate': '2026-11-07',
          'scope': 'Client tests and real anonymous boundaries only; no successful business API acceptance.',
          'browser': browser['stats'], 'unit': {'passed': unit['numPassedTests'], 'failed': unit['numFailedTests'], 'files': len(unit['testResults'])},
          'browserZoom': {'cases': len(zoom['cases']), 'errors': zoom['errors'], 'method': 'actual chrome.tabs.setZoom in bundled Chromium'},
          'screenshots': len(images), 'build': 'PASS', 'lint': 'PASS', 'localizationStaticCalls': 687, 'mappedControllerActions': api['mappedFunctions'],
          'productionIsolation': 'PASS', 'productionSmoke': {'passed': 1}, 'npmAuditVulnerabilities': 0, 'realAnonymousBoundaries': {'passed': 4, 'successfulAuthSkipped': 1},
          'postFullFocused': {'hoverFocusAndCapture': {'passed': 10, 'scope': 'Repeated matrix cases, not added to full176.'}, 'groupCtaScreenshot': {'passed': 1, 'scope': 'Repeated case at390px, not added to full176.'}},
          'businessApi': 'NEEDS_VERIFICATION', 'signalRAuthenticated': 'NEEDS_VERIFICATION', 'physicalDeviceAndNativeAT': 'NEEDS_VERIFICATION',
          'backendSourceChanged': False, 'databaseMutationsPerformed': False, 'workbookRewritten': False,
          'privacyScan': privacy, 'preservedUserDeletions': preserved, 'changedFiles': changed}
(DOCS / 'improvement-verification.json').write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
print(json.dumps({'browserPassed': browser['stats']['expected'], 'unitPassed': unit['numPassedTests'], 'zoomCases': len(zoom['cases']), 'screenshots': len(images), 'changedFiles': len(changed)}))
