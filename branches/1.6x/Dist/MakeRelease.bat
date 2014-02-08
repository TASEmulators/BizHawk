del /s BizHawk.zip
cd ..\output

rem slimdx has a way of not making it into the output directory, so this is a good time to make sure
copy ..\..\SlimDx.dll

rem at the present we support moving all these dlls into the dll subdirectory
rem that might be troublesome some day..... if it does get troublesome, then we'll have to 
rem explicitly list the OK ones here as individual copies. until then....

copy *.dll dll

zip -X -9 -r ..\Dist\BizHawk.zip EmuHawk.exe DiscoHawk.exe defctrl.json dll gamedb Firmware NES\Palettes Lua Gameboy\Palettes Firmware/BIOS_Info.txt  -x *.pdb -x *.lib -x *.pgd -x *.exp -x dll\libsneshawk-64*.exe
