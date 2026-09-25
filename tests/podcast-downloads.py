"""Run against the built Migo.dll in LXD; no public services or user data."""
import http.server
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import threading

PAYLOAD = b"Banshee podcast recovery fixture\n" * 2048
requests = []
class Handler(http.server.BaseHTTPRequestHandler):
    protocol_version = "HTTP/1.0"
    def log_message(self, *args): pass
    def do_GET(self):
        offset = int(self.headers.get("Range", "bytes=0-").split("=")[1].split("-")[0])
        requests.append((self.path, offset))
        if self.path == "/missing":
            self.send_error(404); return
        if offset >= len(PAYLOAD):
            self.send_response(416)
            self.send_header("Content-Range", "bytes */%d" % len(PAYLOAD))
            self.send_header("Content-Length", "0")
            self.end_headers(); return
        if self.path == "/ignore": offset = 0
        body = PAYLOAD[offset:]
        self.send_response(206 if offset else 200)
        if offset:
            start = offset + 1 if self.path == "/bad-range" else offset
            self.send_header("Content-Range", "bytes %d-%d/%d" % (start,len(PAYLOAD)-1,len(PAYLOAD)))
        if self.path != "/unknown-length": self.send_header("Content-Length", str(len(body)))
        self.send_header("Content-Type", "audio/mpeg")
        self.end_headers()
        self.wfile.write(body[:100] if self.path == "/truncated" else body)

server = http.server.ThreadingHTTPServer(("127.0.0.1",0), Handler)
threading.Thread(target=server.serve_forever, daemon=True).start()
try:
    with tempfile.TemporaryDirectory(prefix="banshee-download-tests-") as directory:
        for label, endpoint, seed, success in [
            ("fresh", "/ok", b"", True),
            ("resume", "/ok", PAYLOAD[:777], True),
            ("ignored-range", "/ignore", PAYLOAD[:777], True),
            ("wrong-range", "/bad-range", PAYLOAD[:777], True),
            ("complete-partial-416", "/ok", PAYLOAD, True),
            ("unknown-length-resume", "/unknown-length", PAYLOAD[:777], True),
            ("truncated", "/truncated", b"", False),
            ("http-404", "/missing", b"", False),
        ]:
            target = Path(directory)/label/"episode.mp3"
            target.parent.mkdir()
            if seed: target.write_bytes(seed)
            requests.clear()
            result = subprocess.run(["mono",sys.argv[1],"http://127.0.0.1:%d%s"%(server.server_port,endpoint),str(target)],capture_output=True,text=True,timeout=20)
            assert (result.returncode == 0) == success, (label,result.stdout,result.stderr)
            if success: assert target.read_bytes() == PAYLOAD, label + " corrupted payload"
            else: assert not target.exists(), label + " left a failed download"
            if label in ("ignored-range","wrong-range","complete-partial-416"):
                assert len(requests)==2 and requests[-1][1]==0, (label,requests)
            print("PASS",label,result.stdout.strip())
        # A filesystem failure must also complete and report an actionable error.
        blocked = Path(directory)/"blocked"
        blocked.write_text("not a directory")
        result = subprocess.run(["mono",sys.argv[1],"http://127.0.0.1:%d/ok"%server.server_port,str(blocked/"episode.mp3")],capture_output=True,text=True,timeout=20)
        assert result.returncode == 1 and "written" in result.stdout, result.stdout
        print("PASS filesystem failure completes")
finally:
    server.shutdown()
