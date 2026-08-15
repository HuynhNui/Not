param(
    [Parameter(Mandatory = $true)]
    [string]$RunId,

    [float]$GameplayDurationSeconds = 600,
    [float]$MenuWarmupSeconds = 10,
    [float]$MenuMeasurementSeconds = 30,
    [float]$GameplayWarmupSeconds = 10,
    [float]$SampleIntervalSeconds = 1,
    [string]$PackageName = "com.mimicompany.truegate",
    [string]$AdbPath = "D:\PhanMem\UnityEditor\6000.4.2f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe",
    [string]$OutputRoot = "Assets\_Project\Documentation\Performance\Runs"
)

$ErrorActionPreference = "Stop"

function Invoke-Adb {
    param([Parameter(Mandatory = $true)][string[]]$AdbArguments)

    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    $output = & $AdbPath @AdbArguments 2>&1
    $exitCode = $LASTEXITCODE
    $ErrorActionPreference = $previousErrorActionPreference
    if ($exitCode -ne 0) {
        throw "adb $($AdbArguments -join ' ') failed: $($output -join [Environment]::NewLine)"
    }

    return $output
}

function Get-RemoteHash {
    param([string]$RemotePath)

    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    $hashOutput = & $AdbPath shell sha256sum $RemotePath 2>&1
    $exitCode = $LASTEXITCODE
    $ErrorActionPreference = $previousErrorActionPreference
    if ($exitCode -ne 0) {
        return $null
    }

    $line = $hashOutput | Select-Object -First 1
    if ($line -match '^([0-9a-fA-F]{64})') {
        return $Matches[1].ToLowerInvariant()
    }

    return $null
}

if (!(Test-Path -LiteralPath $AdbPath)) {
    throw "adb was not found at '$AdbPath'."
}

if ($RunId -notmatch '^[A-Za-z0-9_-]+$') {
    throw "RunId may contain only letters, numbers, '-' and '_'."
}

$deviceState = (Invoke-Adb -AdbArguments @("get-state") | Select-Object -First 1).Trim()
if ($deviceState -ne "device") {
    throw "ADB device is not authorized and ready. Current state: '$deviceState'."
}

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$outputDirectory = Join-Path $projectRoot (Join-Path $OutputRoot $RunId)
New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null

$remoteRoot = "/sdcard/Android/data/$PackageName/files/PerformanceBenchmark"
$remoteRun = "$remoteRoot/$RunId"
$remoteRequest = "$remoteRoot/benchmark_request.json"
$remoteSave = "/sdcard/Android/data/$PackageName/files/save.json"
$remoteSaveBackup = "/sdcard/Android/data/$PackageName/files/save.bak"
$localSave = Join-Path $outputDirectory "save_before.json"
$localSaveBackup = Join-Path $outputDirectory "save_before.bak"
$sourceCommit = (git -C $projectRoot rev-parse HEAD).Trim()

$request = [ordered]@{
    enabled = $true
    runId = $RunId
    sourceCommit = $sourceCommit
    benchmarkProfileId = "performance-baseline-full-meta"
    menuWarmupSeconds = $MenuWarmupSeconds
    menuMeasurementSeconds = $MenuMeasurementSeconds
    gameplayWarmupSeconds = $GameplayWarmupSeconds
    gameplayDurationSeconds = $GameplayDurationSeconds
    sampleIntervalSeconds = $SampleIntervalSeconds
    noGateBaseline = $true
    invulnerable = $true
    startingDamage = 3.0
    startingFireRate = 6.4
    startingMaxHp = 20.0
    startingProjectileCount = 3
    startingSquadSize = 4
    autoconnectProfiler = $false
    deepProfiling = $false
}

$requestPath = Join-Path $outputDirectory "benchmark_request.json"
$requestJson = $request | ConvertTo-Json -Depth 4
[IO.File]::WriteAllText($requestPath, $requestJson, [Text.UTF8Encoding]::new($false))

$preSaveHash = Get-RemoteHash $remoteSave
$preSaveBackupHash = Get-RemoteHash $remoteSaveBackup
if ($preSaveHash) {
    Invoke-Adb -AdbArguments @("pull", $remoteSave, $localSave) | Out-Null
}
if ($preSaveBackupHash) {
    Invoke-Adb -AdbArguments @("pull", $remoteSaveBackup, $localSaveBackup) | Out-Null
}

Invoke-Adb -AdbArguments @("shell", "am", "force-stop", $PackageName) | Out-Null
Invoke-Adb -AdbArguments @("shell", "mkdir", "-p", $remoteRoot) | Out-Null
Invoke-Adb -AdbArguments @("shell", "rm", "-f", "$remoteRun/benchmark_complete.marker", "$remoteRun/benchmark_failed.marker") | Out-Null
Invoke-Adb -AdbArguments @("push", $requestPath, $remoteRequest) | Out-Null
Invoke-Adb -AdbArguments @("logcat", "-c") | Out-Null
Invoke-Adb -AdbArguments @("shell", "monkey", "-p", $PackageName, "-c", "android.intent.category.LAUNCHER", "1") | Out-Null

$systemCsvPath = Join-Path $outputDirectory "Android_System_Memory_Thermal.csv"
[IO.File]::WriteAllText(
    $systemCsvPath,
    "elapsed_sec,total_pss_kb,total_rss_kb,battery_temp_c,battery_level,thermal_status,foreground_window`n",
    [Text.UTF8Encoding]::new($false))

$stopwatch = [Diagnostics.Stopwatch]::StartNew()
$timeoutSeconds = [Math]::Ceiling(
    $MenuWarmupSeconds + $MenuMeasurementSeconds + $GameplayDurationSeconds + 180)
$nextProgressSeconds = 0
$nextKeepAwakeSeconds = 0
$completionState = $null
$foregroundPreserved = $true

while ($stopwatch.Elapsed.TotalSeconds -lt $timeoutSeconds) {
    Start-Sleep -Seconds 5
    $elapsed = [Math]::Round($stopwatch.Elapsed.TotalSeconds, 1)
    if ($elapsed -ge $nextKeepAwakeSeconds) {
        Invoke-Adb -AdbArguments @("shell", "input", "tap", "1", "1") | Out-Null
        $nextKeepAwakeSeconds += 60
    }

    $markerCommand = "if [ -f '$remoteRun/benchmark_complete.marker' ]; then echo COMPLETE; elif [ -f '$remoteRun/benchmark_failed.marker' ]; then echo FAILED; else echo RUNNING; fi"
    $marker = (Invoke-Adb -AdbArguments @("shell", $markerCommand) | Select-Object -Last 1).Trim()

    $memInfo = Invoke-Adb -AdbArguments @("shell", "dumpsys", "meminfo", $PackageName)
    $memText = $memInfo -join "`n"
    $totalPssKb = ""
    $totalRssKb = ""
    if ($memText -match 'TOTAL PSS:\s*(\d+).*?TOTAL RSS:\s*(\d+)') {
        $totalPssKb = $Matches[1]
        $totalRssKb = $Matches[2]
    }
    elseif ($memInfo | Where-Object { $_ -match '^\s*TOTAL\s+(\d+)' } | Select-Object -First 1) {
        $totalLine = ($memInfo | Where-Object { $_ -match '^\s*TOTAL\s+' } | Select-Object -First 1)
        $parts = ($totalLine.Trim() -split '\s+')
        if ($parts.Count -gt 1) {
            $totalPssKb = $parts[1]
        }
    }

    $battery = Invoke-Adb -AdbArguments @("shell", "dumpsys", "battery")
    $temperatureRaw = (($battery | Where-Object { $_ -match '^\s*temperature:' } | Select-Object -First 1) -replace '.*temperature:\s*', '').Trim()
    $batteryLevel = (($battery | Where-Object { $_ -match '^\s*level:' } | Select-Object -First 1) -replace '.*level:\s*', '').Trim()
    $batteryTemperature = ""
    if ($temperatureRaw -match '^\d+$') {
        $batteryTemperature = ([int]$temperatureRaw / 10.0).ToString("0.0", [Globalization.CultureInfo]::InvariantCulture)
    }

    $thermalStatus = ""
    try {
        $thermal = Invoke-Adb -AdbArguments @("shell", "dumpsys", "thermalservice")
        $thermalLine = $thermal | Where-Object { $_ -match 'Thermal Status|mStatus' } | Select-Object -First 1
        if ($thermalLine) {
            $thermalStatus = ($thermalLine -replace '.*(?:Thermal Status|mStatus)[:=]\s*', '').Trim()
        }
    }
    catch {
        $thermalStatus = "unavailable"
    }

    $focusOutput = Invoke-Adb -AdbArguments @("shell", "dumpsys", "window")
    $focusLine = $focusOutput | Where-Object { $_ -match 'mCurrentFocus=' } | Select-Object -First 1
    $foregroundWindow = if ($focusLine) { $focusLine.Trim() } else { "unavailable" }
    if ($foregroundWindow -ne "unavailable" -and $foregroundWindow -notmatch [regex]::Escape($PackageName)) {
        $foregroundPreserved = $false
    }

    $safeForegroundWindow = $foregroundWindow -replace ',', ';'
    Add-Content -LiteralPath $systemCsvPath -Encoding UTF8 -Value "$elapsed,$totalPssKb,$totalRssKb,$batteryTemperature,$batteryLevel,$thermalStatus,$safeForegroundWindow"

    if ($elapsed -ge $nextProgressSeconds) {
        Write-Host "[$RunId] $elapsed sec: $marker"
        $nextProgressSeconds += 30
    }

    if ($marker -eq "COMPLETE" -or $marker -eq "FAILED") {
        $completionState = $marker
        break
    }
}

Invoke-Adb -AdbArguments @("shell", "am", "force-stop", $PackageName) | Out-Null

if (!$completionState) {
    $completionState = "TIMEOUT"
}

Invoke-Adb -AdbArguments @("pull", "$remoteRun/.", $outputDirectory) | Out-Null
Invoke-Adb -AdbArguments @("logcat", "-d", "-v", "threadtime") | Set-Content -LiteralPath (Join-Path $outputDirectory "logcat.txt") -Encoding UTF8

$postSaveHash = Get-RemoteHash $remoteSave
$postSaveBackupHash = Get-RemoteHash $remoteSaveBackup
$savePreserved = ($preSaveHash -eq $postSaveHash) -and ($preSaveBackupHash -eq $postSaveBackupHash)

if (!$savePreserved) {
    Invoke-Adb -AdbArguments @("shell", "am", "force-stop", $PackageName) | Out-Null
    if (Test-Path -LiteralPath $localSave) {
        Invoke-Adb -AdbArguments @("push", $localSave, $remoteSave) | Out-Null
    }
    if (Test-Path -LiteralPath $localSaveBackup) {
        Invoke-Adb -AdbArguments @("push", $localSaveBackup, $remoteSaveBackup) | Out-Null
    }
}

$verification = [ordered]@{
    runId = $RunId
    completionState = $completionState
    elapsedWallSeconds = [Math]::Round($stopwatch.Elapsed.TotalSeconds, 3)
    sourceCommit = $sourceCommit
    preSaveSha256 = $preSaveHash
    postSaveSha256 = $postSaveHash
    preSaveBackupSha256 = $preSaveBackupHash
    postSaveBackupSha256 = $postSaveBackupHash
    savePreserved = $savePreserved
    device = (Invoke-Adb -AdbArguments @("shell", "getprop", "ro.product.model") | Select-Object -First 1).Trim()
    androidVersion = (Invoke-Adb -AdbArguments @("shell", "getprop", "ro.build.version.release") | Select-Object -First 1).Trim()
    apiLevel = (Invoke-Adb -AdbArguments @("shell", "getprop", "ro.build.version.sdk") | Select-Object -First 1).Trim()
    physicalSize = (Invoke-Adb -AdbArguments @("shell", "wm", "size") | Select-Object -First 1).Trim()
    foregroundPreserved = $foregroundPreserved
}

$verificationPath = Join-Path $outputDirectory "Android_Run_Verification.json"
[IO.File]::WriteAllText(
    $verificationPath,
    ($verification | ConvertTo-Json -Depth 4),
    [Text.UTF8Encoding]::new($false))

if ($completionState -ne "COMPLETE") {
    throw "Benchmark '$RunId' did not complete successfully. State: $completionState."
}

if (!$foregroundPreserved) {
    throw "Benchmark '$RunId' completed but another Android window took foreground; results are not valid for aggregate reporting."
}

Write-Host "Benchmark '$RunId' complete. Output: $outputDirectory"
