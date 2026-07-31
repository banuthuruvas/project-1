#!/usr/bin/env python3
"""
NIE Template fleet registry — production-grade receiver (v2).

Upgrades over receiver.py:
  - SQLite persistence (single-file, transactional, indexed)
  - Optional JWT verification (HS256 shared secret) alongside bearer-token auth
  - HTML drift dashboard at /dashboard
  - Per-repo history view at /v1/audit/<slug>
  - Health + Prometheus-style /metrics endpoint
  - 1 MB payload cap, malformed-JSON rejection, request logging

Stdlib only (auth uses HMAC; JWT is parsed with stdlib + manual HMAC verify).

Usage:
    python tools/template-registry/registry.py \
        --port 8080 --host 0.0.0.0 \
        --db ./registry.db
    REGISTRY_TOKEN=secret    python registry.py     # bearer auth
    REGISTRY_JWT_SECRET=hex  python registry.py     # HS256 JWT auth

If neither REGISTRY_TOKEN nor REGISTRY_JWT_SECRET is set, auth is OFF (pilot mode).
You may set BOTH; either form will be accepted.

Endpoints
---------
POST /v1/audit                  Accept a telemetry payload.
GET  /v1/audit                  Latest snapshot per repo (JSON).
GET  /v1/audit/<repo-slug>      Full history for one repo (JSON).
GET  /dashboard                 HTML drift dashboard.
GET  /metrics                   Prometheus-style metrics.
GET  /healthz                   Liveness.

Compared with receiver.py
-------------------------
This module is the canonical receiver going forward. receiver.py is kept for
small-pilot use where SQLite would be overkill (single-machine, no auth,
ephemeral). registry.py is what you deploy.
"""
from __future__ import annotations

import argparse
import base64
import datetime as dt
import hashlib
import hmac
import json
import os
import re
import sqlite3
import sys
import threading
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from urllib.parse import urlparse

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    sys.stderr.reconfigure(encoding="utf-8", errors="replace")

SLUG_RE = re.compile(r"[^a-zA-Z0-9._-]+")
SCHEMA_VERSION = 1
MAX_PAYLOAD_BYTES = 1_000_000
DB_PATH = Path("registry.db")
DB_LOCK = threading.Lock()


# ---------------------------------------------------------------------------
# Storage layer — SQLite
# ---------------------------------------------------------------------------

SCHEMA = """
CREATE TABLE IF NOT EXISTS schema_meta (
    version INTEGER NOT NULL
);

CREATE TABLE IF NOT EXISTS audit_payload (
    id              INTEGER PRIMARY KEY AUTOINCREMENT,
    repo            TEXT NOT NULL,
    repo_slug       TEXT NOT NULL,
    sha             TEXT,
    template_version TEXT,
    has_critical    INTEGER NOT NULL DEFAULT 0,
    audit_passed    INTEGER,
    audit_total     INTEGER,
    received_at     TEXT NOT NULL,
    payload_json    TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_audit_repo_received
    ON audit_payload (repo_slug, received_at DESC);
CREATE INDEX IF NOT EXISTS idx_audit_critical
    ON audit_payload (has_critical, received_at DESC);
"""


def _slug(repo: str) -> str:
    return SLUG_RE.sub("-", repo).strip("-") or "unknown"


def _connect() -> sqlite3.Connection:
    conn = sqlite3.connect(DB_PATH, isolation_level=None, timeout=10.0)
    conn.row_factory = sqlite3.Row
    conn.execute("PRAGMA journal_mode=WAL")
    conn.execute("PRAGMA synchronous=NORMAL")
    return conn


def init_db() -> None:
    with DB_LOCK, _connect() as conn:
        conn.executescript(SCHEMA)
        cur = conn.execute("SELECT version FROM schema_meta")
        row = cur.fetchone()
        if row is None:
            conn.execute("INSERT INTO schema_meta(version) VALUES (?)",
                         (SCHEMA_VERSION,))
        elif row["version"] != SCHEMA_VERSION:
            print(f"WARN: schema_meta.version={row['version']}, "
                  f"code expects {SCHEMA_VERSION} — manual migration needed",
                  file=sys.stderr)


def store_payload(payload: dict) -> int:
    """Persist a payload, return its row id."""
    repo = payload.get("repo", "")
    audit = payload.get("audit", {}) or {}
    received = dt.datetime.now(dt.timezone.utc).isoformat(timespec="seconds")
    with DB_LOCK, _connect() as conn:
        cur = conn.execute(
            """INSERT INTO audit_payload
               (repo, repo_slug, sha, template_version, has_critical,
                audit_passed, audit_total, received_at, payload_json)
               VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)""",
            (repo, _slug(repo),
             payload.get("sha", ""),
             payload.get("templateVersion", ""),
             1 if audit.get("hasCritical") else 0,
             audit.get("passed"),
             audit.get("total"),
             received,
             json.dumps(payload, separators=(",", ":")))
        )
        return cur.lastrowid


def latest_per_repo() -> list[dict]:
    """Snapshot of the latest payload for each repo. Tiebreaker on `id DESC`
    so two POSTs in the same wall-clock second don't both qualify."""
    with DB_LOCK, _connect() as conn:
        rows = conn.execute("""
            SELECT a.* FROM audit_payload a
            INNER JOIN (
                SELECT repo_slug, MAX(id) AS top_id
                FROM audit_payload GROUP BY repo_slug
            ) m ON a.id = m.top_id
            ORDER BY a.repo
        """).fetchall()
        out = []
        for r in rows:
            try:
                p = json.loads(r["payload_json"])
            except json.JSONDecodeError:
                p = {}
            out.append({
                "repo": r["repo"],
                "slug": r["repo_slug"],
                "sha": r["sha"],
                "templateVersion": r["template_version"],
                "appliedTasks": p.get("appliedTasks", []),
                "auditPassed": r["audit_passed"],
                "auditTotal": r["audit_total"],
                "hasCritical": bool(r["has_critical"]),
                "lastSeen": r["received_at"],
                "history": _history_count(r["repo_slug"]),
            })
        return out


def _history_count(slug: str) -> int:
    with _connect() as conn:
        cur = conn.execute(
            "SELECT COUNT(*) AS n FROM audit_payload WHERE repo_slug=?", (slug,))
        return cur.fetchone()["n"]


def history_for(slug: str) -> list[dict]:
    with DB_LOCK, _connect() as conn:
        rows = conn.execute(
            "SELECT * FROM audit_payload WHERE repo_slug=? ORDER BY received_at DESC LIMIT 200",
            (slug,)
        ).fetchall()
        out = []
        for r in rows:
            try:
                out.append(json.loads(r["payload_json"]))
            except json.JSONDecodeError:
                continue
        return out


def metrics_snapshot() -> dict:
    with DB_LOCK, _connect() as conn:
        total = conn.execute("SELECT COUNT(*) AS n FROM audit_payload").fetchone()["n"]
        repos = conn.execute("SELECT COUNT(DISTINCT repo_slug) AS n FROM audit_payload").fetchone()["n"]
        critical = conn.execute("""
            SELECT COUNT(*) AS n FROM audit_payload a
            INNER JOIN (
                SELECT repo_slug, MAX(id) AS top_id
                FROM audit_payload GROUP BY repo_slug
            ) m ON a.id=m.top_id
            WHERE a.has_critical=1
        """).fetchone()["n"]
        versions = conn.execute("""
            SELECT a.template_version AS v, COUNT(*) AS n
            FROM audit_payload a
            INNER JOIN (
                SELECT repo_slug, MAX(id) AS top_id
                FROM audit_payload GROUP BY repo_slug
            ) m ON a.id=m.top_id
            GROUP BY a.template_version
        """).fetchall()
    return {
        "payloads_total": total,
        "repos_total": repos,
        "repos_with_critical": critical,
        "version_distribution": {row["v"] or "unknown": row["n"] for row in versions},
    }


# ---------------------------------------------------------------------------
# Auth — bearer + HS256 JWT (stdlib only)
# ---------------------------------------------------------------------------

def _b64url_decode(data: str) -> bytes:
    pad = "=" * (-len(data) % 4)
    return base64.urlsafe_b64decode(data + pad)


def verify_jwt_hs256(token: str, secret: str) -> bool:
    """Verify an HS256 JWT against `secret`. Checks signature only — no claim
    enforcement here; callers can layer it on if exp/nbf/aud matter."""
    try:
        head_b64, payload_b64, sig_b64 = token.split(".")
    except ValueError:
        return False
    signing_input = f"{head_b64}.{payload_b64}".encode("ascii")
    try:
        expected = hmac.new(secret.encode("utf-8"), signing_input, hashlib.sha256).digest()
        actual = _b64url_decode(sig_b64)
    except Exception:
        return False
    if not hmac.compare_digest(expected, actual):
        return False
    try:
        head = json.loads(_b64url_decode(head_b64))
    except json.JSONDecodeError:
        return False
    if head.get("alg") != "HS256":
        return False
    try:
        claims = json.loads(_b64url_decode(payload_b64))
    except json.JSONDecodeError:
        return False
    now = int(dt.datetime.now(dt.timezone.utc).timestamp())
    if "exp" in claims and now >= int(claims["exp"]):
        return False
    if "nbf" in claims and now < int(claims["nbf"]):
        return False
    return True


def auth_ok(authorization_header: str | None) -> bool:
    """Accept the request if any configured auth mode passes.
    Modes:
      - REGISTRY_TOKEN set      -> exact bearer match
      - REGISTRY_JWT_SECRET set -> HS256 JWT verification
      - Neither set             -> auth OFF (pilot mode); always pass
    """
    bearer = os.environ.get("REGISTRY_TOKEN")
    jwt_secret = os.environ.get("REGISTRY_JWT_SECRET")
    if not bearer and not jwt_secret:
        return True
    if not authorization_header:
        return False
    if not authorization_header.startswith("Bearer "):
        return False
    token = authorization_header[len("Bearer "):]
    if bearer and hmac.compare_digest(token, bearer):
        return True
    if jwt_secret and verify_jwt_hs256(token, jwt_secret):
        return True
    return False


# ---------------------------------------------------------------------------
# HTTP handler
# ---------------------------------------------------------------------------

class Handler(BaseHTTPRequestHandler):
    server_version = "NIE-TemplateRegistry/2.0"

    # -- helpers --

    def _json(self, code: int, data) -> None:
        body = json.dumps(data, indent=2).encode("utf-8")
        self.send_response(code)
        self.send_header("Content-Type", "application/json")
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        self.wfile.write(body)

    def _html(self, code: int, html: str) -> None:
        body = html.encode("utf-8")
        self.send_response(code)
        self.send_header("Content-Type", "text/html; charset=utf-8")
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        self.wfile.write(body)

    def _text(self, code: int, text: str, content_type: str = "text/plain; charset=utf-8") -> None:
        body = text.encode("utf-8")
        self.send_response(code)
        self.send_header("Content-Type", content_type)
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        self.wfile.write(body)

    def log_message(self, fmt, *args):
        sys.stderr.write(f"[{dt.datetime.now(dt.timezone.utc).isoformat()}] "
                         f"{self.address_string()} {fmt % args}\n")

    # -- GET --

    def do_GET(self):  # noqa: N802
        path = urlparse(self.path).path
        if path == "/healthz":
            return self._json(200, {"status": "ok"})
        if path == "/metrics":
            return self._metrics()
        if path == "/dashboard":
            return self._dashboard()
        if path == "/v1/audit":
            return self._json(200, {"count": _count_repos(), "repos": latest_per_repo()})
        if path.startswith("/v1/audit/"):
            slug = _slug(path[len("/v1/audit/"):])
            entries = history_for(slug)
            if not entries:
                return self._json(404, {"error": "no such repo"})
            return self._json(200, {"slug": slug, "count": len(entries), "entries": entries})
        return self._json(404, {"error": "not found"})

    def _metrics(self) -> None:
        m = metrics_snapshot()
        lines = [
            "# HELP nie_template_payloads_total Total telemetry payloads received",
            "# TYPE nie_template_payloads_total counter",
            f"nie_template_payloads_total {m['payloads_total']}",
            "# HELP nie_template_repos_total Distinct repos seen",
            "# TYPE nie_template_repos_total gauge",
            f"nie_template_repos_total {m['repos_total']}",
            "# HELP nie_template_repos_with_critical Repos whose latest audit had a critical finding",
            "# TYPE nie_template_repos_with_critical gauge",
            f"nie_template_repos_with_critical {m['repos_with_critical']}",
            "# HELP nie_template_version_repos Repos at each templateVersion (latest)",
            "# TYPE nie_template_version_repos gauge",
        ]
        for ver, n in m["version_distribution"].items():
            safe = ver.replace('"', '\\"')
            lines.append(f'nie_template_version_repos{{template_version="{safe}"}} {n}')
        self._text(200, "\n".join(lines) + "\n",
                   content_type="text/plain; version=0.0.4; charset=utf-8")

    def _dashboard(self) -> None:
        repos = latest_per_repo()
        m = metrics_snapshot()
        rows = []
        for r in sorted(repos, key=lambda x: (not x["hasCritical"], x["repo"].lower())):
            crit = '<span class="crit">CRITICAL</span>' if r["hasCritical"] else '<span class="ok">ok</span>'
            tasks = ", ".join(r["appliedTasks"]) or "(none)"
            audit_str = f'{r["auditPassed"]}/{r["auditTotal"]}' if r["auditTotal"] else "—"
            rows.append(
                f'<tr><td><a href="/v1/audit/{r["slug"]}">{html_escape(r["repo"])}</a></td>'
                f'<td>{html_escape(r["templateVersion"] or "—")}</td>'
                f'<td>{audit_str}</td>'
                f'<td>{crit}</td>'
                f'<td title="{tasks}">{len(r["appliedTasks"])}</td>'
                f'<td>{html_escape(r["lastSeen"] or "")}</td></tr>'
            )
        version_table = "".join(
            f"<tr><td>{html_escape(v)}</td><td>{n}</td></tr>"
            for v, n in sorted(m["version_distribution"].items())
        )
        html = f"""<!DOCTYPE html>
<html lang="en"><head>
<meta charset="utf-8">
<title>NIE Template Fleet Drift</title>
<style>
  :root {{ color-scheme: light dark; }}
  body {{ font-family: ui-sans-serif, system-ui, sans-serif; margin: 2rem; max-width: 1200px; }}
  h1 {{ margin-bottom: 0; }} .sub {{ color: #888; margin-top: 0.25rem; }}
  table {{ border-collapse: collapse; width: 100%; margin-top: 1rem; }}
  th, td {{ text-align: left; padding: 0.5rem 0.75rem; border-bottom: 1px solid #2222; }}
  th {{ background: #f4f4f4; position: sticky; top: 0; }}
  @media (prefers-color-scheme: dark) {{ th {{ background: #222; }} }}
  .crit {{ color: #c33; font-weight: 600; }}
  .ok {{ color: #393; }}
  .grid {{ display: grid; grid-template-columns: 1fr 1fr 1fr; gap: 1rem; margin-top: 1rem; }}
  .card {{ border: 1px solid #2222; border-radius: 6px; padding: 1rem; }}
  .card .n {{ font-size: 2rem; font-weight: 700; }}
</style>
</head><body>
<h1>NIE Template Fleet Drift</h1>
<p class="sub">Generated {dt.datetime.now(dt.timezone.utc).isoformat()} from {m["payloads_total"]} payloads</p>
<div class="grid">
  <div class="card"><div>repos seen</div><div class="n">{m["repos_total"]}</div></div>
  <div class="card"><div>repos with critical findings</div>
    <div class="n" style="color:{'#c33' if m['repos_with_critical'] else '#393'}">{m["repos_with_critical"]}</div></div>
  <div class="card"><div>distinct templateVersions</div>
    <div class="n">{len(m["version_distribution"])}</div></div>
</div>
<h2>Repos (latest snapshot)</h2>
<table><thead><tr>
  <th>Repo</th><th>templateVersion</th><th>audit</th><th>status</th><th>tasks</th><th>last seen</th>
</tr></thead><tbody>{"".join(rows) or "<tr><td colspan=6>no data yet</td></tr>"}</tbody></table>
<h2>Version distribution</h2>
<table><thead><tr><th>templateVersion</th><th>repos at this version</th></tr></thead>
<tbody>{version_table or "<tr><td colspan=2>no data</td></tr>"}</tbody></table>
<p style="margin-top:2rem;color:#888"><small>Endpoints: <code>/v1/audit</code>,
<code>/v1/audit/&lt;slug&gt;</code>, <code>/metrics</code>, <code>/healthz</code></small></p>
</body></html>"""
        self._html(200, html)

    # -- POST --

    def do_POST(self):  # noqa: N802
        path = urlparse(self.path).path
        if path != "/v1/audit":
            return self._json(404, {"error": "not found"})
        if not auth_ok(self.headers.get("Authorization")):
            return self._json(401, {"error": "unauthorized"})

        length = int(self.headers.get("Content-Length", "0"))
        if length <= 0 or length > MAX_PAYLOAD_BYTES:
            return self._json(413, {"error": f"payload empty or > {MAX_PAYLOAD_BYTES} bytes"})
        raw = self.rfile.read(length)
        try:
            payload = json.loads(raw)
        except json.JSONDecodeError:
            return self._json(400, {"error": "invalid JSON"})
        if not isinstance(payload, dict):
            return self._json(400, {"error": "payload must be an object"})
        if not payload.get("repo"):
            return self._json(400, {"error": "missing 'repo' field"})

        try:
            row_id = store_payload(payload)
        except sqlite3.Error as e:
            return self._json(500, {"error": f"storage error: {e}"})
        self._json(202, {"accepted": True, "id": row_id})


def _count_repos() -> int:
    with DB_LOCK, _connect() as conn:
        return conn.execute(
            "SELECT COUNT(DISTINCT repo_slug) AS n FROM audit_payload"
        ).fetchone()["n"]


def html_escape(s: str) -> str:
    return (s or "").replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")


# ---------------------------------------------------------------------------
# Entry point
# ---------------------------------------------------------------------------

def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--port", type=int, default=int(os.environ.get("PORT", "8080")))
    ap.add_argument("--host", default=os.environ.get("HOST", "127.0.0.1"))
    ap.add_argument("--db", default=os.environ.get("REGISTRY_DB", "registry.db"))
    args = ap.parse_args(argv)

    global DB_PATH
    DB_PATH = Path(args.db).resolve()
    DB_PATH.parent.mkdir(parents=True, exist_ok=True)
    init_db()

    auth_modes = []
    if os.environ.get("REGISTRY_TOKEN"):
        auth_modes.append("bearer")
    if os.environ.get("REGISTRY_JWT_SECRET"):
        auth_modes.append("jwt-hs256")
    auth_label = "+".join(auth_modes) if auth_modes else "OFF (pilot)"

    print(f"NIE Template Registry v2")
    print(f"  listening : http://{args.host}:{args.port}")
    print(f"  database  : {DB_PATH}")
    print(f"  auth      : {auth_label}")
    print(f"  endpoints : POST/GET /v1/audit, /v1/audit/<slug>, /dashboard, "
          f"/metrics, /healthz")

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
