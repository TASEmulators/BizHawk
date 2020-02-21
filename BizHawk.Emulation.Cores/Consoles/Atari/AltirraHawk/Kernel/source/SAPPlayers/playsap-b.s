;	Altirra - Atari 800/800XL/5200 emulator
;	SAP type B player
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

		icl		'kerneldb.inc'
		icl		'hardware.inc'

		org		$0400
InitMusic:
		jmp		dummy
PlayMusic:
		jmp		dummy

vcountsPerTick	dta		0
defSong			dta		0
songCount		dta		0

playfield:		;0123456789012345678901234567890123456789
		dta		"  Name:                                 "
		dta		"  Author:                               "
playfield_song:
		dta		"  Song:     /                           "

dlist:
		:7 dta	$70
		dta		$42,a(playfield)
		dta		$02
		dta		$02
		dta		$41,a(dlist)

;==========================================================================
linecount	dta		0
lineaccum	dta		0
lastvcount	dta		0

;==========================================================================
dummy:
		rts

;==========================================================================
.proc Main
		;turn off interrupts
		sei
		mva		#0 nmien

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

		;take over keyboard vector
		mwa		#ProcessKey vkeybd

		;initialize POKEY
		mva		#3 skctl
		lda		#0
		ldx		#8
		sta:rpl	$d200,x-
		sta		irqen

		;turn on keyboard
		lda		#$40
		sta		irqen
		sta		pokmsk

		lda		defSong
		jsr		LoadSong

		lda		vcount
		sta		lastvcount

		;enable IRQs
		cli

playloop:
		lda		vcount
		tax
		sec
		sbc		lastvcount
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

		sei
		jsr		PlayMusic
		cli
		jmp		playloop
.endp

;==========================================================================
.proc ProcessKey
		tya
		pha
		txa
		pha

		lda		kbcode
		and		#$3f
		cmp		#$06
		bne		not_lt

		lda		defSong
		sne:lda	songCount
		sec
		sbc		#1
		sta		defSong
		jsr		LoadSong
		jmp		xit

not_lt:
		cmp		#$07
		bne		not_gt

		lda		defSong
		adc		#0
		cmp		songCount
		scc:lda	#0
		sta		defSong
		jsr		LoadSong
		jmp		xit

not_gt:
xit:
		pla
		tax
		pla
		tay
		pla
		rti
.endp

;==========================================================================
.proc LoadSong
		pha

		;convert to BCD
		clc
		adc		#1
		ldx		#$ff
		sec
conv_loop:
		inx
		sbc		#10
		bcs		conv_loop
		adc		#$1a
		sta		playfield_song+11
		txa
		beq		below_ten
		ora		#$10
below_ten:
		sta		playfield_song+10

		pla
		jmp		InitMusic
.endp

;==========================================================================
		run		Main
