#Requires -Version 5.1
<#
.SYNOPSIS
    Resolves the Unity editor the project declares, and fails if it is absent.

.DESCRIPTION
    CI runs the version in ProjectSettings/ProjectVersion.txt and no other. This
    script exists to turn "that editor is missing" into one clear line naming the
    version, rather than a downstream Unity CLI failure that reads like a
    compile error.

    EditorsJson is a parameter rather than something this script fetches, so it
    can be tested without a Unity install.

    NOTE: an editor can be installed and still be unable to compile -- see
    section 8 of docs/specs/2026-08-30-pr-test-ci-design.md. This check does not
    catch that; Publish-UnityLog.ps1 diagnoses it after the fact.

.EXAMPLE
    $json = unity editors --json | Out-String
    ./Tools/ci/Resolve-UnityEditor.ps1 -ProjectVersionPath ProjectSettings/ProjectVersion.txt -EditorsJson $json
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $ProjectVersionPath,
    [Parameter(Mandatory)] [string] $EditorsJson
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not (Test-Path $ProjectVersionPath))
{
    Write-Host "::error::No ProjectVersion.txt at $ProjectVersionPath."
    exit 1
}

$versionLine = Select-String -Path $ProjectVersionPath -Pattern '^m_EditorVersion:\s*(\S+)' |
    Select-Object -First 1

if (-not $versionLine)
{
    Write-Host "::error::$ProjectVersionPath contains no m_EditorVersion line."
    exit 1
}

$declared = $versionLine.Matches[0].Groups[1].Value

try
{
    $editors = ($EditorsJson | ConvertFrom-Json).data
}
catch
{
    Write-Host "::error::Could not parse the output of 'unity editors --json': $_"
    exit 1
}

# 'location' is present only for installed editors. Matching on version alone
# would pass for an editor the Hub merely knows about.
$installed = @($editors | Where-Object { $_.PSObject.Properties.Name -contains 'location' })
$editor = $installed | Where-Object { $_.version -eq $declared } | Select-Object -First 1

if (-not $editor)
{
    $available = ($installed | ForEach-Object { $_.version }) -join ', '
    if (-not $available) { $available = '(none)' }
    Write-Host "::error::Unity $declared (from $ProjectVersionPath) is not installed on this runner. Installed: $available"
    exit 1
}

Write-Host "Resolved Unity $declared at $($editor.location)"

if ($env:GITHUB_OUTPUT)
{
    "unity-version=$declared"         | Out-File $env:GITHUB_OUTPUT -Append -Encoding utf8
    "editor-path=$($editor.location)" | Out-File $env:GITHUB_OUTPUT -Append -Encoding utf8
}

exit 0
