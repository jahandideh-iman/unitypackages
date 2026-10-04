@echo off
rem Runs the release flow: each run opens the next pull request. With
rem [Unreleased] entries on origin/dev it prepares them on a branch and opens a
rem pull request into dev; once that is merged, a second run opens the release
rem pull request from dev into master. Takes no arguments. Merging the release
rem pull request is what publishes -- this script stops short of that, on purpose.
rem
rem For a single step, or for --dry-run / --only / --bump:
rem   node Tools/upm-release.mjs <validate^|pack^|tag^|prepare> [options]
node "%~dp0release-flow.mjs" %*
