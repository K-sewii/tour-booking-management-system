"""Architecture rules from docs/architecture/ADR-001-modular-monolith.md.

1. A module may use another module only through its public port (type named I*Api).
2. Layers inside a module: Presentation -> BusinessLogic -> DataAccess. BusinessLogic and DataAccess must not use Presentation (*Endpoints).
3. The SPA reads catalog data and limits from the API: no region names, no filter/sort/slice on tours or destinations.
4. Presentation (*Endpoints) never touches data: no DbContext, no EF Core, no DataAccess types of its module.
5. DataAccess never depends on BusinessLogic or Presentation types of its module.
6. BusinessLogic does not use DbContext/EF Core directly; it goes through a Repository in DataAccess
   (known debt: LEGACY_DB_IN_BUSINESS_LOGIC, shrink the list, never grow it).
7. Endpoints are registered through MapGroup, not endpoints.MapGet/MapPost/... on the root builder.
8. Contract strings (item kinds, activity scopes, payment methods) and rate-limit names come from constants
   (DongNamTrip.Contracts Vocabulary.cs, RateLimits.cs), not string literals.
9. Every module folder has a <Name>Module.cs that is registered in Api/Program.cs.
10. SPA: no localStorage, no fetch() outside api/client.ts (see src/Frontend/CLAUDE.md).
See src/Backend/CLAUDE.md for the reasoning and how to add a module/endpoint.
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
layer_of = lambda module, path: path.relative_to(MODULES / module).parts[0]
layer_names = {m: {layer: {n for f in fs if layer_of(m, f) == layer for n in DECL.findall(f.read_text(encoding='utf-8-sig'))}
                   for layer in ('Presentation', 'BusinessLogic', 'DataAccess')} for m, fs in files.items()}

# Legacy exceptions, relative to the Modules folder. Remove entries when the file is refactored to use a Repository.
LEGACY_DB_IN_BUSINESS_LOGIC = {'Administration/BusinessLogic/SqlAuthentication.cs'}
CONTRACT_LITERALS = re.compile(r'"(?:tour|destination|search|viewed|bank_transfer|cash)"')
ROOT_MAP = re.compile(r'\bendpoints\.Map(?:Get|Post|Put|Delete|Patch)\(')
RATE_LIMIT_LITERAL = re.compile(r'RequireRateLimiting\("')
EF_USE = re.compile(r'\bDongNamTripDbContext\b|using\s+Microsoft\.EntityFrameworkCore\b')

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
        layer = layer_of(module, path)
        if layer in ('BusinessLogic', 'DataAccess') and re.search(r'\w+Endpoints', text):
            errors.append(f'{rel}: {layer} must not depend on Presentation')

        def uses(names):
            return sorted(n for n in names if re.search(rf'\b{re.escape(n)}\b', text))

        if layer == 'Presentation':
            if EF_USE.search(text):
                errors.append(f'{rel}: Presentation must not use DbContext/EF Core; call a BusinessLogic service')
            for name in uses(layer_names[module]['DataAccess']):
                errors.append(f'{rel}: Presentation must not use DataAccess type {name}; call a BusinessLogic service')
            if path.name.endswith('Endpoints.cs') and ROOT_MAP.search(text):
                errors.append(f'{rel}: register routes on a group (endpoints.MapGroup("/api/...")), not on the root builder')
        if layer == 'DataAccess':
            for name in uses(layer_names[module]['BusinessLogic'] | layer_names[module]['Presentation']):
                errors.append(f'{rel}: DataAccess must not depend on {name} (BusinessLogic/Presentation)')
        if layer == 'BusinessLogic' and EF_USE.search(text) and path.relative_to(MODULES).as_posix() not in LEGACY_DB_IN_BUSINESS_LOGIC:
            errors.append(f'{rel}: BusinessLogic must not use DbContext/EF Core; add a method to the module Repository (DataAccess)')
        if CONTRACT_LITERALS.search(text):
            errors.append(f'{rel}: use constants from DongNamTrip.Contracts (ItemKinds, ActivityKinds, ActivityScopes, PaymentMethods), not string literals')
        if RATE_LIMIT_LITERAL.search(text):
            errors.append(f'{rel}: use RateLimits.* constants in RequireRateLimiting')

program = (ROOT / 'src' / 'Backend' / 'DongNamTrip.Api' / 'Program.cs').read_text(encoding='utf-8-sig')
for module in files:
    if not (MODULES / module / f'{module}Module.cs').exists():
        errors.append(f'src/Backend/DongNamTrip.Modules/{module}: missing {module}Module.cs (IModule)')
    elif f'new {module}Module()' not in program:
        errors.append(f'Api/Program.cs: module {module} is not registered in the modules array')
for legacy in LEGACY_DB_IN_BUSINESS_LOGIC:
    if not (MODULES / legacy).exists():
        errors.append(f'rules.py: LEGACY_DB_IN_BUSINESS_LOGIC lists missing file {legacy}; remove the entry')

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
    if 'localStorage' in text:
        errors.append(f'{path.relative_to(ROOT)}: do not use localStorage; session is the HttpOnly cookie, drafts may use sessionStorage')
    if re.search(r'\bfetch\(', text) and path.name != 'client.ts':
        errors.append(f'{path.relative_to(ROOT)}: call the API through api/client.ts (CSRF, errors, session expiry), not fetch()')

if errors:
    print('\n'.join(errors))
    sys.exit(1)
print(f'Architecture rules OK ({len(files)} modules)')
