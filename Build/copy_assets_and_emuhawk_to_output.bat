CHDIR "%~dp0\.."

ROBOCOPY /E Assets output

FOR /D %%D IN (src\BizHawk.Client.EmuHawk\bin\*) DO (
	ROBOCOPY %%D output
	RENAME output\BizHawk.Client.EmuHawk.exe EmuHawk.exe
	RENAME output\BizHawk.Client.EmuHawk.exe.config EmuHawk.exe.config
	goto end
)
:end
