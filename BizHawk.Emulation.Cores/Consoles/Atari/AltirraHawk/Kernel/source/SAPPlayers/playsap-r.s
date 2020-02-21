;	Altirra - Atari 800/800XL/5200 emulator
;	SAP type R player
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

		org		$0800

vcountsPerTick	dta		0

playfield:		;0123456789012345678901234567890123456789
		dta		"  Name:                                 "
		dta		"  Author:                               "

dlist:
		:7 dta	$70
		dta		$42,a(playfield)
		dta		$02
		dta		$41,a(dlist)

;==========================================================================
linecount	dta		0
lineaccum	dta		0
lastvcount	dta		0

;==========================================================================
.proc Main
		;turn off interrupts
		sei
		mva		#0 nmien

		;wait for VBI
		lda		#248/2
		cmp:rne	vcount

		;set up display
		mva		#$2e dmactl
		mva		#$e0 chbase
		mwa		#dlist dlistl
		mva		#0 colbk
		sta		colpf2
		sta		vdelay
		mva		#$0e colpf1

		;detect ANTIC type
		ldx		#0
detloop:
		lda		vcount
		cmp		#248/2
		bcc		detdone
		tax
		bcs		detloop
detdone:

		lda		#312/2
		cpx		#282/2
		bcs		is_pal
		lda		#262/2
is_pal:
		sta		linecount

		;setup missiles
		mva		#>missiles pmbase
		mva		#$01 prior
		mva		#$00 sizem
		lda		#3
		sta		sizep0
		sta		sizep1
		sta		sizep2
		sta		sizep3
		mva		#3 gractl
		lda		#0
		tax
pmclear_loop:
		sta		missiles,x
		sta		player0,x
		sta		player2,x
		inx
		bne		pmclear_loop

		ldx		#3
		mva:rpl	pmcolors,x colpm0,x-

		mva		#$03 missiles+$30
		mva		#$0c missiles+$32
		mva		#$30 missiles+$34
		mva		#$c0 missiles+$36
		lda		#$40
		sta		hposp0
		sta		hposp1
		sta		hposp2
		sta		hposp3

		jsr		InitMusic

		lda		vcount
		sta		lastvcount

playloop:
		lda		vcount
		tax
		sec
		sbc		lastvcount
		beq		playloop
		stx		lastvcount
		scs:adc	linecount

		sta		linedelta

		lda		lineaccum
		sec
		sbc		#0
linedelta = *-1
		sta		lineaccum
		bcs		playloop

		adc		vcountsPerTick
		sta		lineaccum

		jsr		PlayMusic

		;update VUmeter bars
		ldx		#3
vumeter_loop:
		txa
		lsr
		ora		#>player0
		sta		tmpptr+1
		lda		#0
		ror
		sta		tmpptr

		ldy		audc_offsets,x
		lda		pkshadow,y

		and		#15
		pha
		asl
		adc		#$40
		sta		hposm0,x
		pla
		tay
		lda		vuplayers,y
		ldy		pmypos,x
		sta		(tmpptr),y

		dex
		bpl		vumeter_loop

		jmp		playloop

audc_offsets:
		dta		1,3,5,7

vutab1:
		dta		1,$00
		dta		11,$c0
		dta		21,$f0
		dta		31,$fc

vuplayers:
		dta		$00,$80,$80,$c0,$c0,$e0,$e0,$f0,$f0,$f8,$f8,$fc,$fc,$fe,$fe,$ff

pmcolors:
		dta		$18,$38,$58,$78

pmypos:
		dta		$30,$32,$34,$36
.endp

;==========================================================================
musptr		= $80
musdelay	= $82
deltamask	= $83
dmhindex	= $84

tmpptr		= $90

pkshadow	= $e0
dmhistory	= $f0

missiles	= $0580
player0		= $0600
player1		= $0680
player2		= $0700
player3		= $0780

;==========================================================================
.proc InitMusic
		mva		#$10 musptr+1
		mva		#$00 musptr
		sta		musdelay
		inc		musdelay
		sta		dmhindex
		ldx		#15
		sta:rpl	dmhistory,x-
		rts
.endp

;==========================================================================
.proc PlayMusic
		dec		musdelay
		beq		delay_complete
		rts

delay_complete:
		jsr		fetch
		tax
		bpl		is_delayed

		;bit 7 is set, so this is a one-tick command
		ldx		#1
		stx		musdelay
		and		#$7f
		bpl		have_command

is_delayed:
		sta		musdelay
		jsr		fetch
have_command:
		tax
		beq		no_op
		dex
		bne		not_done
		jmp		InitMusic

not_done:
		;check for uncompressed ($02)
		dex
		bne		not_uncompressed
		;update POKEY state
		ldx		#0
play_loop:
		jsr		fetch
		sta		$d200,x
		sta		pkshadow,x
		inx
		cpx		#9
		bne		play_loop
no_op:
		rts

not_uncompressed:
		lsr
		php
		cmp		#$30
		bcc		not_remask

		;reuse a mask in the delta mask history
		clc
		adc		dmhindex
		and		#$0f
		tax
		lda		dmhistory,x
		jmp		with_dmask

not_remask:
		;update POKEY state (delta, no AUDCTL)
		lda		dmhindex
		and		#$0f
		tax
		inc		dmhindex
		jsr		fetch
		sta		dmhistory,x
with_dmask:
		sta		deltamask
		ldx		#8
		plp
delta_loop:
		bcc		no_delta
		jsr		fetch
		sta		$d200,x
		sta		pkshadow,x
no_delta:
		asl		deltamask
		dex
		bpl		delta_loop
		rts

fetch:
		ldy		#0
		lda		(musptr),y
		inw		musptr
		rts
.endp

		run		Main
