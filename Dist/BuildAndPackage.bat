set PATH=%PATH%;C:\Program Files (x86)\git\bin;C:\Program Files\git\bin

if "%1"=="" (
	SET NAME=BizHawk.zip
) else (
	SET NAME=%1
)

git --version > NUL
@if errorlevel 1 goto MISSINGGIT

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

rem Now, we're about to zip and then unzip. Why, you ask? Because that's just the way this evolved.
..\dist\zip.exe -X -r ..\Dist\%NAME% EmuHawk.exe DiscoHawk.exe defctrl.json dll shaders gamedb Tools NES\Palettes Lua Gameboy\Palettes -x *.pdb -x *.lib -x *.pgd -x *.ipdb -x *.iobj -x *.exp -x dll\libsneshawk-64*.exe -x *.ilk -x dll\gpgx.elf -x dll\miniclient.* 

cd ..\Dist
.\unzip.exe %NAME% -d temp
del %NAME%

rem Remove things we can't allow the user's junky files to pollute the dist with. We'll export fresh copies from git
rmdir /s /q temp\lua
rmdir /s /q temp\firmware

rmdir /s /q gitsucks
git --git-dir ../.git archive --format zip --output lua.zip HEAD Assets/Lua
git --git-dir ../.git archive --format zip --output firmware.zip HEAD output/Firmware
rem Getting externaltools example from my repo
rem I once talked about a dedicated repo for external tools, think about moving the exemple to it it it happend
git clone https://github.com/Hathor86/HelloWorld_BizHawkTool.git
git --git-dir HelloWorld_BizHawkTool/.git archive --format zip --output HelloWorld_BizHawkTool.zip master
rmdir /s /q  HelloWorld_BizHawkTool

unzip lua.zip -d gitsucks
rem del lua.zip
move gitsucks\Assets\Lua temp
unzip Firmware.zip -d gitsucks
rem del firmware.zip
move gitsucks\output\Firmware temp

rmdir /s /q gitsucks

rem remove UPX from any files we have checked in, because people's lousy security software hates it
rem: wait, why did I comment this out? did it have to do with CGC not roundtripping de-UPXing? then we should just not UPX it in the first place (but it's big)
upx -d temp\dll\*.dll
upx -d temp\dll\*.exe
upx -d temp\*.exe

rem dont need docs xml for assemblies and whatnot
del temp\dll\*.xml

cd temp

rem Patch up working dir with a few other things we want
mkdir ExternalTools
copy ..\HelloWorld_BizHawkTool.dll ExternalTools
copy ..\HelloWorld_BizHawkTool.zip ExternalTools
mkdir Firmware

rem compress nescart 7z
cd gamedb
..\..\7za a -t7z -mx9 NesCarts.7z NesCarts.xml
del NesCarts.xml
cd ..

rem Build the final zip
..\zip.exe -X -9 -r ..\%NAME% . -i \*
cd ..

rem DONE!
rmdir /s /q temp
goto END

:MISSINGMSBUILD
@echo Missing msbuild.exe. can't make distro without that.
goto END
:MISSINGGIT
@echo missing git.exe. can't make distro without that.
:END
