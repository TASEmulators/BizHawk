;	Altirra - Atari 800/800XL/5200 emulator
;	Ultimate1MB Recovery BIOS
;	Copyright (C) 2008-2012 Avery Lee
;
;	This program is free software; you can redistribute it and/or modify
;	it under the terms of the GNU General Public License as published by
;	the Free Software Foundation; either version 2 of the License, or
;	(at your option) any later version.
;
;	This program is distributed in the hope that it will be useful,
;	but WITHOUT ANY WARRANTY; without even the implied warranty of
;	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
;	GNU General Public License for more details.
;
;	You should have received a copy of the GNU General Public License
;	along with this program; if not, write to the Free Software
;	Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.

		_KERNEL_PRE_BOOT_HOOK = 1
		_KERNEL_PBI_SUPPORT = 1
		_KERNEL_XLXE = 1
		_KERNEL_USE_BOOT_SCREEN = 0
		_KERNEL_PRINTER_SUPPORT = 1

		icl		'version.inc'
		icl		'cio.inc'
		icl		'sio.inc'

;==========================================================================
; LOWER KERNEL ROM
;
		org		$c000
		opt		f+

		icl		'interrupt.s'
		icl		'keytable.s'
		icl		'irq.s'
		icl		'vbi.s'
		icl		'disk.s'
		icl		'boot.s'
		icl		'blackboard.s'
		icl		'pbi.s'
		icl		'phandler.s'
		icl		'misc.s'
		
BootScreen = Blackboard
		
		org		$cc00
		icl		'atariifont.inc'
		
;==========================================================================
; SELF-TEST ROM
;
		org		$d000


;==========================================================================
; UPPER KERNEL ROM
;
		org		$d800
		icl		'mathpack.s'

		org		$e000
		icl		'atarifont.inc'
		
		org		$e400

editrv	dta		a(EditorOpen-1)
		dta		a(EditorClose-1)
		dta		a(EditorGetByte-1)
		dta		a(EditorPutByte-1)
		dta		a(EditorGetStatus-1)
		dta		a(EditorSpecial-1)
		jmp		EditorInit
		dta		$00

screnv	dta		a(ScreenOpen-1)
		dta		a(ScreenClose-1)
		dta		a(ScreenGetByte-1)
		dta		a(ScreenPutByte-1)
		dta		a(ScreenGetStatus-1)
		dta		a(ScreenSpecial-1)
		jsr		ScreenInit
		dta		$00

keybdv	dta		a(KeyboardOpen-1)
		dta		a(KeyboardClose-1)
		dta		a(KeyboardGetByte-1)
		dta		a(KeyboardPutByte-1)
		dta		a(KeyboardGetStatus-1)
		dta		a(KeyboardSpecial-1)
		jmp		KeyboardInit
		dta		$00
	
printv	dta		a(PrinterOpen-1)
		dta		a(PrinterClose-1)
		dta		a(PrinterGetByte-1)
		dta		a(PrinterPutByte-1)
		dta		a(PrinterGetStatus-1)
		dta		a(PrinterSpecial-1)
		jsr		PrinterInit
		dta		$00

casetv	dta		a(CassetteOpen-1)
		dta		a(CassetteClose-1)
		dta		a(CassetteGetByte-1)
		dta		a(CassettePutByte-1)
		dta		a(CassetteGetStatus-1)
		dta		a(CassetteSpecial-1)
		jsr		CassetteInit
		dta		$00

		org	$e450
diskiv	jsr		DiskInit
dskinv	jmp		DiskHandler
ciov	jmp		CIO
siov	jmp		SIO
setvbv	jmp		VBISetVector
sysvbv	jmp		VBIStage1
xitvbv	jmp		VBIExit
sioinv	jmp		SIOInit
sendev	jsr		SIOSendEnable
intinv	jmp		IntInitInterrupts
cioinv	jmp		CIOInit
blkbdv	jmp		Blackboard
warmsv	jmp		InitWarmStart
coldsv	jmp		InitColdStart
rblokv	jmp		CassetteReadBlock
csopiv	jmp		CassetteOpenRead
pupdiv	jsr		SelfTestEntry		;$E480	1200XL: Power-on display; XL/XE: self-test
slftsv	jmp		$5000				;$E483	XL: Self-test ($5000)
pentv	jmp		PHAddHandler		;$E486	XL: add handler to HATABS
phunlv	jmp		PHRemoveHandler		;$E489	XL: 
phiniv	jmp		PHInitHandler		;$E48C	XL:
gpdvv	PBI_VECTOR_TABLE			;$E48F	XL: Generic device vector

;==========================================================================

		icl		'init.s'
		icl		'sio.s'
		icl		'cio.s'
		icl		'cassette.s'
		icl		'printer.s'
		icl		'selftestentry.s'

;==========================================================================

.proc InitPreBootHook
		mva		#CIOCmdPutChars iccmd
		mwa		#message_start icbal
		mwa		#message_end-message_start icbll
		ldx		#0
		jsr		ciov
		jmp		KeyboardGetByte

message_start:
		dta		c"Altirra Ultimate1MB recovery BIOS",$9B
		dta		$9B
		dta		c"Insert BIOS flash disk, press key",$9B
message_end:
.endp

;==========================================================================

		icl		'screen.s'
		icl		'editor.s'
		icl		'screenext.s'
		icl		'keyboard.s'
		
;==========================================================================

		org		$fffa
		dta		a(IntDispatchNMI)
		dta		a(InitReset)
		dta		a(IntDispatchIRQ)
		
		opt		f-
