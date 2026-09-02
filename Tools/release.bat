@echo off
rem Runs the whole release flow in one go: validate, prepare, commit, push, and
rem open the release pull request. Takes no arguments. Merging the pull request
rem it opens is what publishes -- this script stops short of that, on purpose.
rem
rem For a single step, or for --dry-run / --only / --bump:
rem   node Tools/upm-release.mjs <validate^|pack^|tag^|prepare> [options]
node "%~dp0release-flow.mjs" %*
