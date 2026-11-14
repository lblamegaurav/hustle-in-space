#!/usr/bin/env python3
"""
Hustle in Space - Live Preview Server with Instant Hot-Reload
Zero-dependency Python 3 HTTP Server with Server-Sent Events (SSE) file watcher.

Usage:
    python3 live_server.py [port]
"""

import os
import sys
import time
import threading
import webbrowser
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer

PORT = int(sys.argv[1]) if len(sys.argv) > 1 else 3000
WORKSPACE_DIR = os.path.dirname(os.path.abspath(__file__))
WATCH_DIRS = [
    os.path.join(WORKSPACE_DIR, "live-preview"),
    os.path.join(WORKSPACE_DIR, "Assets"),
    os.path.join(WORKSPACE_DIR, "WHAT TO DO")
]

# Track connected SSE clients
clients = []
clients_lock = threading.Lock()

def get_dir_mtime():
    """Calculates max mtime of watched files."""
    max_mtime = 0
    for w_dir in WATCH_DIRS:
        if not os.path.exists(w_dir):
            continue
        for root, dirs, files in os.walk(w_dir):
            # Ignore hidden files, Library, and Git
            if ".git" in root or "Library" in root:
                continue
            for f in files:
                if f.startswith(".") or f.endswith(".tmp"):
                    continue
                path = os.path.join(root, f)
                try:
                    mt = os.path.getmtime(path)
                    if mt > max_mtime:
                        max_mtime = mt
                except OSError:
                    pass
    return max_mtime

def file_watcher_loop():
    """Background thread watching for changes and triggering reloads."""
    last_mtime = get_dir_mtime()
    while True:
        time.sleep(0.5)
        current_mtime = get_dir_mtime()
        if current_mtime > last_mtime:
            last_mtime = current_mtime
            print("\n🔄 [LIVE SERVER] File change detected! Notifying browser...")
            with clients_lock:
                for client in list(clients):
                    try:
                        client.wfile.write(b"data: reload\n\n")
                        client.wfile.flush()
                    except Exception:
                        if client in clients:
                            clients.remove(client)

class LiveRequestHandler(SimpleHTTPRequestHandler):
    """Custom HTTP handler with SSE live-reload and range request support."""
    
    def __init__(self, *args, **kwargs):
        super().__init__(*args, directory=WORKSPACE_DIR, **kwargs)

    def do_GET(self):
        # Default route serves live-preview/index.html
        if self.path == "/" or self.path == "/index.html":
            self.path = "/live-preview/index.html"
            return super().do_GET()

        # SSE Endpoint for hot-reloading
        if self.path == "/events":
            self.send_response(200)
            self.send_header("Content-Type", "text/event-stream")
            self.send_header("Cache-Control", "no-cache")
            self.send_header("Connection", "keep-alive")
            self.send_header("Access-Control-Allow-Origin", "*")
            self.end_headers()

            with clients_lock:
                clients.append(self)
                print(f"📡 [LIVE SERVER] Client connected to hot-reload stream ({len(clients)} active)")

            # Keep connection open
            try:
                while True:
                    time.sleep(15)
                    self.wfile.write(b": ping\n\n")
                    self.wfile.flush()
            except Exception:
                with clients_lock:
                    if self in clients:
                        clients.remove(self)
            return

        return super().do_GET()

    def end_headers(self):
        # Disable browser caching for live editing
        self.send_header("Cache-Control", "no-store, must-revalidate")
        super().end_headers()

    def log_message(self, format, *args):
        try:
            msg = format % args
            if "/events" not in msg and ".mp4" not in msg:
                sys.stdout.write(f"[{time.strftime('%H:%M:%S')}] {msg}\n")
                sys.stdout.flush()
        except Exception:
            pass

def main():
    print("=" * 65)
    print(" 🚀 HUSTLE IN SPACE - LIVE SIMULATION & 3D PALETTE SERVER")
    print("=" * 65)
    print(f" • Local URL:       http://localhost:{PORT}")
    print(f" • Root Workspace:  {WORKSPACE_DIR}")
    print(" • Hot-Reloading:   ENABLED (Watches Assets/, live-preview/, etc.)")
    print(" • Features:        Video Splash, 3D Saturn V & Space Shuttle,")
    print("                    Blender Orbit Camera, NASA Engineering Dossiers")
    print("=" * 65)
    print("Press Ctrl+C to stop the server anytime.\n")

    # Start file watcher
    watcher_thread = threading.Thread(target=file_watcher_loop, daemon=True)
    watcher_thread.start()

    # Open browser automatically after 0.5s
    def open_browser():
        time.sleep(0.6)
        webbrowser.open(f"http://localhost:{PORT}")
    threading.Thread(target=open_browser, daemon=True).start()

    # Start HTTP server
    server = ThreadingHTTPServer(("0.0.0.0", PORT), LiveRequestHandler)
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        print("\n🛑 Server stopped.")
        server.server_close()

if __name__ == "__main__":
    main()
