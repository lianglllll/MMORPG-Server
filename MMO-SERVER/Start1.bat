@echo off
setlocal enabledelayedexpansion

goto :main

rem 函数：启动程序并记录PID
:StartAndRecordPID
    echo Starting program in directory: %~dp0%~1
    cd /d "%~dp0%~1" || (echo Failed to change directory & exit /b)
    if not exist "%~2" (
        echo File not found: %~2
        exit /b
    )
    echo Current directory: %CD%
    start "%~3" "%~2" %~4  %~5  %~6  %~7  %~8  %~9
    echo Started program: %~2 with parameters: %~4 %~5 %~6 %~7 %~8 %~9
    rem timeout /t %DELAY% /nobreak >nul
    for /f "tokens=2 delims=," %%A in ('tasklist /fi "imagename eq %~2" /fo csv /nh') do (
        echo %%~A >> "%~dp0pids.txt"
    )
    exit /b

:main

rem 定义启动间隔（单位为s）
set DELAY=0.1

rem 定义所有服务器的目录路径（相对于脚本所在位置）
set "CONTROL_CENTER_DIR=ControlCenter\bin\Debug\net6.0"
set "DB_PROXY_SERVER_DIR=DBProxyServer\bin\Debug\net6.0"
set "LOGIN_GATE_MGR_SERVER_DIR=LoginGateMgrServer\bin\Debug\net6.0"
set "LOGIN_GATE_SERVER_DIR=LoginGateServer\bin\Debug\net6.0"
set "LOGIN_SERVER_DIR=LoginServer\bin\Debug\net6.0"
set "GAME_GATE_MGR_SERVER_DIR=GameGateMgrServer\bin\Debug\net6.0"
set "GAME_GATE_SERVER_DIR=GameGateServer\bin\Debug\net6.0"
set "GAME_SERVER_DIR=GameServer\bin\Debug\net6.0"
set "SPACE_SERVER_DIR=SceneServer\bin\Debug\net6.0"

rem 删除旧的 pids.txt 文件
del "%~dp0pids.txt" >nul 2>&1

rem 启动每个服务并记录其 PID
echo CONTROL_CENTER_DIR=%CONTROL_CENTER_DIR%
call :StartAndRecordPID "%CONTROL_CENTER_DIR%" "ControlCenter.exe" "ControlCenter"
call :StartAndRecordPID "%DB_PROXY_SERVER_DIR%" "DBProxyServer.exe" "DBProxyServer"
call :StartAndRecordPID "%LOGIN_GATE_MGR_SERVER_DIR%" "LoginGateMgrServer.exe" "LoginGateMgrServer"
call :StartAndRecordPID "%LOGIN_GATE_SERVER_DIR%" "LoginGateServer.exe" "LoginGateServer"
call :StartAndRecordPID "%LOGIN_SERVER_DIR%" "LoginServer.exe" "LoginServer"
call :StartAndRecordPID "%GAME_GATE_MGR_SERVER_DIR%" "GameGateMgrServer.exe" "GameGateMgrServer"
call :StartAndRecordPID "%GAME_GATE_SERVER_DIR%" "GameGateServer.exe" "GameGateServer"
call :StartAndRecordPID "%GAME_SERVER_DIR%" "GameServer.exe" "GameServer"
call :StartAndRecordPID "%SPACE_SERVER_DIR%" "SceneServer.exe" "SceneServer1" "-config" "config.yaml"
call :StartAndRecordPID "%SPACE_SERVER_DIR%" "SceneServer.exe" "SceneServer2" "-config" "config2.yaml"

endlocal

pause