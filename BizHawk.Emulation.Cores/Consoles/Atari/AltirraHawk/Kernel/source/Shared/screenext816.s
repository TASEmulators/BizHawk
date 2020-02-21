;	Altirra - Atari 800/800XL/5200 emulator
;	Modular Kernel ROM - 65C816 Screen Handler extension routines
;	Copyright (C) 2008-2018 Avery Lee
;
;	Copying and distribution of this file, with or without modification,
;	are permitted in any medium without royalty provided the copyright
;	notice and this notice are preserved.  This file is offered as-is,
;	without any warranty.

;==========================================================================
ScreenEncodingOffsetTable:
	dta		$00			;4-bit
	dta		$10			;2-bit
	dta		$14			;1-bit

.if !_KERNEL_XLXE
	_SCREEN_TABLES_3
.endif

;==========================================================================
; ScreenFineScrollDLI
;
; This DLI routine is used to set the PF1 color to PF2 to kill junk that
; would appear on the extra line added with vertical scrolling.
;
.if _KERNEL_XLXE
.proc ScreenFineScrollDLI
	pha
	lda		color2
	eor		colrsh
	and		drkmsk
	sta		colpf1
	pla
	rti
.endp
.endif

;==========================================================================
; ScreenResetLogicalLineMap
;
; Marks all lines as the start of logical lines.
;
; Exit:
;	X = 0
;
.proc ScreenResetLogicalLineMap
	ldx		#$ff
	stx		logmap
	stx		logmap+1
	stx		logmap+2
	
	;reset line read position
	inx
	stx		bufstr
	lda		lmargn
	sta		bufstr+1
	
	;note - X=0 relied on here by EditorOpen
	rts
.endp

;==========================================================================
; ScreenSetLastPosition
;
; Copies COLCRS/ROWCRS to OLDCOL/OLDROW.
;
.proc ScreenSetLastPosition
	ldx		#2
loop:
	lda		rowcrs,x
	sta		oldrow,x
	dex
	bpl		loop
	rts
.endp

;==========================================================================
; ScreenAdvancePosMode0
;
; Advance to the next cursor position in reading order, for mode 0.
;
; Exit:
;	C = 1 if no wrap, 0 if wrapped
;
; Modified:
;	X
;
; Preserved:
;	A
;
.proc ScreenAdvancePosMode0
	inc		colcrs
	ldx		rmargn
	cpx		colcrs
	bcs		post_wrap
	ldx		lmargn
	stx		colcrs
	inc		rowcrs
post_wrap:
	rts
.endp

;==========================================================================
; Also returns with Y=1 for convenience.
.proc ScreenShowCursorAndXitOK
	;;##ASSERT dw(oldadr) >= dw(savmsc)
	;check if the cursor is enabled
	ldy		crsinh
	bne		cursor_inhibited
	lda		(oldadr),y
	sta		oldchr
	eor		#$80
	sta		(oldadr),y
	iny
	rts
	
cursor_inhibited:
	;mark no cursor
	stz		oldadr+1
exit_success:
	ldy		#1
	rts
.endp

;==========================================================================
; Close screen (S:).
;
; This is a no-op in OS-B mode. In XL/XE mode, it reopens the device in
; Gr.0 if fine scrolling is on, since this is necessary to clear the DLI.
; This happens even if S: doesn't correspond to the text window. Only
; the high bit of FINE is checked.
;
.if !_KERNEL_XLXE
ScreenClose = CIOExitSuccess
.else
.proc ScreenClose
	bit		fine
	bpl		ScreenShowCursorAndXitOK.exit_success
	
	;turn off DLI
	mva		#$40 nmien
	
	;restore vdslst
	ldx		#<IntExitHandler_None
	lda		#>IntExitHandler_None
	jsr		ScreenOpenGr0.write_vdslst
	
	jmp		ScreenOpenGr0
.endp
.endif

;==========================================================================
.if !_KERNEL_XLXE
	_SCREEN_TABLES_2
.endif
