#!/usr/bin/env python3
"""
NIE Template fleet registry — reference receiver.

A tiny stdlib HTTP server that accepts POSTs from `audit.yml`'s telemetry step
and accumulates them as JSON files on disk. Useful for piloting fleet
visibility before any production registry exists.

This is INTENTIONALLY minimal:
  - No database, no auth provider, no rotation.
  - Single optional bearer-token check via REGISTRY_TOKEN env var.
  - Writes one JSON file per (repo, sha) under <data_dir>/.
  - GET /v1/audit returns a flat index of all received payloads.

Usage:
  python tools/template-registry/receiver.py --port 8080 --data ./registry-data
  REGISTRY_TOKEN=secret python tools/template-registry/receiver.py    # auth on

Endpoints:
  POST /v1/audit       # accept a telemetry payload
  GET  /v1/audit       # list all known repos + their latest audit summary
  GET  /v1/audit/<repo-slug>   # full history for one repo
  GET  /healthz        # liveness probe

This is a reference. Swap for a real backend (S3 + Lambda, GitHub Pages
static index, FastAPI + Postgres, etc.) once the schema stabilises. The
schema lives in REGISTRY-SCHEMA.md alongside this file.
"""
from __future__ import annotations

import argparse
import datetime as dt
import json
import os
import re
import sys
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from urllib.parse import urlparse

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")

DATA_DIR = Path("registry-data")
SLUG_RE = re.compile(r"[^a-zA-Z0-9._-]+")


def _slug(repo: str) -> str:
    return SLUG_RE.sub("-", repo).strip("-") or "unknown"


class Handler(BaseHTTPRequestHandler):
    server_version = "NIE-TemplateRegistry/0.1"

    # --- helpers --------------------------------------------------------

    def _check_auth(self) -> bool:
        expected = os.environ.get("REGISTRY_TOKEN", "")
        if not expected:
            return True
        got = self.headers.get("Authorization", "")
        return got == f"Bearer {expected}"

    def _json(self, code: int, data) -> None:
        body = json.dumps(data, indent=2).encode("utf-8")
        self.send_response(code)
        self.send_header("Content-Type", "application/json")
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        self.wfile.write(body)

    def log_message(self, fmt, *args):
        # Compact log line
        sys.stderr.write(f"[{dt.datetime.now(dt.timezone.utc).isoformat()}] "
                         f"{self.address_string()} {fmt % args}\n")

    # --- GET ------------------------------------------------------------

    def do_GET(self):  # noqa: N802
        path = urlparse(self.path).path
        if path == "/healthz":
            return self._json(200, {"status": "ok"})
        if path == "/v1/audit":
            return self._list_all()
        if path.startswith("/v1/audit/"):
            return self._list_repo(path[len("/v1/audit/"):])
        self._json(404, {"error": "not found"})

    def _list_all(self) -> None:
        latest = []
        for repo_dir in sorted(DATA_DIR.iterdir() if DATA_DIR.is_dir() else []):
            if not repo_dir.is_dir():
                continue
            files = sorted(repo_dir.glob("*.json"))
            if not files:
                continue
            try:
                payload = json.loads(files[-1].read_text(encoding="utf-8"))
            except json.JSONDecodeError:
                continue
            latest.append({
                "repo": payload.get("repo", repo_dir.name),
                "templateVersion": payload.get("templateVersion"),
                "appliedTasks": payload.get("appliedTasks", []),
                "auditPassed": payload.get("audit", {}).get("passed"),
                "auditTotal": payload.get("audit", {}).get("total"),
                "hasCritical": payload.get("audit", {}).get("hasCritical"),
                "lastSeen": payload.get("timestamp"),
                "lastSha": payload.get("sha"),
                "history": len(files),
            })
        self._json(200, {"count": len(latest), "repos": latest})

    def _list_repo(self, slug: str) -> None:
        repo_dir = DATA_DIR / _slug(slug)
        if not repo_dir.is_dir():
            return self._json(404, {"error": "no such repo"})
        history = []
        for f in sorted(repo_dir.glob("*.json")):
            try:
                history.append(json.loads(f.read_text(encoding="utf-8")))
            except json.JSONDecodeError:
                continue
        self._json(200, {"slug": slug, "count": len(history), "entries": history})

    # --- POST -----------------------------------------------------------

    def do_POST(self):  # noqa: N802
        path = urlparse(self.path).path
        if path != "/v1/audit":
            return self._json(404, {"error": "not found"})
        if not self._check_auth():
            return self._json(401, {"error": "unauthorized"})

        length = int(self.headers.get("Content-Length", "0"))
        if length <= 0 or length > 1_000_000:  # 1 MB hard cap
            return self._json(413, {"error": "payload too large or empty"})
        raw = self.rfile.read(length)
        try:
            payload = json.loads(raw)
        except json.JSONDecodeError:
            return self._json(400, {"error": "invalid JSON"})

        repo = payload.get("repo", "")
        sha = payload.get("sha", "")
        if not repo:
            return self._json(400, {"error": "missing 'repo' field"})

        slug = _slug(repo)
        repo_dir = DATA_DIR / slug
        repo_dir.mkdir(parents=True, exist_ok=True)
        ts = dt.datetime.now(dt.timezone.utc).strftime("%Y%m%dT%H%M%SZ")
        fname = f"{ts}_{(sha or 'nosha')[:12]}.json"
        (repo_dir / fname).write_text(json.dumps(payload, indent=2),
                                      encoding="utf-8")
        self._json(202, {"accepted": True, "stored": str(repo_dir / fname)})


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--port", type=int, default=8080)
    ap.add_argument("--host", default="127.0.0.1")
    ap.add_argument("--data", default="registry-data",
                    help="Directory to write payloads to (default ./registry-data)")
    args = ap.parse_args()

    global DATA_DIR
    DATA_DIR = Path(args.data).resolve()
    DATA_DIR.mkdir(parents=True, exist_ok=True)

    auth = "ON (REGISTRY_TOKEN set)" if os.environ.get("REGISTRY_TOKEN") else "OFF"
    print(f"NIE Template registry receiver")
    print(f"  listening : http://{args.host}:{args.port}")
    print(f"  data dir  : {DATA_DIR}")
    print(f"  auth      : {auth}")
    print(f"  endpoints : POST /v1/audit, GET /v1/audit[/<repo-slug>], GET /healthz")

    server = ThreadingHTTPServer((args.host, args.port), Handler)
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        print("\nshutting down")
    finally:
        server.server_close()
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
