ATCOMPILER = $(OUT)\atcompiler.exe

SRCFILES = source\main.xasm \
source\loader.xasm

SHARED_SRCFILESMIN = \
source\Shared\atarifont.bin \
source\Shared\atarifont.inc

SHARED_SRCFILESINC = \
source\Shared\cio.inc \
source\Shared\sio.inc \
source\Shared\hardware.inc \
source\Shared\kernel.inc \
source\Shared\kerneldb.inc

SHARED_SRCFILES = \
source\Shared\atarifont.bin \
source\Shared\atariifont.bin \
source\Shared\atarifont.inc \
source\Shared\atariifont.inc \
source\Shared\boot.s \
source\Shared\bootscreen.s \
source\Shared\blackboard.s \
source\Shared\cio.inc \
source\Shared\cio.s \
source\Shared\disk.s \
source\Shared\editor.s \
source\Shared\hardware.inc \
source\Shared\init.s \
source\Shared\interrupt.s \
source\Shared\irq.s \
source\Shared\kerneldb.inc \
source\Shared\keyboard.s \
source\Shared\keytable.s \
source\Shared\mathpack.s \
source\Shared\screen.s \
source\Shared\screenext.s \
source\Shared\vbi.s \
source\Shared\sio.inc \
source\Shared\sio.s \
source\Shared\pbi.s \
source\Shared\phandler.s \
source\Shared\cassette.s \
source\Shared\misc.s \
source\Shared\bootscreen.s \
source\Shared\driveimage.inc \
source\Shared\selftestentry.s \
source\Shared\printer.s \
source\Shared\version.inc

SHARED_SRCFILES816 = \
source\Shared\cio816.s \
source\Shared\init816.s \
source\Shared\interrupt816.s \
source\Shared\irq816.s \
source\Shared\keyboard816.s \
source\Shared\screenext816.s \
source\Shared\syscall816.s \
source\Shared\sysdev816.s \
source\Shared\vbi816.s

NOKERNEL_SRCFILES = source\nokernel\main.xasm
BASIC_SRCFILES = source\basic\main.xasm
SUPERKERNEL_SRCFILES = source\superkernel\main.xasm source\superkernel\includes.xasm
NOCARTRIDGE_SRCFILES = source\nocartridge\main.s
ATDISKLOADER_SRCFILES = source\ATDiskLoader\main.s
NOHDBIOS_SRCFILES = source\NoHDBIOS\main.s
NOGAME_SRCFILES = source\NoGame\main.s
ULTIMATE_SRCFILES = source\Ultimate\main.s source\Ultimate\bios.s source\Ultimate\kernel.s
BOOTSECDOS2_SRCFILES = source\BootSector\bootsecdos2.s
SOFTKICK_SRCFILES = source\softkick.s

VERSION_SRCFILES =

all: madscheck atcompilercheck kernel kernelxl kernelxlsoftkick kernel816 \
	nokernel basic superkernel nocartridge \
	atdiskloader64 atdiskloader128 \
	nohdbios nomio noblackbox \
	nogame ultimate bootsecdos2 \
	r850 fw1030 additions \
	exeloader \
	sapplayers \
	pbidisk \
	rapidflash \
	rapidpbi16

clean:
	if exist "$(OUT)\kernel.rom" del "$(OUT)\kernel.rom"
	if exist "$(OUT)\kernelxl.rom" del "$(OUT)\kernelxl.rom"
	if exist "$(OUT)\kernel816.rom" del "$(OUT)\kernel816.rom"
	if exist "$(OUT)\nokernel.rom" del "$(OUT)\nokernel.rom"
	if exist "$(OUT)\basic.rom" del "$(OUT)\basic.rom"
	if exist "$(OUT)\superkernel.rom" del "$(OUT)\superkernel.rom"
	if exist "$(OUT)\nocartridge.rom" del "$(OUT)\nocartridge.rom"
	if exist "$(OUT)\atdiskloader64.bin" del "$(OUT)\atdiskloader64.bin"
	if exist "$(OUT)\atdiskloader128.bin" del "$(OUT)\atdiskloader128.bin"
	if exist "$(OUT)\nohdbios.rom" del "$(OUT)\nohdbios.rom"
	if exist "$(OUT)\nogame.rom" del "$(OUT)\nogame.rom"
	if exist "$(OUT)\ultimate.rom" del "$(OUT)\ultimate.rom"
	if exist "$(OUT)\ultimate.lzrom" del "$(OUT)\ultimate.lzrom"
	if exist "$(OUT)\bootsecdos2.bin" del "$(OUT)\bootsecdos2.bin"
	if exist "$(OUT)\850relocator.bin" del "$(OUT)\850relocator.bin"
	if exist "$(OUT)\850handler.bin" del "$(OUT)\850handler.bin"
	if exist "$(INT)\*.obx" del "$(INT)\*.obx"
	if exist "$(INT)\*.xex" del "$(INT)\*.xex"
	if exist "$(INT)\*.bin" del "$(INT)\*.bin"
	if exist "$(INT)\*.lzrom" del "$(INT)\*.lzrom"
	if exist "$(OUT)\1030firmware.bin" del "$(OUT)\1030firmware.bin"
	if exist "$(OUT)\rapidflash.bin" del "$(OUT)\rapidflash.bin"
	if exist "$(INT)\rapidos.bin" del "$(INT)\rapidos.bin"
	if exist "$(OUT)\rapidpbi16.bin" del "$(OUT)\rapidpbi16.bin"

madscheck: $(INT)\madscheck.bin

$(INT)\madscheck.bin: makefile
	@if not exist "$(MADS)" ((where "$(MADS)" /q 2>nul) || (echo Error: Cannot find MADS assembler at path: $(MADS). Consult BUILD-HOWTO.html for how to resolve. && exit /b 20))
	@echo. >"$(INT)\madscheck.bin"

atcompilercheck:
	@if not exist "$(ATCOMPILER)" (echo Error: Cannot find ATCompiler at path: $(ATCOMPILER). Check that the ATCompiler tool was properly built. && exit /b 20)

kernel: $(OUT)\kernel.rom

kernelxl: $(OUT)\kernelxl.rom

kernelxlsoftkick: $(OUT)\kernelxlsoftkick.xex

kernel816: $(OUT)\kernel816.rom

nokernel: $(OUT)\nokernel.rom

basic: $(OUT)\basic.rom

superkernel: $(OUT)\superkernel.rom

nocartridge: $(OUT)\nocartridge.rom

atdiskloader64: $(OUT)\atdiskloader64.bin

atdiskloader128: $(OUT)\atdiskloader128.bin

nohdbios: $(OUT)\nohdbios.rom

nomio: $(OUT)\nomio.lzrom

noblackbox: $(OUT)\noblackbox.lzrom

nogame: $(OUT)\nogame.rom

ultimate: $(OUT)\ultimate.lzrom

bootsecdos2: $(OUT)\bootsecdos2.bin

r850: $(OUT)\850relocator.bin $(OUT)\850handler.bin

fw1030: $(OUT)\1030firmware.bin

additions: $(OUT)\Additions.atr

exeloader: $(INT)\exeloader.bin $(INT)\exeloader-0700.bin $(INT)\exeloader-hispeed.bin $(INT)\exeloader-hispeed-0700.bin $(INT)\exeloader-nobasic.bin

sapplayers: $(INT)\playsap-b.xex $(INT)\playsap-c.xex $(INT)\playsap-d-ntsc.xex $(INT)\playsap-d-pal.xex $(INT)\playsap-r.xex

pbidisk: $(INT)\pbidisk.bin

rapidflash: $(INT)\rapidflash.lzrom

rapidpbi16: $(OUT)\rapidpbi16.bin

$(INT)\additions-license.txt: source\Additions\license.txt
	copy "source\Additions\license.txt" "$(INT)\additions-license.txt"

$(OUT)\Additions.atr: $(INT)\dos.sys source\Additions\filelist.txt $(INT)\boot850.xex $(INT)\rhnd850.xex $(INT)\xep80.sys $(INT)\xep80f.sys $(INT)\boot1030.xex $(INT)\thnd1030.xex $(INT)\autorun.sys $(INT)\atbasicx.xex $(INT)\sx212.xex $(INT)\colormap.xex $(INT)\additions-license.txt $(INT)\loadexe.xex
	@"$(ATCOMPILER)"  mkfsdos2 source\Additions\filelist.txt $(INT)\ $(OUT)\Additions.atr

$(OUT)\kernel.rom: $(SRCFILES) $(SHARED_SRCFILES)
	@for %%x in (source\main.xasm) do "$(MADS)" -i:autobuild -i:autobuild_default -d:_KERNEL_XLXE=0 -s -p -i:source\Shared -b:$$d800 -l:$(OUT)\kernel.lst -t:$(OUT)\kernel.lab -o:$(OUT)\kernel.rom %~fx

$(OUT)\kernelxl.rom: $(SRCFILES) $(SHARED_SRCFILES) $(VERSION_SRCFILES)
	@for %%x in (source\main.xasm) do "$(MADS)" -i:autobuild -i:autobuild_default -d:_KERNEL_XLXE=1 -s -p -i:source\Shared -b:$$c000 -l:$(OUT)\kernelxl.lst -t:$(OUT)\kernelxl.lab -o:$(OUT)\kernelxl.rom %~fx

$(OUT)\kernelxlsoftkick.xex: $(SRCFILES) $(SHARED_SRCFILES) $(VERSION_SRCFILES) $(SOFTKICK_SRCFILES) $(OUT)\kernelxlsoft.bin
	@for %%x in (source\softkick.s) do "$(MADS)" -i:$(OUT) -s -p -i:source\Shared -l:$(OUT)\kernelxlsoftkick.lst -t:$(OUT)\kernelxlsoftkick.lab -o:$(OUT)\kernelxlsoftkick.xex %~fx

$(OUT)\kernelxlsoft.bin: $(SRCFILES) $(SHARED_SRCFILES) $(VERSION_SRCFILES)
	@for %%x in (source\main.xasm) do "$(MADS)" -i:autobuild -i:autobuild_default -d:_KERNEL_XLXE=1 -d:_KERNEL_SOFTKICK=1 -s -p -i:source\Shared -b:$$c000 -l:$(OUT)\kernelxlsoft.lst -t:$(OUT)\kernelxlsoft.lab -o:$(OUT)\kernelxlsoft.bin %~fx

$(OUT)\kernel816.rom: $(SRCFILES) $(SHARED_SRCFILES) $(SHARED_SRCFILES816) $(VERSION_SRCFILES)
	@for %%x in (source\main.xasm) do "$(MADS)" -i:autobuild -i:autobuild_default -d:_KERNEL_XLXE=1 -d:_KERNEL_816=1 -s -p -i:source\Shared -b:$$c000 -l:$(OUT)\kernel816.lst -t:$(OUT)\kernel816.lab -o:$(OUT)\kernel816.rom %~fx

$(OUT)\nokernel.rom: $(NOKERNEL_SRCFILES) $(SHARED_SRCFILESMIN)
	@"$(MADS)"  -s -p -i:source\shared -b:$$c000 -l:$(OUT)\nokernel.lst -o:$(OUT)\nokernel.rom source\nokernel\main.xasm

$(OUT)\basic.rom: $(BASIC_SRCFILES)
	@"$(MADS)"  -s -p -b:$$a000 -l:$(OUT)\basic.lst -o:$(OUT)\basic.rom source\basic\main.xasm

$(OUT)\superkernel.rom: $(SUPERKERNEL_SRCFILES)
	@"$(MADS)"  -s -p -i:source\shared -b:$$f800 -l:$(OUT)\superkernel.lst -o:$(OUT)\superkernel.rom source\superkernel\main.xasm

$(OUT)\nocartridge.rom: $(NOCARTRIDGE_SRCFILES)
	@"$(MADS)"  -s -p -b:$$b000 -l:$(OUT)\nocartridge.lst -t:$(OUT)\nocartridge.lab -o:$(OUT)\nocartridge.rom source\nocartridge\main.s

$(OUT)\atdiskloader128.bin: $(ATDISKLOADER_SRCFILES)
	@"$(MADS)"  -d:MEMORY=128 -s -p -b:$$0700 -l:$(OUT)\atdiskloader128.lst -t:$(OUT)\atdiskloader128.lab -o:$(OUT)\atdiskloader128.bin source\ATDiskLoader\main.s

$(OUT)\atdiskloader64.bin: $(ATDISKLOADER_SRCFILES)
	@"$(MADS)"  -d:MEMORY=64 -s -p -b:$$0700 -l:$(OUT)\atdiskloader64.lst -l:$(OUT)\atdiskloader64.lab -o:$(OUT)\atdiskloader64.bin source\ATDiskLoader\main.s

$(OUT)\nohdbios.rom: $(NOHDBIOS_SRCFILES)
	@"$(MADS)"  -s -p -b:$$d800 -l:$(OUT)\nohdbios.lst -o:$(OUT)\nohdbios.rom source\NoHDBIOS\main.s

$(INT)\nomio.rom: source\Placeholders\nomio.s $(SHARED_SRCFILES)
	@"$(MADS)"  -s -p -i:source\Shared -l:$(OUT)\nomio.lst -o:$(INT)\nomio.rom source\Placeholders\nomio.s

$(OUT)\nomio.lzrom: $(INT)\nomio.rom "$(ATCOMPILER)" 
	copy /b "$(INT)\nomio.rom"+"$(INT)\nomio.rom"+"$(INT)\nomio.rom"+"$(INT)\nomio.rom" "$(INT)\nomio2.rom"
	@"$(ATCOMPILER)"  lzpack "$(INT)\nomio2.rom" "$(OUT)\nomio.lzrom"

$(INT)\noblackbox.rom: source\Placeholders\noblackbox.s $(SHARED_SRCFILES)
	@"$(MADS)"  -s -p -i:source\Shared -l:$(OUT)\noblackbox.lst -o:$(INT)\noblackbox.rom source\Placeholders\noblackbox.s

$(OUT)\noblackbox.lzrom: $(INT)\noblackbox.rom "$(ATCOMPILER)" 
	copy /b "$(INT)\noblackbox.rom"+"$(INT)\noblackbox.rom"+"$(INT)\noblackbox.rom"+"$(INT)\noblackbox.rom"+"$(INT)\noblackbox.rom"+"$(INT)\noblackbox.rom"+"$(INT)\noblackbox.rom"+"$(INT)\noblackbox.rom" "$(INT)\noblackbox2.rom"
	@"$(ATCOMPILER)"  lzpack "$(INT)\noblackbox2.rom" "$(OUT)\noblackbox.lzrom"

$(OUT)\nogame.rom: $(NOGAME_SRCFILES)
	@"$(MADS)"  -s -p -b:$$a000 -l:$(OUT)\nogame.lst -o:$(OUT)\nogame.rom source\nogame\main.s

$(OUT)\ultimate.rom: $(ULTIMATE_SRCFILES) $(SHARED_SRCFILES)
	@"$(MADS)"  -s -p -i:source\Shared -l:$(OUT)\ultimate.lst -o:$(OUT)\ultimate.rom source\ultimate\main.s

$(OUT)\ultimate.lzrom: $(OUT)\ultimate.rom "$(ATCOMPILER)" 
	@"$(ATCOMPILER)"  lzpack "$(OUT)\ultimate.rom" "$(OUT)\ultimate.lzrom"

$(OUT)\bootsecdos2.bin: $(BOOTSECDOS2_SRCFILES) $(SHARED_SRCFILESINC)
	@"$(MADS)"  -s -p -i:source\Shared -l:$(OUT)\bootsecdos2.lst -o:$(OUT)\bootsecdos2.bin source\bootsector\bootsecdos2.s

$(INT)\850handler-2800.obx: $(SHARED_SRCFILESINC) source\850\handler.s
	@"$(MADS)"  -D:BASEADDR=10240 -D:HIBUILD=0 -s -p -i:source\Shared -o:$(INT)\850handler-2800.obx -l:$(OUT)\850handler.lst source\850\handler.s
	
$(INT)\850handler-2801.obx: $(SHARED_SRCFILESINC) source\850\handler.s
	@"$(MADS)"  -D:BASEADDR=10241 -D:HIBUILD=0 -s -p -i:source\Shared -o:$(INT)\850handler-2801.obx source\850\handler.s

$(INT)\850handler-a800.obx: $(SHARED_SRCFILESINC) source\850\handler.s
	@"$(MADS)"  -D:BASEADDR=43008 -D:HIBUILD=0 -s -p -i:source\Shared -o:$(INT)\850handler-a800.obx source\850\handler.s

$(INT)\850handler-2800hi.obx: $(SHARED_SRCFILESINC) source\850\handler.s
	@"$(MADS)"  -D:BASEADDR=10240 -D:HIBUILD=1 -s -p -i:source\Shared -o:$(INT)\850handler-2800hi.obx source\850\handler.s

$(INT)\850handler-relocs.inc $(OUT)\850handler.bin: $(INT)\850handler-2800.obx $(INT)\850handler-2801.obx $(INT)\850handler-a800.obx $(INT)\850handler-2800hi.obx "$(ATCOMPILER)" 
	@"$(ATCOMPILER)"  makereloc $(INT)\850handler-2800.obx $(INT)\850handler-2801.obx $(INT)\850handler-a800.obx $(INT)\850handler-2800hi.obx $(OUT)\850handler.bin $(INT)\850handler-relocs.inc

$(OUT)\850relocator.bin: $(SHARED_SRCFILESINC) source\850\relocator.s $(INT)\850handler-2800.obx $(INT)\850handler-2801.obx $(INT)\850handler-a800.obx $(INT)\850handler-relocs.inc
	@"$(MADS)"  -s -p -i:source\Shared -i:$(INT) -l:$(OUT)\850relocator.lst -o:$(OUT)\850relocator.bin source\850\relocator.s

$(INT)\sx212handler-2800.obx: $(SHARED_SRCFILESINC) source\Additions\sx212handler.s
	@"$(MADS)"  -D:BASEADDR=10240 -D:HIBUILD=0 -s -p -i:source\Shared -o:$(INT)\sx212handler-2800.obx -l:$(OUT)\sx212handler.lst source\Additions\sx212handler.s
	
$(INT)\sx212handler-2801.obx: $(SHARED_SRCFILESINC) source\Additions\sx212handler.s
	@"$(MADS)"  -D:BASEADDR=10241 -D:HIBUILD=0 -s -p -i:source\Shared -o:$(INT)\sx212handler-2801.obx source\Additions\sx212handler.s

$(INT)\sx212handler-a800.obx: $(SHARED_SRCFILESINC) source\Additions\sx212handler.s
	@"$(MADS)"  -D:BASEADDR=43008 -D:HIBUILD=0 -s -p -i:source\Shared -o:$(INT)\sx212handler-a800.obx source\Additions\sx212handler.s

$(INT)\sx212handler-2800hi.obx: $(SHARED_SRCFILESINC) source\Additions\sx212handler.s
	@"$(MADS)"  -D:BASEADDR=10240 -D:HIBUILD=1 -s -p -i:source\Shared -o:$(INT)\sx212handler-2800hi.obx source\Additions\sx212handler.s

$(INT)\sx212handler-relocs.inc $(INT)\sx212handler.bin: $(INT)\sx212handler-2800.obx $(INT)\sx212handler-2801.obx $(INT)\sx212handler-a800.obx $(INT)\sx212handler-2800hi.obx "$(ATCOMPILER)" 
	@"$(ATCOMPILER)"  makereloc $(INT)\sx212handler-2800.obx $(INT)\sx212handler-2801.obx $(INT)\sx212handler-a800.obx $(INT)\sx212handler-2800hi.obx $(INT)\sx212handler.bin $(INT)\sx212handler-relocs.inc

$(INT)\sx212.xex: $(SHARED_SRCFILESINC) source\Additions\sx212.s $(INT)\sx212handler.bin
	"$(MADS)"  -s -p -i:source\Shared -i:$(OUT) -i:$(INT) -l:$(INT)\sx212.lst -t:$(INT)\sx212.lab -o:$(INT)\sx212.xex source\Additions\sx212.s

$(INT)\1030handler.bin: $(SHARED_SRCFILESINC) source\1030\handler.s
	@"$(MADS)"  -s -p -D:BASEADDR=7424 -i:source\Shared -i:$(INT) -l:$(INT)\1030handler.lst -t:$(INT)\1030handler.lab -o:$(INT)\1030handler.bin source\1030\handler.s

$(INT)\1030handler-hi.bin: $(SHARED_SRCFILESINC) source\1030\handler.s
	@"$(MADS)"  -s -p -D:BASEADDR=7680 -i:source\Shared -i:$(INT) -o:$(INT)\1030handler-hi.bin source\1030\handler.s

$(OUT)\1030firmware.bin: $(SHARED_SRCFILESINC) source\1030\firmware.s $(INT)\1030handler.bin
	@"$(MADS)"  -s -p -i:source\Shared -i:$(INT) -l:$(OUT)\1030firmware.lst -t:$(OUT)\1030firmware.lab -o:$(OUT)\1030firmware.bin source\1030\firmware.s

$(INT)\boot850.xex: $(SHARED_SRCFILESINC) source\Additions\boot850.s
	"$(MADS)"  -s -p -i:source\Shared -l:$(INT)\boot850.lst -t:$(INT)\boot850.lab -o:$(INT)\boot850.xex source\Additions\boot850.s

$(INT)\rhnd850.xex: $(SHARED_SRCFILESINC) source\Additions\rhnd850.s $(OUT)\850relocator.bin $(OUT)\850handler.bin
	"$(MADS)"  -s -p -i:source\Shared -i:$(OUT) -i:$(INT) -l:$(INT)\rhnd850.lst -t:$(INT)\rhnd850.lab -o:$(INT)\rhnd850.xex source\Additions\rhnd850.s

$(INT)\xep80handler-2800.obx: $(SHARED_SRCFILESINC) source\Additions\xep80handler.s
	"$(MADS)"  -D:BASEADDR=10240 -D:HIBUILD=0 -s -p -i:source\Shared -i:$(OUT) -i:$(INT) -l:$(INT)\xep80handler.lst -t:$(INT)\xep80handler.lab -o:$(INT)\xep80handler-2800.obx source\Additions\xep80handler.s

$(INT)\xep80handler-2801.obx: $(SHARED_SRCFILESINC) source\Additions\xep80handler.s
	"$(MADS)"  -D:BASEADDR=10241 -D:HIBUILD=0 -s -p -i:source\Shared -i:$(OUT) -i:$(INT) -o:$(INT)\xep80handler-2801.obx source\Additions\xep80handler.s

$(INT)\xep80handler-a800.obx: $(SHARED_SRCFILESINC) source\Additions\xep80handler.s
	"$(MADS)"  -D:BASEADDR=43008 -D:HIBUILD=0 -s -p -i:source\Shared -i:$(OUT) -i:$(INT) -o:$(INT)\xep80handler-a800.obx source\Additions\xep80handler.s

$(INT)\xep80handler-2800hi.obx: $(SHARED_SRCFILESINC) source\Additions\xep80handler.s
	"$(MADS)"  -D:BASEADDR=10240 -D:HIBUILD=1 -s -p -i:source\Shared -i:$(OUT) -i:$(INT) -o:$(INT)\xep80handler-2800hi.obx source\Additions\xep80handler.s
	
$(INT)\xep80handler2.bin: $(INT)\xep80handler-2800.obx $(INT)\xep80handler-2801.obx $(INT)\xep80handler-a800.obx $(INT)\xep80handler-2800hi.obx
	"$(ATCOMPILER)"  makereloc $(INT)\xep80handler-2800.obx $(INT)\xep80handler-2801.obx $(INT)\xep80handler-a800.obx $(INT)\xep80handler-2800hi.obx $(INT)\xep80handler2.bin $(INT)\xep80handler-relocs.inc
	
$(INT)\xep80.sys: $(SHARED_SRCFILESINC) source\Additions\xep80.s $(INT)\xep80handler2.bin $(INT)\xep80handler-relocs.inc
	"$(MADS)"  -s -p -i:source\Shared -i:$(OUT) -i:$(INT) -l:$(INT)\xep80.lst -t:$(INT)\xep80.lab -o:$(INT)\xep80.sys source\Additions\xep80.s

$(INT)\xep80handlerf-2800.obx: $(SHARED_SRCFILESINC) source\Additions\xep80handler.s
	"$(MADS)"  -D:BASEADDR=10240 -D:HIBUILD=0 -D:XEP_OPTION_TURBO=1 -s -p -i:source\Shared -i:$(OUT) -i:$(INT) -l:$(INT)\xep80handlerf.lst -t:$(INT)\xep80handlerf.lab -o:$(INT)\xep80handlerf-2800.obx source\Additions\xep80handler.s

$(INT)\xep80handlerf-2801.obx: $(SHARED_SRCFILESINC) source\Additions\xep80handler.s
	"$(MADS)"  -D:BASEADDR=10241 -D:HIBUILD=0 -D:XEP_OPTION_TURBO=1 -s -p -i:source\Shared -i:$(OUT) -i:$(INT) -o:$(INT)\xep80handlerf-2801.obx source\Additions\xep80handler.s

$(INT)\xep80handlerf-a800.obx: $(SHARED_SRCFILESINC) source\Additions\xep80handler.s
	"$(MADS)"  -D:BASEADDR=43008 -D:HIBUILD=0 -D:XEP_OPTION_TURBO=1 -s -p -i:source\Shared -i:$(OUT) -i:$(INT) -o:$(INT)\xep80handlerf-a800.obx source\Additions\xep80handler.s

$(INT)\xep80handlerf-2800hi.obx: $(SHARED_SRCFILESINC) source\Additions\xep80handler.s
	"$(MADS)"  -D:BASEADDR=10240 -D:HIBUILD=1 -D:XEP_OPTION_TURBO=1 -s -p -i:source\Shared -i:$(OUT) -i:$(INT) -o:$(INT)\xep80handlerf-2800hi.obx source\Additions\xep80handler.s
	
$(INT)\xep80handler2f.bin: $(INT)\xep80handlerf-2800.obx $(INT)\xep80handlerf-2801.obx $(INT)\xep80handlerf-a800.obx $(INT)\xep80handlerf-2800hi.obx
	"$(ATCOMPILER)"  makereloc $(INT)\xep80handlerf-2800.obx $(INT)\xep80handlerf-2801.obx $(INT)\xep80handlerf-a800.obx $(INT)\xep80handlerf-2800hi.obx $(INT)\xep80handler2f.bin $(INT)\xep80handlerf-relocs.inc
	
$(INT)\xep80f.sys: $(SHARED_SRCFILESINC) source\Additions\xep80.s $(INT)\xep80handler2f.bin $(INT)\xep80handlerf-relocs.inc
	"$(MADS)"  -s -p -D:XEP_OPTION_TURBO=1 -i:source\Shared -i:$(OUT) -i:$(INT) -l:$(INT)\xep80f.lst -t:$(INT)\xep80f.lab -o:$(INT)\xep80f.sys source\Additions\xep80.s

$(INT)\dos.sys: $(SHARED_SRCFILESINC) source\Additions\dos.s source\Additions\cp.s
	"$(MADS)"  -c -s -p -i:source\Shared -i:$(OUT) -i:$(INT) -l:$(INT)\dos.lst -t:$(INT)\dos.lab -o:$(INT)\dos.sys source\Additions\dos.s

$(INT)\dosexports.inc: $(INT)\dos.lab "$(ATCOMPILER)" 
	"$(ATCOMPILER)"  makeexports $(INT)\dos.lab $(INT)\dosexports.inc export_

$(INT)\autorun.sys: $(SHARED_SRCFILESINC) source\Additions\autorun.s
	"$(MADS)"  -s -p -i:source\Shared -i:$(OUT) -i:$(INT) -l:$(INT)\autorun.lst -t:$(INT)\autorun.lab -o:$(INT)\autorun.sys source\Additions\autorun.s

$(INT)\boot1030.xex: $(SHARED_SRCFILESINC) source\Additions\boot1030.s
	"$(MADS)"  -s -p -i:source\Shared -l:$(INT)\boot1030.lst -t:$(INT)\boot1030.lab -o:$(INT)\boot1030.xex source\Additions\boot1030.s

$(INT)\1030handler-reloc.bin $(INT)\1030handler-reloc.inc: $(INT)\1030handler.bin $(INT)\1030handler-hi.bin
	@"$(ATCOMPILER)"  makereloc3 $(INT)\1030handler.bin $(INT)\1030handler-hi.bin $(INT)\1030handler-reloc.inc $(INT)\1030handler-reloc.bin 0x1D

$(INT)\thnd1030.xex: $(SHARED_SRCFILESINC) source\Additions\thnd1030.s $(INT)\1030handler-reloc.bin $(INT)\1030handler-reloc.inc
	"$(MADS)"  -s -p -i:source\Shared -i:$(OUT) -i:$(INT) -l:$(INT)\thnd1030.lst -t:$(INT)\thnd1030.lab -o:$(INT)\thnd1030.xex source\Additions\thnd1030.s

$(INT)\atbasicx.xex: $(OUT)\atbasicx.xex
	copy $(OUT)\atbasicx.xex $(INT)\atbasicx.xex /y

$(INT)\exeloader.bin: $(SHARED_SRCFILESINC) source\ExeLoader\exeloader.s
	"$(MADS)"  -D:BASEADDR=1536 -s -p -i:source\Shared -i:$(OUT) -i:$(INT) -l:$(INT)\exeloader.lst -t:$(INT)\exeloader.lab -o:$(INT)\exeloader.bin source\ExeLoader\exeloader.s

$(INT)\exeloader-0700.bin: $(SHARED_SRCFILESINC) source\ExeLoader\exeloader.s
	"$(MADS)"  -D:BASEADDR=1792 -s -p -i:source\Shared -i:$(OUT) -i:$(INT) -l:$(INT)\exeloader-0700.lst -t:$(INT)\exeloader-0700.lab -o:$(INT)\exeloader-0700.bin source\ExeLoader\exeloader.s

$(INT)\exeloader-hispeed.bin: $(SHARED_SRCFILESINC) source\ExeLoader\exeloader-hispeed.s
	"$(MADS)"  -D:BASEADDR=1152 -s -p -i:source\Shared -i:$(OUT) -i:$(INT) -l:$(INT)\exeloader-hispeed.lst -t:$(INT)\exeloader-hispeed.lab -o:$(INT)\exeloader-hispeed.bin source\ExeLoader\exeloader-hispeed.s

$(INT)\exeloader-hispeed-0700.bin: $(SHARED_SRCFILESINC) source\ExeLoader\exeloader-hispeed.s
	"$(MADS)"  -D:BASEADDR=1792 -s -p -i:source\Shared -i:$(OUT) -i:$(INT) -l:$(INT)\exeloader-hispeed-0700.lst -t:$(INT)\exeloader-hispeed-0700.lab -o:$(INT)\exeloader-hispeed-0700.bin source\ExeLoader\exeloader-hispeed.s

$(INT)\exeloader-nobasic.bin: $(SHARED_SRCFILESINC) source\ExeLoader\exeloader-nobasic.s
	"$(MADS)"  -s -p -i:source\Shared -i:$(OUT) -i:$(INT) -l:$(INT)\exeloader-nobasic.lst -t:$(INT)\exeloader-nobasic.lab -o:$(INT)\exeloader-nobasic.bin source\ExeLoader\exeloader-nobasic.s

$(INT)\colormap.xex: $(SHARED_SRCFILESINC) source\Additions\colormap.s
	"$(MADS)"  -s -p -i:source\Shared -l:$(INT)\colormap.lst -t:$(INT)\colormap.lab -o:$(INT)\colormap.xex source\Additions\colormap.s

$(INT)\loadexe.xex: $(SHARED_SRCFILESINC) source\Additions\loadexe.s
	"$(MADS)"  -s -p -i:source\Shared -l:$(INT)\loadexe.lst -t:$(INT)\loadexe.lab -o:$(INT)\loadexe.xex source\Additions\loadexe.s

$(INT)\playsap-b.xex: $(SHARED_SRCFILESINC) source\SAPPlayers\playsap-b.s
	"$(MADS)"  -s -p -i:source\Shared -l:$(INT)\playsap-b.lst -t:$(INT)\playsap-b.lab -o:$(INT)\playsap-b.xex source\SAPPlayers\playsap-b.s

$(INT)\playsap-c.xex: $(SHARED_SRCFILESINC) source\SAPPlayers\playsap-c.s
	"$(MADS)"  -s -p -i:source\Shared -l:$(INT)\playsap-c.lst -t:$(INT)\playsap-c.lab -o:$(INT)\playsap-c.xex source\SAPPlayers\playsap-c.s

$(INT)\playsap-d-ntsc.xex: $(SHARED_SRCFILESINC) source\SAPPlayers\playsap-d.s
	"$(MADS)"  -D:PLAYER_PAL=0 -s -p -i:source\Shared -l:$(INT)\playsap-d-ntsc.lst -t:$(INT)\playsap-d-ntsc.lab -o:$(INT)\playsap-d-ntsc.xex source\SAPPlayers\playsap-d.s

$(INT)\playsap-d-pal.xex: $(SHARED_SRCFILESINC) source\SAPPlayers\playsap-d.s
	"$(MADS)"  -D:PLAYER_PAL=1 -s -p -i:source\Shared -l:$(INT)\playsap-d-pal.lst -t:$(INT)\playsap-d-pal.lab -o:$(INT)\playsap-d-pal.xex source\SAPPlayers\playsap-d.s

$(INT)\playsap-r.xex: $(SHARED_SRCFILESINC) source\SAPPlayers\playsap-r.s
	"$(MADS)"  -s -p -i:source\Shared -l:$(INT)\playsap-r.lst -t:$(INT)\playsap-r.lab -o:$(INT)\playsap-r.xex source\SAPPlayers\playsap-r.s

$(INT)\pbidisk.bin: $(SHARED_SRCFILESINC) source\PBIDisk\pbidisk.s
	"$(MADS)"  -s -p -i:source\Shared -l:$(INT)\pbidisk.lst -t:$(INT)\pbidisk.lab -o:$(INT)\pbidisk.bin source\PBIDisk\pbidisk.s

$(INT)\rapidpbi8.bin: $(SHARED_SRCFILESINC) source\Rapidus\rapidpbi8.s
	"$(MADS)"  -s -p -i:source\Shared -l:$(INT)\rapidpbi8.lst -t:$(INT)\rapidpbi8.lab -o:$(INT)\rapidpbi8.bin source\Rapidus\rapidpbi8.s

$(INT)\rapidfirmware.bin: $(SHARED_SRCFILESINC) source\Rapidus\rapidfirmware.s
	"$(MADS)"  -s -p -i:source\Shared -l:$(INT)\rapidfirmware.lst -t:$(INT)\rapidfirmware.lab -o:$(INT)\rapidfirmware.bin source\Rapidus\rapidfirmware.s

$(OUT)\rapidpbi16.bin: $(SHARED_SRCFILESINC) source\Rapidus\rapidpbi16.s
	"$(MADS)"  -s -p -i:source\Shared -l:$(OUT)\rapidpbi16.lst -t:$(OUT)\rapidpbi16.lab -o:$(OUT)\rapidpbi16.bin source\Rapidus\rapidpbi16.s

$(INT)\rapidos.bin: $(SHARED_SRCFILESINC) source\Rapidus\rapidos.s
	"$(MADS)"  -s -p -i:source\Shared -i:$(INT) -o:$(INT)\rapidos.bin source\Rapidus\rapidos.s

$(OUT)\rapidflash.bin: $(INT)\rapidpbi8.bin $(INT)\rapidfirmware.bin source\Rapidus\rapidflash.s $(INT)\rapidos.bin
	"$(MADS)"  -s -p -i:source\Shared -i:$(INT) -o:$(OUT)\rapidflash.bin source\Rapidus\rapidflash.s

$(INT)\rapidflash.lzrom: $(OUT)\rapidflash.bin
	@"$(ATCOMPILER)"  lzpack "$(OUT)\rapidflash.bin" "$(INT)\rapidflash.lzrom"
