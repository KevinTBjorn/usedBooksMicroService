@echo off
echo Starting Docker services...

docker compose up --build -d
if errorlevel 1 (
  echo Docker failed. Make sure Docker Desktop is running.
  pause
  exit /b
)

echo Docker is running.
echo Starting Angular frontend...

cd AngularFrontend
if errorlevel 1 (
  echo Folder "AngularFrontend" not found.
  pause
  exit /b
)


npx ng serve

pause

