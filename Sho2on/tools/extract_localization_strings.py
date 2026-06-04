import pathlib
import re
import json
root = pathlib.Path(__file__).resolve().parent.parent
pattern = re.compile(r'[\u0600-\u06FF]|[\u0750-\u077F]|[\u08A0-\u08FF]')
strings = {}
for path in root.rglob('*.xaml'):
    try:
        text = path.read_text(encoding='utf-8')
    except UnicodeDecodeError:
        text = path.read_text(encoding='cp1256', errors='ignore')
    for match in re.findall(r'"([^"]*?)"', text):
        if pattern.search(match) and match.strip():
            strings.setdefault(str(path.relative_to(root)), []).append(match)
for path in root.rglob('*.cs'):
    try:
        text = path.read_text(encoding='utf-8')
    except UnicodeDecodeError:
        text = path.read_text(encoding='cp1256', errors='ignore')
    for match in re.findall(r'"([^"]*?)"', text):
        if pattern.search(match) and match.strip():
            strings.setdefault(str(path.relative_to(root)), []).append(match)
with open(root / 'localization_strings.json', 'w', encoding='utf-8') as f:
    json.dump(strings, f, ensure_ascii=False, indent=2)
print('written', len(strings), 'files')
