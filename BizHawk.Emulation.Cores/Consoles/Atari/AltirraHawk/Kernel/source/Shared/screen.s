;	Altirra - Atari 800/800XL/5200 emulator
;	Modular Kernel ROM - Screen Handler
;	Copyright (C) 2008-2016 Avery Lee
;
;	Copying and distribution of this file, with or without modification,
;	are permitted in any medium without royalty provided the copyright
;	notice and this notice are preserved.  This file is offered as-is,
;	without any warranty.

;Display list:
;	24 blank lines (3 bytes)
;	initial mode line with LMS (3 bytes)
;	mode lines
;	LMS for modes >4 pages
;	wait VBL (3 bytes)
;
;	total is 8-10 bytes + mode lines

; These are the addresses produced by the normal XL/XE OS:
;
;               Normal       Split, coarse    Split, fine
; Mode       DL   PF   TX     DL   PF   TX    DL   PF   TX
;  0        9C20 9C40 9F60   9C20 9C40 9F60  9C1F 9C40 9F60
;  1        9D60 9D80 9F60   9D5E 9D80 9F60  9D5D 9D80 9F60
;  2        9E5C 9E70 9F60   9E58 9E70 9F60  9E57 9E70 9F60
;  3        9E50 9E70 9F60   9E4E 9E70 9F60  9E4D 9E70 9F60
;  4        9D48 9D80 9F60   9D4A 9D80 9F60  9D49 9D80 9F60
;  5        9B68 9BA0 9F60   9B6A 9BA0 9F60  9B69 9BA0 9F60
;  6        9778 97E0 9F60   9782 97E0 9F60  9781 97E0 9F60
;  7        8F98 9060 9F60   8FA2 9060 9F60  8FA1 9060 9F60
;  8        8036 8150 9F60   8050 8150 9F60  804F 8150 9F60
;  9        8036 8150 9F60   8036 8150 9F60  8036 8150 9F60
; 10        8036 8150 9F60   8036 8150 9F60  8036 8150 9F60
; 11        8036 8150 9F60   8036 8150 9F60  8036 8150 9F60
; 12        9B80 9BA0 9F60   9B7E 9BA0 9F60  9B7D 9BA0 9F60
; 13        9D6C 9D80 9F60   9D68 9D80 9F60  9D67 9D80 9F60
; 14        8F38 9060 9F60   8F52 9060 9F60  8F51 9060 9F60
; 15        8036 8150 9F60   8050 8150 9F60  804F 8150 9F60
;
; *DL = display list (SDLSTL/SDLSTH)
; *PF = playfield (SAVMSC)
; *TX = text window (TXTMSC)
;
; From this, we can derive a few things:
;	- The text window is always 160 ($A0) bytes below the ceiling.
;	- The playfield is always positioned to have enough room for
;	  the text window, even though this wastes a little bit of
;	  memory for modes 1, 2, 3, 4, and 13. This means that the
;	  PF address does not have to be adjusted for split mode.
;	- The display list and playfield addresses are sometimes
;	  adjusted in order to avoid crossing 1K boundaries for the
;	  display list (gr.7) and 4K boundaries for the playfield (gr.8).
;	  However, these are fixed offsets -- adjusting RAMTOP to $9F
;	  does not remove the DL padding in GR.7 and breaks GR.7/8.
;	- Fine-scrolled modes take one additional byte for the extra
;	  mode 2 line. In fact, it displays garbage that is masked by
;	  a DLI that sets COLPF1 equal to COLPF2. (!)
;
; You might ask, why bother replicating these? Well, there are a
; number of programs that rely on the layout of the default screen
; and break if the memory addressing is different, such as ForemXEP.

.macro _SCREEN_TABLES_2

;Mode	Type	Res		Colors	ANTIC	Mem(unsplit)	Mem(split)
; 0		Text	40x24	1.5		2		960+32 (4)		960+32 (4)
; 1		Text	20x24	5		6		480+32 (2)		560+32 (3)
; 2		Text	20x12	5		7		240+20 (2)		360+22 (2)
; 3		Bitmap	40x24	4		8		240+32 (2)		360+32 (2)
; 4		Bitmap	80x48	2		9		480+56 (3)		560+52 (3)
; 5		Bitmap	80x48	4		A		960+56 (4)		960+52 (4)
; 6		Bitmap	160x96	2		B		1920+104 (8)	1760+92 (8)
; 7		Bitmap	160x96	4		D		3840+104 (16)	3360+92 (14)
; 8		Bitmap	320x192	1.5		F		7680+202 (32)	6560+174 (27)
; 9		Bitmap	80x192	16		F		7680+202 (32)	6560+174 (27)
; 10	Bitmap	80x192	9		F		7680+202 (32)	6560+174 (27)
; 11	Bitmap	80x192	16		F		7680+202 (32)	6560+174 (27)
; 12	Text	40x24	5		4		960+32 (4)		960+32 (4)
; 13	Text	40x12	5		5		480+20 (2)		560+24 (3)
; 14	Bitmap	160x192	2		C		3840+200 (16)	3360+172 (14)
; 15	Bitmap	160x192	4		E		7680+202 (32)	6560+172 (27)

;==========================================================================
;
.proc ScreenPlayfieldSizesLo
	dta	<($10000-$03C0)			;gr.0	 960 bytes = 40*24              = 40*24
	dta	<($10000-$0280)			;gr.1	 640 bytes = 20*24  + 40*4      = 40*12  + 40*4
	dta	<($10000-$0190)			;gr.2	 400 bytes = 10*24  + 40*4      = 40*6   + 40*4
	dta	<($10000-$0190)			;gr.3	 400 bytes = 10*24  + 40*4      = 40*6   + 40*4
	dta	<($10000-$0280)			;gr.4	 640 bytes = 10*48  + 40*4      = 40*12  + 40*4
	dta	<($10000-$0460)			;gr.5	1120 bytes = 20*48  + 40*4      = 40*24  + 40*4
	dta	<($10000-$0820)			;gr.6	2080 bytes = 20*96  + 40*4      = 40*48  + 40*4
	dta	<($10000-$0FA0)			;gr.7	4000 bytes = 40*96  + 40*4      = 40*96  + 40*4
	dta	<($10000-$1EB0)			;gr.8	7856 bytes = 40*192 + 40*4 + 16 = 40*192 + 40*4 + 16
	dta	<($10000-$1EB0)			;gr.9	7856 bytes = 40*192 + 40*4 + 16 = 40*192 + 40*4 + 16
	dta	<($10000-$1EB0)			;gr.10	7856 bytes = 40*192 + 40*4 + 16 = 40*192 + 40*4 + 16
	dta	<($10000-$1EB0)			;gr.11	7856 bytes = 40*192 + 40*4 + 16 = 40*192 + 40*4 + 16
	dta	<($10000-$0460)			;gr.12	1120 bytes = 40*24  + 40*4      = 40*24  + 40*4
	dta	<($10000-$0280)			;gr.13	 640 bytes = 40*12  + 40*4      = 40*12  + 40*4
	dta	<($10000-$0FA0)			;gr.14	4000 bytes = 20*192 + 40*4      = 40*96  + 40*4
	dta	<($10000-$1EB0)			;gr.15	7856 bytes = 40*192 + 40*4 + 16 = 40*192 + 40*4 + 16
.endp

.proc ScreenPlayfieldSizesHi
	dta	>($10000-$03C0)			;gr.0
	dta	>($10000-$0280)			;gr.1
	dta	>($10000-$0190)			;gr.2
	dta	>($10000-$0190)			;gr.3
	dta	>($10000-$0280)			;gr.4
	dta	>($10000-$0460)			;gr.5
	dta	>($10000-$0820)			;gr.6
	dta	>($10000-$0FA0)			;gr.7
	dta	>($10000-$1EB0)			;gr.8
	dta	>($10000-$1EB0)			;gr.9
	dta	>($10000-$1EB0)			;gr.10
	dta	>($10000-$1EB0)			;gr.11
	dta	>($10000-$0460)			;gr.12
	dta	>($10000-$0280)			;gr.13
	dta	>($10000-$0FA0)			;gr.14
	dta	>($10000-$1EB0)			;gr.15
.endp

;==========================================================================
; ANTIC mode is in bits 0-3, PRIOR bits in 6-7.
; DL 1K hop: bit 4
; playfield 4K hop: bit 5
;
.proc ScreenModeTable
	dta		$02,$06,$07,$08,$09,$0A,$0B,$1D,$3F,$7F,$BF,$FF,$04,$05,$1C,$3E
.endp

;==========================================================================
;
.proc ScreenHeightShifts
	dta		1
	dta		1
	dta		0
	dta		1
	dta		2
	dta		2
	dta		3
	dta		3
	dta		4
	dta		4
	dta		4
	dta		4
	dta		1
	dta		0
	dta		4
	dta		4
.endp

.proc ScreenHeights
	dta		12, 24, 48, 96, 192
.endp

.proc ScreenPixelWidthIds
	dta		1		;gr.0	40 pixels
	dta		0		;gr.1	20 pixels
	dta		0		;gr.2	20 pixels
	dta		1		;gr.3	40 pixels
	dta		2		;gr.4	80 pixels
	dta		2		;gr.5	80 pixels
	dta		3		;gr.6	160 pixels
	dta		3		;gr.7	160 pixels
	dta		4		;gr.8	320 pixels
	dta		2		;gr.9	80 pixels
	dta		2		;gr.10	80 pixels
	dta		2		;gr.11	80 pixels
	dta		1		;gr.12	40 pixels
	dta		1		;gr.13	40 pixels
	dta		3		;gr.14	160 pixels
	dta		3		;gr.15	160 pixels
.endp
.endm

ScreenHeightsSplit = ScreenWidths
;	dta		10, 20, 40, 80, 160

ScreenPixelWidthsLo = ScreenWidths + 1

.macro _SCREEN_TABLES_1

.proc ScreenWidths
	dta		<10
	dta		<20
	dta		<40
	dta		<80
	dta		<160
	dta		<320
.endp

.proc ScreenPixelWidthsHi
	dta		>20
	dta		>40
	dta		>80
	dta		>160
	dta		>320
.endp

.proc ScreenEncodingTab
	dta		0		;gr.0	direct bytes
	dta		0		;gr.1	direct bytes
	dta		0		;gr.2	direct bytes
	dta		2		;gr.3	two bits per pixel
	dta		3		;gr.4	one bit per pixel
	dta		2		;gr.5	two bits per pixel
	dta		3		;gr.6	one bit per pixel
	dta		2		;gr.7	two bits per pixel
	dta		3		;gr.8	one bit per pixel
	dta		1		;gr.9	four bits per pixel
	dta		1		;gr.10	four bits per pixel
	dta		1		;gr.11	four bits per pixel
	dta		0		;gr.12	direct bytes
	dta		0		;gr.13	direct bytes
	dta		3		;gr.14	one bit per pixel
	dta		2		;gr.15	two bits per pixel
.endp

.proc ScreenPixelMasks
	dta		$ff, $0f, $03, $01, $ff, $f0, $c0, $80
.endp
.endm

.macro _SCREEN_TABLES_3
.proc ScreenEncodingTable
	dta		$00,$11,$22,$33,$44,$55,$66,$77,$88,$99,$aa,$bb,$cc,$dd,$ee,$ff
	dta		$00,$55,$aa,$ff
	dta		$00,$ff
.endp
.endm

.if _KERNEL_XLXE
	_SCREEN_TABLES_3
	_SCREEN_TABLES_2
	_SCREEN_TABLES_1
.endif

;==========================================================================
;==========================================================================

	;Many compilation disks rely on ScreenOpen being at $F3F6.
	
.if *>$f3f6-8
	.error	'ROM overflow into Screen Handler region: ',*,' > $F3EE.'
.endif

.ifdef _KERNEL_REPORT_MODULE_PAD_ADJUST
	_KERNEL_REPORT_MODULE_PAD_ADJUST [$f3f6-8]-*
.endif

	org		$f3f6-8

;==========================================================================
;==========================================================================

;==========================================================================
;
; Return:
;	MEMTOP = first byte used by display
;
; Errors:
;	- If there is not enough memory (MEMTOP > APPMHI), GR.0 is reopened
;	  automatically and an error is returned.
;
; Notes:
;	- Resets character base (CHBAS).
;	- Resets character attributes (CHACT).
;	- Resets playfield colors (COLOR0-COLOR4).
;	- Resets tab map, even if the mode does not have a text window.
;	- Resets logical line map, even if the mode does not have a text window.
;	- Does NOT reset P/M colors (PCOLR0-PCOLR3).
;	- Does NOT reset margins (LMARGN/RMARGN).
;	- Sets up fine scrolling if FINE bit 7 is set. Note that this is
;	  different than the scroll logic itself, which tests the whole byte.
;	- Returns error $80 if BREAK has been pressed.
;	- If clear is bypassed, ROWCRS and COLCRS are preserved.
;
; Modified:
;	- FRMADR: used for bitflags
;		bit 7 = skip clear
;		bit 6 = split screen
;		bit 0 = fine scrolling (XL/XE only)
;	- ADRESS: temporary addressing
;
ScreenOpen = ScreenOpenGr0.use_iocb
ScreenOpenMode0 = ScreenOpenGr0.use_mode0
.proc	ScreenOpenGr0
	mva		#12 icax1z
use_mode0:
	mva		#0 icax2z
use_iocb:
	;shut off ANTIC playfield and instruction DMA
	lda		sdmctl
	and		#$dc
	sta		sdmctl
	sta		dmactl
	
	;reset cursor parameters
	ldx		#11
	lda		#0
clear_parms:
	sta		rowcrs,x
	sta		txtrow,x
	dex
	bne		clear_parms

	;mark us as being in main screen context
	stx		swpflg	
	
	;copy mode value to dindex
	lda		icax2z
	and		#15
	sta		dindex
	tax
	
	;poke PRIOR value (saves us some time to do it now)
	;note that we must preserve bits 0-5 of GPRIOR or else Wayout shows logo artifacts
	lda		ScreenModeTable,x
	eor		gprior
	and		#$c0
	eor		gprior
	sta		gprior
	
	;if a GTIA mode is active or we're in mode 0, force off split mode
	cmp		#$40
	lda		icax1z
	bcs		kill_split
	cpx		#0
	bne		not_gtia_mode_or_gr0
kill_split:
	and		#$ef
not_gtia_mode_or_gr0:

	;save off the split screen and clear flags in a more convenient form
	asl
	asl
	sta		frmadr
	
	;compute number of mode lines that we're going to have and save it off
	ldy		ScreenHeightShifts,x
	ldx		ScreenHeights,y
	asl
	spl:ldx	ScreenHeightsSplit,y
	stx		frmadr+1

	;attempt to allocate playfield memory
	lda		ramtop
	ldx		dindex
	clc
	adc		ScreenPlayfieldSizesHi,x
	bcs		pf_alloc_ok

alloc_fail:
	;we ran out of memory -- attempt to reopen with gr.0 if we aren't
	;already (to prevent recursion), and exit with an error
	txa
	beq		cant_reopen_gr0
	
	jsr		ScreenOpenGr0	
cant_reopen_gr0:
	ldy		#CIOStatOutOfMemory
	rts
	
pf_alloc_ok:
	sta		savmsc+1
	ldy		ScreenPlayfieldSizesLo,x
	sty		savmsc

	;Gr. modes 7 and 14 consume enough space for the playfield that there
	;is not enough space left between the playfield and the next 1K
	;boundary to contain the display list. In these cases, we preallocate
	;to the 1K boundary to prevent a DL crossing error. Gr.8-11 and 15
	;do this too -- I have no idea why, as it's not like the OS correctly
	;handles moving the 4K page split for those modes if RAMTOP is
	;misaligned.
	lda		ScreenModeTable,x
	ldx		savmsc+1
	and		#$30
	beq		no_dlist_page_crossing
	ldy		#0
	dex
no_dlist_page_crossing:
	sty		rowac
	stx		memtop+1
	stx		rowac+1
	stx		sdlsth
		
	;Compute display list size.
	;
	; We need:
	;	- 8 fixed bytes (24 blank lines, LMS address, JVB)
	;	- N bytes for mode lines
	;	- 2 bytes for LMS split address (ANTIC modes E-F only)
	;	- 6 bytes for split
	;
	;Note that the display list never crosses a page boundary. This is
	;conservative, as the display list only can't cross 1K boundaries.
	
	cmp		#$20				;test 4K hop bit (bit 5)
	lda		#$f8				;start with -8 (if carry is clear after test)
	scc:lda	#$f5				;use -11 if so (carry is set)
	sbc		frmadr+1			;subtract mode line count
	bit		frmadr
	svc:sbc	#6					;subtract 6 bytes for a split
	
.if _KERNEL_XLXE
	ldy		#1
	cpy		fine				;clear carry to alloc +1 byte if fine scrolling is enabled
.endif

	adc		rowac				;allocate dl bytes (note that carry is set!)
	sta		memtop
	sta		rowac
	sta		sdlstl
	
	;check if we're below APPMHI
	cpx		appmhi+1
	bcc		alloc_fail
	bne		alloc_ok
	cmp		appmhi
	bcc		alloc_fail
	
alloc_ok:
	;MEMTOP is -1 from first used byte; we cheat here with the knowledge that
	;the low byte of MEMTOP is never $00
	dec		memtop

	;set up text window address (-160 from top)
	ldx		ramtop
	dex
	stx		txtmsc+1
	mva		#$60 txtmsc

	;Turn on keyboard and break interrupts; note that this is done both for S:
	;and E: opens. Close does not do this.
	php
	sei
	asl							;!! - A = $C0
	ora		pokmsk
	sta		pokmsk
	sta		irqen
	plp

	;Set row count: 24 for full-screen. We will fix up the split case to 4 later
	;while we are writing the display list. Mapping the Atari incorrectly states
	;that this value is set to 0 when no text window is present; this is wrong
	;and in fact BAYPILOT.BAS relies on this being set to 24 for a GR.7+16
	;screen since it later aliases it to GR.0.
	ldy		#24
	sty		botscr

	;init colors -- note that we do NOT overwrite pcolr0-3!
	ldy		#5
	mva:rne	standard_colors-1,y color0-1,y-

	sty		crsinh				;!! - Y=0 for this and for write index
	
	;add 24 blank lines
	lda		#$70
	ldx		#3
	jsr		write_repeat
	
	;add in the main screen
	jsr		setup_display
	
	;save off the DL ptr
	sty		countr
	
	;clear it if necessary
	jsr		try_clear
	
	;check if we are doing a split
	bit		frmadr
	bvc		nosplit

	;change text screen height to four lines
	ldx		#4
	stx		botscr

	;swap to the text screen
	jsr		EditorSwapToText
	
	;add text screen dlist
	ldy		countr
	jsr		setup_display
	sty		countr
		
	;clear the split screen
	jsr		try_clear

nosplit:
	mva		#2		chact

	;init tab map to every 8 columns
	ldx		#15
	lsr								;!! A=1
	sta:rne	tabmap-1,x-

	;reset line status
	stx		bufcnt
	
	;init character set
	mvx		#$e0	chbas			;!! - also used for NMIEN

	;enable VBI; note that we have not yet enabled display DMA
.if _KERNEL_XLXE
	bit		frmadr
	sne:ldx	#$40
.else
	ldx		#$40
.endif
	stx		nmien
	
	;terminate display list with jvb
	ldy		countr
	ldx		#rowac
	lda		#$41
	jsr		write_with_zp_address
	
	;init display list and playfield dma
	lda		sdmctl
	ora		#$22
	sta		sdmctl
	
	;wait for screen to establish (necessary for Timewise splash screen to render)
	lda		rtclok+2
	cmp:req	rtclok+2

	;If we're in screen mode 0, show the cursor; otherwise, skip it and wait for
	;E: to do so. We need to skip this even if a split screen is present, or else
	;ACTris 2.1 displays a bogus cursor. Unfortunately we may have swapped to the
	;split screen, so the mode we need may be in either DINDEX or TINDEX; it's
	;easier for us just to re-check AUX2 bits 0-3.
	lda		icax2z
	and		#$0f
	bne		no_cursor

	;show cursor
	jsr		ScreenPutByte.recompute_show_cursor_exit
no_cursor:
	;swap back to main context
	jsr		EditorSwapToScreen_Y1
	
	;exit
	ldx		#0				;required by Qix (v3)
	rts
	
	
;--------------------------------------------------------
setup_display:
	;Add initial mode line with LMS, and check if we need to do an LMS
	;split (playfield exceeds 4K). As it turns out, this only happens if
	;we're using ANTIC mode E or F.
	ldx		dindex
	lda		ScreenModeTable,x
	and		#$0f

.if _KERNEL_XLXE
	;check if we are doing fine scrolling and set the vscrol bit if so
	ldx		dindex
	bne		nofine
	ldx		fine
	beq		nofine
	ora		#$20
nofine:
.endif

	pha
	ora		#$40
	ldx		#savmsc
	jsr		write_with_zp_address

	;retrieve row count
	ldx		frmadr+1
	lda		dindex
	sne:ldx	botscr
	
	;dec row count since we already did the LMS
	dex
	pla
	
.if _KERNEL_XLXE
	;subtract two rows if we are fine scrolling, as we need to do DLI
	;and non-vscrolled row
	cmp		#$22
	beq		dofine
.endif
	
	;check if this is a split mode
	cmp		#$0e
	bcc		no_lms_split
	
	;yes it is -- write 93 lines
	ldx		#93
	jsr		write_repeat
	
	;write LMS to jump over 4K boundary
	pha
	ora		#$40
	sta		(rowac),y+
	lda		#0
	sta		(rowac),y+
	lda		savmsc+1
	adc		#$0f-1				;!! - C=1 from bcc above
	sta		(rowac),y+
	
	;set up to write 95 fewer lines (note that carry is cleared)
	lda		frmadr+1
	sbc		#94
	tax
	pla
	
	;write mode lines and return
no_lms_split:
write_repeat:
	sta		(rowac),y+
	dex
	bne		write_repeat
no_clear:
	rts

.if _KERNEL_XLXE
dofine:
	;write the regular lines (22 or 2)
	dex
	jsr		write_repeat

	;write DLI line
	ora		#$80
	sta		(rowac),y+
	
	;write non-scrolled line
	and		#$5f
	sta		(rowac),y+
	
	;indicate to mainline that DLIs should be turned on
	inc		frmadr
	
	;set up DLI routine
	ldx		#<ScreenFineScrollDLI
	lda		#>ScreenFineScrollDLI
write_vdslst:
	stx		vdslst
	sta		vdslst+1
	rts
.endif

;--------------------------------------------------------
write_with_zp_address:
	sta		(rowac),y+
	lda		0,x
	sta		(rowac),y+
	lda		1,x
	ldx		#1
	bne		write_repeat		;!! - unconditional
	
try_clear:
	bit		frmadr
	bmi		no_clear
	jmp		ScreenClear
	
;--------------------------------------------------------
standard_colors:
	dta		$28
	dta		$ca
	dta		$94
	dta		$46
.def :ScreenBitposFlipTab
	dta		$00			;!! - shared value between tables
	dta		$01
	dta		$03
	dta		$07

.endp

;==========================================================================
.proc	ScreenInit
	mva		memtop+1 ramtop
	
	.ifdef _KERNEL_816
	stz		colrsh
	.else
	mva		#0	colrsh
	.endif

	mva		#$FE	drkmsk
	rts
.endp

;==========================================================================
; Behavior in gr.0:
;	- Reading char advances to next position, but cursor is not moved
;	- Cursor is picked up ($A0)
;	- Wrapping from end goes to left margin on next row
;	- Cursor may be outside of horizontal margins
;	- Error 141 (cursor out of range) if out of range
;	- Cursor will wrap to out of range if at end of screen (no automatic
;	  vertical wrap)
;	- Does NOT update OLDROW/OLDCOL
;
.proc ScreenGetByte
	jsr		ScreenCheckPosition
	bmi		xit
	
	;compute addressing
	ldy		rowcrs
	jsr		ScreenComputeToAddrX0
	lda		colcrs
	ldx		dindex
	ldy		ScreenEncodingTab,x
	eor		ScreenBitposFlipTab,y
	tax
	lda		colcrs+1
	jsr		ScreenSetupPixelAddr.phase2
	
	;retrieve byte containing pixel
	ldy		shfamt
	lda		(toadr),y
	
	;shift down
	jsr		ScreenAlignPixel

	;convert from Internal to ATASCII - must be done before we mask
	;Internal	ATASCII
	;00-1F		20-3F
	;20-3F		40-5F
	;40-5F		00-1F
	;60-7F		60-7F
	asl
	php
	spl:eor	#$40		;00>20, 20>40, 40>60, 60>40
	adc		#$40		;00>20, 20>40, 40>00, 60>60
	plp
	ror

	;mask using right-justified pixel mask for mode
	ldx		dindex
	ldy		ScreenEncodingTab,x
	and		ScreenPixelMasks,y
	
	;advance to next position
	ldx		dindex
	beq		mode0
	jmp		ScreenAdvancePosNonMode0
mode0:
	jsr		ScreenAdvancePosMode0
	ldy		#1
xit:
	rts
.endp

;==========================================================================
; Common behavior:
;	- Output is suspended if SSFLAG is set for non-clear and non-EOL chars
;	- Clear screen ($7D) and EOL ($9B) are always handled
;	- ESCFLG and DSPFLG are ignored (they are E: features)
;
; Behavior in gr.0:
;	- Logical lines are extended
;	- Scrolling occurs if bottom is hit
;	- Control chars other than clear and EOL are NOT handled by S:
;	- ROWCRS or COLCRS out of range results in an error.
;	- COLCRS in left margin is ignored and prints within margin.
;	- COLCRS in right margin prints one char and then does EOL.
;
; Behavior in gr.1+:
;	- No cursor is displayed
;	- LMARGN/RMARGN are ignored
;	- Cursor wraps from right side to next line
;	- ROWCRS may be below split screen boundary as long as it is within the
;	  full screen size.
;
.proc ScreenPutByte
	sta		atachr
	jsr		ScreenCheckPosition
	bmi		error

	;check for screen clear
	lda		atachr
	cmp		#$7d
	bne		not_clear_2
	jsr		ScreenClear
	ldy		#1
error:
	rts
	
graphics_eol:
	jmp		ScreenAdvancePosNonMode0.graphics_eol

	;*** ENTRY POINT FROM EDITOR FOR ESC HANDLING ***
not_clear:
	jsr		ScreenCheckPosition
	bmi		error
	
not_clear_2:	
	;set old position now (used by setup code for plot pixel)
	jsr		ScreenSetLastPosition
	
	;restore char
	lda		atachr
	
	;check if we're in gr.0
	ldx		dindex
	beq		mode_0
	
	;nope, we're in a graphics mode... that makes this easier.
	;check if it's an EOL
	cmp		#$9b
	beq		graphics_eol
	
	;check for display suspend (ctrl+1) and wait until it is cleared
	ldx:rne	ssflag
	
	;fold the pixel, compute masks, and convert ATASCII to Internal
	jsr		ScreenFoldColor
	pha

	;compute addressing and shift mask
	jsr		ScreenSetupPlotPixel
	
	pla
	ldy		shfamt
	eor		(toadr),y
	and		bitmsk
	eor		(toadr),y
	sta		(toadr),y

	;advance cursor position and exit
	jmp		ScreenAdvancePosNonMode0
	
mode_0:
	;check for EOL, which bypasses the ESC check
	cmp		#$9b
	bne		not_eol
	
	;it's an EOL
	lda		lmargn
	sta		colcrs
	inc		rowcrs
	lda		rowcrs
	cmp		botscr
	bcc		noywrap
	
	;We've gone over -- delete logical line 0 to make room.
	;Note that we need to set ROWCRS here first because the scroll may
	;delete more than one physical line.
	ldx		botscr
	stx		rowcrs
	
	jsr		EditorDeleteLine0
noywrap:
	jmp		recompute_show_cursor_exit

not_eol:
	;check for display suspend (ctrl+1) and wait until it is cleared
	ldx:rne	ssflag
	
	pha
	jsr		EditorRecomputeCursorAddr
	pla
	jsr		ScreenConvertATASCIIToInternal
	
	;plot character
	ldy		#0
	sta		(oldadr),y
	
	;inc pos
	inw		oldadr
	jsr		ScreenAdvancePosMode0
	
	;check if we've gone beyond the right margin
	bcs		nowrap
	
	;check if we're beyond the bottom of the screen
	lda		rowcrs
	cmp		botscr
	bcc		no_scroll
	
	;yes -- scroll up
	jsr		EditorDeleteLine0

	;check if we can extend the current logical line -- 3 rows max.
	jsr		check_extend

	;Mark the current physical line as part of the last logical line.
	;
	;NOTE: There is a subtlety here in that we may delete multiple physical
	;      lines if the top logical line is more than one line long, but we
	;      only want to add one physical line onto our current logical line.
	jsr		EditorGetCurLogicalLineInfo
	eor		logmap,y
	sta		logmap,y

	jmp		post_scroll
	
no_scroll:
	;check if we can extend the current logical line -- 3 rows max.
	jsr		check_extend

	;okay, here's the fun part -- we didn't scroll beyond, but we might
	;be on another logical line, in which case we need to scroll everything
	;down to extend it.
	lda		rowcrs
	jsr		EditorTestLogicalLineBit
	beq		post_scroll
	
	;yup, insert a physical line
	jsr		ScreenInsertPhysLine

recompute_show_cursor_exit:
post_scroll:
	jsr		EditorRecomputeCursorAddr
nowrap:
	jmp		ScreenShowCursorAndXitOK
	
check_extend:
	ldx		rowcrs
	dex
	txa
	jsr		EditorPhysToLogicalRow

.ifdef _KERNEL_816
	inc
	inc
.else
	clc
	adc		#2
.endif

	cmp		rowcrs
	scc:rts
	pla
	pla
	bcc		post_scroll
.endp

;==========================================================================
ScreenGetStatus = CIOExitSuccess

;==========================================================================
; Given a color byte, mask it off to the pertinent bits and reflect it
; throughout a byte. The byte is converted from ATASCII to Internal
; if the mode uses byte encoding.
;
; Entry:
;	A = color value
;
; Exit:
;	A = folded color byte
;	Y = encoding mode
;	DMASK = right-justified bit mask
;	DELTAC = left-justified bit mask
;
; Modified:
;	HOLD1, ADRESS
;
.proc ScreenFoldColor
	ldx		dindex
	ldy		ScreenEncodingTab,x			;0 = 8-bit, 1 = 4-bit, 2 = 2-bit, 3 = 1-bit
	mvx		ScreenPixelMasks+4,y deltac
	mvx		ScreenPixelMasks,y dmask
	bmi		fold_byte
	and		dmask
	ora		ScreenEncodingOffsetTable-1,y
	tax
	lda		ScreenEncodingTable,x
	rts
	
fold_byte:
	;convert byte from ATASCII to Internal -- this is required for gr.1
	;and gr.2 to work correctly, and harmless for other modes

;==========================================================================
; Convert an ATASCII character to displayable INTERNAL format.
;
; Entry:
;	A = ATASCII char
;
; Exit:
;	A = INTERNAL char
;
.def :ScreenConvertATASCIIToInternal
		;ATASCII	Internal
		;00-1F		40-5F
		;20-3F		00-1F
		;40-5F		20-3F
		;60-7F		60-7F

		asl
		php
		sbc		#$3f		;00>60, 20>00, 40>20, 60>40
		spl:eor	#$40		;00>40, 20>00, 40>20, 60>60
		plp
		ror
		rts
.endp


;==========================================================================
.proc ScreenSpecial
	lda		iccomz
	cmp		#$11
	beq		ScreenDrawLineFill	;draw line
	cmp		#$12
	beq		ScreenDrawLineFill	;fill
	rts
.endp

;==========================================================================
;
; Inputs:
;	COLCRS,ROWCRS = next point
;	OLDCOL,OLDROW = previous point
;	ATACHR = color/character to use
;
; Outputs:
;	OLDCOL,OLDROW = next point
;
; The Bresenham algorithm we use (from Wikipedia):
;	dx = |x2 - x1|
;	dy = |y2 - y1|
;	e = dx - dy
;
;	loop
;		plot(x1, y1)
;		if x1 == x2 and y1 == y2 then exit
;		e2 = 2*e
;		if e2 + dy > 0 then
;			err = err - dy
;			x0 = x0 + sign(dx)
;		endif
;		if e2 < dx then
;			err = err + dx
;			y0 = y0 + sign(dy)
;		endif
;	end
;	
.proc ScreenDrawLineFill
	;;##TRACE "Drawing line (%d,%d)-(%d,%d) in mode %d" dw(oldcol) db(oldrow) dw(colcrs) db(rowcrs) db(dindex)
	
	;hide cursor if gr.0 (required by Space Way.bas)
	ldx		dindex
	bne		not_gr0
	jsr		ScreenHideCursor
not_gr0:
	
	;initialize bit mask and repeat pertinent pixel bits throughout byte
	lda		fildat
	jsr		ScreenFoldColor
	sta		hold4
	lda		atachr
	jsr		ScreenFoldColor
	sta		hold1

	jsr		ScreenSetupPlotPixel

	;compute screen pitch
	ldy		#1
	jsr		ScreenComputeRangeSize
	sta		deltac+1
	tax
	ldy		#0

	;compute abs(dy) and sign(dy)
	lda		rowcrs
	sub		oldrow
	bcs		going_down
	eor		#$ff					;take abs(dy)
	adc		#1						;
	;negate screen pitch
	pha
	txa
	eor		#$ff
	tax
	inx
	dey
	pla
going_down:
	stx		rowac
	sta		deltar
	sty		rowac+1
	
	;;##TRACE "dy = %d" db(deltar)
	
	;compute abs(dx) and sign(dx)
	ldx		#0
	lda		colcrs
	sub		oldcol
	sta		colac
	lda		colcrs+1
	sbc		oldcol+1
	bcs		going_right
	eor		#$ff
	tay
	lda		colac
	eor		#$ff
	adc		#1
	sta		colac
	tya
	adc		#0
	ldx		#left_shift_8-right_shift_8
going_right:
	sta		colac+1

	;;##TRACE "dx = %d" dw(colac)
	
	;set up x shift routine
	txa
	ldx		dindex
	ldy		ScreenEncodingTab,x
	clc
	adc		shift_lo_tab,y
	sta		adress
	lda		#>left_shift_8
	sta		adress+1
	sta		endpt+1
	
	;set up x fill shift routine
	lda		shift_lo_tab,y
	clc
	adc		#fill_right_8-right_shift_8
	sta		endpt
	
	;compute initial error accumulator in frmadr (dx-dy)
	ldx		colac
	txa
	sub		deltar
	sta		frmadr
	ldy		colac+1				;leave dx in y:x for max() below
	tya
	sbc		#0
	sta		frmadr+1

	;compute max(dx, dy) based on sign of (dx - dy)
	bcs		dx_larger
	ldy		#0
	ldx		deltar
dx_larger:
	stx		countr
	sty		countr+1

	tya
	bne		not_empty
	txa
	beq		skip_showcursor
not_empty:
	
	;;##TRACE "Pixel count = %d" dw(countr)
	
	;enter pixel loop (this will do a decrement for us)
	jmp		next_pixel

;----------------------------------------------
done:
	jsr		ScreenSetLastPosition

	;RYBA PILA requires the quirky behavior of the last character of a non-zero length
	;DRAWTO being stomped by the character saved from the cursor draw of the last PLOT
	;(write to S:). Note that we must NOT do this for length 0 or it breaks SPACEWAY.
	ldy		dindex
	bne		skip_showcursor
	jsr		EditorRecomputeCursorAddr
skip_showcursor:
	ldy		#1
	rts

;----------------------------------------------	
fill_done:
	stx		bitmsk
	jmp		next_pixel

;----------------------------------------------	
do_fill:
	ldy		shfamt				;load current byte offset
	ldx		bitmsk				;save current bitmask
	bne		fill_start			;!! - unconditional

;----------------------------------------------		
shift_lo_tab:
	dta		<right_shift_8	
	dta		<right_shift_4	
	dta		<right_shift_2	
	dta		<right_shift_1

;----------------------------------------------	
left_shift_4:
	asl
	asl
left_shift_2:
	asl
left_shift_1:
	asl
	bcc		left_shift_ok
left_shift_8:
	dec		shfamt
	lda		dmask
left_shift_ok:
	bne		post_xinc			;!! - unconditional

;----------------------------------------------	
fill_right_4:
	lsr
	lsr
fill_right_2:
	lsr
fill_right_1:
	lsr
	bcc		fill_right_ok
fill_right_8:
	lda		deltac
	iny
	cpy		deltac+1
	scc:ldy	#0
fill_right_ok:
	sta		bitmsk
	beq		fill_done
fill_loop:
	lda		(toadr),y			;load screen byte
	bit		bitmsk				;mask to current pixel
	bne		fill_done			;exit loop if non-zero
	eor		hold4				;XOR with fill color
	and		bitmsk				;mask change bits to current pixel
	eor		(toadr),y			;merge with screen byte
	sta		(toadr),y			;save screen byte
fill_start:
	lda		bitmsk
	jmp		(endpt)

;----------------------------------------------	
right_shift_4:
	lsr
	lsr
right_shift_2:
	lsr
right_shift_1:
	lsr
	bcc		right_shift_ok
right_shift_8:
	inc		shfamt
	lda		deltac
right_shift_ok:
	;fall through to post_xinc
post_xinc:
	sta		bitmsk
no_xinc:

	;plot pixel
	;;##TRACE "Plotting at $%04X+%d with mask $%02X" dw(toadr) db(shfamt) db(bitmsk)	
	ldy		shfamt
	lda		hold1
	eor		(toadr),y
	and		bitmsk
	eor		(toadr),y
	sta		(toadr),y

	;do fill if needed
	lda		iccomz
	lsr
	bcc		do_fill
	
next_pixel:
	;loop back for next pixel
	lda		countr
	bne		next_pixel_2
	dec		countr+1
	bmi		done
next_pixel_2:
	dec		countr

	;!! - fall through to pixel loop

	;------- pixel loop state -------
	;	(zp)	frmadr		error accumulator
	;	(zp)	toadr		current row address
	;	(abs)	dmask		right-justified bit mask
	;	(zp)	deltac		left-justified bit mask
	;	(zp)	bitmsk		current bit mask
	;	(zp)	shfamt		current byte offset within row
	;	(zp)	rowac		y step address increment/decrement (note different from Atari OS)
	;	(zp)	adress		left/right shift routine
	;	(zp)	deltac+1	screen pitch, in bytes (for fill)
	;	(zp)	endpt		right shift routine
	;	(zp)	colac		dy
pixel_loop:
	;compute 2*err
	;;##TRACE "Error accum = %d (dx=%d, dy=%d(%d))" dsw(frmadr) dw(colac) db(deltar) dsw(rowac)
	lda		frmadr
	asl
	tay
	lda		frmadr+1
	rol
	tax
	
	;check for y increment (2*e < dx, or A:Y < colac)
	tya
	clc
	sbc		colac
	txa
	pha
	sbc		colac+1
	bpl		no_yinc

do_yinc:
	;bump y (add/subtract pitch, e += dx)
	ldx		#2
yinc_loop:
	lda		rowac,x
	clc
	adc		toadr,x
	sta		toadr,x
	lda		rowac+1,x
	adc		toadr+1,x
	sta		toadr+1,x
	dex
	dex
	bpl		yinc_loop
no_yinc:

	;check for x increment (2*e + dy > 0, or Y:[S] + deltar > 0)
	tya
	clc
	adc		deltar
	pla
	adc		#0
	bmi		no_xinc

	;update error accumulator
	lda		frmadr
	sub		deltar
	sta		frmadr
	scs:dec	frmadr+1

	;bump x
	lda		bitmsk
	jmp		(adress)

.if [right_shift_4 ^ right_shift_8]&$ff00
	.error "Right draw routines cross page: ",right_shift_4,"-",right_shift_8
.endif
.if [left_shift_4 ^ left_shift_8]&$ff00
	.error "Left draw routines cross page: ",left_shift_4,"-",left_shift_8
.endif
.if [fill_right_4 ^ fill_right_8]&$ff00
	.error "Fill routines cross page: ",fill_right_4,"-",fill_right_8
.endif
.if [[right_shift_4^left_shift_4]|[left_shift_4^fill_right_4]]&$ff00
	.error	"Line/fill routines cross page: ",left_shift_4,',',right_shift_4,',',fill_right_4
.endif

.endp

;==========================================================================
; Clear the screen.
;
; Used:
;	ADRESS
;	TOADR
;
; Quirks:
;	Clears the split-screen text area even if the main screen (S:) receives
;	the clear. The In-Store Demonstration Cart depends on this. The cursor
;	state of the split screen is NOT reset, so if the cursor was over a
;	non-blank character, that character will be restored when the cursor
;	moves.
;
;	The logical line map is always reset.
;
.proc ScreenClear
	;first, set up for clearing the split-screen window (4*40 bytes main)
	ldy		#0
	
	;check if we are in the split screen text window
	ldx		swpflg
	bne		is_text_window
	
	;nope, it's the main screen... compute size
	ldy		dindex
	ldx		ScreenHeightShifts,y
	ldy		ScreenHeights,x
is_text_window:
	jsr		ScreenComputeRangeSize

	;add 160 bytes to size if not GR.0 -- important to avoid clearing
	;beyond GR.0 screen (the font breaks in BIKERDAV.BAS otherwise!)
	clc
	ldx		dindex
	beq		is_gr0
	adc		#160
is_gr0:
	tay
	lda		adress+1
	adc		#0

	tax
	adc		savmsc+1
	sta		toadr+1
	mva		savmsc toadr

	;As it turns out, there are no cases where the main screen
	;is an exact number of pages... so we can simply plow into
	;the clear loop.
	lda		#0
loop:
	dey
	sta		(toadr),y
	bne		loop
	dec		toadr+1
	dex
	bpl		loop

	;reset coordinates and cursor (we're going to wipe the cursor)
	sta		colcrs+1
	sta		rowcrs
	sta		oldadr+1
	
	;always reset the logical line map
	ldx		dindex
	bne		is_graphic_screen
	lda		lmargn
is_graphic_screen:
	sta		colcrs

	;always reset the logical line map and exit
	jmp		ScreenResetLogicalLineMap
.endp

;==========================================================================
; Insert a physical line at the current location.
;
; Entry:
;	ROWCRS = row before which to insert new line
;	C = 0 if physical line only, C = 1 if should start new logical line
;
; Modified:
;	HOLD1, ADRESS
;
ScreenInsertLine = ScreenInsertPhysLine.use_c
.proc ScreenInsertPhysLine
	clc
use_c:
	;save new logline flag
	php
	
	;compute addresses
	ldy		botscr
	dey
	jsr		ScreenComputeToAddrX0
	jsr		EditorNextLineAddr
	
	;copy lines
	ldx		botscr
	bne		line_loop_start
line_loop:
	ldy		#39
char_loop:
	lda		(frmadr),y
	sta		(toadr),y
	dey
	bpl		char_loop
line_loop_start:	
	lda		frmadr
	sta		toadr
	sec
	sbc		#40
	sta		frmadr
	lda		frmadr+1
	sta		toadr+1
	sbc		#0
	sta		frmadr+1
	
	dex
	cpx		rowcrs
	bne		line_loop
	
no_copy:
	;clear the current line
	ldy		#39
	lda		#0
clear_loop:
	sta		(toadr),y
	dey
	bpl		clear_loop
	
	;insert bit into logical line mask
	jsr		EditorGetCurLogicalLineInfo
	
	plp
	scs:lda	#0
	sta		hold1

	lda		#0
	sec
	sbc		ReversedBitMasks,x			;-bit
	asl
	and		logmap,y
	clc
	adc		logmap,y
	ror
	ora		hold1
	sta		logmap,y
	
	dey
	spl:ror	logmap+1
	dey
	spl:ror	logmap+2
	rts
.endp

;==========================================================================
; Hide the screen cursor, if it is present.
;
; Modified:
;	Y
;
; Preserved:
;	A
;
.proc ScreenHideCursor
	;check if we had a cursor (note that we CANNOT use CRSINH for this as
	;it can be changed by app code!)
	ldy		oldadr+1
	beq		no_cursor
	
	;erase the cursor
	pha
	ldy		#0
	lda		oldchr
	sta		(oldadr),y
	sty		oldadr+1
	pla
no_cursor:
	rts
.endp

;==========================================================================
.proc	ScreenCheckPosition
	;Check for ROWCRS out of range. Note that for split screen modes we still
	;check against the full height!
	lda		botscr
	ldx		dindex
	beq		rowcheck_gr0
	ldy		ScreenHeightShifts,x
	lda		ScreenHeights,y
rowcheck_gr0:
	;while we know it's GR.0, clamp RMARGN to 39 (required for ARTILLERY.BAS)
	ldy		#39
	cpy		rmargn
	bcs		rmargn_ok
	sty		rmargn
rmargn_ok:
	clc
	sbc		rowcrs
	bcs		rowcheck_pass	
invalid_position:

	;If the cursor is out of range, reset it within bounds.
	ldy		#0
	sty		colcrs+1		;X high = 0
	txa
	sne:ldy	lmargn
	sty		colcrs			;X low = X origin

	ldy		#CIOStatCursorRange
	rts
	
rowcheck_pass:
	;check width
	ldy		ScreenPixelWidthIds,x
	lda		colcrs
	cmp		ScreenPixelWidthsLo,y
	lda		colcrs+1
	sbc		ScreenPixelWidthsHi,y
	bcs		invalid_position

	;check for BREAK
	ldy		#$ff
	lda		brkkey
	bne		no_break
	sty		brkkey
	ldy		#CIOStatBreak-1
no_break:
	iny
	rts
.endp

;==========================================================================
; Swap between the main screen and the split screen.
;
; Conventionally, the main screen is left as the selected context when
; the display handler is not active.
;
; Inputs:
;	C = 0	for main screen
;	C = 1	for split screen
;
; Modified:
;	X
;
; Preserved:
;	A
;	
;==========================================================================
; Swap in the text screen (main if gr.0, split otherwise).
;
.proc	EditorSwapToText
	;set C=0 (main) if gr.0, C=1 (split) otherwise
	ldy		#23
	cpy		botscr

.def :ScreenSwap = *
	;check if the correct set is in place
	pha
	lda		#0
	adc		swpflg
	beq		already_there
	
	;Nope, we need to swap. Conveniently, the data to be swapped
	;is in a 12 byte block:
	;
	;	ROWCRS ($0054)		TXTROW ($0290)
	;	COLCRS ($0055)		TXTCOL ($0291)
	;	DINDEX ($0057)		TINDEX ($0293)
	;	SAVMSC ($0058)		TXTMSC ($0294)
	;	OLDROW ($005A)		TXTOLD ($0296)
	;	OLDCOL ($005B)		TXTOLD ($0297)
	;	OLDCHR ($005D)		TXTOLD ($0299)
	;	OLDADR ($005E)		TXTOLD ($029A)
	
	ldx		#11
swap_loop:
	lda		rowcrs,x
	ldy		txtrow,x
	sty		rowcrs,x
	sta		txtrow,x
	dex
	bpl		swap_loop
	
	;invert swap flag
	txa
	eor		swpflg
	sta		swpflg
	
already_there:
	pla
	rts
.endp

;==========================================================================
; Compute character address.
;
; Entry:
;	X = byte index
;	Y = line index
;
; Exit:
;	A:X = address
;
; Used:
;	ADRESS
;
.proc	ScreenComputeAddr
	jsr		ScreenComputeRangeSize
	sta		adress
	txa
	clc
	adc		adress			;row*10,20,40+col
	scc:inc	adress+1
	clc
	adc		savmsc
	tax
	lda		adress+1
	adc		savmsc+1
	rts
.endp

;==========================================================================
ScreenComputeFromAddr = ScreenComputeFromAddrX0.with_x
.proc	ScreenComputeFromAddrX0
	ldx		#0
with_x:
	jsr		ScreenComputeAddr
	stx		frmadr
	sta		frmadr+1
	rts
.endp

;==========================================================================
ScreenComputeToAddr = ScreenComputeToAddrX0.with_x
.proc	ScreenComputeToAddrX0
	ldx		#0
with_x:
	jsr		ScreenComputeAddr
	stx		toadr
	sta		toadr+1
	rts
.endp

;==========================================================================
; Compute size, in bytes, of a series of lines.
;
; Entry:
;	Y = line count
;
; Exit:
;	ADRESS+1	High byte of size
;	A			Low byte of size
;	
; Preserved:
;	X
;
; Modified:
;	Y
;
.proc	ScreenComputeRangeSize
	mva		#0 adress+1
	sty		adress
	ldy		dindex
	lda		ScreenPixelWidthIds,y
	sec
	sbc		ScreenEncodingTab,y
	tay
	iny
	lda		adress
	asl
	rol		adress+1		;row*2
	asl
	rol		adress+1		;row*4
	clc
	adc		adress			;row*5
	scc:inc	adress+1
shift_loop:
	asl
	rol		adress+1		;row*10,20,40
	dey
	bpl		shift_loop
	rts
.endp

;==========================================================================
; Setup for pixel plot.
;
; Entry:
;	OLDCOL, OLDROW = position
;	DELTAC = left-justified pixel mask
;
; Exit:
;	TOADR = screen row
;	SHFAMT = byte offset within row
;	BITMSK = shifted bit mask for pixel
;
; Modified:
;	ADRESS
;
ScreenAlignPixel = ScreenSetupPlotPixel.rshift_mask
.proc ScreenSetupPlotPixel
	;;##TRACE "Folded pixel = $%02X" db(hold1)
	jsr		ScreenSetupPixelAddr
	
	;preshift bit mask
	lda		deltac
rshift_mask:
	dex
	bmi		xmaskshift_done
xmaskshift_loop:
	lsr
	dex
	bpl		xmaskshift_loop
xmaskshift_done:
	sta		bitmsk
	
	;;##TRACE "Initial bitmasks = $%02X $%02X" db(bitmsk) db(dmask)
	rts
.endp

;==========================================================================
; Setup for pixel addressing.
;
; Entry:
;	COLCRS, ROWCRS = position (ScreenSetupPixelAddr)
;	OLDCOL, OLDROW = position (ScreenSetupPixelAddrOld)
;
; Exit:
;	TOADR = screen row
;	SHFAMT = byte offset within row
;	X = number of bits from left side of byte to left side of pixel
;
.proc ScreenSetupPixelAddr
	;compute initial address
	ldy		oldrow
	jsr		ScreenComputeToAddrX0
	
	;;##TRACE "Initial row address = $%04X" dw(toadr)
	
	;compute initial byte offset
	lda		oldcol+1
	ldx		oldcol
phase2:
	ror
	stx		shfamt
	lda		#0
	ldx		dindex
	ldy		ScreenEncodingTab,x
	beq		no_xshift	
xshift_loop:
	ror		shfamt
	ror
	dey
	bne		xshift_loop
no_xshift:
	rol
	rol
	rol
	rol
	tax

	;;##TRACE "Initial row offset = $%02X" db(shfamt)
	rts
.endp

;==========================================================================
; ScreenAdvancePosNonMode0
;
.proc ScreenAdvancePosNonMode0
	;advance position
	inc		colcrs
	sne:inc	colcrs+1
	ldx		dindex
	ldy		ScreenPixelWidthIds,x
	ldx		ScreenPixelWidthsHi,y
	cpx		colcrs+1
	bne		graphics_no_wrap
	ldx		ScreenPixelWidthsLo,y
	cpx		colcrs
	bne		graphics_no_wrap

graphics_eol:
	;move to left side and then one row down -- note that this may
	;push us into an invalid coordinate, which will result on an error
	;on the next call if not corrected
	ldy		#0
	sty		colcrs
	sty		colcrs+1
	inc		rowcrs
graphics_no_wrap:
	ldy		#1
	rts
.endp

;==========================================================================
.if !_KERNEL_XLXE
	_SCREEN_TABLES_1
.endif
