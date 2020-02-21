;	Altirra - Atari 800/800XL/5200 emulator
;	Modular Kernel ROM - 65C816 Keyboard Handler
;	Copyright (C) 2008-2018 Avery Lee
;
;	Copying and distribution of this file, with or without modification,
;	are permitted in any medium without royalty provided the copyright
;	notice and this notice are preserved.  This file is offered as-is,
;	without any warranty.

;==========================================================================
; Oddly, the keyboard IRQs are not enabled from the keyboard init
; routine. It's done by the Display Handler instead on open.
;
.proc	KeyboardInit
	ldx		#$ff
	stx		ch
	stx		ch1
	stz		keydel
	stz		invflg
		
	;turn on shift lock
	mva		#$40	shflok
	
	;set keyboard definition table pointer
	mwa		#KeyCodeToATASCIITable keydef
	rts
.endp

KeyboardOpen = CIOExitSuccess
KeyboardClose = CIOExitSuccess

;==========================================================================
; K: GET BYTE handler.
;
; Behavior:
;	- Exits with a Break error when break key is pressed.
;	- Ctrl-1 does not suspend input here (it is handled by S:/E:).
;	- Ctrl-3 returns an EOF error.
;	- Caps Lock sets caps mode on OS-A/B depending on Ctrl and Shift key
;	  state. On the XL/XE OS, pressing Caps Lock alone will enable shift
;	  lock if no lock is enabled and disable it otherwise.
;	- Shift/Control lock is applied by K:, but only on alpha keys.
;	- Inverse mode is also applied by K:. Control characters are excluded:
;	  1B-1F/7C-7F/9B-9F/FD-FF.
;	- Any Ctrl+Shift key code (>=$C0) produces a key click but is otherwise
;	  ignored.
;
.nowarn .proc	_KeyboardGetByte
toggle_shift:
	;Caps Lock without Shift or Control is a toggle on the XL/XE line:
	; None -> Shifted
	; Shifted, Control -> None
	ldx		shflok
	bne		caps_off
	ldy		#$40
shift_ctrl_on:
caps_off:
	tya
	and		#$c0
write_shflok:
	sta		shflok

.def :KeyboardGetByte
waitForChar:
	ldx		#$ff
waitForChar2:
	lda		brkkey
	beq		isBreak
	lda		ch
	cmp		#$ff
	beq		waitForChar2
	
	;invalidate char
	stx		ch
	
	;do keyboard click (we do this even for ignored ctrl+shift+keys)
	ldy		#12
	jsr		Bell

	;ignore char if both ctrl and shift are pressed
	cmp		#$c0
	bcs		waitForChar
	
	;trap Ctrl-3 and return EOF
	cmp		#$9a
	beq		isCtrl3
			
	;translate char
	tay
	lda		(keydef),y
	
	;handle special keys (see keytable.s)
	bpl		valid_key
	cmp		#$81
	bcc		waitForChar		;$80 - invalid
	beq		isInverse		;$81 - inverse video
	cmp		#$83
	bcc		toggle_shift	;$82 - caps lock
	cmp		#$85
	bcc		shift_ctrl_on	;$83 - shift caps lock / $84 - ctrl caps lock
	
valid_key:
	;check for alpha key
	cmp		#'a'
	bcc		notAlpha
	cmp		#'z'+1
	bcs		notAlpha
	
	;check for shift/control lock
	bit		shflok
	bvs		doShiftLock
	bpl		notAlpha
	
	;do control lock logic
	and		#$1f

doShiftLock:
	and		#$df

notAlpha:
	;check if we should apply inverse flag -- special characters are excluded
	ldx		#EditorPutByte.special_code_tab_end_2-EditorPutByte.special_code_tab-1
	jsr		EditorIsSpecial
	beq		skip_inverse

	;apply inverse flag
	eor		invflg
skip_inverse:

	;return char
	sta		atachr			;required or CON.SYS (SDX 4.46) breaks
	ldy		#1
	rts
	
isInverse:
	lda		invflg
	eor		#$80
	sta		invflg
	bra		waitForChar

isBreak:
	stx		brkkey
	ldy		#CIOStatBreak
	rts
	
isCtrl3:
	ldy		#CIOStatEndOfFile
	rts
.endp

;==============================================================================
KeyboardPutByte = CIOExitNotSupported
KeyboardGetStatus = CIOExitSuccess
KeyboardSpecial = CIOExitNotSupported

;==============================================================================
; Keyboard IRQ
;
; HELP button ($11, $51, and $91):
; - Affects SRTIMR, ATRACT, KEYDEL, and HELPFG
; - Does NOT affect CH, CH1
;
.proc	KeyboardIRQ
	;reset software repeat timer
	mva		#$30	srtimr
	
	;read new key
	lda		kbcode

	;check for HELP
	and		#$3f
	cmp		#$11
	bne		not_help
	sta		helpfg
	beq		xit2

not_help:
	lda		kbcode	
	
	;check if it is the same as the prev key
	cmp		ch1
	bne		debounced

	;reject key if debounce timer is still running	
	lda		keydel
	bne		xit
	lda		ch1	
debounced:

	;check for Ctrl+1 to toggle display activity
	cmp		#$9f
	beq		is_suspend

	;store key
	sta		ch
	sta		ch1

	;reset attract
	stz		atract
	
xit2:
	;reset key delay
	mva		#3 keydel

xit:
	;all done
	pla
	rti	

is_suspend:
	;toggle stop/start flag
	lda		ssflag
	eor		#$ff
	sta		ssflag
	bra		xit2
.endp

;==============================================================================
.proc	KeyboardBreakIRQ
	stz		brkkey

	;need to clear the suspend flag as BREAK automatically nukes a pending Ctrl+1
	stz		ssflag
	
	;interestingly, the default break handler forces the cursor back on.
	stz		crsinh
	
	pla
	rti
.endp
