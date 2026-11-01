"""Architecture rules from docs/architecture/ADR-001-modular-monolith.md.

1. A module may use another module only through its public port (type named I*Api).
2. Layers inside a module: Presentation -> BusinessLogic -> DataAccess. BusinessLogic and DataAccess must not use Presentation (*Endpoints).
3. The SPA reads catalog data and limits from the API: no region names, no filter/sort/slice on tours or destinations.
Static source scan: needs no build, database or packages.
"""
import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
MODULES = ROOT / 'src' / 'Backend' / 'DongNamTrip.Modules'
DECL = re.compile(r'\b(?:class|interface|record|enum|struct)\s+(\w+)')
PORT = re.compile(r'^I\w+Api$')

files = {m.name: list(m.rglob('*.cs')) for m in MODULES.iterdir() if m.is_dir() and m.name not in ('Persistence', 'bin', 'obj')}
declared = {m: {n for f in fs for n in DECL.findall(f.read_text(encoding='utf-8-sig'))} for m, fs in files.items()}

shared = {n for f in (MODULES / 'Persistence').rglob('*.cs') for n in DECL.findall(f.read_text(encoding='utf-8-sig'))}
errors = []
for module, paths in files.items():
    foreign = {n: other for other, names in declared.items() if other != module
               for n in names if not PORT.match(n) and n not in declared[module] and n not in shared}
    for path in paths:
        text = path.read_text(encoding='utf-8-sig')
        rel = path.relative_to(ROOT)
        for name, other in foreign.items():
            if re.search(rf'\b{re.escape(name)}\b', text):
                errors.append(f'{rel}: uses {other}.{name}; only I*Api ports may cross modules')
        layer = path.relative_to(MODULES / module).parts[0]
        if layer in ('BusinessLogic', 'DataAccess') and re.search(r'\w+Endpoints', text):
            errors.append(f'{rel}: {layer} must not depend on Presentation')

SPA = ROOT / 'src' / 'Frontend' / 'DongNamTrip.Spa' / 'src'
LITERALS = ['TP. Hồ Chí Minh', 'Tây Ninh', 'Vũng Tàu', 'Đồng Nai', 'Bình Dương']
PROCESSING = re.compile(r'\b(tours|destinations)\.data\??\.(filter|sort|slice|find|reduce)\(')
for path in SPA.rglob('*.ts*'):
    text = path.read_text(encoding='utf-8')
    for literal in LITERALS:
        if literal in text:
            errors.append(f'{path.relative_to(ROOT)}: hardcoded region "{literal}"; read it from /api/meta')
    if PROCESSING.search(text):
        errors.append(f'{path.relative_to(ROOT)}: filter/sort/slice on tours/destinations must be done by the API')

if errors:
    print('\n'.join(errors))
    sys.exit(1)
print(f'Architecture rules OK ({len(files)} modules)')
