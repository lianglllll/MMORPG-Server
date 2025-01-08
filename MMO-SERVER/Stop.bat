@echo off
setlocal

rem 定义 pids.txt 文件路径
set "PIDS_FILE=%~dp0pids.txt"

rem 检查 pids.txt 是否存在
if not exist "%PIDS_FILE%" (
    echo File not found: %PIDS_FILE%
    exit /b
)

rem 逐行读取文件并终止每个 PID 对应的进程
for /f "usebackq delims=" %%A in ("%PIDS_FILE%") do (
    echo Terminating process with PID: %%A
    taskkill /PID %%A /F >nul 2>&1
    if errorlevel 1 (
        echo Failed to terminate PID %%A or it might already be closed.
    ) else (
        echo Successfully terminated PID %%A.
    )
)

rem 删除 pids.txt 文件
del "%PIDS_FILE%" >nul 2>&1

endlocal