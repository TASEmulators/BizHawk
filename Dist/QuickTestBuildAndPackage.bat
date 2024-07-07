set PATH=%PATH%;C:\Program Files (x86)\git\bin;C:\Program Files\git\bin

if "%1"=="" (
	SET NAME=BizHawk.zip
) else (
	SET NAME=%1
)

git --version > NUL
@if errorlevel 1 goto MISSINGGIT

dotnet build ..\BizHawk.sln -c Release --no-incremental
@if not errorlevel 0 goto DOTNETBUILDFAILED
rem -p:Platform="Any CPU"
rem -p:RunAnalyzersDuringBuild=true

rmdir /s /q temp
del /s %NAME%
cd ..\output

rem Now, we're about to zip and then unzip. Why, you ask? Because that's just the way this evolved.
..\dist\zip.exe -X -r ..\Dist\%NAME% EmuHawk.exe EmuHawk.exe.config DiscoHawk.exe DiscoHawk.exe.config defctrl.json EmuHawkMono.sh dll Shaders gamedb Tools NES\Palettes Lua Gameboy\Palettes overlay -x *.pdb -x *.lib -x *.pgd -x *.ipdb -x *.iobj -x *.exp -x *.ilk

cd ..\Dist
.\unzip.exe %NAME% -d temp
del %NAME%

rem Remove things we can't allow the user's junky files to pollute the dist with. We'll export fresh copies from git
rmdir /s /q temp\lua
rmdir /s /q temp\firmware

rmdir /s /q gitsucks
git --git-dir ../.git archive --format zip --output lua.zip HEAD Assets/Lua
git --git-dir ../.git archive --format zip --output firmware.zip HEAD output/Firmware

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
mkdir Firmware

rem Build the final zip
..\zip.exe -X -9 -r ..\%NAME% . -i \*
cd ..

rem DONE!
rmdir /s /q temp
goto END

:DOTNETBUILDFAILED
set ERRORLEVEL=1
@echo dotnet build failed. Usual cause: user committed broken code, or unavailable dotnet sdk
goto END

:MISSINGGIT
set ERRORLEVEL=1
@echo missing git.exe. can't make distro without that.
:END
