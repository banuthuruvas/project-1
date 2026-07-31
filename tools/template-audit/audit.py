#!/usr/bin/env python3
"""
NIE Template — compliance audit (Python port).

Replaces tools/template-audit/Program.cs with a stdlib-only Python script that
runs ~30x faster (no .NET startup) and is easy to extend.

Five check categories:
  - structure   .ai/ folders + CLAUDE.md presence
  - metadata    .nie-template-version.json validity, CHANGELOG sync
  - features    every feature dossier has README.md, files.md, do-dont.md
  - code        regex code smells (hardcoded enums, unvalidated API calls)
  - security    SecurityHeadersMiddleware, [Authorize] on controllers, SsrfGuard usage,
                IOwnedEntity adoption

Usage:
  python tools/template-audit/audit.py                # run all checks
  python tools/template-audit/audit.py --strict       # exit 1 on any non-critical finding too
  python tools/template-audit/audit.py --json         # machine-readable
  python tools/template-audit/audit.py --list         # explain every rule and exit
  python tools/template-audit/audit.py --repo /path   # audit another repo

Exit codes:
  0 — no critical findings (or only warnings)
  1 — at least one critical check failed
  2 — invocation error
"""
from __future__ import annotations

import argparse
import json
import re
import sys
from dataclasses import dataclass, field, asdict
from pathlib import Path

# UTF-8 stdout on Windows consoles
if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    sys.stderr.reconfigure(encoding="utf-8", errors="replace")

# ---------------------------------------------------------------------------
# Result types
# ---------------------------------------------------------------------------

@dataclass
class Check:
    name: str
    category: str
    passed: bool
    critical: bool = False
    message: str | None = None
    remediation: list[str] = field(default_factory=list)


@dataclass
class Report:
    repo_path: str
    template_version: str
    checks: list[Check] = field(default_factory=list)

    def add(self, c: Check) -> None:
        self.checks.append(c)

    @property
    def passed(self) -> int:
        return sum(1 for c in self.checks if c.passed)

    @property
    def total(self) -> int:
        return len(self.checks)

    @property
    def has_critical(self) -> bool:
        return any(not c.passed and c.critical for c in self.checks)

    @property
    def has_any_failure(self) -> bool:
        return any(not c.passed for c in self.checks)


# ---------------------------------------------------------------------------
# The auditor
# ---------------------------------------------------------------------------

class Auditor:
    REQUIRED_FEATURE_FILES = ("README.md", "files.md", "do-dont.md")

    def __init__(self, repo_root: Path, use_ast: bool = False):
        self.root = repo_root.resolve()
        self.use_ast = use_ast
        self._ast = None
        if use_ast:
            try:
                # Local import — only loaded when --ast is passed.
                import sys as _sys
                _sys.path.insert(0, str(Path(__file__).resolve().parent))
                import ast_check  # type: ignore
                self._ast = ast_check
            except ImportError as e:
                print(f"WARN: --ast requested but tree-sitter libs missing "
                      f"({e}); falling back to regex checks", file=sys.stderr)
                self.use_ast = False

    def run(self) -> Report:
        version = self._read_template_version()
        report = Report(repo_path=str(self.root), template_version=version)
        self._check_structure(report)
        self._check_metadata(report)
        self._check_features(report)
        self._check_code_quality(report)
        self._check_security(report)
        if self.use_ast:
            self._check_ast(report)
        return report

    # -- helpers ------------------------------------------------------------

    def _read_template_version(self) -> str:
        f = self.root / ".nie-template-version.json"
        if not f.is_file():
            return "unknown"
        try:
            return json.loads(f.read_text(encoding="utf-8")).get("templateVersion", "unknown")
        except json.JSONDecodeError:
            return "invalid-json"

    def _add(self, report: Report, name: str, category: str, passed: bool,
             critical: bool = False, message: str | None = None,
             remediation: list[str] | None = None) -> None:
        report.add(Check(
            name=name, category=category, passed=passed, critical=critical,
            message=message, remediation=remediation or []
        ))

    # -- structure ----------------------------------------------------------

    def _check_structure(self, r: Report) -> None:
        for sub, critical in [
            (".ai",                 True),
            (".ai/common",          True),
            (".ai/features",        True),
            (".ai/tasks",           True),
            (".ai/tool-routes",     False),
        ]:
            self._add(r, f"{sub}/ folder exists", "structure",
                      (self.root / sub).is_dir(), critical=critical,
                      remediation=[f"Restore {sub}/ from the template repo"] if not (self.root / sub).is_dir() else None)

        self._add(r, "CLAUDE.md exists at repo root", "structure",
                  (self.root / "CLAUDE.md").is_file(), critical=False,
                  remediation=["Copy CLAUDE.md from the template — points AI agents at .ai/"])

    # -- metadata -----------------------------------------------------------

    def _check_metadata(self, r: Report) -> None:
        version_file = self.root / ".nie-template-version.json"
        version_exists = version_file.is_file()
        self._add(r, ".nie-template-version.json exists", "metadata", version_exists,
                  critical=True,
                  remediation=["Run `python tools/template-align/align.py` to bootstrap"])

        if version_exists:
            try:
                data = json.loads(version_file.read_text(encoding="utf-8"))
                ok = "templateVersion" in data and data.get("timezone") == "Asia/Singapore"
                self._add(r, "version file shape valid", "metadata", ok,
                          critical=True,
                          message=None if ok else "Missing templateVersion or timezone != Asia/Singapore")
            except json.JSONDecodeError as e:
                self._add(r, "version file shape valid", "metadata", False,
                          critical=True, message=f"JSON parse error: {e}")

        changelog = self.root / "CHANGELOG.md"
        self._add(r, "CHANGELOG.md exists", "metadata", changelog.is_file(),
                  critical=False)

        if changelog.is_file() and version_exists:
            try:
                ver = json.loads(version_file.read_text(encoding="utf-8")).get("templateVersion")
                if ver:
                    text = changelog.read_text(encoding="utf-8")
                    has_entry = f"## [{ver}]" in text or f"## {ver}" in text
                    self._add(r, "CHANGELOG.md mentions current templateVersion",
                              "metadata", has_entry, critical=False,
                              remediation=[f"Add a `## [{ver}]` section to CHANGELOG.md"]
                                          if not has_entry else None)
            except json.JSONDecodeError:
                pass

    # -- features -----------------------------------------------------------

    def _check_features(self, r: Report) -> None:
        feat_dir = self.root / ".ai" / "features"
        if not feat_dir.is_dir():
            return
        for d in sorted(feat_dir.iterdir()):
            if not d.is_dir() or d.name.startswith("_"):
                continue
            for fname in self.REQUIRED_FEATURE_FILES:
                exists = (d / fname).is_file()
                self._add(r, f"feature '{d.name}' has {fname}", "features",
                          exists, critical=False,
                          remediation=[f"Create .ai/features/{d.name}/{fname} per the dossier template"]
                                      if not exists else None)

    # -- code quality -------------------------------------------------------

    HARDCODED_ENUM_PATTERNS = [
        re.compile(r'==\s*"[A-Z][a-zA-Z]+"'),                    # status == "Approved"
        re.compile(r"case\s+\"[A-Z][a-zA-Z]+\"\s*:"),            # case "Approved":
    ]
    UNVALIDATED_API_RE = re.compile(
        r"(fetch|\.get)\([^)]*\)[^;\n]*\.json\(\)", re.DOTALL)

    def _check_code_quality(self, r: Report) -> None:
        src = self.root / "src"
        if not src.is_dir():
            return

        # Hardcoded enum strings in C#
        cs_files = list(src.rglob("*.cs"))
        # Skip generated migration/snapshot files — they're not author code
        cs_files = [f for f in cs_files if "Migrations" not in f.parts and "obj" not in f.parts]
        hardcoded = 0
        for f in cs_files:
            try:
                text = f.read_text(encoding="utf-8", errors="ignore")
            except OSError:
                continue
            for pat in self.HARDCODED_ENUM_PATTERNS:
                hardcoded += len(pat.findall(text))
        self._add(r, "no hardcoded enum strings in backend (heuristic)",
                  "code", hardcoded == 0, critical=False,
                  message=f"{hardcoded} matches" if hardcoded else None,
                  remediation=["Replace with Domain.Enum.E* values"]
                              if hardcoded else None)

        # Unvalidated API calls in TS — sample-only heuristic
        ts_files = list(src.rglob("*.ts"))
        ts_files = [f for f in ts_files if "node_modules" not in f.parts and "dist" not in f.parts]
        unvalidated = 0
        for f in ts_files:
            try:
                text = f.read_text(encoding="utf-8", errors="ignore")
            except OSError:
                continue
            for m in self.UNVALIDATED_API_RE.finditer(text):
                section = m.group(0)
                if not any(tok in section for tok in (" as ", ": typeof ", "zod", "guard", "parse(")):
                    unvalidated += 1
        threshold = max(1, int(len(ts_files) * 0.1))
        passed = unvalidated < threshold
        self._add(r, "API responses are validated (heuristic)", "code", passed,
                  critical=False,
                  message=f"{unvalidated} suspicious calls in {len(ts_files)} TS files (threshold {threshold})"
                          if not passed else None,
                  remediation=["Add type guards / zod schemas around fetch().json() results"]
                              if not passed else None)

    # -- security -----------------------------------------------------------

    def _check_security(self, r: Report) -> None:
        backend = self.root / "src" / "backend"
        if not backend.is_dir():
            return

        # SecurityHeadersMiddleware (task 0005)
        shm = backend / "API" / "Middleware" / "SecurityHeadersMiddleware.cs"
        self._add(r, "SecurityHeadersMiddleware exists (task 0005)",
                  "security", shm.is_file(), critical=False,
                  remediation=["Apply task .ai/tasks/0005-add-security-headers-middleware/apply.md"]
                              if not shm.is_file() else None)

        # SsrfGuard (task 0006)
        ssrf = backend / "Libraries" / "Shared" / "Helpers" / "SsrfGuard.cs"
        self._add(r, "SsrfGuard exists (task 0006)",
                  "security", ssrf.is_file(), critical=False,
                  remediation=["Apply task .ai/tasks/0006-ssrf-outbound-allowlist/apply.md"]
                              if not ssrf.is_file() else None)

        # IOwnedEntity (task 0007)
        owned = backend / "Libraries" / "Domain" / "Models" / "IOwnedEntity.cs"
        self._add(r, "IOwnedEntity marker exists (task 0007)",
                  "security", owned.is_file(), critical=False,
                  remediation=["Apply task .ai/tasks/0007-bola-ownership-pattern/apply.md"]
                              if not owned.is_file() else None)

        # PagedSearchDto cap (task 0009)
        paged = backend / "Libraries" / "Domain" / "Dto" / "PagedSearchDto.cs"
        self._add(r, "PagedSearchDto cap exists (task 0009)",
                  "security", paged.is_file(), critical=False,
                  remediation=["Apply task .ai/tasks/0009-cap-pagesize/apply.md"]
                              if not paged.is_file() else None)

        # [Authorize] / [RequireAccessFunction] on controllers — improved over the
        # dotnet version: also recognise class-level attribute application.
        cs_controllers = [
            f for f in backend.rglob("*Controller.cs")
            if "Migrations" not in f.parts and "obj" not in f.parts
        ]
        unguarded: list[str] = []
        for f in cs_controllers:
            try:
                text = f.read_text(encoding="utf-8", errors="ignore")
            except OSError:
                continue
            # accept any of: [Authorize], [Authorize(...)], [RequireAccessFunction(...)]
            if not re.search(r"\[(Authorize|RequireAccessFunction)\b", text):
                unguarded.append(str(f.relative_to(self.root)))

        passed = not unguarded
        self._add(r, "all controllers carry [Authorize] or [RequireAccessFunction]",
                  "security", passed, critical=False,
                  message=None if passed else f"{len(unguarded)} unguarded controller(s)",
                  remediation=[f"Add attribute to {p}" for p in unguarded[:5]]
                              if not passed else None)

    # -- AST (opt-in via --ast) ---------------------------------------------

    def _check_ast(self, r: Report) -> None:
        """Run tree-sitter-based checks. Each finding becomes a Check entry
        in the matching category; passes/totals reflect file-level aggregates."""
        if self._ast is None:
            return

        backend = self.root / "src" / "backend"
        frontend = self.root / "src" / "frontend"

        # ---- C# checks
        if backend.is_dir():
            cs_files = [f for f in backend.rglob("*.cs")
                        if "Migrations" not in f.parts and "obj" not in f.parts]
            ast_findings = []
            for f in cs_files:
                ast_findings.extend(self._ast.run_csharp_checks(f))

            # Group findings by rule
            by_rule: dict[str, list] = {}
            for fi in ast_findings:
                by_rule.setdefault(fi.rule, []).append(fi)

            # Authorize coverage — one aggregate check
            authz = by_rule.get("cs/missing-authorize", [])
            self._add(r,
                      "AST: every public controller method carries [Authorize] "
                      "/[RequireAccessFunction]/[AllowAnonymous]",
                      "security", not authz, critical=False,
                      message=(f"{len(authz)} method(s) without an auth attribute"
                               if authz else None),
                      remediation=[
                          f"{fi.file.replace(str(self.root) + chr(92), '')}:{fi.line} — "
                          f"{fi.message}" for fi in authz[:8]
                      ] if authz else None)

            # Pagination cap heuristic
            takes = by_rule.get("cs/unbounded-take", [])
            self._add(r,
                      "AST: no `.Take(N)` calls with N>100 (use PagedSearchDto)",
                      "security", not takes, critical=False,
                      message=(f"{len(takes)} suspicious .Take call(s)"
                               if takes else None),
                      remediation=[fi.message for fi in takes[:5]] if takes else None)

        # ---- TS checks
        if frontend.is_dir():
            ts_files = [f for f in frontend.rglob("*.ts")
                        if "node_modules" not in f.parts and "dist" not in f.parts]
            ast_findings = []
            for f in ts_files:
                ast_findings.extend(self._ast.run_typescript_checks(f))

            by_rule: dict[str, list] = {}
            for fi in ast_findings:
                by_rule.setdefault(fi.rule, []).append(fi)

            as_any = by_rule.get("ts/as-any", [])
            self._add(r, "AST: no `as any` casts in TS source",
                      "code", not as_any, critical=False,
                      message=f"{len(as_any)} `as any` cast(s)" if as_any else None,
                      remediation=[
                          f"{fi.file.replace(str(self.root) + chr(92), '')}:{fi.line}"
                          for fi in as_any[:8]
                      ] if as_any else None)

            unguarded = by_rule.get("ts/unvalidated-json", [])
            self._add(r, "AST: `.json()` calls have a parse / type guard nearby",
                      "code", not unguarded, critical=False,
                      message=(f"{len(unguarded)} suspicious .json() call(s)"
                               if unguarded else None),
                      remediation=[
                          f"{fi.file.replace(str(self.root) + chr(92), '')}:{fi.line}"
                          for fi in unguarded[:8]
                      ] if unguarded else None)


# ---------------------------------------------------------------------------
# CLI / output
# ---------------------------------------------------------------------------

ICONS = {True: "OK", False: "FAIL"}

def print_text(report: Report) -> None:
    print(f"\n=== NIE Template Audit — {report.repo_path} ===")
    print(f"Template version: {report.template_version}\n")
    cats = ["structure", "metadata", "features", "code", "security"]
    for cat in cats:
        cat_checks = [c for c in report.checks if c.category == cat]
        passed = sum(1 for c in cat_checks if c.passed)
        total = len(cat_checks)
        if total == 0:
            continue
        status = "PASS" if passed == total else "FAIL"
        print(f"[{status}] {cat:<10} {passed}/{total}")
        for c in cat_checks:
            if c.passed:
                continue
            tag = "CRIT" if c.critical else "warn"
            print(f"    [{tag}] {c.name}")
            if c.message:
                print(f"        {c.message}")
            for fix in c.remediation:
                print(f"        - {fix}")
    print(f"\nSummary: {report.passed}/{report.total} checks passed",
          end="")
    if report.has_critical:
        print("  -- CRITICAL ISSUES PRESENT")
    elif report.has_any_failure:
        print("  -- warnings only")
    else:
        print("  -- all green")


def list_rules() -> None:
    rules = [
        ("structure",  "all required .ai/ folders are present"),
        ("metadata",   ".nie-template-version.json shape + CHANGELOG sync"),
        ("features",   "every feature has README.md + files.md + do-dont.md"),
        ("code",       "no hardcoded enum strings; API calls have validation"),
        ("security",   "task 0005/0006/0007/0009 artefacts present; controllers guarded"),
    ]
    print("\nNIE Template — audit rule families")
    print("-" * 50)
    for cat, desc in rules:
        print(f"  {cat:<10} {desc}")
    print()


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--repo", default=".", help="Repository root (default cwd)")
    ap.add_argument("--strict", action="store_true",
                    help="Exit non-zero on any failure, not just critical ones")
    ap.add_argument("--json", action="store_true", help="Emit JSON")
    ap.add_argument("--list", action="store_true",
                    help="Print rule families and exit")
    ap.add_argument("--ast", action="store_true",
                    help="Enable AST-based checks (requires tree_sitter, "
                         "tree_sitter_c_sharp, tree_sitter_typescript). "
                         "If libs are missing, falls back to regex with a warning.")
    args = ap.parse_args(argv)

    if args.list:
        list_rules()
        return 0

    root = Path(args.repo).resolve()
    if not root.is_dir():
        print(f"ERROR: not a directory: {root}", file=sys.stderr)
        return 2

    report = Auditor(root, use_ast=args.ast).run()

    if args.json:
        out = {
            "repo": report.repo_path,
            "templateVersion": report.template_version,
            "passed": report.passed,
            "total": report.total,
            "hasCritical": report.has_critical,
            "checks": [asdict(c) for c in report.checks],
        }
        print(json.dumps(out, indent=2))
    else:
        print_text(report)

    if args.strict and report.has_any_failure:
        return 1
    if report.has_critical:
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
