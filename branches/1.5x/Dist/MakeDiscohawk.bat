pushd %~dp0\
del /s DiscoHawk.zip
set DIR=..\BizHawk.MultiClient\output
set BUILDDIR=%~dp0
echo %BUILDIR%
cd "%DIR%"
"%BUILDDIR%\zip" -r -X -9 "%BUILDDIR%\DiscoHawk.zip" BizHawk.Emulation.dll DiscoHawk.exe ffmpeg.exe
popd