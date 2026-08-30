#Requires -Version 5.1
<#
.SYNOPSIS
    Summarises a Unity editor log into a CI job log, and names known environment
    failures.

.DESCRIPTION
    `unity test` streams nothing to stdout and exits silently even on success, so
    without this the job log is two command lines and nothing else.

    Only a bounded excerpt goes inline: dumping a multi-megabyte editor log costs
    the end of the trace, which is the part worth reading. The full log rides
    along as an artefact.

    Always exits 0. This is a reporting step; the caller owns the exit code.

.EXAMPLE
    ./Tools/ci/Publish-UnityLog.ps1 -LogPath Logs/editmode-editor.log -Label EditMode -ExitCode 8
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $LogPath,
    [Parameter(Mandatory)] [string] $Label,
    [int] $ExitCode = 0,
    [int] $MaxLines = 200,
    [int] $TailLines = 300
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not (Test-Path $LogPath))
{
    Write-Host "$Label -- Unity wrote no editor log at $LogPath."
    exit 0
}

# 'Exception:' with the colon on purpose: a bare 'Exception' also matches every
# Mono stack frame whose signature mentions System.Exception, which buries the
# real lines under hundreds of lines of noise.
$pattern = 'error CS\d+|Exception:|Unhandled Exception|\[Error\]|Aborting batchmode|Fatal error'
$hits = @(Select-String -Path $LogPath -Pattern $pattern)

Write-Host "----- $Label editor log: $($hits.Count) error/warning line(s) -----"
$hits | Select-Object -First $MaxLines | ForEach-Object { Write-Host "  $($_.LineNumber): $($_.Line)" }
if ($hits.Count -gt $MaxLines)
{
    Write-Host "  ... $($hits.Count - $MaxLines) more; see the uploaded artefact."
}

# A failing step is the one whose log anyone actually opens, so it gets the tail
# too -- Unity's failure summary and shutdown live there.
if ($ExitCode -ne 0)
{
    Write-Host "----- $Label failed (exit $ExitCode); last $TailLines lines -----"
    Get-Content $LogPath -Tail $TailLines | ForEach-Object { Write-Host $_ }
}

# Unity reports the Smart App Control block as 'Scripts have compiler errors.',
# which is false and costs whoever reads it an afternoon. See section 8 of
# docs/specs/2026-08-30-pr-test-ci-design.md.
#
# Both halves of the signature are required so this cannot fire on an unrelated
# [Error] line -- a diagnostic that cries wolf is one that gets ignored on the
# run where it matters.
$logText = Get-Content $LogPath -Raw
if ($logText -match 'Bee\.Tools\.dll' -and $logText -match '0x800711C7')
{
    Write-Host "::error::Unity could not compile because Smart App Control blocked Bee.Tools.dll (0x800711C7). This is an environment block on the runner, not a compile error in this PR. See docs/specs/2026-08-30-pr-test-ci-design.md section 8."
}

Write-Host "----- end $Label editor log -----"
exit 0
