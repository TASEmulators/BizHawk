;	Altirra - Atari 800/800XL/5200 emulator
;	Modular Kernel ROM - Editor Handler
;	Copyright (C) 2008-2016 Avery Lee
;
;	Copying and distribution of this file, with or without modification,
;	are permitted in any medium without royalty provided the copyright
;	notice and this notice are preserved.  This file is offered as-is,
;	without any warranty.

;==========================================================================
.proc	EditorOpen
	jsr		ScreenResetLogicalLineMap
	stx		dspflg		;!! relies on X=0 from ScreenResetLogicalLineMap
	stx		escflg
	
	;we must force mode 0 here -- ACTION calls this with $0C in AUX2 and
	;expects mode 0 to be set
	jmp		ScreenOpenMode0
.endp

;==========================================================================
EditorClose = ScreenClose

;==========================================================================
;
; === Forced read mode ===
;
; If bit 0 if AUX1 is set on the IOCB, it means to read in forced read
; mode. In forced read mode, no keyboard input is fetched and instead
; line read immediately commences, as if ENTER were pressed.
;
; Note that since AUX1 is tested on the IOCB used, it is possible to have
; E: open on more than one IOCB with different forced read modes.
;
.proc	EditorGetByte
	;check if we have anything left in the current line
	lda		bufcnt
	bne		have_line
	
	;nope, fetch a line
	jsr		EditorGetLine
	bpl		have_line
	rts
	
have_line:
	;swap to text context
	jsr		EditorSwapToText
	
	;subtract off a char
	dec		bufcnt
	
	;check if we're at the EOL
	bne		have_char
	
	;yes, we're at the EOL -- print it and return it (this will re-enable
	;the cursor, too)
	lda		#$9b
	jsr		ScreenPutByte

	lda		#$9b
	bne		done
	
have_char:
	;read a char from the screen
	jsr		ScreenGetByte
done:
	jmp		EditorSwapToScreen
.endp

;==========================================================================
; This behavior is quite complex:
;	- The editor remembers the cursor position within the initial logical
;	  line. Only contents beyond the initial position are returned. This
;	  is true even if the cursor is moved out of the logical line and back
;	  in again.
;	- Margins are not included.
;	- Trailing spaces at the end of a logical line are not returned even
;	  if the cursor is after them.
;	- If the cursor is moved to another logical line, that logical line is
;	  read instead. There is no memory of the line start for previous
;	  logical lines, so if there was a prompt on that line it will be
;	  picked up too.
;	- If the logical line length is exceeded, a beep sounds near the end
;	  and after three lines (120ch) the logical line is terminated and a
;	  new logical line is started. However, the previous logical line that
;	  was overflowed is still returned. This still happens if the new
;	  logical line extends to more than one physical line or even if
;	  itself overflows (!). If the physical line is changed with up/down,
;	  this memory is lost, but left/right don't do this.
;	- If a logical line extends exactly to the end of a physical line, an
;	  extra blank line is printed on EOL.
;	- If a logical line extends exactly to the end of the screen, a cursor
;	  out of bounds error results. (We currently do not implement this...
;	  feature.)
;
; === The BUFSTR variable ===
;
; The BUFSTR variable is critical to the way that the get line algorithm
; works. It actually consists of two bytes, the row followed by the column
; of the origin of the line to be read. This is updated whenever a read
; request arrives that starts a new line or whenever the cursor is moved
; to a different logical line. This is how the screen editor knows to
; read at the end of a prompt or from a different line that you have moved
; to, and how it tracks that when scrolling occurs. It does not, however,
; get updated when regular characters are typed, even when spilling to a
; new logical line.
;
; === BUFCNT ===
;
; BUFCNT contains the number of characters left to return before we have
; to get a new line: $00 means we will wait for keys, $01 is the EOL, and
; >$01 returns a typed character. We need to follow this convention or
; else the combination of S_VBXE.SYS + CON.SYS /E from SDX 4.46 acts
; strangely on launch.
;
.proc	EditorGetLine
_start_x equ bufstr+1
_start_y equ bufstr

	;Set line buffer start to current position (NOT the start of the logical
	;line -- this is a special case).
	jsr		EditorSwapToText
	mva		colcrs _start_x
	mva		rowcrs _start_y
	jsr		EditorSwapToScreen

	;check if forced read is enabled on the IOCB -- if so, assume we've gotten
	;an EOL
	lda		icax1z
	lsr
	bcs		do_eol

read_loop:
	;get a character
	jsr		KeyboardGetByte
	bpl		read_ok
read_error:
	rts
	
read_ok:
	cmp		#$9b
	beq		is_eol	

	;echo the character	
	jsr		EditorPutByte
	bmi		read_error
	
	;check if we've hit the warning point (logical pos 113)
	lda		colcrs
	cmp		#33
	bne		read_loop
	
	;convert current row to logical start row
	jsr		EditorGetCurLogicalRow
	
	;check if we're on the third row
	clc
	adc		#2
	cmp		rowcrs
	
	;if so, sound the bell
	bne		read_loop
	jsr		EditorBell
	
	jmp		read_loop

do_eol:
	lda		#$9b
is_eol:
	;echo the character	
	jsr		EditorPutByte
	bmi		read_error

	;swap to text screen context
	jsr		EditorSwapToText
	
	;hide the cursor so we can scan text properly
	jsr		ScreenHideCursor
		
	;compute address
	ldx		#0
	stx		logcol
	ldy		_start_y
	sty		rowcrs
	jsr		ScreenComputeToAddr
	
	ldy		_start_x
char_loop:
	inc		logcol
	lda		(toadr),y
	beq		blank
	mva		logcol bufcnt
blank:
	cpy		rmargn
	iny
	bcc		char_loop
	jsr		EditorNextLineAddr
	
	;check if we're at the bottom of the screen
	inc		rowcrs
	lda		rowcrs
	cmp		botscr
	bcs		scan_scroll
	
	;check if we're at the end of the logical line
	jsr		EditorTestLogicalLineBit
	bne		scan_done
	
	ldy		lmargn
	jmp		char_loop
	
scan_scroll:
	;we're below the screen... need to scroll up
	jsr		EditorDeleteLine0
	
scan_done:
	;adjust BUFCNT so it is the number of chars to read (scanned + EOL)
	inc		bufcnt

	;mark this as the start of a new logical line, trimming off any extra
	;blank lines
	lda		rowcrs
	jsr		EditorGetLogicalLineInfo
	ora		logmap,y
	sta		logmap,y

	;reset cursor to line start, but leave the cursor off
	mvx		_start_x colcrs
	mvy		_start_y rowcrs

	;swap back to main context and exit successfully
	jmp		EditorSwapToScreen_Y1
.endp

;==========================================================================
; This routine must NOT write BUFADR -- doing so breaks the DLT flasher.
;
; Behavior notes:
;	- Ctrl+1 (suspend display) does not affect control codes handled only
;	  by E:. In particular, EOL, move up/down/left/right, ESC, insert, and
;	  delete are not affected by it. CLEAR on the other hand, is, because
;	  it's handled by S:.
;
.proc	EditorPutByte
	;open the text screen if it is not open already
	ldy		dindex
	beq		screenok
	ldy		botscr
	cpy		#4
	beq		screenok

	pha
	jsr		ScreenOpenGr0
	pla
screenok:

	;swap to text context
	jsr		EditorSwapToText

	;hide the cursor -- this must be done before we suspend display on
	;Ctrl+1.
	jsr		ScreenHideCursor

	;save off character to free up acc
	tay

	;Check if [esc] is active
	;
	;Note that the ASL trick relies on ESCFLG being $80 when set; this
	;is in fact guaranteed by the spec in the OS Manual, Appendix L, B26.
	;
	asl		escflg					;test and clear escape flag
	bcs		not_special

	;check if this might be a special character
	and		#$1f
	cmp		#$1b
	bcc		not_special

	;might be special, but not EOL... search the special char table
	ldx		#special_code_tab_end-special_code_tab-1
	tya
	jsr		EditorIsSpecial
	bne		not_special
	
special_found:
	;check if display of control codes is desired; if so, we need to ignore this
	lda		dspflg
	bne		not_special

	;jump to routine
	jsr		dispatch
	jsr		EditorRecomputeCursorAddr
	jsr		ScreenShowCursorAndXitOK
	bpl		xit2				;!! - unconditional jump

not_special:
	;ok, just put the char to the screen
	sty		atachr
	jsr		ScreenPutByte.not_clear
xit2:
	;swap back to main context and exit
	jmp		EditorSwapToScreen

;---------------
special_code_tab:
	dta		$1b
	dta		$1c
	dta		$1d
	dta		$1e
	dta		$1f
	dta		$7d
	dta		$7e
	dta		$7f
	dta		$9c
	dta		$9d
	dta		$9e
	dta		$9f
	dta		$fd
	dta		$fe
	dta		$ff
special_code_tab_end:
	dta		$9b				;these are only for the K: check
	dta		$7c
special_code_tab_end_2:

;---------------
special_escape:
	mva		#$80 escflg
	rts

;---------------
special_clear:
	jmp		ScreenClear
	
;---------------
special_up:
	ldx		rowcrs
	bne		isup2
	ldx		botscr
isup2:
	dex
	bpl		vmoveexit
	
;---------------
special_down:
	ldx		rowcrs
	inx
	cpx		botscr
	scc:ldx	#0
	
vmoveexit:
	stx		rowcrs
	
	;check if we have moved into a different logical line -- if so,
	;we need to reset the read row
	txa
	jsr		EditorPhysToLogicalRow
	cmp		bufstr
	beq		moveexit
	
	;we switched rows -- reset read row
	sta		bufstr
	mva		lmargn bufstr+1
	
moveexit:
	rts

;---------------
special_backspace:
	;check if we are at the left column
	lda		lmargn
	cmp		colcrs
	bcs		sbks_wrap
	
	;nope, we can just back up
	dec		colcrs
	
	;recompute pos and clear character
sbks_recomp:
	jsr		EditorRecomputeCursorAddr
	ldy		#0
	tya
	sta		(oldadr),y
sbks_xit:
	rts
	
sbks_wrap:
	;check if we're at the start of the logical line
	lda		rowcrs
	jsr		EditorTestLogicalLineBit
	bne		sbks_xit
	
	;no, so we need to wrap to the right column of the prev line...
	dec		rowcrs
	lda		rmargn
	sta		colcrs
	
	;recompute everything and exit
	jmp		sbks_recomp

;----------------
special_bell:
	jmp		EditorBell

;----------------
special_set_tab:
	jsr		special_common_tab
	ora		tabmap,x
	bne		special_common_exit_tab

special_clear_tab:
	jsr		special_common_tab
	eor		#$ff
	and		tabmap,x
special_common_exit_tab:
	sta		tabmap,x
	rts
	
special_common_tab:
	jsr		EditorGetLogicalColumn
	tay
	jmp		EditorSetupTabIndex	

;--------------------------------------------------------------------------
; Tab behavior:
;	- Moves cursor to the next tab position within the logical line.
;	- If there are no more tabs, moves cursor to beginning of next line.
;	  This may cause a scroll.
;	- Tab does NOT adjust the read row.
;
special_tab:
	jsr		EditorGetLogicalColumn
	tay
	txa
	sec
	adc		rowcrs
	sta		rowcrs
	
	;scan forward until we find the next bit set, or we hit position 120
tab_scan_loop:
	iny
	cpy		#120
	bcs		tab_found
	jsr		EditorSetupTabIndex
	and		tabmap,x
	beq		tab_scan_loop
tab_found:
	sty		colcrs
tab_adjust_row:
	lda		colcrs
	sec
	sbc		#40							;subtract a row worth of columns
	bcc		tab_adjust_done				;exit if <40
	sta		colcrs
	inc		rowcrs						;next row
	lda		rowcrs						;
	cmp		botscr						;check if we're below the screen
	bcs		tab_adjust_scroll			;if so, do a scroll
	jsr		EditorTestLogicalLineBit	;check if we're on a new log line
	beq		tab_adjust_row				;if not, keep adjusting
	bpl		tab_adjust_left				;position at beginning of new line
	
tab_adjust_scroll:
	jsr		EditorDeleteLine0
tab_adjust_left:
hmove_to_lmargn:
	;move to left margin	
	ldx		lmargn
hmove_to_x:
	stx		colcrs
tab_adjust_done:
	rts

;--------------------------------------------------------------------------
special_left:
	ldx		colcrs
	beq		slft_to_right
	dex
	cpx		lmargn
	bcs		hmove_to_x
	
	;move to right margin
slft_to_right:
	ldx		rmargn
	bcc		hmove_to_x

;--------------------------------------------------------------------------
special_right:
	ldx		colcrs
	cpx		rmargn
	bcs		hmove_to_lmargn

	;right one char
	inc		colcrs
	rts

;--------------------------------------------------------------------------
; Delete line behavior:
;	- The entire logical line that the cursor is in is deleted.
;	- The cursor is positioned at the beginning of the next logical line.
;
special_delete_line:
	;delete current logical line
	jsr		EditorGetCurLogicalRow
	jmp		EditorDeleteLine

;----------------
special_insert_line:
	;insert a new logical line at this point; note that this may split
	;the current line
	sec
	jsr		ScreenInsertLine
	jmp		tab_adjust_left

;--------------------------------------------------------------------------
; Delete character behavior:
;	- Erases the current character and drags in characters from the
;	  remainder of the logical line, excluding the margins.
;	- If the last physical line is blank and not the only physical line in
;	  the logical line, it is deleted and the logical line is shortened.
;	  Only one line is removed even if the last two lines are blank. The
;	  cursor does not move when this happens and may be shifted into the
;	  next logical line. This also does not change the input line!
;
special_delete_char:
	;compute base address of current row (not char)
	ldy		rowcrs
	sty		hold1
	jsr		ScreenComputeToAddrX0
	
	;begin shifting in the first column at the current pos
	ldy		colcrs
	
	;delete chars to end
	jmp		delete_shift_loop_entry	
	
;--------------------------------------------------------------------------
; Insert character behavior:
;	- Inserts a blank at the current position and shifts characters forward
;	  within the margins.
;	- If the character shifted out of the end of the logical line is non-
;	  blank, the logical line will be extended if possible. This can cause
;	  a scroll. If the logical line is already three rows, the last
;	  character is lost.
;
special_insert_char:
	;get logical line start
	jsr		EditorGetCurLogicalRow
	
	;compute line at which we cannot add another physical line
	add		#3
	sta		deltar
	
	;compute address of row
	ldy		rowcrs
	sty		hold1
	jsr		ScreenComputeToAddrX0
	
	ldy		colcrs				;end shift at current column
	ldx		#0					;insert blank character at start
	beq		insert_line_loop_entry
	
insert_line_loop:
	ldy		lmargn				;end shift at left column
	ldx		tmpchr				;character to shift in (from last line)
	
insert_line_loop_entry:
	sty		adress+1			;stash shift origin
	ldy		rmargn				;begin shift at right column
	lda		(toadr),y			;get character being shifted out
	sta		tmpchr				;save it off to later shift in on the next row
insert_shift_loop:
	dey
	lda		(toadr),y
	iny
	sta		(toadr),y
	dey
	cpy		adress+1
	bne		insert_shift_loop

	;put character shifted out from previous line into beginning of this one
	txa
	sta		(toadr),y
	
	;next row
	inc		hold1
	
	jsr		EditorNextLineAddr
	
	;check if we're at the end of the logical line
	lda		hold1
	cmp		botscr
	bcs		insert_crossed_lline
	jsr		EditorTestLogicalLineBit
	beq		insert_line_loop
insert_crossed_lline:

	;check if we shifted out a non-blank character
	lda		tmpchr
	beq		insert_done
		
	;save current row
	lda		rowcrs
	pha
	
	;check if the logical line is already 3 rows -- if so, we cannot extend and
	;the last char goes into the bit bucket, but we still must scroll (!)
	ldx		hold1
	cpx		deltar
	php
	
	;move to the bottom line +1; we use ROWCRS so it stays updated with the scrolling
	stx		rowcrs
	
	;check if we are at the bottom of the screen; if so we must scroll
	lda		#0
	sta		deltar

	cpx		botscr
	bcc		insert_no_scroll
	
	;scroll the screen
	jsr		EditorDeleteLine0
	
insert_no_scroll:
	;if we can't extend, we are done
	plp
	bcs		insert_cant_extend
	
	;just insert a blank line at the end of this logical row to extend it
	jsr		ScreenInsertPhysLine
		
	;restore shifted character and put it in place
	ldy		rowcrs
	jsr		ScreenComputeToAddrX0
	lda		tmpchr
	ldy		lmargn
	sta		(toadr),y
insert_cant_extend:

	;restore cursor row, adjusting for any scroll
	pla
	sec
	sbc		deltar
	sta		rowcrs

insert_done:
	rts

;---------------------------------
dispatch:
	lda		#>special_escape
	pha
	lda		special_dispatch_lo_tab,x
	pha
	rts

;---------------------------------
delete_line_loop:
	;copy first character into right margin of previous row
	ldy		lmargn
	lda		(toadr),y
	ldy		rmargn
	sta		(frmadr),y
	
	;start shifting new row at left margin
	ldy		lmargn
delete_shift_loop:	
	iny
	lda		(toadr),y
	dey
	sta		(toadr),y
	iny
delete_shift_loop_entry:
	cpy		rmargn
	bne		delete_shift_loop
	
	;next line
	jsr		EditorNextLineAddr
	
	;check if the next row is a logical line start
	ldx		hold1
	inx
	cpx		botscr
	bcs		delete_stop_shifting
	stx		hold1
	txa
	jsr		EditorTestLogicalLineBit
	
	;keep going if not
	beq		delete_line_loop
	
delete_stop_shifting:
	;blank the last character of the last line
	ldy		rmargn
	lda		#0
	sta		(frmadr),y
	
	;check if the last line is blank
delete_blank_test_loop:
	lda		(frmadr),y
	bne		delete_not_blank
	dey
	cpy		lmargn
	bcs		delete_blank_test_loop
	
	;the last line is blank... check if it is a logical line start
	dec		hold1
	lda		hold1
	jsr		EditorTestLogicalLineBit
	
	;skip if so -- we can't delete the entire logical line
	bne		delete_not_blank
	
	;delete this physical line... however, do not move the cursor and
	;do not change the read line even if the cursor hops to a new one
	lda		colcrs
	pha
	lda		hold1
	jsr		EditorDeleteLine
	pla
	sta		colcrs
	
delete_not_blank:
	;re-show cursor and exit
	rts

;----------------

special_dispatch_lo_tab:
	dta		<(special_escape-1)
	dta		<(special_up-1)
	dta		<(special_down-1)
	dta		<(special_left-1)
	dta		<(special_right-1)
	dta		<(special_clear-1)
	dta		<(special_backspace-1)
	dta		<(special_tab-1)
	dta		<(special_delete_line-1)
	dta		<(special_insert_line-1)
	dta		<(special_clear_tab-1)
	dta		<(special_set_tab-1)
	dta		<(special_bell-1)
	dta		<(special_delete_char-1)
	dta		<(special_insert_char-1)
	
.if [((special_escape-1)^(special_insert_char-1))&$ff00]
	.error 'Special character routines cross a page boundary: ',special_escape,'-',special_insert_char
.endif
.endp

;==========================================================================
.proc EditorIsSpecial
special_binsearch:
	cmp		EditorPutByte.special_code_tab,x
	beq		special_found
	dex
	bpl		special_binsearch
special_found:
	rts
.endp

;==========================================================================

;ATASCII	Internal
;00-1F		40-5F
;20-3F		00-1F
;40-5F		20-3F
;60-7F		60-7F
;80-9F		C0-DF
;A0-BF		80-9F
;C0-DF		A0-BF
;E0-FF		E0-FF

ATASCIIToInternalTab:
	dta		$40
	dta		$20
	dta		$60
	dta		$00

;==========================================================================
.proc	EditorRecomputeCursorAddr
	ldx		colcrs
	ldy		rowcrs
	jsr		ScreenComputeAddr
	stx		oldadr
	sta		oldadr+1
	rts
.endp

;==========================================================================
; Delete a logical line on screen, and scroll the remainder of the screen
; upward.
;
; Inputs:
;	A = physical line row to delete
;
; Outputs:
;	DELTAR = number of lines scrolled
;
EditorDeleteLine = EditorDeleteLine0.use_line
.proc	EditorDeleteLine0
	lda		#0
use_line:
	sta		hold1
	ldx		#0
	stx		deltar
		
scroll_loop:
	;compute base address and set that as destination
	ldy		hold1

.if _KERNEL_XLXE
	sec
	bne		nofine
	ldx		fine
	beq		nofine
	ldx		#0
xloop:
	lda		rtclok+2
	cmp:req	rtclok+2
	inx
	stx		vscrol
	cpx		#7
	bne		xloop
	lda		#32/2
	cmp:rcc	vcount
	cmp:rcs	vcount
nofine:
	php					;C=0 for fine scroll, C=1 for coarse scroll
.endif

	;set TOADR to the line to be deleted
	jsr		ScreenComputeToAddrX0
	
	ldx		hold1
	bpl		line_loop_start
	
line_loop:
	;bump line: FRMADR <- TOADR, TOADR += 40
	jsr		EditorNextLineAddr
	
	;move line -- note that we are copying backwards (TOADR to FRMADR)!
	ldy		#39
	mva:rpl	(toadr),y (frmadr),y-
	
line_loop_start:
	inx
	cpx		botscr
	bne		line_loop
	
	;clear the last line
	ldy		#39
	lda		#0
	sta:rpl	(toadr),y-
	
.if _KERNEL_XLXE
	plp
	bcs		nofinescroll2
	sta		vscrol
nofinescroll2:
.endif
	
	jsr		adjust_lines
	
	;delete bits out of the logical mask until we get to the next
	;logical line, computing the number of lines to scroll
	lda		hold1
	jsr		EditorGetLogicalLineInfo
	ldx		update_table,y		;get number of masks to shift
	sec							;setup to add a new logical line at the end
	beq		do_mask				;jump to masking if we're on byte 2
	rol		logmap+2			;shift byte 2
	dex							;
	beq		do_mask				;jump to masking if we're on byte 1
	rol		logmap+1			;shift byte 1
do_mask:
	sta		adress				;stash mask
	eor		#$ff				;invert mask
	and		logmap,y			;kill target bit
	sta		adress+1			;stash modified mask
	dec		adress				;form mask for LSBs below bit
	and		adress				;isolate those bits
	adc		adress+1			;shift those up and the bit from the next byte in
	sta		logmap,y			;write to logmap

	inc		deltar				;increment line count
	inc		adress				;revert mask
	bit		adress				;test if we still have a logical line

	;loop back if we need to delete more physical lines
	beq		scroll_loop
	
	;all done
	rts
	
update_table:
	dta		2,1,0

adjust_lines:
	lda		lmargn				;prep for line adjustments
	
	;adjust the read row if it is affected by the deletion
	ldx		#<bufstr
	jsr		adjust_line

	;adjust the cursor row if it is affected by the deletion
	ldx		#<rowcrs
adjust_line:
	ldy		0,x						;get row
	cpy		hold1					;compare against deletion pos
	bcc		adjust_line_above		;nothing to do if it's above del range
	bne		adjust_line_below		;skip col adjust if it's below del range
	sta		1,x						;within range - move cursor to left mgn
	rts
adjust_line_below:
	dec		0,x
adjust_line_above:
	rts	
.endp

;==========================================================================
EditorGetStatus = CIOExitSuccess
EditorSpecial = CIOExitNotSupported
EditorInit = CIOExitNotSupported

;==========================================================================
; Entry:
;	A = line
;
; Exit:
;	P = bit test status
;	A = bit mask
;	X = bit index
;	Y = mask index
;
EditorGetCurLogicalLineInfo = _EditorGetLogicalLineInfo
EditorGetLogicalLineInfo = _EditorGetLogicalLineInfo.use_a
.proc	_EditorGetLogicalLineInfo
	lda		rowcrs
use_a:
	pha
	lsr
	lsr
	lsr
	tay
	pla
	and		#7
	tax
	lda		ReversedBitMasks,x
	rts
.endp

;==========================================================================
; Test whether a physical line is the start of a logical line.
;
; Entry:
;	A = line
;
; Exit:
;	Z = 1 if so, 0 if not
;
; Modified:
;	all registers
;
.proc	EditorTestLogicalLineBit
	jsr		EditorGetLogicalLineInfo
	and		logmap,y
	rts
.endp

;==========================================================================
; Get the starting physical line for a logical line.
;
; Entry:
;	A = physical line
;
; Exit:
;	A = logical line start
;
; Modified:
;	ADRESS
;
EditorPhysToLogicalRow = EditorGetCurLogicalRow.use_line
.proc	EditorGetCurLogicalRow
	lda		rowcrs
use_line:
	sta		adress
test_loop:
	lda		adress
	jsr		EditorTestLogicalLineBit
	bne		found
	dec		adress
	bne		test_loop
found:
	lda		adress
	rts
.endp

;==========================================================================
.proc	EditorBell
	ldy		#0
	jmp		Bell
.endp

;==========================================================================
.proc	EditorSwapToScreen_Y1
	ldy		#1
.def :EditorSwapToScreen = *
	sty		adress
	clc
	jsr		ScreenSwap
	ldy		adress
	rts
.endp

;==========================================================================
; Compute the current logical column.
;
; Exit:
;	A = column
;	X = inverted row index (0-2 => $ff-$fd)
;
.proc	EditorGetLogicalColumn
	;get starting row of logical line
	lda		rowcrs
	jsr		EditorPhysToLogicalRow
	
	;subtract off current row
	clc
	sbc		rowcrs
	tax
	
	;multiply negated difference by 40
	lda		EditorLineLengthTab-$fc,x
	
	;add in physical column
	adc		colcrs
	rts
.endp

;==========================================================================
.proc	EditorSetupTabIndex
	tya
	and		#7
	tax
	lda		ReversedBitMasks,x
	pha
	tya
	lsr
	lsr
	lsr
	tax
	pla
	rts
.endp

;==========================================================================
; Copy TOADR to FRMADR and add 40 to TOADR.
;
.proc EditorNextLineAddr
	lda		toadr
	sta		frmadr
	add		#40
	sta		toadr
	lda		toadr+1
	sta		frmadr+1
	adc		#0
	sta		toadr+1
	rts
.endp

;==========================================================================
.proc	EditorLineLengthTab
	dta		120, 80, 40, 0
.endp

