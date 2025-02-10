# 设置启动间隔（秒）
$delay = 1

# 定义所有服务器的目录路径（相对于脚本所在位置）
$baseDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$controlCenterDir = Join-Path $baseDir "ControlCenter\bin\Debug\net6.0"
$dbProxyServerDir = Join-Path $baseDir "DBProxyServer\bin\Debug\net6.0"
$loginGateMgrServerDir = Join-Path $baseDir "LoginGateMgrServer\bin\Debug\net6.0"
$loginGateServerDir = Join-Path $baseDir "LoginGateServer\bin\Debug\net6.0"
$loginServerDir = Join-Path $baseDir "LoginServer\bin\Debug\net6.0"
$gameGateMgrServerDir = Join-Path $baseDir "GameGateMgrServer\bin\Debug\net6.0"
$gameGateServerDir = Join-Path $baseDir "GameGateServer\bin\Debug\net6.0"
$gameServerDir = Join-Path $baseDir "GameServer\bin\Debug\net6.0"
$spaceServerDir = Join-Path $baseDir "SceneServer\bin\Debug\net6.0"

# 删除旧的 pids.txt 文件
$pidsFile = Join-Path $baseDir "pids.txt"
Remove-Item -Path $pidsFile -ErrorAction SilentlyContinue

Function Start-AndRecordPID {
    param (
        [string]$targetDirectory,
        [string]$executableName,
        [string]$displayName
    )
    
    if (-Not (Test-Path $targetDirectory)) {
        Write-Host "Failed to change directory to $targetDirectory" -ForegroundColor Red
        return
    }

    if (-Not (Test-Path (Join-Path $targetDirectory $executableName))) {
        Write-Host "File not found: $executableName" -ForegroundColor Red
        return
    }
    
    Push-Location $targetDirectory
    try {
        Write-Host "Starting program in directory: $targetDirectory"
        
        # 使用 wt 启动程序并设置窗口标题
        Start-Process -FilePath "wt.exe" -ArgumentList @("new-tab", "--title", $displayName, "powershell", "-NoExit", "-Command", ".\$executableName")

        Start-Sleep -Seconds $delay
        
        $process = Get-Process | Where-Object { $_.Path -eq (Join-Path $targetDirectory $executableName) } | Select-Object -First 1
        if ($process) {
            Add-Content -Path $pidsFile -Value $process.Id
            Write-Host "Started program: $executableName with PID $($process.Id)"
        }
    } finally {
        Pop-Location
    }
}

# 启动每个服务并记录其 PID
Start-AndRecordPID -targetDirectory $controlCenterDir -executableName "ControlCenter.exe" -displayName "ControlCenter"
Start-AndRecordPID -targetDirectory $dbProxyServerDir -executableName "DBProxyServer.exe" -displayName "DBProxyServer"
Start-AndRecordPID -targetDirectory $loginGateMgrServerDir -executableName "LoginGateMgrServer.exe" -displayName "LoginGateMgrServer"
Start-AndRecordPID -targetDirectory $loginGateServerDir -executableName "LoginGateServer.exe" -displayName "LoginGateServer"
Start-AndRecordPID -targetDirectory $loginServerDir -executableName "LoginServer.exe" -displayName "LoginServer"
Start-AndRecordPID -targetDirectory $gameGateMgrServerDir -executableName "GameGateMgrServer.exe" -displayName "GameGateMgrServer"
Start-AndRecordPID -targetDirectory $gameGateServerDir -executableName "GameGateServer.exe" -displayName "GameGateServer"
Start-AndRecordPID -targetDirectory $gameServerDir -executableName "GameServer.exe" -displayName "GameServer"
Start-AndRecordPID -targetDirectory $spaceServerDir -executableName "SceneServer.exe" -displayName "SceneServer"