;	Altirra - Atari 800/800XL/5200 emulator
;	SAP type D player
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

		icl		'hardware.inc'
		icl		'kerneldb.inc'

		org		$0400
InitMusic:
		jmp		dummy
PlayMusic:
		jmp		dummy

playfield:		;0123456789012345678901234567890123456789
		dta		"  Name:                                 "
		dta		"  Author:                               "

dlist:
dlipt_1:
		dta		$70
		dta		$70
		dta		$70
		dta		$70
		dta		$70
		dta		$70
dlipt_2:
		dta		$70
		dta		$42,a(playfield)
		dta		$02
		dta		$70
		dta		$70
		dta		$70
dlipt_3:
		dta		$70
		dta		$70
		dta		$70
		dta		$70
		dta		$70
		dta		$70
dlipt_4:
		dta		$70
		dta		$70
		dta		$70
		dta		$70
		dta		$70
		dta		$70
dlipt_5:
		dta		$70
		dta		$41,a(dlist)

state_table:
state_0:				dta		dlipt_1-dlist, $70, dlipt_1-dlist, $70, state_0-state_0, $41

.if PLAYER_PAL==0
state_ntsc_on_pal_0:	dta		dlipt_5-dlist, $F0, dlipt_5-dlist, $F0, state_ntsc_on_pal_1-state_0, $81
state_ntsc_on_pal_1:	dta		dlipt_5-dlist, $70, dlipt_4-dlist, $F0, state_ntsc_on_pal_2-state_0, $81
state_ntsc_on_pal_2:	dta		dlipt_4-dlist, $70, dlipt_3-dlist, $F0, state_ntsc_on_pal_3-state_0, $81
state_ntsc_on_pal_3:	dta		dlipt_3-dlist, $70, dlipt_2-dlist, $F0, state_ntsc_on_pal_4-state_0, $81
state_ntsc_on_pal_4:	dta		dlipt_2-dlist, $70, dlipt_1-dlist, $F0, state_ntsc_on_pal_5-state_0, $81
state_ntsc_on_pal_5:	dta		dlipt_1-dlist, $70, dlipt_1-dlist, $70, state_ntsc_on_pal_0-state_0, $41
.endif

.if PLAYER_PAL==1
state_pal_on_ntsc_0:	dta		dlipt_1-dlist, $70, dlipt_1-dlist, $70, state_pal_on_ntsc_1-state_0, $41	;fire at VBI
state_pal_on_ntsc_1:	dta		dlipt_1-dlist, $70, dlipt_1-dlist, $F0, state_pal_on_ntsc_2-state_0, $80	;wait at VBI
state_pal_on_ntsc_2:	dta		dlipt_1-dlist, $70, dlipt_1-dlist, $F0, state_pal_on_ntsc_3-state_0, $41	;fire at DLI 1
state_pal_on_ntsc_3:	dta		dlipt_1-dlist, $70, dlipt_2-dlist, $F0, state_pal_on_ntsc_4-state_0, $80	;wait at VBI
state_pal_on_ntsc_4:	dta		dlipt_1-dlist, $70, dlipt_2-dlist, $F0, state_pal_on_ntsc_5-state_0, $41	;fire at DLI 2
state_pal_on_ntsc_5:	dta		dlipt_2-dlist, $70, dlipt_3-dlist, $F0, state_pal_on_ntsc_6-state_0, $80	;wait at VBI
state_pal_on_ntsc_6:	dta		dlipt_2-dlist, $70, dlipt_3-dlist, $F0, state_pal_on_ntsc_7-state_0, $41	;fire at DLI 3
state_pal_on_ntsc_7:	dta		dlipt_3-dlist, $70, dlipt_4-dlist, $F0, state_pal_on_ntsc_8-state_0, $80	;wait at VBI
state_pal_on_ntsc_8:	dta		dlipt_3-dlist, $70, dlipt_4-dlist, $F0, state_pal_on_ntsc_9-state_0, $41	;fire at DLI 4
state_pal_on_ntsc_9:	dta		dlipt_4-dlist, $70, dlipt_5-dlist, $F0, state_pal_on_ntsc_0-state_0, $40	;wait at VBI
.endif

;==========================================================================
dummy:
		rts

;==========================================================================
.proc Main
		;turn off interrupts
		sei
		lda		#0
		sta		nmien
		sta		irqen

		;wait for VBI
		lda		#248/2
		cmp:rne	vcount

		;set up display
		mva		#$22 dmactl
		mva		#$e0 chbase
		mwa		#dlist dlistl
		mva		#0 colbk
		sta		colpf2
		mva		#$0e colpf1

		;take over DLI/VBI vectors
		mwa		#NmiHandler vdslst
		mwa		#VbiHandler vvblki

		;copy down the font in case we can bank out the OS
		ldx		#0
copyfont_loop:
		mva		$e000,x $0c00,x
		mva		$e100,x $0d00,x
		mva		$e200,x $0e00,x
		mva		$e300,x $0f00,x
		inx
		bne		copyfont_loop

		;try to bank out the OS ROM
		lda		#$3c
		ldx		#$38

		sta		pbctl		;switch to IORB
		ldy		#$fe
		sty		portb		;port B outputs -> $FE
		stx		pbctl		;switch to DDRB
		iny
		sty		portb		;port B -> all output
		lda		$fffa
		eor		#$ff
		sta		$fffa
		cmp		$fffa
		bne		os_rom_only

		;we have OS RAM now -- copy the font back
		ldx		#0
copyfont2_loop:
		mva		$0c00,x $e000,x
		mva		$0d00,x $e100,x
		mva		$0e00,x $e200,x
		mva		$0f00,x $e300,x
		inx
		bne		copyfont2_loop

		;set up NMI vector
		mwa		#NmiHandler $fffa
		jmp		post_osram_setup

os_rom_only:
		;we are stuck with OS ROM -- switch port B back to input
		lda		#0
		sta		portb

post_osram_setup:

		;detect ANTIC type
		lda:rpl	vcount
		ldx		#0
detloop:
		lda		vcount
		bpl		detdone
		tax
		bcs		detloop
detdone:

		cpx		#282/2
.if PLAYER_PAL==1
		bcs		system_match
		lda		#state_pal_on_ntsc_0-state_0
		sta		state
.else
		bcc		system_match
		lda		#state_ntsc_on_pal_0-state_0
		sta		state
.endif

system_match:

		;initialize POKEY
		mva		#3 skctl
		lda		#0
		ldx		#8
		sta:rpl	$d200,x-

		;turn on the VBI
		mva		#$40 nmien

		;enable IRQs (note that all are off)
		cli

		;run mainline player
		jmp		InitMusic
.endp

;==========================================================================
lock	dta		$FF

NmiHandler:
		pha
		txa
		pha
		tya
		pha
VbiHandler:
		inc		lock
		bne		already_running

		ldx		#0
state = *-1
		ldy		state_table,x
		lda		state_table+1,x
		sta		dlist,y
		ldy		state_table+2,x
		lda		state_table+3,x
		sta		dlist,y
		lda		state_table+4,x
		sta		state

		lda		state_table+5,x
		sta		nmien
		lsr
		scc:jsr	PlayMusic

already_running:
		dec		lock

		pla
		tay
		pla
		tax
		pla
		rti

;==========================================================================

		run		Main
