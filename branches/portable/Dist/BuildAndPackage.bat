if "%1"=="" (
	SET NAME=BizHawk.zip
) else (
	SET NAME=%1
)

svn --version > NUL
@if errorlevel 1 goto MISSINGSVN

reg query "HKLM\SOFTWARE\Microsoft\MSBuild\ToolsVersions\4.0" /v MSBuildToolsPath > nul 2>&1
if ERRORLEVEL 1 goto MISSINGMSBUILD

for /f "skip=2 tokens=2,*" %%A in ('reg.exe query "HKLM\SOFTWARE\Microsoft\MSBuild\ToolsVersions\4.0" /v MSBuildToolsPath') do SET MSBUILDDIR=%%B

IF NOT EXIST %MSBUILDDIR%nul goto MISSINGMSBUILD
IF NOT EXIST %MSBUILDDIR%msbuild.exe goto MISSINGMSBUILD

call "%MSBUILDDIR%msbuild.exe" ..\BizHawk.sln /p:Configuration=Release /p:Platform="x86" /t:rebuild

rmdir /s /q temp
del /s %NAME%
cd ..\output

rem slimdx has a way of not making it into the output directory, so this is a good time to make sure
copy ..\..\SlimDx.dll

rem at the present we support moving all these dlls into the dll subdirectory
rem that might be troublesome some day..... if it does get troublesome, then we'll have to 
rem explicitly list the OK ones here as individual copies. until then....

copy *.dll dll

..\dist\zip.exe -X -r ..\Dist\%NAME% EmuHawk.exe DiscoHawk.exe defctrl.json dll shaders gamedb NES\Palettes Lua Gameboy\Palettes -x *.pdb -x *.lib -x *.pgd -x *.exp -x dll\libsneshawk-64*.exe -x *.ilk

cd ..\Dist
.\unzip.exe %NAME% -d temp
del %NAME%

rmdir /s /q temp\lua
svn export ..\output\lua temp\Lua
svn export ..\output\firmware temp\Firmware

cd temp
upx -d dll\*.dll
upx -d dll\*.exe
upx -d *.exe
..\zip.exe -X -9 -r ..\%NAME% . -i \*
cd ..

rmdir /s /q temp
goto END

:MISSINGMSBUILD
@echo Missing msbuild.exe. can't make distro without that.
goto END
:MISSINGSVN
@echo missing svn.exe. can't make distro without that.
:END
exit