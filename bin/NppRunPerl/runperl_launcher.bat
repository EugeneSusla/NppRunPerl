rem		Arguments expected:
rem			1. Name of a text file with input for script, provided by notepad++
rem			2. Name of the script file, notepad++ opens on shorctut, presumably edited by user
rem			3. Name of the output file, contents of which will be pasted into notepad++ afterwards

@echo off
cd /d %~dp0
perl runperl_runner.pl "%2" "%1" "%3"