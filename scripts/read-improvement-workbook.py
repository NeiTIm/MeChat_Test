"""Read-only OOXML inventory. Never rewrites the Product Owner workbook."""
import json
import posixpath
import sys
import zipfile
from pathlib import Path
from xml.etree import ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'docs/frontend/MeChat_Danh_sach_cai_thien_va_kiem_thu_v1.xlsx'
OUT = ROOT / 'frontend/test-results/workbook-audit'
NS = {'m': 'http://schemas.openxmlformats.org/spreadsheetml/2006/main',
      'r': 'http://schemas.openxmlformats.org/officeDocument/2006/relationships',
      'd': 'http://schemas.openxmlformats.org/drawingml/2006/spreadsheetDrawing',
      'a': 'http://schemas.openxmlformats.org/drawingml/2006/main'}

def targets(z, part):
    name = posixpath.join(posixpath.dirname(part), '_rels', posixpath.basename(part) + '.rels')
    if name not in z.namelist():
        return {}
    return {rel.attrib['Id']: posixpath.normpath(posixpath.join(posixpath.dirname(part), rel.attrib['Target'])).lstrip('/')
            for rel in ET.fromstring(z.read(name))}

def analyze(extract_images=False):
    sheets = []
    OUT.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(SOURCE) as z:
        strings = []
        if 'xl/sharedStrings.xml' in z.namelist():
            strings = [''.join(x.itertext()) for x in ET.fromstring(z.read('xl/sharedStrings.xml'))]
        wb = ET.fromstring(z.read('xl/workbook.xml'))
        rels = targets(z, 'xl/workbook.xml')
        for sheet in wb.find('m:sheets', NS):
            part = rels[sheet.attrib['{' + NS['r'] + '}id']]
            xml = ET.fromstring(z.read(part))
            rows = []
            for row in xml.findall('m:sheetData/m:row', NS):
                cells = {}
                for c in row:
                    v = c.find('m:v', NS)
                    value = v.text if v is not None else ''
                    if c.attrib.get('t') == 's':
                        value = strings[int(value)]
                    elif c.attrib.get('t') == 'inlineStr':
                        value = ''.join(c.find('m:is', NS).itertext())
                    if value:
                        cells[c.attrib['r']] = value
                if cells:
                    rows.append(cells)
            images = []
            for drawing in xml.findall('m:drawing', NS):
                dp = targets(z, part)[drawing.attrib['{' + NS['r'] + '}id']]
                dr = targets(z, dp)
                for anchor in ET.fromstring(z.read(dp)):
                    origin = anchor.find('d:from', NS)
                    blip = anchor.find('.//a:blip', NS)
                    if blip is None:
                        continue
                    ip = dr[blip.attrib['{' + NS['r'] + '}embed']]
                    local = OUT / posixpath.basename(ip)
                    if extract_images:
                        local.write_bytes(z.read(ip))
                    images.append({'part': ip, 'path': str(local), 'row': int(origin.find('d:row', NS).text) + 1,
                                   'column': int(origin.find('d:col', NS).text) + 1})
            sheets.append({'name': sheet.attrib['name'], 'rows': rows, 'images': images,
                           'formulas': len(xml.findall('.//m:f', NS))})
    (OUT / 'source.json').write_text(json.dumps(sheets, ensure_ascii=False, indent=2), encoding='utf-8')
    return sheets

if __name__ == '__main__':
    data = analyze('--extract-images' in sys.argv)
    args = [arg for arg in sys.argv[1:] if not arg.startswith('--')]
    if args:
        print(json.dumps(next(s for s in data if s['name'] == args[0]), ensure_ascii=False, indent=2))
    else:
        print(json.dumps([{k: s[k] for k in ('name', 'images', 'formulas')} | {'rows': len(s['rows'])} for s in data], ensure_ascii=False, indent=2))
