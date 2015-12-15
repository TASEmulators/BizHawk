@echo off
ECHO Hello. I am TASJudy. Prepare to be Judged.
setlocal EnableDelayedExpansion
del results\failed.txt >nul 2>&1

for /r %%i in (movies\*) do (
	set currentmovie=%%i

	echo Processing %%i...
	start "" /B "F:\competition\bizhawk\EmuHawk.exe" rom\mystery.nes --movie=%%i
	
	echo Waiting for run to finish...
	TIMEOUT /T 1 >nul
	call :CheckForExe
)

echo Done
pause
exit /b

:CheckForExe
tasklist /FI "IMAGENAME eq EmuHawk.exe" 2>NUL | find /I /N "EmuHawk.exe">NUL

if %ERRORLEVEL% == 1 goto ResultsAreIn

TIMEOUT /T 1 >nul
call :CheckForExe
exit /b


:ResultsAreIn
echo Results are in for !currentmovie!
call :kill
exit /b

:kill
taskkill /f /im EmuHawk.exe >nul 2>&1
exit /b
