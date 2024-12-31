@echo off
setlocal

set DELAY=2

set CONTROL_CENTER_DIR="ControlCenter\bin\Debug\net6.0"
set DB_PROXY_SERVER_DIR="DBProxyServer\bin\Debug\net6.0"
set GAME_GATE_MGR_SERVER_DIR="GameGateMgrServer\bin\Debug\net6.0"
set GAME_GATE_SERVER_DIR="GameGateServer\bin\Debug\net6.0"
set GAME_SERVER_DIR="GameServer\bin\Debug\net6.0"
set LOGIN_GATE_MGR_SERVER_DIR="LoginGateMgrServer\bin\Debug\net6.0"
set LOGIN_GATE_SERVER_DIR="LoginGateServer\bin\Debug\net6.0"
set LOGIN_SERVER_DIR="LoginServer\bin\Debug\net6.0"
set SPACE_SERVER_DIR="SpaceServer\bin\Debug\net6.0"

cd /d %CONTROL_CENTER_DIR%
start "" "ControlCenter.exe"
timeout /t %DELAY% /nobreak > nul

cd /d "%~dp0%DB_PROXY_SERVER_DIR%"
start "" "DBProxyServer.exe"
timeout /t %DELAY% /nobreak > nul

cd /d "%~dp0%GAME_GATE_MGR_SERVER_DIR%"
start "" "GameGateMgrServer.exe"
timeout /t %DELAY% /nobreak > nul

cd /d "%~dp0%GAME_GATE_SERVER_DIR%"
start "" "GameGateServer.exe"
timeout /t %DELAY% /nobreak > nul

cd /d "%~dp0%GAME_SERVER_DIR%"
start "" "GameServer.exe"
timeout /t %DELAY% /nobreak > nul

cd /d "%~dp0%LOGIN_GATE_MGR_SERVER_DIR%"
start "" "LoginGateMgrServer.exe"
timeout /t %DELAY% /nobreak > nul

cd /d "%~dp0%LOGIN_GATE_SERVER_DIR%"
start "" "LoginGateServer.exe"
timeout /t %DELAY% /nobreak > nul

cd /d "%~dp0%LOGIN_SERVER_DIR%"
start "" "LoginServer.exe"
timeout /t %DELAY% /nobreak > nul

cd /d "%~dp0%SPACE_SERVER_DIR%"
start "" "SceneServer.exe"

endlocal