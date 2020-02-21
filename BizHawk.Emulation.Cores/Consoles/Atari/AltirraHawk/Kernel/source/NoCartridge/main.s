;	Altirra - Atari 800/800XL/5200 emulator
;	5200 default cartridge
;	Copyright (C) 2008-2016 Avery Lee
;
;	This file is licensed differently than the rest of Altirra, with
;	the following permissive license:
;
;	Copying and distribution of this file, with or without modification,
;	are permitted in any medium without royalty provided the copyright
;	notice and this notice are preserved.  This file is offered as-is,
;	without any warranty.

hposp0	equ		$c000
hposp1	equ		$c001
hposp2	equ		$c002
hposp3	equ		$c003
hposm0	equ		$c004
hposm1	equ		$c005
hposm2	equ		$c006
hposm3	equ		$c007
sizep0	equ		$c008
sizep1	equ		$c009
sizep2	equ		$c00a
sizep3	equ		$c00b
sizem	equ		$c00c
trig0	equ		$c010
trig1	equ		$c011
trig2	equ		$c012
colpm0	equ		$c012
trig3	equ		$c013
colpm1	equ		$c013
colpm2	equ		$c014
colpm3	equ		$c015
colpf0	equ		$c016
colpf1	equ		$c017
colpf2	equ		$c018
colpf3	equ		$c019
colbk	equ		$c01a
prior	equ		$c01b
gractl	equ		$c01d
consol	equ		$c01f
pot0	equ		$e800
audf1	equ		$e800
pot1	equ		$e801
audc1	equ		$e801
pot2	equ		$e802
audf2	equ		$e802
pot3	equ		$e803
audc2	equ		$e803
pot4	equ		$e804
audf3	equ		$e804
pot5	equ		$e805
audc3	equ		$e805
pot6	equ		$e806
audf4	equ		$e806
pot7	equ		$e807
audc4	equ		$e807
audctl	equ		$e808
kbcode	equ		$e809
skres	equ		$e80a
potgo	equ		$e80b
serin	equ		$e80d
serout	equ		$e80d
irqen	equ		$e80e
irqst	equ		$e80e
skctl	equ		$e80f
dmactl	equ		$d400
chactl	equ		$d401
dlistl	equ		$d402
dlisth	equ		$d403
hscrol	equ		$d404
vscrol	equ		$d405
pmbase	equ		$d407
chbase	equ		$d409
wsync	equ		$d40a
vcount	equ		$d40b
nmien	equ		$d40e
nmist	equ		$d40f
nmires	equ		$d40f

;=========================================================================
pokmsk	equ		$00
rtclok	equ		$01
;		equ		$02
critic	equ		$03
atract	equ		$04
sdlstl	equ		$05
sdlsth	equ		$06
sdmctl	equ		$07
pcolr0	equ		$08
pcolr1	equ		$09
pcolr2	equ		$0a
pcolr3	equ		$0b
color0	equ		$0c
color1	equ		$0d
color2	equ		$0e
color3	equ		$0f
color4	equ		$10
paddl0	equ		$11
paddl1	equ		$12
paddl2	equ		$13
paddl3	equ		$14
paddl4	equ		$15
paddl5	equ		$16
paddl6	equ		$17
paddl7	equ		$18
 
vimirq	equ		$0200		;IRQ immediate vector
vvblki	equ		$0202		;VBI immediate vector
vvblkd	equ		$0204		;VBI deferred vector
vdslst	equ		$0206		;display list vector
vkybdi	equ		$0208		;keyboard immediate vector
vkybdf	equ		$020a		;keyboard deferred vector
vtrigr	equ		$020c		;soft-trigger vector (BREAK key)
vbrkop	equ		$020e		;BRK opcode vector
vserin	equ		$0210		;serial input ready vector
vseror	equ		$0212		;serial output ready vector
vseroc	equ		$0214		;serial output complete vector
vtimr1	equ		$0216		;POKEY timer #1 vector
vtimr2	equ		$0218		;POKEY timer #2 vector
vtimr4	equ		$021a		;POKEY timer #4 vector

jveck	equ		$021e

;=========================================================================
		opt		o-

		org		$20
a0			.ds		2
a1			.ds		2
a2			.ds		2
a3			.ds		2

ctselect	.ds		1
ctprevsel	.ds		1
ctcycle		.ds		1
ctpotx		.ds		4
ctpoty		.ds		4
cttrakball	.ds		4
cttrakcenx	.ds		4
cttrakceny	.ds		4
ctkeymask1	.ds		4
ctkeymask2	.ds		4
cttopbtn	.ds		4

dothpos		.ds		4

		org		$2000
framebuffer:
fb_keypad:
		.ds		40*5

fb_sticks1:
		.ds		40

		org		$3400
ramfont:

		org		$3b00
missiles:

		org		$3c00
player0:

		org		$3d00
player1:

		org		$3e00
player2:

		org		$3f00
player3:

;=========================================================================
		opt		h-o+f+

		org		$b000
main:
		;interrupts off
		sei
		mva		#0 nmien

		;clear hardware registers and zero page
		lda		#0
		tax
clearloop:
		sta		$c000,x
		sta		$d400,x
		sta		$e800,x
		sta		$20,x
		sta		$80,x
		inx
		bpl		clearloop

		;clear memory
		mva		#>framebuffer a0+1
		ldx		#$20
		ldy		#0
		tya
clearloop2:
		sta:rne	(a0),y+
		inc		a0+1
		dex
		bne		clearloop2

		;load ram font
		ldy		#0
ramfont_loop:
		mva		$f800,y ramfont,y
		mva		$f900,y ramfont+$0100,y
		mva		$fa00,y ramfont+$0200,y
		mva		$fb00,y ramfont+$0300,y
		iny
		bne		ramfont_loop

		ldy		#[.len ramfont_data]
		mva:rne	ramfont_data-1,y ramfont+$40*8-1,y-

		;draw players
		lda		#$7e
		ldy		#103
player_loop:
		sta		player0+122,y
		sta		player1+122,y
		sta		player2+122,y
		sta		player3+122,y
		dey
		bpl		player_loop

		lda		#0
		ldy		#32
player_loop2:
		sta		player0+139,y
		sta		player1+139,y
		sta		player2+139,y
		sta		player3+139,y
		dey
		bpl		player_loop2

		;draw missiles
		ldx		#3
		lda		#$ff
missile_loop:
		sta		missiles+124,x
		sta		missiles+133,x
		dex
		bpl		missile_loop

		;draw keypad
		lda		#6
keypad_loop:
		jsr		DrawKeypad
		clc
		adc		#8
		cmp		#35
		bcc		keypad_loop
		
		;copy playfields to RAM
		ldx		#39
		mva:rpl	pf_sticks1,x fb_sticks1,x-

		;set up interrupts
		jsr		WaitVBL

		mva		#$02 skctl
		mva		#$03 gractl

		mwa		#VbiHandler vvblki
		mwa		#DliHandler1 vdslst
		mwa		#IrqHandler vimirq
		mva		#$40 pokmsk
		sta		irqen

		;interrupts hot
		mva		#$c0 nmien
		cli
		
		;sit tight
		jmp		*

;=========================================================================
.proc DrawKeypad
		ldx		#14
write_loop:
		pha
		clc
		adc		offsets,x
		tay
		lda		text,x
		sta		fb_keypad,y
		pla
		dex
		bpl		write_loop
		rts

text:
		dta		"SPR123456789*0#"

offsets:
		dta		0,2,4
		dta		40,42,44
		dta		80,82,84
		dta		120,122,124
		dta		160,162,164
.endp

;=========================================================================
.proc UpdateKeypad
		ldx		#15
bitloop:
		ldy		offsets-1,x
		lda		(a0),y
		asl
		asl		a1
		rol		a1+1
		ror
		sta		(a0),y
		dex
		bne		bitloop
		rts

offsets:
		dta		164,162,160
		dta		4,124,122,120
		dta		2,84,82,80
		dta		0,44,42,40
.endp

;=========================================================================
.proc DliHandler1
		pha
		lda		#0
		sta		wsync
		sta		colpf2
		lda		#$32
		sta		colpf3
		mwa		#DliHandler2 vdslst
		pla
		rti
.endp

;=========================================================================
.proc DliHandler2
		pha
		lda		dothpos
		:6 sta	wsync
		sta		hposm0
		:3		mva dothpos+#+1 hposm1+#
		lda		#$0f
		sta		colpf3
		pla
		rti
.endp

;=========================================================================
.proc VbiHandler
		pha
		txa
		pha
		tya
		pha

		;reset display
		mva		#$38 pmbase
		mva		#>ramfont chbase
		mva		#$02 chactl
		mva		#$ca colpf1
		mva		#$94 colpf2
		mva		#$0f colpf3
		mva		#$05 colpm0
		mva		#$05 colpm1
		mva		#$05 colpm2
		mva		#$05 colpm3
		mva		#$3e dmactl
		mwa		#dlist dlistl
		mva		#$42 hposp0
		mva		#$62 hposp1
		mva		#$82 hposp2
		mva		#$a2 hposp3
		mva		#$44 hposm0
		mva		#$64 hposm1
		mva		#$84 hposm2
		mva		#$a4 hposm3
		mva		#0 sizem
		mva		#$10 prior
		lda		#$03
		sta		sizep0
		sta		sizep1
		sta		sizep2
		sta		sizep3
		mwa		#DliHandler1 vdslst

		;read controller pots (note reordering to SoA)
		ldx		#7
		ldy		#3
potread_loop:
		lda		pot0,x-
		sta		paddl4,y
		lda		pot0,x-
		sta		paddl0,y-
		bpl		potread_loop

		;capture bottom button state
		ldx		ctselect
		lda		skctl
		ldy		#0
		and		#8
		sne:iny
		sty		cttopbtn,x

		;jump to next controller
		stx		ctprevsel
		inx
		txa
		and		#3
		sta		ctselect
		inc		ctcycle

		;select next controller and choose whether to do trackball detection
		lda		ctcycle
		and		#$3f
		cmp		#1
		lda		ctselect
		scc:ora	#4
		sta		consol

		;restart pot scan
		sta		potgo

		;clear key mask for newly selected controller
		ldx		ctselect
		lda		#0
		sta		ctkeymask1,x
		sta		ctkeymask2,x

		;check whether this was a detection loop
		ldx		#3
		lda		ctcycle
		and		#$3f
		cmp		#1
		bne		regular_scan

trakball_scan:
		lda		paddl0,x
		sta		cttrakcenx,x
		ldy		#$ff
		cmp		#$b0
		scc:iny
		sty		cttrakball,x
		lda		paddl4,x
		sta		cttrakceny,x
		dex
		bpl		trakball_scan
		jmp		scan_done

regular_scan:
		lda		cttrakball,x
		bmi		trackball_update
		lda		paddl0,x
		sta		ctpotx,x
		lda		paddl4,x
		sta		ctpoty,x
		jmp		next_pot

trackball_update:
		lda		paddl0,x
		sec
		sbc		cttrakcenx,x
		bcs		xdel_pos
		adc		#$ff
		ora		#$03
		sec
		bcs		xdel_update
xdel_pos:
		clc
		and		#$fc
xdel_update:
		ror
		ror
		adc		ctpotx,x
		sta		ctpotx,x

		lda		paddl4,x
		sec
		sbc		cttrakceny,x
		bcs		ydel_pos
		adc		#$ff
		ora		#$03
		sec
		bcs		ydel_update
ydel_pos:
		clc
		and		#$fc
ydel_update:
		ror
		ror
		adc		ctpoty,x
		sta		ctpoty,x
next_pot:
		dex
		bpl		regular_scan

scan_done:

		jsr		DrawButtons

		;draw keypad state for last controller
		.if 0
		lda		ctprevsel
		ora		#>player0
		sta		a0+1
		lda		#0
		sta		a0

		ldx		ctprevsel
		lda		ctkeymask1,x
		and		#$10
		adc		#$f0
		lda		ctkeymask2,x
		and		#$11
		rol
		adc		#%00001110
		and		#%00110001
		adc		#%00000111
		lsr
		lsr
		ldy		#100
		jsr		draw_button2

		ldx		ctprevsel
		lda		ctkeymask2,x
		ldy		#148+0*12
		jsr		draw_button1

		ldx		ctprevsel
		lda		ctkeymask2,x
		ldy		#148+1*12
		jsr		draw_button2

		ldx		ctprevsel
		lda		ctkeymask1,x
		ldy		#148+2*12
		jsr		draw_button1

		ldx		ctprevsel
		lda		ctkeymask1,x
		ldy		#148+3*12
		jsr		draw_button2
		.else
		ldx		ctprevsel
		mva		ctkeymask1,x a1
		mva		ctkeymask2,x a1+1
		txa
		asl
		asl
		asl
		adc		#<[fb_keypad+6]
		sta		a0
		lda		#>[fb_keypad+6]
		sta		a0+1
		jsr		UpdateKeypad
		.endif

		;clear missiles
		lda		#0
		ldx		#32
		sta:rpl	missiles+140,x-

		;draw sticks
		mva		#3 ctprevsel
grid_loop:
		ldx		ctprevsel
		lda		cttrakball,x
		bmi		trackball_grid_update

		lda		ctprevsel
		asl
		asl
		asl
		tax
		lda		#0
		ldy		#7
stickpat_loop:
		sta		ramfont+$4c*8,x+
		dey
		bpl		stickpat_loop

		ldx		ctprevsel
		lda		ctpotx,x
		lsr
		lsr
		tay
		lda		missilepos_xtab,y
		clc
		adc		missile_xposbase_tab,x
		sta		dothpos,x

		lda		ctpoty,x
		lsr
		lsr
		tay
		lda		missilepos_ytab,y
		tay
		lda		missiles,y
		ora		missilebit_tab,x
		sta		missiles,y
		lda		missiles+1,y
		ora		missilebit_tab,x
		sta		missiles+1,y

		jmp		next_grid

trackball_grid_update:
		lda		ctprevsel
		tax
		asl
		asl
		asl
		tay
		lda		ctpotx,x
		and		#7
		tax
		lda		xgrid_tab,x
		:8 sta	ramfont+$4c*8+#,y

		ldx		ctprevsel
		lda		ctpoty,x
		and		#7
		eor		#7
		tax
trackball_grid_yloop:
		lda		ygrid_tab,x
		and		ramfont+$4c*8,y
		sta		ramfont+$4c*8,y
		inx
		iny
		tya
		and		#7
		bne		trackball_grid_yloop

next_grid:
		dec		ctprevsel
		jpl		grid_loop

		;forcibly clear keyboard IRQ
		lda		#0
		sta		skctl
		lda		#2
		sta		skctl

		lda		#$bf
		sta		irqen
		lda		pokmsk
		sta		irqen

		pla
		tay
		pla
		tax
		pla
		rti

draw_button1:
		lsr
		lsr
		lsr
		lsr
draw_button2:
		lsr
		and		#7
		tax
		lda		bitmask,x
		:7 sta	(a0),y+
		sta		(a0),y
		rts

bitmask:
		dta		$00,$08,$20,$28,$80,$88,$a0,$a8

xgrid_tab:
		dta		$c0,$60,$30,$18,$0c,$06,$03,$81

ygrid_tab:
		dta		$ff,$00,$00,$00,$00,$00,$00,$ff
		dta		$ff,$00,$00,$00,$00,$00,$00

missile_xposbase_tab:
		dta		$48,$68,$88,$a8

missilebit_tab:
		dta		$02,$08,$20,$80
.endp

;=========================================================================
.proc DrawButtons
		ldx		#3
loop:
		ldy		trig0,x
		lda		bottom_charcode,y
		ldy		offsets,x
		sta		fb_sticks1,y

		ldy		cttopbtn,x
		lda		top_charcode,y
		ldy		offsets,x
		sta		fb_keypad,y

		dex
		bpl		loop
		rts

top_charcode:
		dta		$00,$49

bottom_charcode:
		dta		$48,$40

offsets:
		:4 dta	5+8*#
.endp

;=========================================================================
.proc IrqHandler
		pha

		bit		irqst
		bvc		is_keyboard

		;check if we have any interrupts
		lda		irqst
		eor		#$ff
		beq		xit

		;whatever it is, we don't care about it
		eor		#$ff
		sta		irqen
		lda		pokmsk
		sta		irqen

xit:
		pla
		rti

is_keyboard:
		lda		#$bf
		sta		irqen
		lda		pokmsk
		sta		irqen
		txa
		pha
		tya
		pha

		ldy		ctselect
		lda		kbcode
		lsr
		and		#$0f
		tax
		cmp		#$08
		bcs		hikey
		lda		ctkeymask1,y
		ora		bitmask,x
		sta		ctkeymask1,y
		bne		xit2		;!! - unconditional

hikey:
		lda		ctkeymask2,y
		ora		bitmask-8,x
		sta		ctkeymask2,y
xit2:
		pla
		tay
		pla
		tax
		bne		xit			;!! - unconditional
.endp

;=========================================================================
.proc WaitVBL
		lda		#124
		cmp:rne	vcount
		rts
.endp

;=========================================================================
bitmask:
		dta		$01,$02,$04,$08,$10,$20,$40,$80

;=========================================================================
		org		$be00

dlist:
		:5 dta $70
		dta		$30
		dta		$c2,a(playfield)
		:6 dta $70
		dta		$02
		dta		$70
		dta		$c2,a(fb_keypad)
		dta		$42,a(fb_sticks1)
		dta		$42,a(pf_sticks2)
		dta		$42,a(pf_sticks2)
		dta		$42,a(pf_sticks2)
		dta		$42,a(pf_sticks2)
		dta		$02
		dta		$42,a(fb_keypad+40)
		dta		$30
		dta		$02
		dta		$30
		dta		$02
		dta		$30
		dta		$02
		dta		$41,a(dlist)

		.if [*^dlist]&$fc00
		.error "Display list crosses 1K boundary"
		.endif
		
;=========================================================================
playfield:
		;		  0123456789012345678901234567890123456789
		dta		d"          Insert 5200 cartridge.        "

pf_controllertest:
		dta		d"             Controller test            "

pf_sticks1:
		:5		dta		0
		:4		dta		$40,$41,$41,$41,$41,$41,$42,0
		:3		dta		0

pf_sticks2:
		:5		dta		0
		:4		dta		$46,$4c+#,$4c+#,$4c+#,$4c+#,$4c+#,$47,0
		:3		dta		0

pf_sticks3:
		:5		dta		0
		:4		dta		$43,$44,$44,$44,$44,$44,$45,0
		:3		dta		0

;=========================================================================
.proc ramfont_data
		dta		$00,$00,$00,$00,$00,$00,$00,$03
		dta		$00,$00,$00,$00,$00,$00,$00,$ff
		dta		$00,$00,$00,$00,$00,$00,$00,$c0
		dta		$03,$00,$00,$00,$00,$00,$00,$00
		dta		$ff,$00,$00,$00,$00,$00,$00,$00
		dta		$c0,$00,$00,$00,$00,$00,$00,$00
		dta		$03,$03,$03,$03,$03,$03,$03,$03
		dta		$c0,$c0,$c0,$c0,$c0,$c0,$c0,$c0
		dta		$00,$f0,$f0,$f0,$f0,$00,$00,$03
		dta		$f0,$f0,$f0,$f0,$00,$00,$00,$00
.endp

;=========================================================================
missilepos_ytab:
		:58		dta [#*31*4]/228+140

missilepos_xtab:
		:58		dta [#*20*4]/228

;=========================================================================
		org		$bffd
		dta		$ff
		dta		a(main)
