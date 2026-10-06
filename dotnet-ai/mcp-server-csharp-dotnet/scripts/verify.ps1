[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

$RequiredSdk = '10.0.401'
$TargetFramework = 'net10.0'
$McpVersion = '2.2.0'
$HostingVersion = '10.0.12'
$XunitVersion = '4.0.1'

$Root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$SolutionPath = Join-Path $Root 'FirstMcpServer.slnx'
$ServerProject = Join-Path $Root 'src\FirstMcpServer\FirstMcpServer.csproj'
$ClientProject = Join-Path $Root 'src\McpClientDemo\McpClientDemo.csproj'
$TestProject = Join-Path $Root 'tests\FirstMcpServer.Tests\FirstMcpServer.Tests.csproj'
$ServerDll = Join-Path $Root 'src\FirstMcpServer\bin\Release\net10.0\FirstMcpServer.dll'

$Results = Join-Path $Root 'verification-results'
$PackageGraphFile = Join-Path $Root 'docs\package-graph.txt'
$RawClientFile = Join-Path $Results 'mcp-client-output.txt'
$RawTestFile = Join-Path $Results 'test-output.txt'
$RawBuildFile = Join-Path $Results 'build-output.txt'
$VerifiedMd = Join-Path $Root 'docs\verified-environment.md'
$SampleRunsMd = Join-Path $Root 'docs\sample-runs.md'
$VerifiedJson = Join-Path $Root 'verified-environment.json'

function Write-Section([string]$Text) {
    Write-Host ''
    Write-Host ('=' * 78) -ForegroundColor DarkGray
    Write-Host $Text -ForegroundColor Cyan
    Write-Host ('=' * 78) -ForegroundColor DarkGray
}

function Write-Step([string]$Text) {
    Write-Host "[STEP] $Text" -ForegroundColor Yellow
}

function Write-Pass([string]$Text) {
    Write-Host "[PASS] $Text" -ForegroundColor Green
}

function Write-Fail([string]$Text) {
    Write-Host "[FAIL] $Text" -ForegroundColor Red
}

function Write-Utf8NoBom([string]$Path, [string]$Content) {
    $parent = Split-Path -Parent $Path
    if ($parent -and -not (Test-Path $parent)) {
        New-Item -ItemType Directory -Force -Path $parent | Out-Null
    }
    [System.IO.File]::WriteAllText($Path, $Content, [System.Text.UTF8Encoding]::new($false))
}

function Invoke-Native {
    param(
        [Parameter(Mandatory=$true)][string]$FilePath,
        [Parameter(Mandatory=$false)][string[]]$Arguments = @(),
        [Parameter(Mandatory=$false)][string]$WorkingDirectory = $Root,
        [Parameter(Mandatory=$false)][switch]$Capture,
        [Parameter(Mandatory=$false)][string]$CapturePath
    )

    Push-Location $WorkingDirectory
    try {
        if ($Capture) {
            $output = & $FilePath @Arguments 2>&1
            $exitCode = $LASTEXITCODE
            $text = ($output | ForEach-Object { $_.ToString() }) -join [Environment]::NewLine

            if ($CapturePath) {
                Write-Utf8NoBom $CapturePath ($text + [Environment]::NewLine)
            }

            $output | ForEach-Object { Write-Host $_ }

            if ($exitCode -ne 0) {
                throw "Command failed with exit code ${exitCode}: $FilePath $($Arguments -join ' ')"
            }

            return $text
        }

        & $FilePath @Arguments
        $exitCode = $LASTEXITCODE
        if ($exitCode -ne 0) {
            throw "Command failed with exit code ${exitCode}: $FilePath $($Arguments -join ' ')"
        }
    }
    finally {
        Pop-Location
    }
}

Write-Section 'FIRST MCP SERVER - LOCAL VERIFICATION'
Write-Host "Root:                         $Root"
Write-Host "Required SDK:                 $RequiredSdk"
Write-Host "Target framework:             $TargetFramework"
Write-Host "ModelContextProtocol:         $McpVersion"
Write-Host "Microsoft.Extensions.Hosting: $HostingVersion"
Write-Host "xunit.v3.mtp-v2:              $XunitVersion"
Write-Host 'Transport:                    stdio'
Write-Host 'LLM/API calls:                NONE'

try {
    if (Test-Path $Results) {
        Remove-Item -Recurse -Force $Results
    }
    New-Item -ItemType Directory -Force -Path $Results | Out-Null

    Write-Step 'Check required .NET SDK'
    $installedSdks = & dotnet --list-sdks 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw 'dotnet --list-sdks failed.'
    }

    if (-not ($installedSdks | Where-Object { $_ -match "^$([regex]::Escape($RequiredSdk))\s" })) {
        throw "Required .NET SDK $RequiredSdk is not installed."
    }

    $selectedSdk = Invoke-Native -FilePath 'dotnet' -Arguments @('--version') -Capture
    if ($selectedSdk.Trim() -ne $RequiredSdk) {
        throw "global.json selected '$($selectedSdk.Trim())' instead of '$RequiredSdk'."
    }
    Write-Pass ".NET SDK $RequiredSdk selected"

    Write-Step 'Restore solution'
    Invoke-Native -FilePath 'dotnet' -Arguments @('restore', $SolutionPath)
    Write-Pass 'Restore PASS'

    Write-Step 'Build solution in Release'
    $buildText = Invoke-Native -FilePath 'dotnet' -Arguments @('build', $SolutionPath, '-c', 'Release', '--no-restore') -Capture -CapturePath $RawBuildFile
    if (-not (Test-Path $ServerDll)) {
        throw "Expected server DLL was not produced: $ServerDll"
    }
    Write-Pass 'Release build PASS'

    Write-Step 'Run deterministic test project'
    $testText = Invoke-Native -FilePath 'dotnet' -Arguments @('test', $TestProject, '-c', 'Release', '--no-build') -Capture -CapturePath $RawTestFile
    Write-Pass 'Deterministic tests PASS'

    $testTotal = $null
    $testPassed = $null
    $testFailed = $null
    $testSkipped = $null

    # .NET 10 + Microsoft Testing Platform emits:
    #   Test run summary: Passed!
    #     total: 13
    #     failed: 0
    #     succeeded: 13
    #     skipped: 0
    # Parse the four summary lines independently so CRLF/wording changes in the
    # heading do not erase otherwise valid counts.
    $testLines = $testText -split '\r?\n'

    foreach ($line in $testLines) {
        if ($null -eq $testTotal -and $line -match '^\s*total:\s*(\d+)\s*$') {
            $testTotal = [int]$Matches[1]
            continue
        }
        if ($null -eq $testFailed -and $line -match '^\s*failed:\s*(\d+)\s*$') {
            $testFailed = [int]$Matches[1]
            continue
        }
        if ($null -eq $testPassed -and $line -match '^\s*succeeded:\s*(\d+)\s*$') {
            $testPassed = [int]$Matches[1]
            continue
        }
        if ($null -eq $testSkipped -and $line -match '^\s*skipped:\s*(\d+)\s*$') {
            $testSkipped = [int]$Matches[1]
            continue
        }
    }

    if ($null -eq $testTotal -or
        $null -eq $testFailed -or
        $null -eq $testPassed -or
        $null -eq $testSkipped) {
        throw 'Deterministic tests passed, but the verifier could not parse the Microsoft Testing Platform summary counts.'
    }

    if ($testFailed -ne 0 -or ($testPassed + $testFailed + $testSkipped) -ne $testTotal) {
        throw "Parsed test counts are inconsistent: total=$testTotal passed=$testPassed failed=$testFailed skipped=$testSkipped."
    }

    Write-Step 'Capture package graph'
    Invoke-Native -FilePath 'dotnet' -Arguments @('list', $SolutionPath, 'package', '--include-transitive') -Capture -CapturePath $PackageGraphFile | Out-Null
    Write-Pass 'Package graph captured'

    Write-Step 'Run real MCP stdio client/server end-to-end verification'
    $clientText = Invoke-Native -FilePath 'dotnet' -Arguments @(
        'run',
        '--project', $ClientProject,
        '-c', 'Release',
        '--no-build',
        '--',
        $ServerDll
    ) -Capture -CapturePath $RawClientFile

    $requiredPassKeys = @(
        'SERVER_DLL',
        'TOOL_DISCOVERY',
        'AIFUNCTION_ASSIGNABILITY',
        'SERVICE_HEALTH_CALL',
        'DEPLOYMENT_CALL',
        'RUNBOOK_CALL',
        'UNKNOWN_TOOL_REJECTION',
        'STDIO_END_TO_END'
    )

    foreach ($key in $requiredPassKeys) {
        if ($clientText -notmatch "(?m)^VERIFY\|$([regex]::Escape($key))\|PASS\|") {
            throw "Expected PASS marker was not found for $key."
        }
    }

    if ($clientText -notmatch '(?m)^FINAL\|PASS$') {
        throw 'McpClientDemo did not emit FINAL|PASS.'
    }

    Write-Pass 'MCP stdio E2E PASS'

    $clientLines = $clientText -split '\r?\n'

    $runtimeTransportLine = $clientLines |
        Where-Object { $_ -match '^RUNTIME\|TransportType\|(.+)$' } |
        Select-Object -First 1
    $runtimeClientLine = $clientLines |
        Where-Object { $_ -match '^RUNTIME\|ClientType\|(.+)$' } |
        Select-Object -First 1
    $toolCountLine = $clientLines |
        Where-Object { $_ -match '^RUNTIME\|ToolCount\|(\d+)$' } |
        Select-Object -First 1

    if (-not $runtimeTransportLine -or -not $runtimeClientLine -or -not $toolCountLine) {
        throw 'MCP E2E passed, but runtime metadata lines could not be parsed.'
    }

    [void]($runtimeTransportLine -match '^RUNTIME\|TransportType\|(.+)$')
    $runtimeTransport = $Matches[1].Trim()

    [void]($runtimeClientLine -match '^RUNTIME\|ClientType\|(.+)$')
    $runtimeClient = $Matches[1].Trim()

    [void]($toolCountLine -match '^RUNTIME\|ToolCount\|(\d+)$')
    $toolCount = [int]$Matches[1]

    if ($toolCount -ne 3) {
        throw "Expected exactly 3 discovered tools, but parsed $toolCount."
    }

    $evidenceLines = $clientLines |
        Where-Object { $_ -match '^(RUNTIME|TOOL|VERIFY|FINAL)\|' }

    $timestamp = (Get-Date).ToString('yyyy-MM-ddTHH:mm:ssK')
    $dateOnly = (Get-Date).ToString('yyyy-MM-dd')

    $testSummaryText = "$testPassed/$testTotal passed; $testFailed failed; $testSkipped skipped"

    $verifiedMdContent = @"
# Verified Environment

- Verification timestamp: $timestamp
- .NET SDK: $RequiredSdk
- Target framework: $TargetFramework
- ModelContextProtocol: $McpVersion
- Microsoft.Extensions.Hosting: $HostingVersion
- xunit.v3.mtp-v2: $XunitVersion
- Transport: stdio
- Release build: PASS
- Deterministic tests: $testSummaryText
- MCP stdio end-to-end: PASS
- Runtime transport type: $runtimeTransport
- Runtime client type: $runtimeClient
- Discovered tool count: $toolCount

## Verified behaviors

- real MCP client/server handshake;
- exactly three tool definitions discovered;
- `get_service_health` invoked over MCP stdio;
- `get_recent_deployment` invoked over MCP stdio;
- `get_runbook` invoked over MCP stdio;
- unknown tool rejected;
- `McpClientTool` assignable to `AIFunction`;
- no LLM or cloud API required.

## Evidence boundary

This verification does not establish HTTP transport behavior, authentication/OAuth, LLM-driven tool selection, Agent Framework integration, production readiness, or coverage of MCP features outside this sample.

Raw local outputs are under `verification-results/` and are intentionally gitignored.
"@
    Write-Utf8NoBom $VerifiedMd $verifiedMdContent

    $sampleRun = @"
# Sample Runs

Captured from the successful local MCP stdio verification on $dateOnly.

```text
$($evidenceLines -join [Environment]::NewLine)
```

Server diagnostic lines are omitted from this curated view. The raw local output remains in `verification-results/mcp-client-output.txt`.
"@
    Write-Utf8NoBom $SampleRunsMd $sampleRun

    $jsonObject = [ordered]@{
        status = 'PASS'
        verificationTimestamp = $timestamp
        sdk = $RequiredSdk
        targetFramework = $TargetFramework
        packages = [ordered]@{
            ModelContextProtocol = $McpVersion
            MicrosoftExtensionsHosting = $HostingVersion
            xunitV3MtpV2 = $XunitVersion
        }
        transport = 'stdio'
        tests = [ordered]@{
            status = 'PASS'
            total = $testTotal
            passed = $testPassed
            failed = $testFailed
            skipped = $testSkipped
        }
        runtime = [ordered]@{
            transportType = $runtimeTransport
            clientType = $runtimeClient
            discoveredToolCount = $toolCount
        }
        checks = [ordered]@{
            restore = 'PASS'
            releaseBuild = 'PASS'
            deterministicTests = 'PASS'
            toolDiscovery = 'PASS'
            aiFunctionAssignability = 'PASS'
            serviceHealthCall = 'PASS'
            deploymentCall = 'PASS'
            runbookCall = 'PASS'
            unknownToolRejection = 'PASS'
            stdioEndToEnd = 'PASS'
        }
        exclusions = @(
            'LLM-driven tool selection',
            'Agent Framework',
            'Streamable HTTP',
            'OAuth/authentication/authorization',
            'MCP resources/prompts/sampling/elicitation/tasks',
            'write-capable tools',
            'database/Redis',
            'cloud deployment',
            'production readiness'
        )
    }

    Write-Utf8NoBom $VerifiedJson ($jsonObject | ConvertTo-Json -Depth 8)

    Write-Section 'FINAL RESULT'
    Write-Host 'FIRST MCP SERVER VERIFICATION: PASS' -ForegroundColor Green
    Write-Host ''
    Write-Host "Project:            $Root"
    Write-Host "Build:              PASS"
    Write-Host "Tests:              $testSummaryText"
    Write-Host "MCP stdio E2E:      PASS"
    Write-Host "Discovered tools:   $toolCount"
    Write-Host "Verified JSON:      $VerifiedJson"
    Write-Host "Verified Markdown:  $VerifiedMd"
    Write-Host "Sample run:         $SampleRunsMd"
    Write-Host "Package graph:      $PackageGraphFile"
    Write-Host ''
    Write-Host 'Please send the complete console output back to ChatGPT.' -ForegroundColor Cyan
    exit 0
}
catch {
    Write-Section 'FINAL RESULT'
    Write-Fail 'FIRST MCP SERVER VERIFICATION: FAIL'
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host ''
    Write-Host "Project has been preserved for diagnosis: $Root"
    Write-Host "Local raw outputs, if created: $Results"
    Write-Host ''
    Write-Host 'Please send the complete console output back to ChatGPT.' -ForegroundColor Cyan
    exit 1
}
