svn --version > NUL
@if errorlevel 1 goto MISSINGSVN

call msbuild.exe  ..\BizHawk.sln /p:Configuration=Release /p:Platform="x86" /t:rebuild

rmdir /s /q temp
del /s BizHawk.zip
cd ..\output

rem slimdx has a way of not making it into the output directory, so this is a good time to make sure
copy ..\..\SlimDx.dll

rem at the present we support moving all these dlls into the dll subdirectory
rem that might be troublesome some day..... if it does get troublesome, then we'll have to 
rem explicitly list the OK ones here as individual copies. until then....

copy *.dll dll

..\dist\zip.exe -X -r ..\Dist\BizHawk.zip EmuHawk.exe DiscoHawk.exe defctrl.json dll shaders gamedb NES\Palettes Lua Gameboy\Palettes -x *.pdb -x *.lib -x *.pgd -x *.exp -x dll\libsneshawk-64*.exe -x *.ilk

cd ..\Dist
.\unzip.exe BizHawk.zip -d temp
del BizHawk.zip

rmdir /s /q temp\lua
svn export ..\output\lua temp\Lua
svn export ..\output\firmware temp\Firmware

cd temp
upx -d dll\*.dll
upx -d dll\*.exe
upx -d *.exe
..\zip.exe -X -9 -r ..\BizHawk.zip . -i \*
cd ..

rmdir /s /q temp
goto END

:MISSINGSVN
@echo missing svn.exe. can't make distro without that.
:END