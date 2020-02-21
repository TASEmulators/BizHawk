;	Altirra - Atari 800/800XL/5200 emulator
;	Disk-based snapshot loader
;	Copyright (C) 2008-2017 Avery Lee
;
;	Copying and distribution of this file, with or without modification,
;	are permitted in any medium without royalty provided the copyright
;	notice and this notice are preserved.  This file is offered as-is,
;	without any warranty.

rtclok	equ	$12
a0		equ	$80
a1		equ	$82
a2		equ	$84
a3		equ	$86
d0		equ	$88
d1		equ	$89
d2		equ	$8a
loadpr	equ	$8c
		;	$8d
sdlstl	equ	$0230			;shadow for DLISTL ($D402)
sdlsth	equ	$0231			;shadow for DLISTH ($D403)
gprior	equ	$026f			;shadow for PRIOR ($D01B)
pcolr0	equ	$02c0			;shadow for COLPM0 ($D012
color1	equ	$02c5
dunit	equ $0301			;device number
dcomnd	equ $0302			;command byte
dbuflo	equ $0304			;buffer address lo
dbufhi	equ $0305			;buffer address hi
dbytlo	equ $0308			;byte count lo
dbythi	equ $0309			;byte count hi
daux1	equ $030a			;sector number lo
daux2	equ $030b			;sector number hi
hposm0	equ	$d004
sizem	equ	$d00c
grafm	equ	$d011
colpf1	equ	$d017
colbk	equ	$d01a
gractl	equ	$d01d
skctl	equ	$d20f
portb	equ	$d301
pbctl	equ	$d303
dmactl	equ	$d400
vcount	equ	$d40b
nmien	equ	$d40e
wsync	equ	$d40a
dskinv	equ	$e453
siov	equ	$e459

;==========================================================================
; Memory map timeline:
;	0700-09FF				Loader
;	4000-77FF				Stage 1 load (14K, 112 sectors)
;	4000-4FFF -> C000-CFFF	Stage 1a copy
;	5000-77FF -> D800-FFFF	Stage 1b copy
;	0A00-BFFF				Stage 2 load (45.5K, 364 sectors)
;	4000-49FF(E)			Stage 3 load (10K, 80 sectors)
;	2400-27FF -> 7400-77FF(E)	Relocation
;	4000-49FF -> 0000-09FF	Stage 3 copy
;	
		opt		o+h-f+
		org		$0700
		dta		$00				;flags
		dta		$06				;number of sectors
		dta		a($0700)		;load address
		dta		a($0706)		;init address

__main:
		;replace display list
		sei
		mwa		#display_list sdlstl
		cli
	
		;set up progress bar
		lda		#0
		sta		loadpr
		sta		loadpr+1
		
		;wait for vbl
		lda		rtclok+2
		cmp:req	rtclok+2

		;4000-77FF				Stage 1-A load (14K, 112 sectors)
		ldx		#0
		jsr		LoadBlock

		;kill interrupts and page out kernel ROM
		sei
		mva		#0 nmien
		lda		pbctl
		ora		#$04
		sta		pbctl
		lda		portb
		and		#$fe
		sta		portb

		;4000-4FFF -> C000-CFFF	Stage 1a copy
		ldy		#$40
		lda		#$c0
		ldx		#$10
		jsr		MoveBlock

		;5000-77FF -> D800-FFFF	Stage 1b copy
		ldy		#$50
		lda		#$d8
		ldx		#$28
		jsr		MoveBlock

		;page kernel ROM back in and re-enable interrupts
		lda		portb
		ora		#$01
		sta		portb
		mva		#$40 nmien
		cli

		;0A00-BFFF				Stage 2 load (45.5K, 364 sectors)
		ldx		#6
		jsr		LoadBlock

		;enable extended RAM at bank 0
		lda		#$ef
		sta		portb
		
		;4000-49FF(E)			Stage 3 load (2.5K, 20 sectors)
		ldx		#12
		jsr		LoadBlock

		;copy stage 3 loader
		ldx		#0
s3copy:
		lda		$0900,x
		sta		$7f00,x
		inx
		bne		s3copy
		jmp		stage3

;==========================================================================
; display
;==========================================================================

display_list:
		dta		$70
		dta		$70
		dta		$70
		dta		$42,a(playfield)
		dta		$41,a(display_list)

playfield:
		;		  0123456789012345678901234567890123456789
		dta		d'Altirra Loader                          '

;==========================================================================
; Inputs:
;	X = offset in load table
;	DAUX2:DAUX1	Starting sector
;
.proc LoadBlock
		stx		d1

		;set read address
		mva		load_table+2,x dbufhi
		lda		#$80
		sta		dbytlo
		asl
		sta		dbuflo
		sta		dbythi
		
		;set read page count
		mva		load_table+3,x d0

		;do reads
		mva		#$52 dcomnd
		mva		#1 dunit
sectorloop:
		jsr		read_page
		dec		d0
		bne		sectorloop
		
		;check whether we need to do LZ decompression
		ldx		d1
		ldy		load_table+5,x
		sne:rts
		
		;do LZ decompression
		lda		load_table+4,x
		pha
		lda		load_table+1,x
		clc
		adc		load_table,x
		sta		a3+1
		lda		load_table,x
		tax
		pla
		jmp		LZUnpack

read_page:
		jsr		read_sector
read_sector:
		inw		daux1
		jsr		dskinv
		lda		loadpr
		clc
		adc		#0
loadpr_speed_lo = *-1
		sta		loadpr
		lda		loadpr+1
		adc		#0
loadpr_speed_hi = *-1
		sta		loadpr+1
		tax
		lda		#$80
		sta		playfield+20,x
		eor		dbuflo
		sta		dbuflo
		sne:inc	dbufhi
		rts
.endp

;==========================================================================
; Inputs:
;  Y		Source page
;  A		Destination page
;  X		Pages to move
;
.proc MoveBlock
		sty		a0+1
		sta		a1+1
		ldy		#0
		sty		a0
		sty		a1
copyloop:
		lda		(a0),y
		sta		(a1),y
		iny
		bne		copyloop
		inc		a0+1
		inc		a1+1
		dex
		bne		copyloop
		rts
.endp

;===========================================================================
; Entry:
;	Y:A = source starting address
;	X = destination starting page
;	A3+1 = stop page
;
.proc LZUnpack
		sta		a0
		sty		a0+1
		stx		a2+1
		lda		#0
		sta		a2
control_loop:
		;get control byte
		jsr		LZGetByte
		sta		colpf1
		sta		d0
		
		;shift down literal count
		lsr
		lsr
		lsr
		lsr
		beq		no_literals
		ldy		#$0f
		jsr		LZGetLengthExtension
		ldy		a0
		lda		a0+1
		jsr		LZCopy
		mwa		a1 a0
no_literals:

		;check if we are at the end
		lda		a2+1
		cmp		a3+1
		beq		done

		;get match count
		lda		d0
		and		#$0f
		clc
		adc		#$04
		ldy		#$13
		jsr		LZGetLengthExtension
		
		;get and apply offset
		ldx		#0
		clc
		lda		a2
		sbc		(a0,x)
		tay
		jsr		LZIncPos
		lda		a2+1
		sbc		(a0,x)
		jsr		LZIncPos
		jsr		LZCopy
		jmp		control_loop
		
done:
		lda		color1
		sta		colpf1
		rts
.endp

;===========================================================================
.proc LZGetByte
		ldx		#0
		lda		(a0,x)
.def :LZIncPos = *
		inw		a0
		rts
.endp

;===========================================================================
; Entry:
;	A:Y = source pointer
;	[D2-1]:D1 = bytes to copy (must be >0)
;
.proc LZCopy
		sty		a1
		sta		a1+1
		
		;copy literal bytes
range_loop:
		ldy		#0
byte_loop:
		lda		(a1),y
		sta		(a2),y
		iny
		cpy		d1
		bne		byte_loop
		tya
		bne		partial_page
		inc		a1+1
		inc		a2+1
		bcs		next
		
partial_page:
		clc
		adc		a1
		sta		a1
		scc:inc	a1+1
		tya
		clc
		adc		a2
		sta		a2
		scc:inc	a2+1
		lda		#0
		sta		d1
next:
		dec		d2
		bne		range_loop
		rts
.endp

;===========================================================================
.proc LZGetLengthExtension
		;check if we have extension bytes
		ldx		#$01
		stx		d2
		sta		d1
		cpy		d1
		bne		done
byte_loop:
		jsr		LZGetByte
		tay
		clc
		adc		d1
		sta		d1
		scc:inc	d2
		cpy		#$ff
		beq		byte_loop
done:
		;adjust page count if low byte is 0
		lda		d1
		sne:dec	d2
		rts		
.endp

;==========================================================================
; extra data
;==========================================================================

load_table:
		;decompress address, decompress pages, load address, load pages, LZ start
		:18 dta $0

;===========================================================================
; STAGE 3 LOADER
		.if * >= $900
		org		*+$7600,*
		.else
		org		$7f00,$900
		.endif
.proc stage3
		;disable interrupts and DMA
		sei
		ldx		#0
		stx		nmien
		stx		dmactl
		
		;copy $4A00-4AFF -> $BF00-BFFF
		mva:rne	$4A00,x $BF00,x+

		;*** NO ZERO PAGE PAST THIS POINT ***

		;copy $4000-49FF down to $0000-09FF
		ldy		#$0a
copyloop:
		lda		$4000,x
		dta $9d, $00, $00		;sta $0000,x
		inx
		bne		copyloop
		inc		copyloop+2
		inc		copyloop+5
		dey
		bne		copyloop

		;reload GTIA
		ldx		#$1d
gtload:
		lda		gtiadat,x
		sta		$d000,x
		dex
		bpl		gtload

		;reload POKEY
		ldx		#8
pkload:
		lda		pokeydat,x
		sta		$d200,x
		dex
		bpl		pkload
		
		mva		pokeydat+9 skctl

		;reload ANTIC
		; $D401 CHACTL
		; $D402 DLISTL
		; $D403 DLISTH
		; $D404 HSCROL
		; $D405 VSCROL
		; $D407 PMBASE
		; $D409 CHBASE
		; $D40E NMIEN
		ldx		#8
anload:
		lda		anticdat,x
		sta		$d401,x
		dex
		bpl		anload
		
		; $D400 DMACTL
		
		;reload PBCTL, PORTB
		lda		#$00
_insert_pbctl = *-1;
		ldx		#$00
_insert_portb1 = *-1;
		sta		pbctl
		stx		portb
		eor		#$04
		sta		pbctl
			
		lda		#244/2
		cmp:rne	vcount		;end 243
		cmp:req	vcount		;end 245
		sta		wsync		;end 246
		sta		wsync		;end 247

		;reload S, X, and Y registers
		ldx		#$ff
_insert_s = * - 1
		txs					;[104, 105]
		ldx		#$00		;[106, 107]
_insert_x = * - 1
		ldy		#$00		;[108, 109]
_insert_y = * - 1
		lda		#$00		;[110, 111] load DMACTL value
_insert_dmactl = * - 1
		sta		dmactl		;[112, 113, 0, 1]
		lda		#$00		;[2, 3]
_insert_nmien = * - 1
		sta		nmien		;[4, 5, 6, 7] reenable NMIs
		lda		#$fe		;[8, 9]
_insert_portb2 = * - 1
		jmp		$0100		;[10, 11, 12] jump to stack thunk
_insert_ipc = * - 2

;==========================================================================
; extra data (stage 3)
;==========================================================================

gtiadat:
		:30 dta 0			;$D000 to $D01D

pokeydat:
		:10 dta 0			;$D200 to $D208, $D20F

anticdat:
		:9 dta 0			;$D401 to $D409
.endp
		
		.print	"End of loader: ",$8000-*

		org		$0a00
		
;==========================================================================
; metadata
;==========================================================================

		;$0300
		dta		a(_insert_a - $0700)
		dta		a(.adr stage3._insert_x - $0700)
		dta		a(.adr stage3._insert_y - $0700)
		dta		a(.adr stage3._insert_s - $0700)
		dta		a(.adr stage3._insert_ipc - $0700)
		dta		a(.adr stage3._insert_pbctl - $0700)
		dta		a(.adr stage3._insert_portb1 - $0700)
		dta		a(.adr stage3._insert_portb2 - $0700)

		;$0310
		dta		a(.adr stage3._insert_dmactl - $0700)
		dta		a(.adr stage3._insert_nmien - $0700)
		dta		a(.adr stage3.gtiadat - $0700)
		dta		a(.adr load_table - $0700)
		dta		a(LoadBlock.loadpr_speed_lo - $0700)
		dta		a(LoadBlock.loadpr_speed_hi - $0700)
		dta		stack_thunk_end - stack_thunk_begin

stack_thunk_begin:
		sta		portb		;[13, 14, 15, 16] restore PORTB state
		lda		#$00		;[17, 18] reload accumulator
_insert_a = * - 1
		rti					;[19, 20, 21, 22, 23, 24] jump to VBI routine
stack_thunk_end:

		end
