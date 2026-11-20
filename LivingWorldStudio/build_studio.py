#!/usr/bin/env python3
"""Offline-only studio compiler. References are read-only; no profile installation."""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parent
SOURCES = ('StudioPlugin.cs', 'StudioRoom.cs', 'StudioRenderScope.cs', 'StudioJournal.cs', 'StudioEnvironment.cs')

def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ('managed', 'bepinex', 'jotunn', 'compiler', 'dotnet', 'framework'):
        parser.add_argument('--' + name, type=Path, required=True)
    parser.add_argument('--output', type=Path, default=ROOT / 'build')
    args = parser.parse_args()
    args.output.mkdir(parents=True, exist_ok=True)
    report = args.output / 'build-verification.json'
    report.write_text(json.dumps({'status': 'INCOMPLETE', 'installed': False, 'live_tested': False}, indent=2))
    sources = [ROOT / 'src' / name for name in SOURCES]
    unexpected = set((ROOT / 'src').glob('*.cs')) - set(sources)
    if unexpected:
        raise SystemExit('Source outside explicit allowlist: ' + ', '.join(map(str, sorted(unexpected))))
    for path in sources:
        if not path.is_file():
            raise SystemExit('Missing source: ' + str(path))
    # Target .NET Framework references, never runtime implementation facades.
    refs = [args.framework / name for name in ('mscorlib.dll', 'System.dll', 'System.Core.dll', 'System.Xml.dll')]
    facade = args.managed / 'netstandard.dll'
    if facade.is_file():
        refs.append(facade)
    refs += [args.bepinex / 'BepInEx.dll', args.bepinex / '0Harmony.dll', args.jotunn]
    # Actual game dependencies are necessary for signature type resolution. Do not
    # include Unity's copied framework assemblies alongside framework references.
    framework_names = {p.name.lower() for p in args.framework.glob('*.dll')}
    refs += [p for p in sorted(args.managed.glob('*.dll'))
             if p.name.lower() not in framework_names and p.name.lower() != 'netstandard.dll']
    for path in refs + [args.compiler, args.dotnet]:
        if not path.is_file():
            raise SystemExit('Missing build reference/tool: ' + str(path))
    output = args.output / 'LivingWorld.Studio.dll'
    if output.exists():
        output.unlink()  # Failed compile must not leave an older apparent result.
    options = ['-nologo', '-target:library', '-nostdlib+', '-langversion:latest', '-optimize+',
               '-out:' + str(output)]
    options += ['-r:' + str(path) for path in refs]
    options += [str(path) for path in sources]
    rsp = args.output / 'compile.rsp'
    rsp.write_text('\n'.join('"' + value.replace('"', '\\"') + '"' for value in options) + '\n')
    result = subprocess.run([str(args.dotnet), str(args.compiler), '@' + str(rsp)], capture_output=True, text=True)
    log = result.stdout + result.stderr
    (args.output / 'compile.log').write_text(log)
    print(log, end='')
    if result.returncode or not output.is_file():
        raise SystemExit(result.returncode or 1)
    report.write_text(json.dumps({
        'status': 'COMPILED', 'installed': False, 'live_tested': False,
        'output': output.name, 'sha256': digest(output),
        'sources': [{'path': str(p.relative_to(ROOT)), 'sha256': digest(p)} for p in sources],
        'references': [{'path': str(p), 'sha256': digest(p)} for p in refs],
        'compiler': {'path': str(args.compiler), 'sha256': digest(args.compiler)}
    }, indent=2) + '\n')
    print('COMPILED: ' + str(output) + '; NOT INSTALLED; NOT LIVE-TESTED')

if __name__ == '__main__':
    main()
