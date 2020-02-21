;	Altirra - Atari 800/800XL/5200 emulator
;	Modular Kernel ROM - Cassette tape support
;	Copyright (C) 2008-2016 Avery Lee
;
;	Copying and distribution of this file, with or without modification,
;	are permitted in any medium without royalty provided the copyright
;	notice and this notice are preserved.  This file is offered as-is,
;	without any warranty.

;==========================================================================
.proc CassetteInit
		;Set CBAUDL/CBAUDH to $05CC, the nominal POKEY divisor for 600
		;baud. We don't care about this, but it's documented in the OS
		;Manual.
		mwa		#$05CC cbaudl
		rts
.endp

;==========================================================================
; Cassette open routine.
;
; XL/XE OS behavior notes:
;	- Attempting to open for none (AUX1=$00) or read/write (AUX1=$0C)
;	  results in a Not Supported error (146).
;	- Open for read gives one beep, while open for write gives two beeps.
;	- Break will break out of the wait.
;	- Ctrl+3 during the keypress causes an EOF error (!). This is a side
;	  effect of reusing the keyboard handler.
;
; CSOPIV behavior:
;	- Does NOT set FTYPE (continuous mode flag).
;	- Sets read mode (WMODE).
;	- Does NOT require ICAX1Z or ICAX2Z to be set.
;
CassetteOpenRead = CassetteOpen.do_open_read
.proc CassetteOpen
	;stash continuous mode flag
	lda		icax2z			;!! FIRST TWO BYTES CHECKED BY ARCHON
	sta		ftype
	
	;check mode byte for read/write modes
	ldx		#$80
	lda		icax1z
	and		#$0c
	cmp		#$04			;read?
	beq		found_read_mode
	cmp		#$08			;write?
	beq		found_write_mode
	
	;invalid mode -- return not supported
	rts
	
do_open_read:
found_read_mode:
	ldx		#$00
found_write_mode:
	stx		wmode

	;set cassette buffer size to 128 bytes and mark it empty
	lda		#$80
	sta		bptr
	sta		blim
	
	;clear EOF flag
	asl
	sta		feof
	
	;request one beep for read, or two for write
	bit		wmode
	bpl		one_ping_only
	
	asl		bptr			;!! - set bptr=0 when starting write

	jsr		CassetteBell

one_ping_only:
	jsr		CassetteBell
	
	;wait for a key press
	jsr		KeyboardGetByte
	bmi		aborted

	;need to set up POKEY for writes now to write leader
	bit		wmode
	bpl		no_write_init
	lda		#$ff
	sta		casflg
	jsr		SIOSendEnable.no_irq_setup
no_write_init:
	
	;turn on motor (continuous mode or not)
	lda		#$34
	sta		pactl
	
	;wait for leader (9.6 seconds read, 19.2s write)
	ldx		#0
	bit		wmode
	smi:ldx	#1
	jsr		CassetteWait
	
	;all done
	ldy		#1
aborted:
	rts
.endp

;==========================================================================
.proc CassetteClose
	;check if we are in write mode
	lda		wmode			;!! FIRST TWO BYTES CHECKED BY ARCHON
	bpl		notwrite
	
	;check if we have data to write
	lda		bptr
	beq		nopartial
	
	;flush partial record ($FA)
	jsr		CassetteFlush
		
nopartial:
	;write EOF record ($FE)
	jsr		CassetteFlush
	
notwrite:
	;stop the motor
	lda		#$3c
	sta		pactl
	
	;kill audio
	ldy		#0
	sty		audc1
	sty		audc2
	sty		audc4
	
	;all done
	iny
	rts
.endp

;==========================================================================
.proc CassetteGetByte
	;check if we have an EOF condition
	lda		feof			;!! FIRST TWO BYTES CHECKED BY ARCHON
	bne		xit_eof
	
fetchbyte:
	;check if we can still fetch a byte
	ldx		bptr
	cpx		blim
	beq		nobytes

	lda		casbuf+3,x
	inc		bptr
	ldy		#1
	rts
	
nobytes:
	;fetch more bytes
	jsr		CassetteReadBlock
	bmi		error
	
	;check control byte
	lda		casbuf+2
	cmp		#$fe
	bne		noteofbyte
	
	;found $FE (EOF) - set flag and return EOF
	sta		feof
xit_eof:
	ldy		#CIOStatEndOfFile
error:
	rts
	
noteofbyte:
	;reset buffer ptr
	mvx		#0 bptr
	
	;assume full block first (128 bytes)
	ldx		#$80
	
	;check if it actually is one
	cmp		#$fc
	bne		not_full_block
	
init_new_block:
	;reset block length and loop back
	stx		blim
	jmp		fetchbyte
	
not_full_block:
	;check if we have a partial block
	cmp		#$fa
	bne		not_partial_block
	
	;set length of partial block and the jump to init+loop
	ldx		casbuf+130
	bcs		init_new_block
	
not_partial_block:	
	;uh oh... bad control byte.
	ldy		#CIOStatFatalDiskIO
	rts
.endp

;==========================================================================
.proc CassettePutByte
	;put a byte into the buffer
	ldx		bptr			;!! FIRST TWO BYTES CHECKED BY ARCHON
	sta		casbuf+3,x
	
	;bump and check if it's time to write
	inx
	stx		bptr
	cpx		blim
	bcs		CassetteFlush
	
	;all done
	ldy		#1
	rts
.endp

;==========================================================================
.proc CassetteFlush
	lda		#0

	;set control byte based on buffer level
	ldx		#$fe			;empty -> EOF
	ldy		bptr			;get buffer level
	bne		not_empty		;skip not empty
	
	;clear buffer for EOF
	sta:rpl	casbuf+3,y+
	bmi		is_empty		;!! - unconditional
	
not_empty:
	ldx		#$fc			;load complete code
	cpy		blim			;check if buffer is full
	bcs		is_complete		;skip if so
	ldx		#$fa			;load partial code
	sty		casbuf+130		;store level in last byte
is_complete:
is_empty:

	;store code
	stx		casbuf+2
	
	;reset buffer level
	sta		bptr
	
	;setup sync bytes
	lda		#$55
	sta		casbuf
	sta		casbuf+1
	
	;issue write request and exit
	ldx		#$80
	ldy		#'P'
	jmp		CassetteDoIO
.endp

;==========================================================================
CassetteGetStatus = CIOExitSuccess
CassetteSpecial = CIOExitNotSupported

;==========================================================================
; CassetteDoIO
;
;	X = DSTATS value
;	Y = SIO command byte
;
; Note that SOUNDR must take effect each time a cassette I/O operation
; occurs.
;
CassetteDoIO = _CassetteDoIO.do_io
CassetteReadBlock = _CassetteDoIO.read_block

.proc _CassetteDoIO
read_block:
	ldx		#$40
	ldy		#'R'
do_io:
	;start the motor if not already running
	lda		#$34
	sta		pactl

	;set up SIO read/write
	stx		dstats
	sty		dcomnd
	mwa		#casbuf dbuflo
	mwa		#131 dbytlo
	mva		#$60 ddevic
	mva		#0 dunit
	mva		ftype daux2

	;do it
	jsr		siov
	
	;check if we are in continuous mode (again)
	lda		ftype
	bmi		rolling_stop
	
	;not in continuous mode -- wait for post-write tone if writing
	bit		wmode
	bpl		no_pwt
	ldx		#6
	jsr		CassetteWait
no_pwt:
	
	;stop the motor
	lda		#$3c
	sta		pactl
	
rolling_stop:
	ldy		status
	rts
.endp

;==========================================================================
;Entry:
;	X = delay type
;
CassetteWait = CassetteWaitLongShortCheck.normal_entry
.proc CassetteWaitLongShortCheck
	bit		daux2
	spl:inx
normal_entry:
	jsr		SIOSetTimeoutVector
	ldy		wait_table_lo,x
	lda		wait_table_hi,x
	tax
	lda		#1
	sta		timflg
	jsr		VBISetVector
	lda:rne	timflg
	rts
	
wait_table_lo:
	dta		<$0480			;$00 - write file leader (19.2 seconds NTSC)
	dta		<$0240			;$01 - read leader delay (9.6 seconds NTSC)
	dta		<$00B4			;$02 - long pre-record write tone (3.0s NTSC)
	dta		<$000F			;$03 - short pre-record write tone (0.25s NTSC)
	dta		<$0078			;$04 - long read IRG (2.0s NTSC)
	dta		<$000A			;$05 - short read IRG (0.16s NTSC)
	dta		<$003C			;$06 - post record gap (1s NTSC)

wait_table_hi:
	dta		>$0480			;$00 - write file leader (19.2 seconds NTSC)
	dta		>$0240			;$01 - read leader delay (9.6 seconds NTSC)
	dta		>$00B4			;$02 - long pre-record write tone (3.0s NTSC)
	dta		>$000F			;$03 - short pre-record write tone (0.25s NTSC)
	dta		>$0078			;$04 - long read IRG (2.0s NTSC)
	dta		>$000A			;$05 - short read IRG (0.16s NTSC)
	dta		>$003C			;$06 - post record gap (1s NTSC)
.endp

;==========================================================================
; Sound a bell using the console speaker (cassette version)
;
; Modified:
;	A, X, Y
;
.proc CassetteBell
	ldy		#0
	tya
soundloop:
	ldx		#10
	pha
delay:
	lda		vcount
	cmp:req	vcount
	dex
	bne		delay
	pla
	eor		#$08
	sta		consol
	bne		soundloop
	dey
	bne		soundloop
	rts
.endp
