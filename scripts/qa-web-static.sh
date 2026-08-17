#!/usr/bin/env bash
# Serve o frontend web como build estático, direto na porta fixa do
# qa-review (8081). Existe por dois motivos, os dois já vividos em rodadas
# anteriores de qa-review — não são hipotéticos:
#   1. `npm run web` (Metro/watch) estoura ENOSPC nesta máquina: o limite de
#      inotify watches do SO (`fs.inotify.max_user_watches`) já está tomado
#      por VS Code/C# Dev Kit. Sem watch, sem problema.
#   2. O build embute `EXPO_PUBLIC_API_URL` em tempo de export, não de
#      serve — por isso é passado aqui, apontando pro container da Api
#      subido por qa-up.sh (porta 8080), não pro `localhost:5005` do dev
#      local (`dotnet run` direto, fora de container).
# Roda em foreground — inicie em background (`run_in_background`) e derrube
# com scripts/qa-down.sh ao final.
# Uso: scripts/qa-web-static.sh [porta]
set -euo pipefail
cd "$(dirname "$0")/../frontend"

PORT="${1:-8081}"

rm -rf dist
EXPO_PUBLIC_API_URL="${EXPO_PUBLIC_API_URL:-http://localhost:${API_PORT:-8080}}" \
  npx expo export -p web

python3 - "$PORT" <<'PYEOF'
import http.server
import os
import socketserver
import sys

port = int(sys.argv[1])
os.chdir("dist")

class SpaHandler(http.server.SimpleHTTPRequestHandler):
    def do_GET(self):
        # Rotas dinâmicas do Expo Router (ex: /professor/[professorId]) não
        # têm arquivo próprio no export estático — cai pro index.html, que
        # resolve a rota no cliente.
        path = self.translate_path(self.path)
        if not os.path.exists(path) or os.path.isdir(path):
            self.path = "/index.html"
        return super().do_GET()

with socketserver.TCPServer(("", port), SpaHandler) as httpd:
    print(f"servindo frontend/dist em :{port}")
    httpd.serve_forever()
PYEOF
