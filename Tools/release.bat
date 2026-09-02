@echo off
rem Thin wrapper so the release tool is one word to run from a Windows shell:
rem
rem   Toolselease.bat validate
rem   Toolselease.bat prepare --dry-run
rem   Toolselease.bat tag --push
rem
rem Every argument is forwarded verbatim; the script itself is the documentation.
node "%~dp0upm-release.mjs" %*
