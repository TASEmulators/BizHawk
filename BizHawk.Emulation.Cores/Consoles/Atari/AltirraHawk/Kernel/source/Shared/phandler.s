;	Altirra - Atari 800/800XL/5200 emulator
;	Modular Kernel ROM - Peripheral Handler routines
;	Copyright (C) 2008-2016 Avery Lee
;
;	Copying and distribution of this file, with or without modification,
;	are permitted in any medium without royalty provided the copyright
;	notice and this notice are preserved.  This file is offered as-is,
;	without any warranty.

;==========================================================================
; Add handler to HATABS.
;
; Input:
;	X		Name of device
;	A:Y		CIO handler table address
;
; Returns:
;	N=1		HATABS is full.
;	C=0		Handler added successfully.
;	C=1		Handler already exists; X points to address entry
;			A:Y preserved (required by SDX 4.43rc)
;
.proc	PHAddHandler
		pha
		tya
		pha
		txa
		ldx		#33
search_loop:
		cmp		hatabs,x
		beq		found_existing
		dex
		dex
		dex
		bpl		search_loop	
		
insert_loop:
		inx
		inx
		inx
		ldy		hatabs,x
		beq		found_empty
		cpx		#36
		bne		insert_loop
		
		;oops... table is full!
		pla
		pla
		lda		#$ff
		sec
		rts

found_existing:
		pla
		tay
		pla
		inx					;X=address offset, N=0 (not full)
		sec					;C=1 (already exists)
		rts

found_empty:
		sta		hatabs,x
		pla
		sta		hatabs+1,x
		pla
		sta		hatabs+2,x
		asl					;N=0 (not full)
		clc					;C=0 (added successfully)
		rts
.endp

;==========================================================================
; Remove peripheral handler.
;
; Entry:
;	A:Y		Address of handler to remove
;
; Exit:
;	C=0		Removal was successful
;	C=1		Removal failed (not found, broken chain or handler size was
;			non-zero)
;
; This routine unlinks a peripheral handler from the CHLINK chain. It fails
; if the handler size in the handler table is non-zero, which means that it
; was loaded on power-up.
;
; Note that this routine does NOT remove any handler entries in HATABS.
;
.proc PHRemoveHandler
		;stash handler address
		sty		reladr
		sta		reladr+1
		
		;load "chain head" by pretending the chain address is in a handler
		;entry
		ldx		#<(chlink-$12)
		lda		#>(chlink-$12)
		
search_loop:
		;check if we're at the end
		tay
		bne		not_end
		cpx		#0
		beq		epic_fail
		
not_end:
		;save off the prev pointer
		stx		ltemp
		sta		ltemp+1

		;load the next pointer
		ldy		#$12
		lda		(ltemp),y
		tax
		iny
		lda		(ltemp),y
		cmp		reladr+1
		bne		search_loop
		cpx		reladr
		bne		search_loop
		
		;we've found it -- move it into a pointer var
		stx		zchain
		sta		zchain+1
		
		;check if the handler size is zero
		ldy		#$10
		lda		(zchain),y
		iny
		ora		(zchain),y
		beq		size_ok
		
		;whoops... we can't remove this handler.
epic_fail:
		sec
		rts
		
size_ok:
		;unlink the handler
		ldy		#$12
		lda		(zchain),y			;load low byte
		tax							;stash it
		iny
		lda		(zchain),y			;load high byte
		sta		(ltemp),y			;prev->link_hi = next->link_hi
		dey
		txa
		sta		(ltemp),y			;prev->link_lo = next->link_lo
		
		;all done
		clc
		rts
.endp


;==========================================================================
; Init peripheral handler.
;
; Entry:
;	A:Y		Address of handler table
;	C		Set if MEMLO should be adjusted, cleared otherwise
;
; Exit:
;	C		Cleared on success, set on failure
;
; Modified:
;	DVSTAT+2, DVSTAT+3
;
; See also: Sweet 16 OS Supplement part 3, Handler Loader
;
.proc PHInitHandler
		;save off the table pointer; we use DVSTAT+2/3 per the Sweet16 spec
		sty		dvstat+2
		sta		dvstat+3
		
		;save off MEMLO adjust flag
		php

		;move chain to zero page pointer
		ldx		#<(chlink-$12)
		lda		#>(chlink-$11)
		
		;find zero link
end_search_loop:
		;move to next link
		stx		zchain
		sta		zchain+1
		
		;load low byte
		ldy		#$12
		lda		(zchain),y
		tax
		iny
		
		;load high byte and check if it is zero
		lda		(zchain),y
		bne		end_search_loop
		
		;check if low byte is zero too
		cpx		#0
		bne		end_search_loop
		
		;alright, we've found the end of the chain -- link in the table
		lda		dvstat+3
		sta		(zchain),y
		tax
		dey
		lda		dvstat+2
		sta		(zchain),y
		
		;switch to the new link
		stx		zchain+1
		sta		zchain
		
		;zero the forward pointer
		ldy		#$13
		lda		#0
		sta		(zchain),y
		dey
		sta		(zchain),y
		
		;jump to handler init
		jsr		PHCallHandlerInit
		bcc		init_ok
		
		;remove handler and return with error (C=1)
		lda		zchain
		ldy		zchain+1
		jsr		PHRemoveHandler
		plp
		sec
		rts
		
init_ok:
		;retrieve the MEMLO adjust flag
		plp
		
		;zero out the handler size if we aren't adjusting MEMLO
		bcs		adjust_memlo
		
		ldy		#$10
		lda		#0
		tax
		sta		(zchain),y
		iny
		sta		(zchain),y
		
adjust_memlo:
		jsr		PHAdjustMemlo
		
		;zero checksum
		ldy		#$0f
		lda		#0
		sta		(zchain),y
		
		;compute checksum
		ldy		#$11
		clc
checksum_loop:
		adc		(zchain),y
		dey
		bpl		checksum_loop
		adc		#0
		
		;store checksum
		ldy		#$0f
		sta		(zchain),y
		
		;all done - return success
		clc
		rts
.endp

;==========================================================================
.proc PHAdjustMemlo
	;load handler size
	ldy		#$11
	lda		(zchain),y
	tax
	dey
	lda		(zchain),y
	
	;adjust MEMLO by handler size
	clc
	adc		memlo
	sta		memlo
	txa
	adc		memlo+1
	sta		memlo+1
	rts
.endp

;==========================================================================
; Initializes a single handler pointed to by ZCHAIN.
;
.proc PHCallHandlerInit
	lda		zchain
	clc
	adc		#$0b
	tax
	lda		zchain+1
	adc		#0
	pha
	txa
	pha
	rts
.endp

;==========================================================================
; Reinitialize all handlers in the handler chain.
;
.proc PHReinitHandlers
	;move chain to zero page pointer
	ldx		chlink
	lda		chlink+1
	
	;find zero link
init_loop:
	;move to next link
	stx		zchain
	sta		zchain+1
	ora		zchain
	beq		init_done
	
	;recompute checksum (except for checksum byte)
	ldy		#$11
	clc
	lda		(zchain),y-
	adc		(zchain),y-
	dey
	adc:rpl	(zchain),y-
	adc		#0
	
	;check if the checksum matches
	ldy		#$0f
	cmp		(zchain),y
	bne		init_done

	;reinitialize the handler
	jsr		PHCallHandlerInit
	bcs		init_done
	
	;adjust MEMLO
	jsr		PHAdjustMemlo
	
	;check for a next link
	ldy		#$13
	lda		(zchain),y
	tax
	iny
	lda		(zchain),y
	bne		init_loop
	
	;load high byte and check if it is zero
	cpx		#0
	bne		init_loop
	
init_done:
	rts
.endp

;==========================================================================
; Startup poll for peripheral handlers.
;
; The protocol for initing handlers from the SIO bus is as follows:
;
;	1. Issue type 3 poll reset (4F/40/4F/4F).
;	2. Issue type 3 poll (4F/40/00/00), reading 4 bytes.
;	2a. If this poll fails with a timeout, we're done.
;	2b. If the poll fails for any other reason, issue a type 3 null poll
;	    (4F/40/4E/4E) and go back to step 2.
;	3. Compare the size in DVSTAT+0/1 against the amount of memory between
;	   MEMTOP and MEMLO. If there isn't enough, issue a type 3 null poll
;	   and go to step 2.
;	4. Call the relocating loader to load the handler. The handler is
;	   loaded 128 bytes at a time using command $26 (&), AUX1=block, where
;	   block 0 is the first block.
;	5. Go back to step 2.
;
.proc PHStartupPoll
	;issue type 3 poll reset (address $4F, command $40, aux $4F4F, no data)
	ldx		#$4F
	jsr		do_simple_poll
	
	;issue type 3 poll (address $4F, command $40, aux $0000, read 4 bytes)
poll_loop:
	lda		#$00
	sta		daux1
	sta		daux2
	sta		dbythi
	mwa		#dvstat dbuflo
	mva		#4 dbytlo
	mva		#$40 dstats
	jsr		siov
	bpl		poll_ok
	cpy		#SIOErrorTimeout
	bne		poll_fail
exit:
	rts
poll_fail:
	;!! - Black Box 2.16 firmware intercepts type 3 polls and forces
	;a user abort error (Y=$80), which we must exit on to avoid an infinite
	;loop.
	cpy		#$80
	beq		exit

	;we had a failure -- issue a null poll before trying another device
	ldx		#$4e
	jsr		do_simple_poll
	jmp		poll_loop	
	
poll_ok:
	;DVSTAT now contains the following frame:
	;
	;	DVSTAT+0	handler size, low byte (should be even)
	;	DVSTAT+1	handler size, high byte
	;	DVSTAT+2	device address
	;	DVSTAT+3	peripheral revision number

	;Check whether we have enough room between MEMLO and MEMTOP to load
	;this handler -- remember that MEMTOP is inclusive. Also, we need to
	;alloc an additional byte if MEMLO is odd.
	lda		memlo
	tax
	ror
	txa
	adc		dvstat
	tax
	lda		memlo+1
	adc		dvstat+1
	cmp		memtop+1
	bcc		mem_ok
	bne		poll_fail
	cpx		memtop
	bcs		mem_ok
	bne		poll_fail
mem_ok:
	
	;set up for handler load
	lda		memlo
	tax
	ror
	txa
	adc		#0
	sta		loadad
	lda		memlo+1
	adc		#0
	sta		loadad+1
	
	mva		dvstat+2 ddevic
	
	sec
	jsr		PHLoadHandler
	bcs		poll_fail
	
	;look for another device
	jmp		poll_loop
	
;--------------------------------------------------------------------------
do_simple_poll:
	lda		#$4f
	sta		ddevic
	stx		daux1
	stx		daux2
	lda		#$40
	sta		dcomnd
	lda		#$01
	sta		dunit
	lda		#$00
	sta		dstats
	jmp		siov

.endp

;==========================================================================
; Load peripheral handler over the SIO bus.
;
; Entry:
;	C		Set to adjust MEMLO, clear to not
;	DDEVIC	SIO address to load for loading
;	LOADAD	Handler load address
;
; Modified:
;	All relocatable handler variables
;	DCB
;	DVSTAT
;
.proc PHLoadHandler
	;save MEMLO adjustment flag
	php
	
	;set zero-page relocation destination to $80 (should never be used)
	mva		#$80 zloada
	
	;set up relocator get byte handler
	mwa		#PHGetByte gbytea

	;set up for get byte, using command $26 and the cassette buffer
	mva		#$26 dcomnd
	mvx		#0 daux1
	stx		dbythi
	stx		dbuflo
	dex
	stx		bptr
	mva		#$04 dbufhi
	mva		#$80 dbytlo
	
	;load the handler
	jsr		PHRelocateHandler
	bcs		reloc_fail
	
	;try to init handler, adjusting MEMLO as necessary
	ldy		loadad
	lda		loadad+1
	plp
	jsr		PHInitHandler
failed:
	rts
reloc_fail:
	pla
	rts
.endp

;==========================================================================
; Get byte handler used with relocating loader when loading handlers over
; the SIO bus.
;
.proc PHGetByte
	;try to grab a byte
	ldx		bptr
	bpl		fetch

	;issue SIO call to fetch next block
	mva		#$40 dstats
	jsr		siov
	bmi		get_byte_fail
	ldx		#0
	
fetch:
	;read next byte from cassette buffer
	lda		$0400,x
	inx
	stx		bptr
	
	;all clear
	clc
	rts
	
get_byte_fail:
	sec
	rts
	
.endp

;==========================================================================
; Relocating loader.
;
; Modified:
;	LTEMP (word)
;	LCOUNT
;	RELADR
;	RECLEN
;	HIBYTE
;	RUNADR (word)
;	HIUSED (word)
;	ZHIUSE
;
; Record types:
;	Non-zp text				$00 len { offset-lo offset-hi data* }
;	Zero-page text			$01 len { offset-lo offset-hi data* }
;	Non-zp <ref to non-zp	$02 len { offset* }
;	Zp <ref to non-zp		$03 len { offset* }
;	Non-zp ref to zp		$04 len { offset* }
;	Zp ref to zp			$05 len { offset* }
;	Non-zp ref to non-zp	$06 len { offset* }
;	Zp ref to non-zp		$07 len { offset* }
;	Non-zp >ref to non-zp	$08 len { {offset lo-byte}* }
;	Zp >ref to non-zp		$09 len { {offset lo-byte}* }
;	Absolute text			$0A len { addr-lo addr-hi data* }
;	End record				$0B self-start addr-lo addr-hi
;
; The references need some explanation:
;	- The program is split into two sections, a zero-page section and a
;	  non-zero page section. The two sections are each contiguous.
;	- Both the zero-page and non-zero-page sections can reference each
;	  other.
;	- Low-byte references include only the low byte of an address.
;	- High-byte references include only the high byte of an address. A
;	  low byte offset is included in the relocation to enable this.
;	- Relocation records are always applied to the last text record
;	  that was loaded.
;
.proc PHRelocateHandler
	;make the zero-page load address 16-bit
	lda		#0
	sta		zloada+1

reloc_loop:
	;get a control byte and stash it
	jsr		PHRelocateGetByte
	sta		lcount
	
	;get the length or self-start byte
	jsr		PHRelocateGetByte
	sta		reclen
	
	;dispatch to handler
	jsr		dispatch
	
	;process more records
	jmp		reloc_loop
	
dispatch:
	ldx		lcount
	ldy		cmd_table_ind_tab,x
	lda		cmd_table_hi,y
	pha
	lda		cmd_table_lo,y
	pha
	rts

;---------------------------------------------------------
cmd_table_ind_tab:
	dta		0,0,1,1,1,1,1,1,2,2,0,3
	
cmd_table_lo:
	dta		<(PHRelocateLoadText-1)
	dta		<(PHRelocateApplyFixups-1)
	dta		<(PHRelocateApplyHiFixups-1)
	dta		<(PHRelocateEnd-1)

cmd_table_hi:
	dta		>(PHRelocateLoadText-1)
	dta		>(PHRelocateApplyFixups-1)
	dta		>(PHRelocateApplyHiFixups-1)
	dta		>(PHRelocateEnd-1)
.endp

;==========================================================================
.proc PHRelocateLoadText
	;retrieve the address into LTEMP
	jsr		PHRelocateGetByte
	sta		ltemp
	jsr		PHRelocateGetByte
	sta		ltemp+1
	
	;check if we are doing an absolute load ($0A)
	lda		lcount
	cmp		#$0a
	beq		is_absolute
	
	;check whether we should use zero-page ($01) or non-zero-page ($00),
	;and adjust LTEMP
	asl
	tax
	adw		loadad,x ltemp ltemp
	
is_absolute:
	;init counter
	lda		#0
	sta		lcount

	;load reclen-2 bytes
	dec		reclen
	bne		load_loop_start
	
load_loop:
	jsr		PHRelocateGetByte
	ldy		lcount
	inc		lcount
	sta		(ltemp),y
load_loop_start:
	dec		reclen
	bne		load_loop
	rts
.endp

;==========================================================================
.proc PHRelocateApplyFixups
	;get command byte and copy the appropriate reloc target into zchain
	lda		lcount
	lsr
	ldx		loadad
	cmp		#2
	sne
	ldx		zloada
	stx		zchain
	
	;set flag if we are doing a word reloc (types $06, $07)
	cmp		#3
	ror		zchain+1

reloc_loop:
	;fetch offset byte
	jsr		PHRelocateGetByte
	tay

	;fixup low byte
	lda		(ltemp),y
	clc
	adc		zchain
	sta		(ltemp),y
	
	;check if we're doing a high byte reloc
	bit		zchain+1
	bpl		low_byte_only

	;fixup high byte
	iny
	lda		(ltemp),y
	adc		loadad+1
	sta		(ltemp),y
	
low_byte_only:
	;loop back if there is more fixup data
	dec		reclen
	bne		reloc_loop
	rts
.endp

;==========================================================================
.proc PHRelocateApplyHiFixups
reloc_loop:
	;fetch offset byte
	jsr		PHRelocateGetByte
	sta		zchain
	
	;fetch low fixup byte
	jsr		PHRelocateGetByte

	;add low byte to base
	clc
	adc		loadad
	
	;fixup high byte
	ldy		zchain
	lda		loadad+1
	adc		(ltemp),y
	sta		(ltemp),y
		
	;loop back if there is more fixup data
	dec		reclen
	dec		reclen
	bne		reloc_loop
	rts
.endp

;==========================================================================
.proc PHRelocateEnd
	;read in the address to RUNADR
	jsr		PHRelocateGetByte
	sta		runadr
	jsr		PHRelocateGetByte
	sta		runadr+1
	
	;check the self-start byte
	ldx		lcount
	bne		do_self_start
	
	;no self-start... zero the addr and exit
	lda		#0
	sta		runadr
	sta		runadr+1
xit:
	clc
	pla
	pla
	ldy		#1
	rts
	
do_self_start:
	;check if we need to relocate the address... if it
	;is absolute ($01), we're done
	dex
	beq		xit
	
	;add the non-zp base address and exit
	adw		runadr loadad runadr
	jmp		xit
.endp

;==========================================================================
.proc PHRelocateGetByte
	jsr		call_get_byte
	bcc		get_byte_ok
	pla
	pla
get_byte_ok:
	rts
	
call_get_byte:
	jmp		(gbytea)
.endp
