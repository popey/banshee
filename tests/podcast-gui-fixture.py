"""One local test episode: 404 until the supplied marker file exists."""
import http.server
from pathlib import Path
import sys

media, marker = map(Path, sys.argv[1:3])
class Handler(http.server.BaseHTTPRequestHandler):
    def log_message(self, *args): pass
    def do_GET(self):
        if self.path == "/feed.xml":
            body = ('''<?xml version="1.0"?><rss version="2.0"><channel>
<title>Podcast recovery test</title><description>Local reliability fixture</description>
<link>http://127.0.0.1:8766/</link><item><title>Retry this episode</title>
<guid>preservation-retry-fixture-v1</guid><pubDate>Thu, 10 Sep 2026 00:00:00 GMT</pubDate>
<enclosure url="http://127.0.0.1:8766/episode.mp3" type="audio/mpeg" length="%d"/>
</item></channel></rss>''' % media.stat().st_size).encode()
            content_type = "application/rss+xml"
        elif self.path == "/episode.mp3" and marker.exists():
            body = media.read_bytes(); content_type = "audio/mpeg"
        else:
            self.send_error(404); return
        self.send_response(200)
        self.send_header("Content-Type",content_type)
        self.send_header("Content-Length",str(len(body)))
        self.end_headers(); self.wfile.write(body)
http.server.ThreadingHTTPServer(("127.0.0.1",8766),Handler).serve_forever()
