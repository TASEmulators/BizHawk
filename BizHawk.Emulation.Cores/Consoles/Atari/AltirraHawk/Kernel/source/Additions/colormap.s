;	Altirra - Atari 800/800XL/5200 emulator
;	Additions - color map utility
;	Copyright (C) 2008-2017 Avery Lee
;
;	Copying and distribution of this file, with or without modification,
;	are permitted in any medium without royalty provided the copyright
;	notice and this notice are preserved.  This file is offered as-is,
;	without any warranty.

		icl		'kernel.inc'
		icl		'kerneldb.inc'
		icl		'hardware.inc'

pfptr	= $80
		; $81
dlsav	= $87
		; $88
dlisav	= $89
		; $8A
prsav	= $8B
gtiadet = $8C

colsav	= $a0

		org		$2200

;==========================================================================
.proc Main
		;initialize tables
		ldx		#136
		ldy		#0
colinit_loop:
		txa
		sec
		sbc		#1
		lsr
		lsr
		lsr
		tay
		lda		col_tab,y
		sta		coldat,x
		dex
		bne		colinit_loop

		ldy		#16
		ldx		#136
charinit_loop1:
		mva		char_tab_lo,y pfptr
		mva		char_tab_hi,y pfptr+1
		tya
		pha
		ldy		#0
charinit_loop2:
		lda		(pfptr),y
		sta		chardat,x
		dex
		iny
		cpy		#8
		bne		charinit_loop2
		pla
		tay
		dey
		bpl		charinit_loop1

		;save and swap colors
		ldx		#8
swapcol_loop:
		mva		pcolr0,x colsav,x
		mva		color_table,x pcolr0,x
		dex
		bpl		swapcol_loop

		;swap in display list handler
		mva		#$40 nmien
		mwa		vdslst dlisav
		mwa		#DliHandlerGtia vdslst

		;swap in display list and enable DLI
		mwa		sdlstl dlsav
		mva		gprior prsav
		sei
		mwa		#dlist sdlstl
		mva		#$c0 nmien
		mva		#$11 gprior
		cli

		;position masking sprites
		mva		#0 gractl
		sta		pcolr0
		sta		pcolr1
		ldx		#17
		mva:rpl	pmsetup_table_gtia,x hposp0,x-

		;wait for screen to swap in
		jsr		WaitForScreenSwap

		;wait for one frame for collection check
		lda		#0
		sta		gtiadet
		jsr		WaitVbl

		;check for a PF2-P1 collision -- this indicates that Gr.9 is not working,
		;which means a CTIA
		lda		gtiadet
		and		#$04
		beq		is_gtia

		;we have a CTIA -- kill the odd letters
		lda		#0
		ldx		#14
remove_odd_loop:
		sta		playfield2+19,x
		dex
		dex
		bpl		remove_odd_loop

		;rewrite playfield color map pattern
		ldx		#14
		lda		#0
		sta:rpl	playfield+14,x-

		mva		#$0e pcolr1
		mva		#$04 pcolr2
		mva		#$08 pcolr3
		mva		#$02 colpf3

		ldx		#17
		mva:rpl	pmsetup_table_ctia,x hposp0,x-

		mva		#0 playfield+30

		;turn off priority
		mva		#$30 gprior

		;change DLI routine
		jsr		WaitVbl

		mwa		#DliHandlerCtia vdslst

is_gtia:

		;mute the detection line
		lda		#$11
		sta		DliHandlerGtia.priordet_mode

		;wait for a key
		lda		#$ff
		sta		ch
		ldx		#0
waitkey:
		stx		atract
		cmp		ch
		beq		waitkey
		sta		ch

		;restore colors, display list, and character set
		sei
		mva		#$40 nmien
		mwa		dlisav vdslst
		mwa		dlsav sdlstl
		mva		prsav gprior
		ldx		#8
		mva:rpl	colsav,x pcolr0,x-
		cli

		;shut off players and missiles
		ldx		#7
		lda		#0
pmoff_loop:
		sta		hposp0,x
		sta		grafp0,x
		dex
		bpl		pmoff_loop

		;wait for display change to take place and exit
		jmp		WaitForScreenSwap

pmsetup_table_gtia:
		dta		$74,$b8,$20,$c0		;hposp0-p3
		dta		$40,$48,$50,$58		;hposm0-m3
		dta		$00,$00,$03,$03,$ff	;sizep0-p3,sizem
		dta		$f0,$ff,$ff,$ff,$00	;grafp0-p3,grafm

pmsetup_table_ctia:
		dta		$b8,$40,$78,$98		;hposp0-p3
		dta		$80,$90,$a0,$b0		;hposm0-m3
		dta		$03,$03,$03,$03,$ff	;sizep0-p3,sizem
		dta		$fc,$ff,$0f,$ff,$ff	;grafp0-p3,grafm

color_table:
		dta		$00,$00,$00,$00
		dta		$00,$0e,$00,$00,$00

col_tab:
		dta		$10
		:16 dta	[15-#]*16

char_tab_lo:
		dta		<["1"*8+$E000]
		:6 dta <[["A"+(5-#)]*8+$E000]
		:10 dta <[["0"+(9-#)]*8+$E000]

char_tab_hi:
		dta		>["1"*8+$E000]
		:6 dta >[["A"+(5-#)]*8+$E000]
		:10 dta >[["0"+(9-#)]*8+$E000]
.endp

;==========================================================================
.proc WaitVbl
		lda		rtclok+2
		cmp:req	rtclok+2
		rts
.endp

;==========================================================================
.proc WaitForScreenSwap
		sec
		ror		strig0
		lda:rmi	strig0
.def :Delay12
		rts
.endp

;==========================================================================
.proc DliHandlerGtia
		pha
		tya
		pha
		txa
		pha
		ldx		#0
		stx		colpf1
		stx		hitclr
		lda		#$51
priordet_mode = *-1
		sta		wsync
		sta		prior
		ldx		#136
		lda		#0
		sta		hitclr
		sta		wsync
		lda		p1pf
		sta		gtiadet
		mva		#$21 dmactl
		lda		#$0e
		sta		colpf1
		ldy		#$11
		sty		prior
loop:
		sta		wsync
		sta		colbk
		lda		chardat,x
		sta		playfield+12
		lda		#$ff
		sta		grafm
		lda		coldat,x
		sta		colpf3
		sta		colbk
		nop
		nop
		nop
		ldy		#$51
		sty		prior
		nop
		nop
		bit		$01
		lda		#0
		ldy		#$11
		dex
		sty		prior
		bne		loop

		lda		#0
		sta		wsync
		mvy		#$22 dmactl
		sta		colbk
		sta		grafm

		pla
		tax
		pla
		tay
		pla
		rti
.endp

;==========================================================================
.proc DliHandlerCtia
		pha
		tya
		pha
		txa
		pha
		mva		#$20 dmactl
		sta		wsync
		sta		wsync
		mva		#$21 dmactl
		ldx		#136
		lda		#$ff
		ldy		#10
		dey:rne
		nop
		nop
		sta		grafm
		sta		grafp1
		sta		grafp3
		lda		#$0f
		sta		grafp2
		lda		#0
loop:
		sta		colpf2
		sta		wsync
		lda		chardat,x
		sta		playfield+12
		lda		coldat,x
		sta		colpm1
		sta		colpf2
		ldy		#$60
		sty		hposp0
		ldy		#$78
		sty		hposp2
		ora		#$02
		sta		colpf3
		nop
		ldy		#$b8
		lda		#$98
		sta		hposp2
		sty		hposp0
		lda		#0
		dex
		bne		loop

		lda		#0
		sta		wsync
		sta		colpf2
		sta		grafm
		mvy		#$22 dmactl
		sta		grafp1
		sta		grafp2
		sta		grafp3

		pla
		tax
		pla
		tay
		pla
		rti
.endp

;==========================================================================

		org		$2e00
chardat:

		org		$2f00
coldat:


		org		$3000

dlist:
		dta		$70
		dta		$70

		;CTIA/GTIA detection line
		;
		;This line has playfield data overlapping the right border
		;blocking player. On a GTIA, the playfield uses mode 9 and
		;reports no collisions. On a CTIA, the playfield uses mode
		;8 and triggers a P1-PF2 collision. The line is then
		;blanked after the check.
		;
		;Normally, could just do this check on the right border of
		;the regular GTIA display. However, Atari800 doesn't work
		;with this. While it supports mixed GTIA modes on the same
		;scanline, the collision detection fails because it uses
		;mode 8 collisions for the mode 9 part of the scanline.
		;We work around this by using a separate line with no
		;mixed modes.

		dta		$d0
		dta		$4f, a(playfield+2)

		dta		$00
		:136 dta $4f, a(playfield)
		dta		$42, a(playfield2)
		dta		$70
		dta		$02
		dta		$41, a(dlist)

playfield:
		:4 dta	$AA
		:4 dta	$55
		:6 dta	0
		:16	dta #*$11
		dta		0
		dta		0
playfield2:
		;		 0123456789012345678901234567890123456789
		dta		"    Even Odd      0123456789ABCDEF      "
		dta		"   Artifacting      Color table         "

		run		Main
