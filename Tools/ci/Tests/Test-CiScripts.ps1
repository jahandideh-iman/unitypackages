#Requires -Version 5.1
<#
.SYNOPSIS
    Fixture-driven tests for the CI helper scripts under Tools/ci/.

.DESCRIPTION
    No external test framework on purpose: this has to run on a bare self-hosted
    Windows runner (PowerShell 5.1, where Pester is 3.4 and useless) and on a
    hosted ubuntu runner (pwsh). Adding a Pester dependency would mean an
    Install-Module step on both.

    Run: pwsh -File Tools/ci/Tests/Test-CiScripts.ps1
    Exits 0 if every assertion holds, 1 otherwise.
#>
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$script:Failures = 0
$script:Root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$script:Fixtures = Join-Path $PSScriptRoot 'fixtures'

function Assert-Equal
{
    param($Expected, $Actual, [string] $Because)

    if ($Expected -ne $Actual)
    {
        Write-Host "  FAIL $Because"
        Write-Host "       expected: $Expected"
        Write-Host "       actual:   $Actual"
        $script:Failures++
        return
    }
    Write-Host "  ok   $Because"
}

function Assert-Match
{
    param([string] $Pattern, [string] $Actual, [string] $Because)

    if ($Actual -notmatch $Pattern)
    {
        Write-Host "  FAIL $Because"
        Write-Host "       pattern: $Pattern"
        Write-Host "       actual:  $Actual"
        $script:Failures++
        return
    }
    Write-Host "  ok   $Because"
}

# Runs a script and captures its output plus its exit code. Two traps here:
#
#   * A script invoked with & does not set $LASTEXITCODE unless it calls exit.
#     These scripts always call exit, so this is safe.
#   * `*>&1`, not `2>&1`. The scripts report through Write-Host, which writes to
#     the information stream (6), not stdout or stderr -- `2>&1` merges only
#     stderr and would capture nothing at all.
function Invoke-Script
{
    param([string] $Path, [hashtable] $Arguments)

    $output = & $Path @Arguments *>&1 | Out-String
    return @{ Output = $output; ExitCode = $LASTEXITCODE }
}

Write-Host 'Resolve-UnityEditor.ps1'
$resolve = Join-Path $script:Root 'ci/Resolve-UnityEditor.ps1'

$result = Invoke-Script $resolve @{
    ProjectVersionPath = Join-Path $script:Fixtures 'ProjectVersion.txt'
    EditorsJson        = (Get-Content (Join-Path $script:Fixtures 'editors-both.json') -Raw)
}
Assert-Equal 0 $result.ExitCode 'succeeds when the declared editor is installed'
Assert-Match '6000\.5\.10f1\\Editor\\Unity\.exe' $result.Output 'reports the resolved editor path'

$result = Invoke-Script $resolve @{
    ProjectVersionPath = Join-Path $script:Fixtures 'ProjectVersion.txt'
    EditorsJson        = (Get-Content (Join-Path $script:Fixtures 'editors-missing.json') -Raw)
}
Assert-Equal 1 $result.ExitCode 'fails when the declared editor is known but not installed'
Assert-Match '::error::' $result.Output 'emits a GitHub error annotation'
Assert-Match '6000\.5\.10f1' $result.Output 'names the missing version'
Assert-Match '6000\.5\.0f1' $result.Output 'lists what is installed instead'

$result = Invoke-Script $resolve @{
    ProjectVersionPath = Join-Path $script:Fixtures 'does-not-exist.txt'
    EditorsJson        = '{"data":[]}'
}
Assert-Equal 1 $result.ExitCode 'fails when ProjectVersion.txt is absent'

Write-Host ''
if ($script:Failures -gt 0)
{
    Write-Host "$($script:Failures) assertion(s) failed."
    exit 1
}
Write-Host 'All CI script tests passed.'
exit 0
