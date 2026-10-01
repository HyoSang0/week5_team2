# Unity CI build pipeline script.
# Implements docs/CONTRACT.md sections 1, 2, 4, 5, 6, 7 and 8.
# Target: Windows PowerShell 5.1. ASCII only.

param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('Build', 'Finalize')]
    [string]$Mode,

    [switch]$Clean,

    [ValidateSet('success', 'failure', 'cancelled')]
    [string]$JobStatus = 'failure'
)

$ErrorActionPreference = 'Stop'

# ---------------------------------------------------------------------------
# Script scope state
# ---------------------------------------------------------------------------

$script:MainLogFile = $null
$script:Record = $null
$script:RecordFile = $null
$script:StartedUtc = $null
$script:ExitCode = 0
$script:BuildsDir = $null
$script:LogsDir = $null
$script:ArtifactsDir = $null
$script:MachineCfg = $null
$script:ProjectCfg = $null
$script:RcloneConf = $null

# ---------------------------------------------------------------------------
# Generic helpers
# ---------------------------------------------------------------------------

function New-Utf8NoBomEncoding {
    New-Object System.Text.UTF8Encoding $false
}

function Write-JsonFileAtomic {
    # Writes JSON records atomically as UTF-8 without BOM (tmp file + move).
    param([string]$Path, [object]$Object)
    $json = ConvertTo-Json -InputObject $Object -Depth 8
    $tmpPath = "$Path.tmp"
    [System.IO.File]::WriteAllText($tmpPath, $json, (New-Utf8NoBomEncoding))
    Move-Item -Force -Path $tmpPath -Destination $Path
}

function Read-JsonConfig {
    param([string]$Path)
    if ([string]::IsNullOrEmpty($Path) -or -not (Test-Path -LiteralPath $Path)) { return $null }
    try {
        return Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json
    } catch {
        return $null
    }
}

function Get-ConfigValue {
    param($Config, [string]$Name, $Default)
    if ($null -ne $Config) {
        $prop = $Config.PSObject.Properties[$Name]
        if ($null -ne $prop -and $null -ne $prop.Value) { return $prop.Value }
    }
    return $Default
}

function ConvertTo-BoolSafe {
    param($Value, [bool]$Default)
    if ($null -eq $Value) { return $Default }
    if ($Value -is [bool]) { return $Value }
    if ($Value -is [string]) { return ($Value.Trim().ToLowerInvariant() -eq 'true') }
    return [bool]$Value
}

function ConvertTo-IntSafe {
    param($Value, [int]$Default)
    if ($null -eq $Value) { return $Default }
    try { return [int]$Value } catch { return $Default }
}

function ConvertTo-SafeName {
    # Replaces every char outside [A-Za-z0-9._-] with '-'.
    param([string]$Value)
    if ([string]::IsNullOrEmpty($Value)) { return '' }
    return [regex]::Replace($Value, '[^A-Za-z0-9._-]', '-')
}

function Get-EnvDefault {
    param([string]$Name, [string]$Default)
    $v = [Environment]::GetEnvironmentVariable($Name)
    if ([string]::IsNullOrEmpty($v)) { return $Default }
    return $v
}

function Quote-Arg {
    # Quotes an argument for Start-Process when it contains spaces.
    param([string]$Value)
    if ($Value -match '\s') { return ('"' + $Value + '"') }
    return $Value
}

# ---------------------------------------------------------------------------
# CI log helpers
# ---------------------------------------------------------------------------

function Write-CiLog {
    # Writes a pipeline message to stdout and appends it, prefixed with "[ci] ",
    # to the main build log file.
    param([string]$Message)
    $line = "[ci] $Message"
    Write-Host $line
    if (-not [string]::IsNullOrEmpty($script:MainLogFile)) {
        try {
            $logDir = Split-Path -Parent $script:MainLogFile
            if (-not [string]::IsNullOrEmpty($logDir) -and -not (Test-Path -LiteralPath $logDir)) {
                New-Item -ItemType Directory -Force -Path $logDir | Out-Null
            }
            [System.IO.File]::AppendAllText($script:MainLogFile, $line + "`r`n", (New-Utf8NoBomEncoding))
        } catch { }
    }
}

function Read-LogChunk {
    # Reads newly appended bytes of the Unity log (FileShare ReadWrite so the
    # writer keeps the handle), appends them to the main log and mirrors them
    # to stdout. Returns the new offset.
    param([string]$Path, [long]$Offset)
    if ([string]::IsNullOrEmpty($Path) -or -not (Test-Path -LiteralPath $Path)) { return $Offset }
    $bytesRead = 0
    $buffer = $null
    try {
        $stream = New-Object System.IO.FileStream($Path, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
        try {
            if ($stream.Length -le $Offset) { return $Offset }
            $stream.Position = $Offset
            $available = [long]($stream.Length - $Offset)
            $toRead = [int][Math]::Min($available, 1048576)
            $buffer = New-Object byte[] $toRead
            while ($bytesRead -lt $toRead) {
                $n = $stream.Read($buffer, $bytesRead, $toRead - $bytesRead)
                if ($n -le 0) { break }
                $bytesRead += $n
            }
        } finally {
            $stream.Dispose()
        }
    } catch {
        return $Offset
    }
    if ($bytesRead -le 0) { return $Offset }
    $text = [System.Text.Encoding]::UTF8.GetString($buffer, 0, $bytesRead)
    if (-not [string]::IsNullOrEmpty($script:MainLogFile)) {
        try {
            [System.IO.File]::AppendAllText($script:MainLogFile, $text, (New-Utf8NoBomEncoding))
        } catch { }
    }
    Write-Host -NoNewline $text
    return ($Offset + $bytesRead)
}

# ---------------------------------------------------------------------------
# Build record helpers
# ---------------------------------------------------------------------------

function New-DefaultRecord {
    param([string]$Id, [string]$Project)
    [pscustomobject]@{
        id = $Id
        project = $Project
        repo = ''
        branch = ''
        commit = ''
        commitShort = ''
        commitMessage = ''
        author = ''
        trigger = ''
        runId = ''
        runNumber = 0
        runAttempt = 1
        runUrl = $null
        version = ''
        versionLabel = ''
        status = 'running'
        stage = 'prepare'
        clean = $false
        startedAt = ''
        finishedAt = $null
        durationSec = 0
        unityPid = 0
        logFile = ''
        artifactName = ''
        artifactPath = $null
        artifactSizeBytes = 0
        drivePath = $null
        driveLink = $null
        error = $null
    }
}

function Merge-RecordWithDefaults {
    # Normalizes a record read from disk so every contract field exists.
    param($Existing, [string]$Id, [string]$Project)
    $merged = New-DefaultRecord -Id $Id -Project $Project
    if ($null -ne $Existing) {
        foreach ($p in $Existing.PSObject.Properties) {
            $target = $merged.PSObject.Properties[$p.Name]
            if ($null -ne $target) { $target.Value = $p.Value }
        }
    }
    return $merged
}

function Save-Record {
    if ($null -eq $script:Record -or [string]::IsNullOrEmpty($script:RecordFile)) { return }
    Write-JsonFileAtomic -Path $script:RecordFile -Object $script:Record
}

function Complete-RecordTiming {
    $end = (Get-Date).ToUniversalTime()
    if ($null -ne $script:Record) {
        $script:Record.finishedAt = $end.ToString('o')
    }
    $start = $script:StartedUtc
    $startedStr = ''
    if ($null -ne $script:Record) {
        $prop = $script:Record.PSObject.Properties['startedAt']
        if ($null -ne $prop) { $startedStr = [string]$prop.Value }
    }
    if (-not [string]::IsNullOrEmpty($startedStr)) {
        try {
            $parsed = [DateTime]::Parse($startedStr, [System.Globalization.CultureInfo]::InvariantCulture, [System.Globalization.DateTimeStyles]::RoundtripKind)
            $start = $parsed.ToUniversalTime()
        } catch { }
    }
    if ($null -ne $start -and $null -ne $script:Record) {
        $duration = [int]($end - $start).TotalSeconds
        if ($duration -lt 0) { $duration = 0 }
        $script:Record.durationSec = $duration
    }
}

# ---------------------------------------------------------------------------
# Sheet report (section 8)
# ---------------------------------------------------------------------------

function Get-GoogleAccessToken {
    # Returns a fresh Google OAuth access token from the rclone Drive remote, or ''.
    # Apps Script web apps deployed with access "Anyone with a Google account"
    # reject anonymous calls (401) but accept a bearer token with drive scope.
    $remote = [string]$script:MachineCfg.rcloneRemote
    if ([string]::IsNullOrEmpty($remote) -or -not (Test-Path -LiteralPath $script:RcloneConf)) { return '' }
    $oldEap = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        # Any API call refreshes an expired token and rewrites rclone.conf.
        $null = & $script:MachineCfg.rcloneExe about ($remote + ':') --config $script:RcloneConf 2>$null
    } catch { }
    $ErrorActionPreference = $oldEap
    $inSection = $false
    foreach ($line in [System.IO.File]::ReadAllLines($script:RcloneConf)) {
        if ($line -match '^\s*\[(.+)\]\s*$') { $inSection = ($Matches[1] -eq $remote); continue }
        if ($inSection -and $line -match '^\s*token\s*=\s*(\{.*\})\s*$') {
            try { return [string](ConvertFrom-Json $Matches[1]).access_token } catch { return '' }
        }
    }
    return ''
}

function Send-SheetReport {
    # POSTs the build report to the sheet webhook. Never fails the build.
    if ($null -eq $script:Record) { return }
    if (-not (ConvertTo-BoolSafe $script:ProjectCfg.reportToSheet $true)) { return }
    $url = [string]$script:MachineCfg.sheetWebhookUrl
    if ([string]::IsNullOrEmpty($url)) { return }
    try {
        [Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12
        $body = [pscustomobject]@{
            token = [string]$script:MachineCfg.sheetToken
            project = [string]$script:Record.project
            timestamp = (Get-Date).ToUniversalTime().ToString('o')
            version = [string]$script:Record.version
            versionLabel = [string]$script:Record.versionLabel
            branch = [string]$script:Record.branch
            commit = [string]$script:Record.commitShort
            author = [string]$script:Record.author
            message = [string]$script:Record.commitMessage
            status = [string]$script:Record.status
            durationSec = [int](ConvertTo-IntSafe $script:Record.durationSec 0)
            driveLink = $script:Record.driveLink
            runUrl = $script:Record.runUrl
        }
        $json = ConvertTo-Json -InputObject $body -Depth 4
        $bodyBytes = [System.Text.Encoding]::UTF8.GetBytes($json)
        $headers = @{}
        $accessToken = Get-GoogleAccessToken
        if (-not [string]::IsNullOrEmpty($accessToken)) { $headers['Authorization'] = 'Bearer ' + $accessToken }
        $response = Invoke-WebRequest -Uri $url -Method Post -Headers $headers -ContentType 'application/json' -Body $bodyBytes -UseBasicParsing -TimeoutSec 30
        if ($response.Content -match '<title>Authorization needed') {
            Write-CiLog 'WARNING: sheet report failed: the Apps Script web app is not authorized yet. Open the web app URL in a browser once and approve the permissions.'
            return
        }
        try {
            $respObj = $response.Content | ConvertFrom-Json
            if ($null -ne $respObj -and $respObj.ok -ne $true) {
                Write-CiLog ("WARNING: sheet report rejected: {0}" -f $respObj.error)
                return
            }
        } catch { }
        Write-CiLog "Sheet report sent"
    } catch {
        Write-CiLog ("WARNING: sheet report failed: {0}" -f $_.Exception.Message)
    }
}

# ---------------------------------------------------------------------------
# Retention (section 6, package stage)
# ---------------------------------------------------------------------------

function Invoke-ArtifactRetention {
    # Among records of the same project with an existing artifactPath, keeps
    # the newest $Keep artifact folders and deletes the older ones.
    param([string]$ProjectName, [int]$Keep, [string]$CurrentBuildId)
    if ($Keep -lt 0) { return }
    if ([string]::IsNullOrEmpty($script:BuildsDir) -or -not (Test-Path -LiteralPath $script:BuildsDir)) { return }
    $candidates = @()
    $recordFiles = @(Get-ChildItem -LiteralPath $script:BuildsDir -Filter '*.json' -File -ErrorAction SilentlyContinue)
    foreach ($file in $recordFiles) {
        $r = $null
        try { $r = Get-Content -LiteralPath $file.FullName -Raw -Encoding UTF8 | ConvertFrom-Json } catch { continue }
        if ($null -eq $r) { continue }
        if ([string]$r.project -ne $ProjectName) { continue }
        $artifactPath = ''
        $prop = $r.PSObject.Properties['artifactPath']
        if ($null -ne $prop) { $artifactPath = [string]$prop.Value }
        if ([string]::IsNullOrEmpty($artifactPath)) { continue }
        $folder = Split-Path -Parent $artifactPath
        if ([string]::IsNullOrEmpty($folder) -or -not (Test-Path -LiteralPath $folder)) { continue }
        $startedAt = ''
        $prop2 = $r.PSObject.Properties['startedAt']
        if ($null -ne $prop2) { $startedAt = [string]$prop2.Value }
        $id = ''
        $prop3 = $r.PSObject.Properties['id']
        if ($null -ne $prop3) { $id = [string]$prop3.Value }
        $candidates += [pscustomobject]@{ StartedAt = $startedAt; Folder = $folder; Id = $id }
    }
    if ($candidates.Count -le $Keep) { return }
    $sorted = @($candidates | Sort-Object -Property StartedAt -Descending)
    $old = @($sorted | Select-Object -Skip $Keep)
    $artifactsFull = [System.IO.Path]::GetFullPath($script:ArtifactsDir)
    foreach ($item in $old) {
        if ($item.Id -eq $CurrentBuildId) { continue }
        try { $folderFull = [System.IO.Path]::GetFullPath($item.Folder) } catch { continue }
        if (-not $folderFull.StartsWith($artifactsFull, [System.StringComparison]::OrdinalIgnoreCase)) { continue }
        Write-CiLog ("Retention: removing old artifact folder {0}" -f $item.Folder)
        try {
            Remove-Item -LiteralPath $folderFull -Recurse -Force
        } catch {
            Write-CiLog ("WARNING: retention could not remove {0}: {1}" -f $item.Folder, $_.Exception.Message)
        }
    }
}

# ---------------------------------------------------------------------------
# Unity project helpers (sections 4 and 7)
# ---------------------------------------------------------------------------

function Get-UnityProjectVersion {
    # Reads "m_EditorVersion: <ver>" from ProjectSettings/ProjectVersion.txt.
    param([string]$UnityProjectDir)
    $versionFile = Join-Path $UnityProjectDir 'ProjectSettings\ProjectVersion.txt'
    if (Test-Path -LiteralPath $versionFile) {
        $lines = @(Get-Content -LiteralPath $versionFile -ErrorAction SilentlyContinue)
        foreach ($line in $lines) {
            if ("$line" -match '^\s*m_EditorVersion\s*:\s*(.+?)\s*$') { return $Matches[1] }
        }
    }
    return ''
}

# ---------------------------------------------------------------------------
# Resolve repo, CI home and configuration (sections 1, 2, 4)
# ---------------------------------------------------------------------------

$RepoRoot = Split-Path -Parent $PSScriptRoot

$CiHome = Get-EnvDefault 'UNITY_CI_HOME' 'C:\UnityCI'
$CiHome = [System.IO.Path]::GetFullPath($CiHome)

$BuildsDir = Join-Path $CiHome 'data\builds'
$LogsDir = Join-Path $CiHome 'data\logs'
$ArtifactsDir = Join-Path $CiHome 'data\artifacts'
$WorkBaseDir = Join-Path $CiHome 'data\work'
$RcloneConf = Join-Path $CiHome 'rclone.conf'
$script:BuildsDir = $BuildsDir
$script:LogsDir = $LogsDir
$script:ArtifactsDir = $ArtifactsDir
$script:RcloneConf = $RcloneConf

$machineRaw = Read-JsonConfig (Join-Path $CiHome 'config.json')
$MachineCfg = [pscustomobject]@{
    rcloneExe = [string](Get-ConfigValue $machineRaw 'rcloneExe' 'rclone')
    rcloneRemote = [string](Get-ConfigValue $machineRaw 'rcloneRemote' 'gdrive')
    driveRootFolder = [string](Get-ConfigValue $machineRaw 'driveRootFolder' 'UnityBuilds')
    sheetWebhookUrl = [string](Get-ConfigValue $machineRaw 'sheetWebhookUrl' '')
    sheetToken = [string](Get-ConfigValue $machineRaw 'sheetToken' '')
    unityHubEditorRoot = [string](Get-ConfigValue $machineRaw 'unityHubEditorRoot' 'C:\Program Files\Unity\Hub\Editor')
    localArtifactRetention = (ConvertTo-IntSafe (Get-ConfigValue $machineRaw 'localArtifactRetention' 20) 20)
    dashboardHost = [string](Get-ConfigValue $machineRaw 'dashboardHost' '127.0.0.1')
    dashboardPort = (ConvertTo-IntSafe (Get-ConfigValue $machineRaw 'dashboardPort' 8787) 8787)
}
$script:MachineCfg = $MachineCfg

$projectRaw = Read-JsonConfig (Join-Path $RepoRoot '.ci\ci.config.json')
$ProjectCfg = [pscustomobject]@{
    projectName = [string](Get-ConfigValue $projectRaw 'projectName' '')
    unityProjectPath = [string](Get-ConfigValue $projectRaw 'unityProjectPath' '.')
    unityVersion = [string](Get-ConfigValue $projectRaw 'unityVersion' '')
    unityExe = [string](Get-ConfigValue $projectRaw 'unityExe' '')
    executableName = [string](Get-ConfigValue $projectRaw 'executableName' 'MyGame')
    versionPrefix = [string](Get-ConfigValue $projectRaw 'versionPrefix' '0.1')
    development = (ConvertTo-BoolSafe (Get-ConfigValue $projectRaw 'development' $true) $true)
    uploadToDrive = (ConvertTo-BoolSafe (Get-ConfigValue $projectRaw 'uploadToDrive' $true) $true)
    reportToSheet = (ConvertTo-BoolSafe (Get-ConfigValue $projectRaw 'reportToSheet' $true) $true)
}
$script:ProjectCfg = $ProjectCfg

if ([string]::IsNullOrEmpty($ProjectCfg.projectName)) {
    throw ("projectName is missing in {0}\.ci\ci.config.json" -f $RepoRoot)
}
$projectName = $ProjectCfg.projectName

# ---------------------------------------------------------------------------
# GitHub environment values (section 5/6)
# ---------------------------------------------------------------------------

$branch = Get-EnvDefault 'GITHUB_REF_NAME' 'unknown'
$sha = Get-EnvDefault 'GITHUB_SHA' ''
$actor = Get-EnvDefault 'GITHUB_ACTOR' 'unknown'
$eventName = Get-EnvDefault 'GITHUB_EVENT_NAME' 'manual'
$runId = Get-EnvDefault 'GITHUB_RUN_ID' ([DateTime]::UtcNow.ToString('yyyyMMddHHmmss'))
$runNumber = ConvertTo-IntSafe (Get-EnvDefault 'GITHUB_RUN_NUMBER' '0') 0
$runAttempt = ConvertTo-IntSafe (Get-EnvDefault 'GITHUB_RUN_ATTEMPT' '1') 1
$repoFull = Get-EnvDefault 'GITHUB_REPOSITORY' ''
$serverUrl = Get-EnvDefault 'GITHUB_SERVER_URL' 'https://github.com'

$branchSafe = ConvertTo-SafeName $branch
$commitShort = $sha
if ($commitShort.Length -ge 7) { $commitShort = $commitShort.Substring(0, 7) }
$version = "{0}.{1}" -f $ProjectCfg.versionPrefix, $runNumber
$versionLabel = "{0}-{1}-{2}" -f $version, $branchSafe, $commitShort
$artifactName = "{0}_{1}.zip" -f $projectName, $versionLabel
$buildId = ConvertTo-SafeName ("{0}-{1}-{2}" -f $projectName, $runId, $runAttempt)

$runUrl = $null
if (-not [string]::IsNullOrEmpty($repoFull)) {
    $runUrl = "{0}/{1}/actions/runs/{2}" -f $serverUrl, $repoFull, $runId
}

# Commit message via git (first line of the subject).
$commitMessage = ''
if ($Mode -eq 'Build') {
    $oldEap = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $gitLines = @(& git -C $RepoRoot log -1 --format=%s 2>$null)
        if ($LASTEXITCODE -eq 0 -and $gitLines.Count -gt 0) {
            $joined = ($gitLines | ForEach-Object { [string]$_ }) -join ' '
            $commitMessage = $joined.Trim()
        }
    } catch {
        $commitMessage = ''
    }
    $ErrorActionPreference = $oldEap
}

# ---------------------------------------------------------------------------
# Per-build paths (section 1)
# ---------------------------------------------------------------------------

$RecordFile = Join-Path $BuildsDir ("{0}.json" -f $buildId)
$MainLogFilePath = Join-Path $LogsDir ("{0}.log" -f $buildId)
$UnityLogFilePath = Join-Path $LogsDir ("{0}.unity.log" -f $buildId)
$WorkDir = Join-Path $WorkBaseDir $buildId
$script:RecordFile = $RecordFile

$unityProjectRel = $ProjectCfg.unityProjectPath
if ([string]::IsNullOrEmpty($unityProjectRel)) { $unityProjectRel = '.' }
if ([System.IO.Path]::IsPathRooted($unityProjectRel)) {
    $UnityProjectDir = $unityProjectRel
} else {
    $UnityProjectDir = Join-Path $RepoRoot $unityProjectRel
}

# ---------------------------------------------------------------------------
# Main flow
# ---------------------------------------------------------------------------

try {
    if ($Mode -eq 'Build') {
        # ============================ BUILD ============================
        $script:StartedUtc = (Get-Date).ToUniversalTime()

        # ----- stage: prepare -----
        New-Item -ItemType Directory -Force -Path $BuildsDir, $LogsDir, $ArtifactsDir, $WorkDir | Out-Null
        $script:MainLogFile = $MainLogFilePath

        $script:Record = [pscustomobject]@{
            id = $buildId
            project = $projectName
            repo = $repoFull
            branch = $branch
            commit = $sha
            commitShort = $commitShort
            commitMessage = $commitMessage
            author = $actor
            trigger = $eventName
            runId = $runId
            runNumber = $runNumber
            runAttempt = $runAttempt
            runUrl = $runUrl
            version = $version
            versionLabel = $versionLabel
            status = 'running'
            stage = 'prepare'
            clean = [bool]$Clean
            startedAt = $script:StartedUtc.ToString('o')
            finishedAt = $null
            durationSec = 0
            unityPid = 0
            logFile = $MainLogFilePath
            artifactName = $artifactName
            artifactPath = $null
            artifactSizeBytes = 0
            drivePath = $null
            driveLink = $null
            error = $null
        }
        Save-Record
        Write-CiLog ("Build {0} started (project {1}, version {2}, branch {3})" -f $buildId, $projectName, $versionLabel, $branch)
        Write-CiLog ("CI home: {0}, repo root: {1}" -f $CiHome, $RepoRoot)

        # Resolve the Unity executable (section 4).
        $unityExe = $ProjectCfg.unityExe
        if ([string]::IsNullOrEmpty($unityExe)) {
            $unityVersion = $ProjectCfg.unityVersion
            if ([string]::IsNullOrEmpty($unityVersion)) {
                $unityVersion = Get-UnityProjectVersion -UnityProjectDir $UnityProjectDir
            }
            if ([string]::IsNullOrEmpty($unityVersion)) {
                throw ("Cannot resolve Unity version: unityVersion is empty and no m_EditorVersion found in {0}\ProjectSettings\ProjectVersion.txt" -f $UnityProjectDir)
            }
            $unityExe = Join-Path $MachineCfg.unityHubEditorRoot (Join-Path $unityVersion 'Editor\Unity.exe')
        } elseif (-not [System.IO.Path]::IsPathRooted($unityExe)) {
            $unityExe = Join-Path $RepoRoot $unityExe
        }
        if (-not (Test-Path -LiteralPath $unityExe -PathType Leaf)) {
            throw ("Unity executable not found: {0}" -f $unityExe)
        }
        Write-CiLog ("Unity executable: {0}" -f $unityExe)

        if ($Clean) {
            $libraryDir = Join-Path $UnityProjectDir 'Library'
            if (Test-Path -LiteralPath $libraryDir) {
                Write-CiLog ("Clean requested: deleting {0}" -f $libraryDir)
                Remove-Item -LiteralPath $libraryDir -Recurse -Force
            }
        }

        # ----- stage: unity (section 6 step 2 / section 7) -----
        $script:Record.stage = 'unity'
        Save-Record

        $ciOutputPath = Join-Path $WorkDir ("{0}.exe" -f $ProjectCfg.executableName)
        $ciDevelopment = 'false'
        if ($ProjectCfg.development) { $ciDevelopment = 'true' }
        $unityArgs = @(
            (Quote-Arg '-batchmode'),
            (Quote-Arg '-nographics'),
            (Quote-Arg '-quit'),
            (Quote-Arg '-projectPath'), (Quote-Arg $UnityProjectDir),
            (Quote-Arg '-buildTarget'), (Quote-Arg 'Win64'),
            (Quote-Arg '-logFile'), (Quote-Arg $UnityLogFilePath),
            (Quote-Arg '-executeMethod'), (Quote-Arg 'UnityCI.BuildScript.Build'),
            (Quote-Arg '-ciOutput'), (Quote-Arg $ciOutputPath),
            (Quote-Arg '-ciVersion'), (Quote-Arg $version),
            (Quote-Arg '-ciVersionLabel'), (Quote-Arg $versionLabel),
            (Quote-Arg '-ciDevelopment'), (Quote-Arg $ciDevelopment)
        )
        Write-CiLog ("Starting Unity: {0} {1}" -f $unityExe, ($unityArgs -join ' '))
        $unityProc = Start-Process -FilePath $unityExe -ArgumentList $unityArgs -PassThru -NoNewWindow
        # Reading Handle right after start makes ExitCode reliable on PS 5.1.
        $null = $unityProc.Handle
        $script:Record.unityPid = $unityProc.Id
        Save-Record
        Write-CiLog ("Unity process started, pid {0}, log file {1}" -f $unityProc.Id, $UnityLogFilePath)

        # Tail the Unity log while it runs, then flush the remainder.
        $logOffset = [long]0
        while (-not $unityProc.HasExited) {
            Start-Sleep -Milliseconds 1000
            $logOffset = Read-LogChunk -Path $UnityLogFilePath -Offset $logOffset
        }
        $logOffset = Read-LogChunk -Path $UnityLogFilePath -Offset $logOffset

        $unityExitCode = 0
        try { $unityExitCode = [int]$unityProc.ExitCode } catch { $unityExitCode = -1 }
        Write-CiLog ("Unity exited with code {0}" -f $unityExitCode)
        if ($unityExitCode -ne 0) {
            throw ("Unity build failed with exit code {0}" -f $unityExitCode)
        }

        # ----- stage: package (section 6 step 3) -----
        $script:Record.stage = 'package'
        Save-Record

        if (-not (Test-Path -LiteralPath $WorkDir)) {
            throw ("Unity output folder is missing: {0}" -f $WorkDir)
        }
        $artifactDir = Join-Path $ArtifactsDir $buildId
        New-Item -ItemType Directory -Force -Path $artifactDir | Out-Null
        $zipPath = Join-Path $artifactDir $artifactName
        if (Test-Path -LiteralPath $zipPath) { Remove-Item -LiteralPath $zipPath -Force }
        Write-CiLog ("Packaging {0} into {1}" -f $WorkDir, $zipPath)
        # Prefer the Windows bsdtar in System32 (supports -a zip archives);
        # fall back to whatever tar.exe is on PATH.
        $tarExe = 'tar.exe'
        $systemTar = Join-Path $env:SystemRoot 'System32\tar.exe'
        if (Test-Path -LiteralPath $systemTar -PathType Leaf) { $tarExe = $systemTar }
        $null = & $tarExe -a -c -f $zipPath -C $WorkDir '*'
        if ($LASTEXITCODE -ne 0) {
            throw ("tar.exe failed with exit code {0}" -f $LASTEXITCODE)
        }
        if (-not (Test-Path -LiteralPath $zipPath)) {
            throw ("Archive was not created: {0}" -f $zipPath)
        }
        $script:Record.artifactPath = (Resolve-Path -LiteralPath $zipPath).Path
        $script:Record.artifactSizeBytes = (Get-Item -LiteralPath $zipPath).Length
        Save-Record

        try {
            Remove-Item -LiteralPath $WorkDir -Recurse -Force
        } catch {
            Write-CiLog ("WARNING: could not delete work folder {0}: {1}" -f $WorkDir, $_.Exception.Message)
        }

        Write-CiLog ("Applying artifact retention (keep {0}) for project {1}" -f $MachineCfg.localArtifactRetention, $projectName)
        Invoke-ArtifactRetention -ProjectName $projectName -Keep ([int]$MachineCfg.localArtifactRetention) -CurrentBuildId $buildId

        # ----- stage: upload (section 6 step 4) -----
        $doUpload = $false
        if ($ProjectCfg.uploadToDrive -and -not [string]::IsNullOrEmpty($MachineCfg.rcloneRemote)) { $doUpload = $true }
        if ($doUpload) {
            $script:Record.stage = 'upload'
            Save-Record
            $remotePath = "{0}:{1}/{2}/{3}/{4}" -f $MachineCfg.rcloneRemote, $MachineCfg.driveRootFolder, $projectName, $branchSafe, $artifactName
            $script:Record.drivePath = $remotePath
            Save-Record
            Write-CiLog ("Uploading artifact to Drive: {0}" -f $remotePath)
            $null = & $MachineCfg.rcloneExe copyto $script:Record.artifactPath $remotePath --config $RcloneConf
            if ($LASTEXITCODE -ne 0) {
                throw ("rclone copyto failed with exit code {0}" -f $LASTEXITCODE)
            }
            $linkLines = @(& $MachineCfg.rcloneExe link $remotePath --config $RcloneConf)
            if ($LASTEXITCODE -ne 0) {
                throw ("rclone link failed with exit code {0}" -f $LASTEXITCODE)
            }
            if ($linkLines.Count -gt 0) { $script:Record.driveLink = [string]$linkLines[0] }
            Save-Record
            Write-CiLog "Upload finished"
        }

        # ----- stage: report / done (section 6 steps 5 and 6) -----
        $script:Record.status = 'succeeded'
        Complete-RecordTiming
        $script:Record.stage = 'report'
        Save-Record
        Send-SheetReport
        $script:Record.stage = 'done'
        Save-Record
        Write-CiLog ("Build succeeded in {0} seconds" -f $script:Record.durationSec)
    }
    else {
        # ============================ FINALIZE ============================
        if (Test-Path -LiteralPath $RecordFile) {
            $script:MainLogFile = $MainLogFilePath
            $existing = Read-JsonConfig $RecordFile
            if ($null -eq $existing) {
                Write-CiLog ("Finalize: could not read record {0}, nothing to do" -f $RecordFile)
            }
            elseif ([string]$existing.status -ne 'running') {
                Write-CiLog ("Finalize: build {0} status is '{1}' (not running), no-op" -f $buildId, $existing.status)
            }
            else {
                $logFileProp = $existing.PSObject.Properties['logFile']
                if ($null -ne $logFileProp -and -not [string]::IsNullOrEmpty([string]$logFileProp.Value)) {
                    $script:MainLogFile = [string]$logFileProp.Value
                }
                $script:Record = Merge-RecordWithDefaults -Existing $existing -Id $buildId -Project $projectName
                $script:Record.status = 'running'
                Save-Record

                # Kill the Unity process tree if it is still alive.
                $unityPid = ConvertTo-IntSafe $script:Record.unityPid 0
                if ($unityPid -gt 0) {
                    $proc = Get-Process -Id $unityPid -ErrorAction SilentlyContinue
                    if ($null -ne $proc) {
                        Write-CiLog ("Finalize: killing Unity process tree (pid {0})" -f $unityPid)
                        $oldEap = $ErrorActionPreference
                        $ErrorActionPreference = 'Continue'
                        try { $null = & taskkill /PID $unityPid /T /F 2>&1 } catch { }
                        $ErrorActionPreference = $oldEap
                    } else {
                        Write-CiLog ("Finalize: Unity process (pid {0}) is not running" -f $unityPid)
                    }
                } else {
                    Write-CiLog "Finalize: no Unity process recorded"
                }

                $newStatus = 'failed'
                if ($JobStatus -eq 'cancelled') { $newStatus = 'cancelled' }
                $script:Record.status = $newStatus
                if ($newStatus -eq 'failed' -and [string]::IsNullOrEmpty([string]$script:Record.error)) {
                    $script:Record.error = ("finalized as failed (workflow job status: {0})" -f $JobStatus)
                }
                Complete-RecordTiming
                $script:Record.stage = 'done'
                Save-Record
                Write-CiLog ("Finalize: build {0} marked {1}" -f $buildId, $newStatus)
                Send-SheetReport
            }
        }
        else {
            Write-Host ("[ci] Finalize: no record found for {0}, nothing to do" -f $buildId)
        }
    }
}
catch {
    $errorMessage = $_.Exception.Message
    if ($Mode -eq 'Build') {
        # On any failure: mark failed, still try the sheet report, exit 1.
        if ($null -ne $script:Record) {
            try {
                $script:Record.status = 'failed'
                $script:Record.error = $errorMessage
                Complete-RecordTiming
                $script:Record.stage = 'report'
                Save-Record
                Write-CiLog ("ERROR: {0}" -f $errorMessage)
                Send-SheetReport
                $script:Record.stage = 'done'
                Save-Record
            } catch {
                Write-Host ("[ci] ERROR while saving failed record: {0}" -f $_.Exception.Message)
            }
        } else {
            Write-Host ("[ci] ERROR: {0}" -f $errorMessage)
        }
        $script:ExitCode = 1
    }
    else {
        # Finalize always exits 0.
        Write-Host ("[ci] ERROR during Finalize: {0}" -f $errorMessage)
        $script:ExitCode = 0
    }
}

exit $script:ExitCode
