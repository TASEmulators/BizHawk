;	Altirra - Atari 800/800XL/5200 emulator
;	MIO firmware placeholder
;	Copyright (C) 2008-2015 Avery Lee
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

grafp0	equ	$d00d
grafp1	equ	$d00e
grafp2	equ	$d00f
grafp3	equ	$d010
trig0	equ	$d010
grafm	equ	$d011
trig1	equ	$d011
trig2	equ	$d012
colpm0	equ	$d012
trig3	equ	$d013
colpm1	equ	$d013
colpm2	equ	$d014
colpm3	equ	$d015
colpf0	equ	$d016
colpf1	equ	$d017
colpf2	equ	$d018
colpf3	equ	$d019
colbk	equ	$d01a
prior	equ	$d01b
gractl	equ	$d01d
consol	equ	$d01f
pot0	equ	$d200
audf1	equ	$d200
pot1	equ	$d201
audc1	equ	$d201
pot2	equ	$d202
audf2	equ	$d202
pot3	equ	$d203
audc2	equ	$d203
pot4	equ	$d204
audf3	equ	$d204
pot5	equ	$d205
audc3	equ	$d205
pot6	equ	$d206
audf4	equ	$d206
pot7	equ	$d207
audc4	equ	$d207
audctl	equ	$d208
kbcode	equ	$d209
skres	equ	$d20a
serin	equ	$d20d
serout	equ	$d20d
irqen	equ	$d20e
irqst	equ	$d20e
skctl	equ	$d20f
porta	equ	$d300
portb	equ	$d301
pactl	equ	$d302
pbctl	equ	$d303
dmactl	equ	$d400
chactl	equ	$d401
dlistl	equ	$d402
dlisth	equ	$d403
hscrol	equ	$d404
vscrol	equ	$d405
chbase	equ	$d409
wsync	equ	$d40a
vcount	equ	$d40b
nmien	equ	$d40e
nmist	equ	$d40f
nmires	equ	$d40f

		opt		h-o+f+

		org		$d800
		
		dta		0				;$D800 *ROM checksum low byte
		dta		0				;$D801 *ROM checksum high byte
		dta		$01				;$D802 *ROM revision number
		dta		$80				;$D803 ID number
		dta		$00				;$D804 *Device type
		jmp		DoIO			;$D805 I/O vector
		jmp		DoIRQ			;$D808 Interrupt vector
		dta		$91				;$D80B ID number
		dta		$00				;$D80C Device name (ASCII)
		dta		a(0)			;$D80D CIO open vector (for generic device handler)
		dta		a(0)			;$D80F CIO close vector (for generic device handler)
		dta		a(0)			;$D811 CIO get byte vector (for generic device handler)
		dta		a(0)			;$D813 CIO put byte vector (for generic device handler)
		dta		a(0)			;$D815 CIO status vector (for generic device handler)
		dta		a(0)			;$D817 CIO special vector (for generic device handler)
		jmp		DoInit			;$D819 Initialization vector
		dta		$00				;$D81C *unused
		
;==========================================================================
.proc DoIO
		brk
.endp

;==========================================================================
.proc DoIRQ
		brk
.endp

;==========================================================================
.proc	DoInit
		;disable all interrupts and ANTIC DMA
		sei
		lda		#0
		sta		nmien
		sta		dmactl
		
		;blank all sprites
		sta		gractl
		sta		grafp0
		sta		grafp1
		sta		grafp2
		sta		grafp3
		sta		grafm
		
		;restore sane state
		cld
		ldx		#$ff
		txs
		
		;wait for vertical blank (scan line 248)
		lda		#124
		cmp:rne	vcount
		
		;set up display list
		mwa		#display_list dlistl
		
		;set up character set and colors
		mva		#$e0 chbase
		mva		#$02 chactl
		mva		#$28 colpf0
		mva		#$ca colpf1
		mva		#$94 colpf2
		mva		#$46 colpf3
		mva		#$00 colbk

		;turn on display DMA, normal playfield width
		mva		#$22 dmactl
		
		;jam the system
		jmp		*
.endp
	
;==========================================================================

		org		$dc00

display_list:
		dta		$70
		dta		$70
		dta		$70
		dta		$42,a(message)
		dta		$30
		dta		$02
		dta		$02
		dta		$02
		dta		$02
		dta		$02
		dta		$02
		dta		$30
		dta		$02
		dta		$41,a(display_list)
	
message:
		;		 0123456789012345678901234567890123456789
		dta		"Altirra MIO placeholder ROM             "
		dta		"MIO emulation is enabled under Devices, "
		dta		"but no firmware ROM has been set up for "
		dta		"it. This is a placeholder for the       "
		dta		"missing firmware ROM. See the help file "
		dta		"for instructions on how to use a real   "
		dta		"firmware ROM image.                     "
		dta		"System halted                           "


;==========================================================================

		org		$dfff
		dta		$00

		end
