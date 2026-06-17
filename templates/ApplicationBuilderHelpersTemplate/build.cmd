@echo off
pwsh -ExecutionPolicy ByPass -NoProfile -File "%~dp0build.ps1" %*
