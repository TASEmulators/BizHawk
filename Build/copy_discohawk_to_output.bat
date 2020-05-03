CHDIR "%~dp0\.."

FOR /D %%D IN (src\BizHawk.Client.DiscoHawk\bin\*) DO (
	ROBOCOPY %%D output
	RENAME output\BizHawk.Client.DiscoHawk.exe DiscoHawk.exe
	RENAME output\BizHawk.Client.DiscoHawk.exe.config DiscoHawk.exe.config
	goto end
)
:end
