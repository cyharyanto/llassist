@echo off
REM LLAssist local server — spec requires HTTP, not file://
where npx >nul 2>nul
if %errorlevel%==0 (
  if exist package.json (
    echo Starting Vite dev server...
    npx vite
    exit /b
  )
)
where python >nul 2>nul
if %errorlevel%==0 (
  echo Starting Python HTTP server on dist/...
  cd dist && python -m http.server 8080
  exit /b
)
echo Error: No suitable HTTP server found. Install Node.js or Python 3.
exit /b 1
