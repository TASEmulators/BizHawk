;	Altirra - Atari 800/800XL/5200 emulator
;	Rapidus emulator bootstrap firmware - 65C816 PBI device
;	Copyright (C) 2008-2017 Avery Lee
;
;	Copying and distribution of this file, with or without modification,
;	are permitted in any medium without royalty provided the copyright
;	notice and this notice are preserved.  This file is offered as-is,
;	without any warranty.

;==========================================================================
; Rapidus 65C816 PBI bootstrap firmware
;
; This PBI firmware appears in place of the 6502 PBI firmware when the
; 65C816 is active. The FPGA core is already initialized by this point,
; so this firmware simply needs to invoke the boot code in flash. Unlike
; the 6502 PBI firmware, this firmware comes directly from the FPGA core
; itself, which we don't have an easy way to access.
;
; Note that the OS in use may not be '816-capable and so interrupts need
; to be managed while the boot code is active. This firmware will work
; with the real Rapidus boot code but is a bit ad-hoc -- the firmware
; needs an NMI hook pointed to by $D81E to stuff into the native NMI
; vector to handle VBIs while it is running. It isn't clear what this
; handler is supposed to do and the SRAM overlays are active while this
; is invoked, so calling the OS routines doesn't work and we just stub
; it for now.
;

		icl		'kerneldb.inc'
		icl		'hardware.inc'

coldsv = $E477

;==========================================================================
; Rapidus firmware entry points (may be wrong...)
;
rpfw_menu_check		= $F00010
rpfw_menu_vbi		= $F00020
rpfw_eeprom_init	= $F00024
rpfw_eeprom_reset	= $F00028
rpfw_apply_config	= $F0002C
rpfw_eeprom_read	= $F00030
rpfw_eeprom_chk		= $F00038
rpfw_syscall		= $F0003C
rpfw_init			= $F00048
rpfw_cleanup		= $F00050

;==========================================================================
; Rapidus EEPROM usage:
;
;	$00: checksum
;	$03: Memory configuration register value ($FF0080)
;	$04: Complementary memory configuration register value ($FF0081)
;	$05: SDRAM configuration register value ($FF0082)
;	$06: Add-on configuration register value ($FF0083)
;	$07:
;		D0 = 1: Boot in 6502C mode
;
; Default: A5 00 00 EF 81 80 00 00

;==========================================================================
		org		$D800
		opt		h-f+c+

		dta		a(0)			;D800 | checksum (unused)
		dta		0				;D802 | revision (unused)
		dta		$80				;D803 | ID byte
		dta		0				;D804 | device type (unused)
		jmp		SIOHandler		;D805 | SIO vector
		jmp		IRQHandler		;D808 | IRQ vector
		dta		$91				;D80B | ID byte
		dta		0				;D80C | device name (unused)
		dta		a(CIOOpen)		;D80D | 
		dta		a(CIOClose)		;D80F | 
		dta		a(CIOGetByte)	;D811 | 
		dta		a(CIOPutByte)	;D813 | 
		dta		a(CIOGetStatus)	;D815 | 
		dta		a(CIOSpecial)	;D817 | 
		jmp		Init			;D819 |

		dta		a(0)
		dta		a(NativeNMI)	;D81E | Used by Rapidus boot code

;==========================================================================
.proc Init
		;check for 'RA' at start of boot ROM
		lda		$F00000
		cmp		#$52
		bne		invalid_boot
		lda		$F00001
		cmp		#$41
		bne		invalid_boot

		;looks valid, let's run it
		lda:rne	vcount
		lda:req	vcount
		lda:rne	vcount
		lda:req	vcount
		sta		wsync
		sta		wsync

		;enter native mode
		php
		sei
		mva		#0 nmien
		clc
		xce

		;initialize firmware
		jsl		rpfw_init

		;invoke menu if inverse key held
		jsl		rpfw_menu_check

		;set up EEPROM I2C interface
		jsl		rpfw_eeprom_init

		;compute EEPROM checksum of $07 down to $01, but not $00
		ldx		#7
		lda		#1
		jsl		rpfw_eeprom_chk
		pha

		;read EEPROM checksum byte
		ldx		#0
		jsl		rpfw_eeprom_read

		;check if checksum is valid
		eor		1,s
		plx
		tax
		beq		eeprom_valid

		;reset EEPROM contents
		jsl		rpfw_eeprom_reset

eeprom_valid:
		;check for 6502 boot
		ldx		#7
		jsl		rpfw_eeprom_read
		lsr
		bcs		do_6502

		;set hardware registers $FF0080-FF0084 from EEPROM
		lda		$FF0080
		pha
		jsl		rpfw_apply_config

		;check if the OS was changed -- if so, force a reboot
		pla
		eor		$FF0080
		bmi		reset_OS

		;clean up
		jsl		rpfw_cleanup

exit:
		;reenter emulation mode
		sec
		xce
		mva		#$40 nmien
		plp

invalid_boot:
		rts

do_6502:
		lda		#0
		xba
		lda		#7
		jsl		rpfw_syscall

reset_OS:
		;reenter emulation mode
		sec
		xce

		;reset stack and copy stub to bottom of it
		ldx		#$ff
		txs
		pea		#coldsv		;STZ $D1FF / JMP COLDSV
		pea		#$4CD1
		pea		#$FF9C

		;Run stub; note that NMIs and IRQs are already disabled. This will
		;restart on the new OS and re-run us, but we won't repeat the
		;process because the desired OS will already have been selected.
		jmp		$01FA
.endp

;==========================================================================
SIOHandler:
IRQHandler:
CIOOpen:
CIOClose:
CIOGetByte:
CIOPutByte:
CIOGetStatus:
CIOSpecial:
		rts

;==========================================================================
.proc NativeNMI
		;save and reset DBK
		phb
		phk
		plb

		;check if it's an VBI or DLI
		bit		nmist
		bpl		is_vbi

		;DLI - ignore it
		plb
		rti

is_vbi:
		jmp		rpfw_menu_vbi
.endp

;==========================================================================

		.align	$800,$FF
