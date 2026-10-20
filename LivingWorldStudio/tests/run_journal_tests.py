#!/usr/bin/env python3
"""Compile and run the pure C# recovery journal tests using supplied local .NET tools.

Example:
  python3 tests/run_journal_tests.py --dotnet /path/to/dotnet \
    --compiler /path/to/csc.dll --runtime /path/to/Microsoft.NETCore.App/8.0.21

No game, profile, network or installed-mod directories are written.
The current test transcript is always written to build/journal-tests.txt.
"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess
import sys


ROOT = Path(__file__).resolve().parents[1]
SOURCES = (ROOT / "src" / "StudioJournal.cs", ROOT / "tests" / "JournalTests.cs")


def sha256(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def quote_response(value):
    # Roslyn response-file quoting; reject embedded newlines/quotes rather than
    # interpreting an untrusted local path as an additional compiler option.
    if any(character in value for character in ('"', '\r', '\n')):
        raise ValueError("Unsupported quote/newline in compiler argument.")
    return '"' + value + '"'


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--dotnet", required=True, type=Path, help="Local dotnet host executable")
    parser.add_argument("--compiler", required=True, type=Path, help="Roslyn csc.dll")
    parser.add_argument("--runtime", required=True, type=Path,
                        help="Installed Microsoft.NETCore.App version directory containing System.Private.CoreLib.dll")
    args = parser.parse_args()
    build = ROOT / "build"
    artifacts = build / "journal-tests"
    artifacts.mkdir(parents=True, exist_ok=True)
    report = build / "journal-tests.txt"
    transcript = ["StudioJournal standalone tests", "Status: INCOMPLETE"]
    report.write_text("\n".join(transcript) + "\n", encoding="utf-8")
    output = artifacts / "JournalTests.dll"
    if output.exists():
        output.unlink()
    try:
        dotnet = args.dotnet.resolve(strict=True)
        compiler = args.compiler.resolve(strict=True)
        runtime = args.runtime.resolve(strict=True)
        if not dotnet.is_file() or not compiler.is_file() or not runtime.is_dir():
            raise ValueError("Supply an existing dotnet executable, csc.dll and runtime directory.")
        version = runtime.name
        if not re.fullmatch(r"[0-9]+\.[0-9]+\.[0-9]+(?:-[A-Za-z0-9.-]+)?", version):
            raise ValueError("Runtime directory name must be its installed version, e.g. 8.0.21.")
        if not (runtime / "System.Private.CoreLib.dll").is_file():
            raise ValueError("Runtime directory has no System.Private.CoreLib.dll.")
        for source in SOURCES:
            if not source.is_file():
                raise ValueError("Missing test source: " + str(source))
        references = sorted(runtime.glob("*.dll"))
        options = ["-nologo", "-target:exe", "-nostdlib+", "-out:" + str(output)]
        options += ["-r:" + str(reference) for reference in references]
        options += [str(source) for source in SOURCES]
        response = artifacts / "compile.rsp"
        response.write_text("\n".join(quote_response(option) for option in options) + "\n", encoding="utf-8")
        major_minor = ".".join(version.split(".")[:2])
        (artifacts / "JournalTests.runtimeconfig.json").write_text(json.dumps({
            "runtimeOptions": {
                "tfm": "net" + major_minor,
                "framework": {"name": "Microsoft.NETCore.App", "version": version}
            }
        }, indent=2) + "\n", encoding="utf-8")
        transcript += ["Runtime: " + str(runtime), "Compiler SHA256: " + sha256(compiler)]
        transcript += ["Source " + str(source.relative_to(ROOT)) + " SHA256: " + sha256(source) for source in SOURCES]
        environment = os.environ.copy()
        environment.setdefault("DOTNET_SKIP_FIRST_TIME_EXPERIENCE", "1")
        environment.setdefault("DOTNET_CLI_TELEMETRY_OPTOUT", "1")
        compiled = subprocess.run([str(dotnet), str(compiler), "@" + str(response)],
                                  capture_output=True, text=True, env=environment, timeout=120)
        transcript += ["Compile exit: " + str(compiled.returncode), compiled.stdout, compiled.stderr]
        if compiled.returncode != 0 or not output.is_file():
            raise RuntimeError("Journal test compilation failed.")
        tested = subprocess.run([str(dotnet), str(output)], capture_output=True, text=True,
                                env=environment, timeout=120)
        transcript += ["Test exit: " + str(tested.returncode), tested.stdout, tested.stderr]
        if tested.returncode != 0 or not re.search(r"^PASS StudioJournal: [0-9]+ assertions\.$", tested.stdout, re.MULTILINE):
            raise RuntimeError("Journal tests failed or omitted their PASS result.")
        transcript.append("Status: PASS; no game/runtime test or installation claimed.")
        report.write_text("\n".join(transcript) + "\n", encoding="utf-8")
        print(tested.stdout, end="")
        print("Report: " + str(report))
        return 0
    except Exception as error:
        transcript += ["Status: FAILED", type(error).__name__ + ": " + str(error)]
        report.write_text("\n".join(transcript) + "\n", encoding="utf-8")
        print("Journal tests FAILED: " + str(error), file=sys.stderr)
        print("Report: " + str(report), file=sys.stderr)
        return 1


if __name__ == "__main__":
    sys.exit(main())
