#!/bin/bash
# LLAssist local server — spec requires HTTP, not file://
# Prefers Vite dev server; falls back to static servers for dist/
set -e

if command -v npx &> /dev/null && [ -f "package.json" ]; then
  echo "Starting Vite dev server..."
  npx vite
elif command -v python3 &> /dev/null; then
  echo "Starting Python HTTP server on dist/..."
  cd dist && python3 -m http.server 8080
elif command -v npx &> /dev/null; then
  echo "Starting npx serve on dist/..."
  npx serve dist -l 8080
else
  echo "Error: No suitable HTTP server found. Install Node.js or Python 3."
  exit 1
fi
