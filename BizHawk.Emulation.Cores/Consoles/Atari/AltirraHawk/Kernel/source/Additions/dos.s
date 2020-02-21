; Altirra DOS - Kernel
; Copyright (C) 2014-2017 Avery Lee, All Rights Reserved.
;
; Copying and distribution of this file, with or without modification,
; are permitted in any medium without royalty provided the copyright
; notice and this notice are preserved.  This file is offered as-is,
; without any warranty.

		opt		h-o-
		org		$0043
zbufp	dta		a(0)		;Buffer pointer
zdrva	dta		a(0)		;Drive pointer
zsba	dta		a(0)		;Sector buffer pointer
errno	dta		0			;Error number

		icl		'hardware.inc'
		icl		'kerneldb.inc'
		icl		'cio.inc'
		icl		'sio.inc'
		
runad	equ		$02e0
initad	equ		$02e2
dskinv	equ		$e453
ciov	equ		$e456

;=========================================================================
; Boot sector / BCB
;
		opt		o+
		org		$0700
		
base:
		dta		0			;flags
		dta		3
		dta		a($0700)
		dta		a(DOSInit)
		jmp		boot					;$0706 ($0714)
bcb_maxfiles	dta		3				;$0709 ($03)
bcb_drivebits	dta		1				;$070A ($03)
bcb_allocdirc	dta		0				;$070B ($00) (unused)
bcb_secbuf		dta		a(dos_sectorbuffers)	;$070C ($1A7C)
bcb_bootflag	dta		$01				;$070E ($01)
bcb_firstsec	dta		a(4)			;$070F ($0004) first sector of DOS.SYS
bcb_linkoffset	dta		125				;$0711 ($7D)
bcb_loadaddr	dta		a($0880)		;$0712 ($07CB)
		
boot:
		;check if we're actually bootable
		lda		bcb_bootflag
		beq		error

		;replace serial vectors
		ldx		#3
		mva:rpl	newvecs,x vserin,x-
		
		;read sectors
		mwa		bcb_loadaddr dbuflo
		mva		#'R' dcomnd

		lda		bcb_firstsec
		ldx		bcb_firstsec+1
next_sector:
		sta		daux1
		stx		daux2
		jsr		dskinv
		bmi		error
		
		lda		dbuflo
		sta		zsba
		clc
		adc		bcb_linkoffset
		sta		dbuflo
		lda		dbufhi
		sta		zsba+1
		adc		#0
		sta		dbufhi
		
		ldy		bcb_linkoffset
		lda		(zsba),y
		and		#$03
		tax
		iny
		lda		(zsba),y
		bne		next_sector
		cpx		#1
		bcs		next_sector
		rts
error:
		sec
		rts
		
newvecs:
		dta		a(SIOInputReadyHandler)
		dta		a(SIOOutputReadyHandler)

;==============================================================================
; SIO serial input routine
;
; DOS 2.0S replaces (VSERIN), so it's critical that this routine follow the
; rules compatible with DOS. The rules are as follows:
;
;	BUFRLO/BUFRHI:	Points to next byte to read. Note that this is different
;					from (VSEROR)!
;	BFENLO/BFENHI:	Points one after last byte in buffer.
;	BUFRFL:			Set when all data bytes have been read.
;	NOCKSM:			Set if no checksum byte is expected. Cleared after checked.
;	RECVDN:			Set when receive is complete, including any checksum.	
;
.proc SIOInputReadyHandler
	lda		bufrfl
	bne		receiveChecksum

	;receive data byte
	tya
	pha
	lda		serin
	ldy		#$00
	sta		(bufrlo),y
	clc
	adc		chksum
	adc		#$00
	sta		chksum
	
	;bump buffer pointer
	inw		bufrlo
	
	;check for EOB
	lda		bufrlo
	cmp		bfenlo
	beq		possiblyEnd
xit:
	pla
	tay
	pla
	rti
	
receiveChecksum:
	;read and compare checksum
	lda		serin
	cmp		chksum
	beq		checksumOK
	
	mva		#SIOErrorChecksum	status
checksumOK:
	
	;set receive done flag
	mva		#$ff	recvdn

	;exit
	pla
	rti
	
possiblyEnd:	
	lda		bufrhi
	cmp		bfenhi
	bne		xit

	mva		#$ff	bufrfl
	
	;should there be a checksum?
	lda		nocksm
	beq		xit

	;set receive done flag
	sta		recvdn
	
	;clear no checksum flag
	lda		#0
	sta		nocksm
	beq		xit
.endp

;==============================================================================
; SIO serial output ready routine
;
; DOS 2.0S replaces (VSEROR), so it's critical that this routine follow the
; rules compatible with DOS. The rules are as follows:
;
;	BUFRLO/BUFRHI:	On entry, points to one LESS than the next byte to write.
;	BFENLO/BFENHI:	Points to byte immediately after buffer.
;	CHKSUM:			Holds running checksum as bytes are output.
;	CHKSNT:			$00 if checksum not yet sent, $FF if checksum sent.
;	POKMSK:			Used to enable the serial output complete IRQ after sending
;					checksum.
;
.proc SIOOutputReadyHandler
		;increment buffer pointer
		inc		bufrlo
		bne		addrcc
		inc		bufrhi
addrcc:

		;compare against buffer end
		lda		bufrlo
		cmp		bfenlo
		lda		bufrhi
		sbc		bfenhi			;set flags according to (dst - end)
		bcs		doChecksum

		;save Y
		tya
		pha

		;send out next byte
		ldy		#0
		lda		(bufrlo),y
		sta		serout
		
		;update checksum
		adc		chksum
		adc		#0
		sta		chksum

		;restore registers and exit
		pla
		tay
		pla
		rti
		
doChecksum:
		;send checksum
		lda		chksum
		sta		serout
		
		;set checksum sent flag
		mva		#$ff	chksnt
		
		;enable output complete IRQ and disable serial output IRQ
		lda		pokmsk
		ora		#$08
		and		#$ef
		sta		pokmsk
		sta		irqen
		
		pla
		rti
.endp

;==========================================================================
; BOOT SECTOR END
;

		.if * > $880
		.error	'Boot routine exceeds three SD sectors: ',*
		.endif

;==========================================================================
.struct FCB
secbuf	.byte
offset	.byte
sector	.word
secnxt	.word		;next sector
secvlen	.byte		;sector valid length (conventionally offset 127)
flags	.byte		;bit 7 = sector buffer dirty
fileid	.byte
secncnt	.word		;new sector count
pad2b	.byte
pad3	.dword
.ends

;==========================================================================
dos_fcb_table dta FCB [7] (255,0,0,0,0,0,0,0,0,0)

dos_secbuf_seclo:
		:8 dta 0

dos_secbuf_sechi:
		:8 dta 0

dos_secbuf_dunit:
		:8 dta $ff
		
dos_secbuf_fcbidx:
		:8 dta $ff

dos_secbuf_dirty:
		:8 dta 0
		
dos_filename:
		:11 dta 0
		
dos_iocb	dta		0
dos_dunit	dta		0
dos_dssec	dta		0			;dirent scan: directory sector low byte
dos_dsoff	dta		0			;dirent scan: directory sector offset
dos_dsfid	dta		0			;dirent scan: file ID
dos_dsfsec	dta		0			;dirent scan: first free sector low byte
dos_dsfoff	dta		0			;dirent scan: first free offset
dos_secbidx	dta		0
dos_seclo	dta		0
dos_sechi	dta		0
dos_segadrs	dta		a(0), a(0)
dos_burstok	dta		0
dos_linksav	dta		0,0,0		;three-byte save area for temporary link when bursting

;==========================================================================
.proc DOSInit
		ldx		#0
search_loop:
		lda		hatabs,x
		beq		found_slot
		inx
		inx
		inx
		cpx		#33
		bne		search_loop
		rts
		
found_slot:
		mva		#'D' hatabs,x
		mwa		#DOSHandlerTable hatabs+1,x
		
		;set DOSVEC
		mwa		#dosvec_start dosvec

		;allocate maxfiles+2 buffers and set MEMLO
		lda		bcb_maxfiles
		lsr
		tay
		lda		#0
		ror
		adc		#<dos_sectorbuffers
		tax
		tya
		adc		#[>dos_sectorbuffers + 1]
		cmp		memlo+1
		bcc		memlo_already_higher
		cpx		memlo
		bcc		memlo_already_higher
		stx		memlo
		sta		memlo+1
memlo_already_higher:
		
		;check if we're doing a warmstart or coldstart
		lda		warmst
		bne		skip_autorun

		;attempt to load AUTORUN.SYS with IOCB #1
		mva		#CIOCmdOpen iccmd+$10
		mwa		#autorunsys_fname icbal+$10
		mva		#$04 icax1+$10
		ldx		#$10
		jsr		ciov
		bmi		load_failed
		
		;load the executable
		jsr		DOSLoadExecutable
		bcs		load_failed
		
		jsr		DOSCloseIOCB1
		jmp		(runad)
		
load_failed:
		;close IOCB #1
		jsr		DOSCloseIOCB1
		
skip_autorun:
		clc
		rts
		
autorunsys_fname:
		dta		'D:AUTORUN.SYS',$9b
.endp

;==========================================================================
.proc DOSRun
		;DOS 2.0/2.5 calls CIOINV to reinitialize CIO -- this is a bit
		;janky, so we close all IOCBs instead
		;reset E:
		ldx		#$70
iocb_close_loop:
		mva		#CIOCmdClose iccmd,x
		jsr		ciov
		txa
		sec
		sbc		#$10
		tax
		bne		iocb_close_loop

dup_loop:
		;clear WARMST as we are about to stomp user memory
		mva		#0 warmst

		;invoke DUP
		jsr		DupMain

		jmp		dup_loop
.endp

;==========================================================================
.proc DOSLoadExecutable
		mwa		#default_run runad
segment_loop:
		mwa		#default_run initad
		
		mwa		#dos_segadrs icbal+$10
		mwa		#2 icbll+$10
		mva		#CIOCmdGetChars iccmd+$10
		ldx		#$10
		jsr		ciov
		bpl		startaddr_ok
		cpy		#CIOStatEndOfFile
		bne		load_error
		clc
		rts

startaddr_ok:
		;check for $FFFF marker
		lda		dos_segadrs
		and		dos_segadrs+1
		cmp		#$ff
		beq		segment_loop

		mwa		#dos_segadrs+2 icbal+$10
		jsr		ciov
		bmi		load_error
		
		sbw		dos_segadrs+2 dos_segadrs icbll+$10
		inw		icbll+$10
		mwa		dos_segadrs icbal+$10
		jsr		ciov
		bmi		load_error
		
		jsr		do_init
		jmp		segment_loop
		
default_run:
load_error:
		sec
		rts
		
do_init:
		jmp		(initad)
.endp

;==========================================================================
.proc DOSHandlerTable
		dta		a(DOSOpen-1)
		dta		a(DOSClose-1)
		dta		a(DOSGetByte-1)
		dta		a(DOSPutByte-1)
		dta		a(DOSGetStatus-1)
		dta		a(DOSSpecial-1)
.endp

;==========================================================================
; OPEN command (command $03)
;
; Modes:
;	4 = read
;	6 = read directory
;	8 = write
;	9 = append
;	12 = update (read/write)
;
; DOS 2.0S notes:
;	- The following flags are allowed:
;
;		2		(directory read - GET CHARS only)
;		3		(directory read - GET CHARS only)
;		4		read
;		6		directory read
;		7		directory read (extended in DOS 2.5)
;		8		write
;		9		append
;		10		(directory read - GET CHARS ONLY)
;		11		(directory read - GET CHARS ONLY)
;		12		update
;		14
;		15
;
;	- Multiple directory streams (mode 6) can be open but will collide.
;	  They each have their own line buffers, but share the same directory
;	  buffer and will cross outputs: one of the streams will jump sectors
;	  to the other, or an EOF in one will cause an EOF in the others.
;	- A file always has at least one sector linked to it, even if empty.
;	  This sector is allocated immediately on open, and the open attempt
;	  will fail with code 162 if no sectors are free.
;	- A file newly opened for write first has any existing sector chain
;	  freed and then is initialized in the directory with the write flag
;	  set ($43), with a zero sector count.
;	- Because files opened for write are not visible, it is possible to
;	  open more than one IOCB for write with the same filename.
;	- Update and append operations require the file to exist.
;	- The write flag (bit 0) is not set on files open for update or append.
;
.proc DOSOpen
		;check if FCB is already open
		lda		dos_fcb_table[0].secbuf,x
		bmi		fcb_closed
		ldy		#CIOStatIOCBInUse
		rts

too_many_open:
		ldy		#CIOStatTooManyFiles
		rts

fnerr:
		ldy		#CIOStatFileNameErr
		rts

fcb_closed:
		;test for invalid mode
		lda		icax1z
		and		#$f0
		beq		mode_ok

		ldy		#CIOStatInvalidCmd
		rts

mode_ok:
		jsr		DOSInvalidateMetadataBuffers

		;set up drive
		lda		icdnoz
		sta		dos_dunit

		;try to find an available sector buffer
		jsr		DOSFindOpenBuffer
		beq		too_many_open

		stx		dos_iocb

		jsr		DOSParseFullFilename
		bmi		error

		;search directory
		jsr		DOSFindFirst
		bpl		found

		;check if it was file not found
		cpy		#CIOStatFileNotFound
		beq		is_eof
error:
		rts

is_eof:
		;check if it is a write and not an append or update operation
		lda		icax1z
		cmp		#8
		bne		not_write

		;okay, it's a write and we need to create -- check if we have
		;room
		lda		dos_dsfsec
		bne		have_free_dirent

		ldy		#CIOStatDirFull
		rts

have_free_dirent:
		;read directory sector
		ldx		#0
		ldy		#>361
		jsr		DOSReadSector

		;begin populating entry
		ldy		dos_dsfoff
		lda		#$43
		sta		(zsba),y+
		ldx		#4
		lda		#0
fill_1:
		sta		(zsba),y+
		dex
		bne		fill_1
copy_1:
		lda		dos_filename,x
		sta		(zsba),y+
		inx
		cpx		#11
		bne		copy_1

		;rewrite directory sector
		ldx		#0
		jsr		DOSWriteSector
		bmi		error
		jmp		alloc

not_write:
		;check if this is a dir operation -- EOF is OK in that case
		and		#2
		beq		error

found:
		;check if we are doing a write
		lda		icax1z
		cmp		#8
		bne		not_write_2

		;check if the file is locked
		ldy		dos_dsoff
		lda		dos_dirbuffer,y
		and		#$20
		beq		not_locked

		;return file locked error
		ldy		#CIOStatFileLocked
		rts

not_locked:
		;the file isn't locked, but it does exist -- we must free the
		;sector chain
		jsr		DOSFreeSectorChain

alloc:
		;allocate a sector for this file
		jsr		DOSAllocateSector
		bpl		alloc_ok
		rts

alloc_ok:
		ldy		dos_dsoff
		mwa		zbufp dos_dirbuffer+3,y

not_write_2:
		;restore IOCB#
		ldx		dos_iocb

		;find open sector buffer (we confirmed there was one earlier)
		jsr		DOSFindOpenBuffer
		sta		dos_fcb_table[0].secbuf,x
		txa
		sta		dos_secbuf_fcbidx,y
		jsr		DOSSetSectorPointer
		
		;set up base FCB
		ldx		dos_iocb
		lda		#0
		sta		dos_fcb_table[0].offset,x
		sta		dos_fcb_table[0].secvlen,x

		;check if we are doing a directory open
		lda		#2
		bit		icax1z
		bne		do_diropen

		;set up FCB
		lda		#0
		ldy		icax1z
		cpy		#8
		sne:lda	#1
		sta		dos_fcb_table[0].secncnt,x
		lda		#0
		sta		dos_fcb_table[0].secncnt+1,x

		ldy		dos_dsoff
		lda		dos_dirbuffer+3,y
		sta		dos_fcb_table[0].secnxt,x
		lda		dos_dirbuffer+4,y
		sta		dos_fcb_table[0].secnxt+1,x
		lda		dos_dsfid
		asl
		asl
		sta		dos_fcb_table[0].fileid,x
				
		;check if we should seek to the end of the file
		lda		icax1z
		lsr
		bcc		start_beginning

		;advance sectors until we are at the last sector
advance_loop:
		ldx		dos_iocb
		lda		dos_fcb_table[0].secnxt,x
		ldy		dos_fcb_table[0].secnxt+1,x
		bne		advance_next
		cmp		#0
		beq		advance_done
advance_next:
		pha
		lda		dos_fcb_table[0].secbuf,x
		tax
		pla
		jsr		DOSReadDataSector
		bmi		fail
		bpl		advance_loop

do_diropen:
		;pre-populate buffer
		ldx		dos_dsoff
		jsr		DOSFormatLine

		;set dirent flags mode
		;jimmy dummy value into sector next pointer to supress EOF
		lda		#$01
		ldx		dos_iocb
		sta		dos_fcb_table[0].secnxt,x

		ldy		#1
		rts

advance_done:
		;set offset to end
		lda		dos_fcb_table[0].secvlen,x
		sta		dos_fcb_table[0].offset,x

start_beginning:
		;check if this is write and we should init the data sector
		lda		icax1z
		cmp		#8
		bne		no_dsec_init

		lda		dos_fcb_table[0].secbuf,x
		tax
		jsr		DOSSetSectorPointer
		ldx		dos_iocb

		ldy		bcb_linkoffset
		lda		dos_fcb_table[0].fileid,x
		sta		(zsba),y+
		lda		#0
		sta		(zsba),y+
		sta		(zsba),y

		;mark data and directory sector buffers as dirty
		lda		#$ff
		sta		dos_secbuf_dirty
		ldy		dos_fcb_table[0].secbuf,x
		sta		dos_secbuf_dirty,y
		lda		dos_fcb_table[0].secnxt,x
		sta		dos_fcb_table[0].sector,x
		sta		dos_secbuf_seclo,y
		lda		dos_fcb_table[0].secnxt+1,x
		sta		dos_fcb_table[0].sector+1,x
		sta		dos_secbuf_sechi,y
		lda		#0
		sta		dos_fcb_table[0].secnxt,x
		sta		dos_fcb_table[0].secnxt+1,x

no_dsec_init:
		jsr		DOSFlushMetadata
		ldy		#1
fail:
		rts
.endp

;==========================================================================
; Find a free sector buffer to use for a file.
;
; Exit:
;	Y = 2+ if free sector buffer found, 0 if not
;
.proc DOSFindOpenBuffer
		ldy		bcb_maxfiles
search_loop:
		lda		dos_secbuf_fcbidx+1,y
		bmi		found_unused
		dey
		bne		search_loop
		dey
found_unused:
		iny
		tya
		rts
.endp

;==========================================================================
; Result:
;	C=0		Alphanumeric or ?/*
;	C=1		Not alphanumeric or ?/*
;
DOSIsalphaWild = DOSIsalnumWild.isalphawild_entry
.proc DOSIsalnumWild
		cmp		#'0'
		bcc		not_digit
		cmp		#'9'+1
		bcc		done
not_digit:
isalphawild_entry:
		cmp		#'*'
		beq		done
		cmp		#'?'
		beq		done
		and		#$df
		cmp		#'A'
		bcc		fail
		cmp		#'Z'+1
		bcc		done
fail:
		sec
		rts
done:
		clc
		rts
.endp

;==========================================================================
.proc DOSClose
		;check if channel is open
		ldy		#CIOStatNotOpen
		lda		dos_fcb_table[0].secbuf,x
		bmi		done

		jsr		DOSInvalidateMetadataBuffers

		;set up drive
		lda		icdnoz
		sta		dos_dunit

		;stash IOCB#
		stx		dos_iocb

		;check if file was open for write
		lda		icax1z
		and		#8
		beq		not_write

		;check if the sector buffer is dirty
		ldy		dos_fcb_table[0].secbuf,x
		lda		dos_secbuf_dirty,y
		bpl		secbuf_clean

		;rewrite the sector
		tya
		tax
		jsr		DOSWriteSector

secbuf_clean:

		;check if we added new sectors
		ldx		dos_iocb
		lda		dos_fcb_table[0].secncnt,x
		ora		dos_fcb_table[0].secncnt,x+1
		beq		dirent_update_skip

		;reload directory entry and update sector count
		lda		#0
		asl		dos_fcb_table[0].fileid,x
		rol
		asl		dos_fcb_table[0].fileid,x
		rol
		asl		dos_fcb_table[0].fileid,x
		rol
		adc		#<361
		ldy		#>361
		ldx		#0
		jsr		DOSReadSector
		bmi		dirent_update_skip

		ldx		dos_iocb
		lda		dos_fcb_table[0].fileid,x
		lsr
		tay
		
		;clear open for write flag
		lda		dos_dirbuffer,y
		and		#$fe
		sta		dos_dirbuffer,y

		;update sector count
		lda		dos_dirbuffer+1,y
		clc
		adc		dos_fcb_table[0].secncnt,x
		sta		dos_dirbuffer+1,y
		lda		dos_dirbuffer+2,y
		clc
		adc		dos_fcb_table[0].secncnt+1,x
		sta		dos_dirbuffer+2,y

		;mark directory buffer as dirty
		lda		#$80
		sta		dos_secbuf_dirty

dirent_update_skip:
		;flush directory and VTOC buffers
		jsr		DOSFlushMetadata

not_write:
		;free the sector buffer
		ldx		dos_iocb
		ldy		dos_fcb_table[0].secbuf,x
		lda		#$ff
		sta		dos_secbuf_fcbidx,y
		sta		dos_fcb_table[0].secbuf,x

done:
		ldy		#1
		rts
.endp

;==========================================================================
.proc DOSGetByte
		;check if the FCB is open
		ldy		#CIOStatNotOpen
		lda		dos_fcb_table[0].secbuf,x
		bpl		is_open
		rts
is_open:
		;set up drive and stash IOCB#
		lda		icdnoz
		sta		dos_dunit
		stx		dos_iocb

		;check if we can safely burst
		jsr		DOSCheckBurst
		ldx		dos_iocb

		;set sector pointer
		lda		dos_fcb_table[0].secbuf,x
		tax
		jsr		DOSSetSectorPointer
		
		;get byte offset
		ldx		dos_iocb
		lda		dos_fcb_table[0].offset,x
		
		;check if we're at the end of the data area
		cmp		dos_fcb_table[0].secvlen,x
		jne		still_bytes
		
need_bytes:
		;yes, we need to advance a sector. check if sector buffer is
		;dirty
		ldy		dos_fcb_table[0].secbuf,x
		lda		dos_secbuf_dirty,y
		bpl		secbuf_clean

		;sector buffer is dirty -- flush it
		tya
		tax
		jsr		DOSWriteSector
		bmi		error

secbuf_clean:
		;get next sector pointer (dummy for dirent mode)
		ldx		dos_iocb
		ldy		dos_fcb_table[0].secnxt+1,x
		lda		dos_fcb_table[0].secnxt,x
		sty		dos_sechi
		sta		dos_seclo
		bne		nextsec_valid
		tya
		bne		nextsec_valid

at_eof:
		;huh... we're at EOF.
		ldy		#CIOStatEndOfFile
error:
		rts

nextsec_valid:
		;check if this is directory mode
		lda		#$02
		bit		icax1z
		beq		not_dirent

		lda		dos_dssec
		beq		at_eof
		jsr		DOSFindNext
		bpl		dirent_ok
		cpy		#CIOStatFileNotFound
		bne		error
dirent_ok:
		jsr		DOSFormatLine
		bmi		error
		jmp		still_bytes

not_dirent:

		;check if we can read directly to the user buffer
		ldx		dos_iocb
		bit		dos_burstok
		bpl		no_burst_sector

		lda		icblhz
		bne		burst_sector_ok
		lda		icbllz
		bpl		no_burst_sector
burst_sector_ok:

		;read next sector directly to user buffer
		mwa		icbalz zsba
		mva		dos_seclo daux1
		mva		dos_sechi daux2
		mva		#'R' dcomnd
		jsr		DOSDoIO
		bmi		error

		;load next sector pointer
		jsr		DOSReadSectorMetadata

		;reset offset to vlen
		lda		dos_fcb_table[0].secvlen,x
		sta		dos_fcb_table[0].offset,x

		;update CIO pointers
		clc
		adc		icbalz
		sta		icbalz
		scc:inc	icbahz

		lda		icbllz
		sec
		sbc		dos_fcb_table[0].offset,x
		sta		icbllz
		scs:dec	icblhz

		;jump back for more data
		jmp		need_bytes

no_burst_sector:
		lda		dos_fcb_table[0].secbuf,x
		tax
		lda		dos_seclo
		jsr		DOSReadDataSector
		
		;reset offset
		lda		#0
		sta		dos_fcb_table[0].offset,x
		tay
		
still_bytes:
		;check if we can burst (retaddr >= $c000, cmd=get byte)
		bit		dos_burstok
		bpl		no_burst

		;convert valid sector length to end offset and stash it
		ldx		dos_iocb
		ldy		dos_fcb_table[0].secvlen,x
		dey
		sty		zbufp

		;copy data from sector buffer to user buffer
		tay
		ldx		#0
		beq		burst_loop_start

burst_loop:
		lda		icbllz
		lsr
		ora		icblhz
		beq		burst_exit
		
		lda		(zsba),y
		sta		(icbalz,x)
		inw		icbalz
		dew		icbllz
		iny
burst_loop_start:
		cpy		zbufp
		bne		burst_loop
		tya
		
no_burst:
		tay
burst_exit:
		lda		(zsba),y
		pha
		iny
		ldx		dos_iocb
		tya
		sta		dos_fcb_table[0].offset,x
		cmp		dos_fcb_table[0].secvlen,x
		bne		not_at_eof
		
		;check if sector link is zero
		lda		dos_fcb_table[0].secnxt,x
		ora		dos_fcb_table[0].secnxt+1,x
		bne		eof_imminent

not_at_eof:
		;not at EOF -- return normal success
		pla
		ldy		#1
		rts
		
eof_imminent:
		;return EOF imminent
		pla
		ldy		#3
		rts
.endp

;==========================================================================
;
; DOS 2.0S behavior notes:
;	- Internal R/W modes are checked by DOS on put. Attempts to write to a
;	  read-only file will fail even if a direct call bypasses the CIO
;	  check (BASIC).
;	- Files can only be extended in write (mode 8) or append (mode 9)
;	  modes. Files in update mode (mode 12) cannot be extended.
;
.proc DOSPutByte
		;stash character
		sta		ciochr

		;stash IOCB and drive index
		stx		dos_iocb
		mva		icdno,x dos_dunit

		jsr		DOSCheckBurst
		ldx		dos_iocb

		;check sector buffer #
		lda		dos_fcb_table[0].secbuf,x
		bpl		is_open
		
		ldy		#CIOStatNotOpen
		rts

is_open:
		;check if we are at the end of the data area
		lda		dos_fcb_table[0].offset,x
		cmp		dos_fcb_table[0].secvlen,x
		sne:jmp	no_room

have_room:
		;write byte to buffer and mark buffer dirty
		pha
		lda		dos_fcb_table[0].secbuf,x
		tax
		mva		#$ff dos_secbuf_dirty,x
		jsr		DOSSetSectorPointer
		ldx		dos_iocb
		pla
		tay
		lda		ciochr
		sta		(zsba),y
		inc		dos_fcb_table[0].offset,x

		;check if we can burst:
		;- return address >= $C000
		;- command is PUT CHARS, not PUT RECORD
		bit		dos_burstok
		bpl		no_burst

		;Okay, we can burst... next check how far we can burst. If we're in
		;update mode, we can only burst up to VLEN... but if we're in write
		;mode, we can burst up to the limit if it's the last sector.
		ldx		dos_iocb
		ldy		dos_fcb_table[0].secvlen,x
		lda		icax1z
		cmp		#8
		bne		burst_no_extend
		lda		dos_fcb_table[0].secnxt,x
		ora		dos_fcb_table[0].secnxt+1,x
		bne		burst_no_extend
		ldy		bcb_linkoffset
burst_no_extend:
		sty		zbufp

		;do burst
		ldy		dos_fcb_table[0].offset,x
		ldx		#0
		beq		burst_loop_start

burst_loop:
		lda		icbllz
		lsr
		ora		icblhz
		beq		burst_exit

		inw		icbalz
		dew		icbllz

		lda		(icbalz,x)
		sta		(zsba),y

		iny
burst_loop_start:
		cpy		zbufp
		bne		burst_loop
burst_exit:
		ldx		dos_iocb
		tya
		sta		dos_fcb_table[0].offset,x

		;check if we extended the sector
		cmp		dos_fcb_table[0].secvlen,x
		bcc		no_extend
		sta		dos_fcb_table[0].secvlen,x
		ldy		bcb_linkoffset
		iny
		iny
		sta		(zsba),y
no_extend:
no_burst:
		;all done
		ldy		#1

error:
		rts

no_room:
		;Check if we already have another sector -- if so, spool it in.
		;We can't extend the sector in that case even if there is room,
		;as that would insert data in the middle of the file.
		lda		dos_fcb_table[0].secnxt,x
		bne		have_next_sector
		ldy		dos_fcb_table[0].secnxt+1,x
		beq		no_next_sector
have_next_sector:
		pha
		lda		dos_fcb_table[0].secbuf,x
		tax
		pla
		jsr		DOSReadDataSector

no_next_sector:
		;Check if we can extend the sector data area.
		lda		dos_fcb_table[0].secvlen,x
		jne		no_sector_burst

		;Hmm... the sector is empty. Can we burst?
		bit		dos_burstok
		jpl		no_sector_burst

		;Burst I/O possible... check if we have enough data length to do it.
		lda		icblhz
		bne		do_sector_burst
		lda		icbllz
		bpl		no_sector_burst
do_sector_burst:
		;stash the 3 bytes at the link location in the user buffer
		ldy		bcb_linkoffset
		ldx		#$fd
savelink_loop:
		mva		(icbalz),y+ dos_linksav-$fd,x
		inx
		bne		savelink_loop

		;we can! allocate a new sector
		jsr		DOSAllocateSector
		bmi		error
		
		;rewrite the sector link in the user buffer
		ldy		bcb_linkoffset
		ldx		dos_iocb
		lda		zbufp+1
		ora		dos_fcb_table[0].fileid,x
		sta		(icbalz),y
		iny
		lda		zbufp
		sta		(icbalz),y
		iny
		lda		bcb_linkoffset
		sta		(icbalz),y

		;write sector from user buffer
		mwa		dos_fcb_table[0].sector,x daux1
		ldx		icbalz
		ldy		icbahz
		lda		#'P'
		jsr		DOSDoIOBufXY

		;restore user data bytes -- we must do this even if the write failed
		sty		status
		ldy		bcb_linkoffset
		ldx		#$fd
restorelink_loop:
		mva		dos_linksav-$fd,x (icbalz),y+
		inx
		bne		restorelink_loop

		;bail if error
		ldy		status
		bmi		error

		;update sector numbers
		ldx		dos_iocb
		ldy		dos_fcb_table[0].secbuf,x
		lda		zbufp
		sta		dos_fcb_table[0].sector,x
		sta		dos_secbuf_seclo,y
		lda		zbufp+1
		sta		dos_fcb_table[0].sector+1,x
		sta		dos_secbuf_sechi,y

		;reset sector buffer for new sector and increment new sector count
		ldx		dos_iocb
		lda		dos_secbuf_seclo,x
		tax
		jsr		DOSSetSectorPointer
		jsr		init_new_sector

		;advance CIO pointers
		jsr		DOSBurstUpdateCIOPointers

		;return success
		ldy		#1
error2:
		rts

no_sector_burst:
		cmp		bcb_linkoffset
		beq		cant_extend

		adc		#1
		sta		dos_fcb_table[0].secvlen,x
		ldy		bcb_linkoffset
		iny
		iny
		sta		(zsba),y

		;mark sector dirty
		lda		#$80
		ldy		dos_fcb_table[0].secbuf,x
		sta		dos_secbuf_dirty,y

		;go back and write the byte
		jmp		is_open

cant_extend:
		;attempt to allocate a sector
		jsr		DOSAllocateSector
		bmi		error2

		ldx		dos_iocb
		lda		dos_fcb_table[0].secbuf,x
		tax
		jsr		DOSSetSectorPointer

		;update next sector links in FCB and in data sector
		ldy		bcb_linkoffset
		ldx		dos_iocb
		lda		zbufp+1
		sta		dos_fcb_table[0].sector+1,x
		ora		dos_fcb_table[0].fileid,x
		sta		(zsba),y
		iny
		lda		zbufp
		sta		dos_fcb_table[0].sector,x
		sta		(zsba),y

		;flush data sector
		lda		dos_fcb_table[0].secbuf,x
		tax
		jsr		DOSWriteSector
		bmi		error2

		jsr		init_new_sector

		;go back and write the byte
		jmp		is_open

init_new_sector:
		;reassign sector buffer to new sector
		ldx		dos_iocb
		ldy		dos_fcb_table[0].secbuf,x
		mva		dos_fcb_table[0].sector,x dos_secbuf_seclo,y
		mva		dos_fcb_table[0].sector+1,x dos_secbuf_sechi,y

		;mark sector buffer dirty
		mva		#$80 dos_secbuf_dirty,y

		;initialize sector length and valid lengths
		ldy		bcb_linkoffset
		lda		dos_fcb_table[0].fileid,x
		sta		(zsba),y+
		lda		#0
		sta		dos_fcb_table[0].secnxt,x
		sta		dos_fcb_table[0].secnxt+1,x
		sta		(zsba),y+
		sta		dos_fcb_table[0].offset,x
		lda		#0
		sta		(zsba),y
		sta		dos_fcb_table[0].secvlen,x

		;increment new sector count
		inw		dos_fcb_table[0].secncnt,x
		rts
.endp

;==========================================================================
.proc DOSGetStatus
		ldy		#CIOStatNotOpen
		lda		dos_fcb_table[0].secbuf,x
		bmi		not_open
		ldy		#1
		rts
not_open:
		ldy		errno
		rts
.endp

;==========================================================================
; Commands:
;	$20 - rename
;	$21 - delete
;	$23 - lock
;	$24 - unlock
;	$25 - point
;	$26 - note
;	$28 - load DOS binary (MyDOS 4.5 / SDX)
;
.proc DOSSpecial
		;set up drive
		lda		icdnoz
		sta		dos_dunit

		lda		iccomz
		cmp		#$fd
		bne		not_format
		jmp		DOSSpecialFormat
not_format:
		eor		#$20
		cmp		#9
		bcs		invalid_cmd
		tay
		lda		cmd_table_hi,y
		pha
		lda		cmd_table_lo,y
		pha
invalid_cmd:
		ldy		#CIOStatInvalidCmd
		rts

cmd_table_lo:
		dta		<[DOSSpecialRename-1]
		dta		<[DOSSpecialDelete-1]
		dta		<[invalid_cmd-1]
		dta		<[DOSSpecialLock-1]
		dta		<[DOSSpecialUnlock-1]
		dta		<[DOSSpecialPoint-1]
		dta		<[DOSSpecialNote-1]
		dta		<[invalid_cmd-1]
		dta		<[DOSSpecialLoadEXE-1]

cmd_table_hi:
		dta		>[DOSSpecialRename-1]
		dta		>[DOSSpecialDelete-1]
		dta		>[invalid_cmd-1]
		dta		>[DOSSpecialLock-1]
		dta		>[DOSSpecialUnlock-1]
		dta		>[DOSSpecialPoint-1]
		dta		>[DOSSpecialNote-1]
		dta		>[invalid_cmd-1]
		dta		>[DOSSpecialLoadEXE-1]
.endp

;==========================================================================
; DOS 2.0S quirks/bugs:
;	- No check for a duplicate file.
;
.proc DOSSpecialRename
		;look for the file
		jsr		DOSParseFullFilename
		bmi		error
		sta		pathcont_idx
		jsr		DOSFindFirst
		bmi		error

		;check for a comma
		ldy		#0
pathcont_idx = *-1
		lda		(icbalz),y+
		cmp		#','
		bne		fnerr

		;parse second filename (no prefix)
		jsr		DOSParseFilename
		bmi		error

		;merge the filename into the directory entry
		lda		dos_dsoff
		clc
		adc		#15
		tay
		ldx		#10
merge_loop:
		lda		dos_filename,x
		cmp		#'?'
		seq:sta	dos_dirbuffer,y
		dey
		dex
		bpl		merge_loop

		;rewrite the directory sector
		stx		dos_secbuf_dirty

		jmp		DOSFlushMetadata

fnerr:
		ldy		#CIOStatFileNameErr
error:
		rts
.endp

;==========================================================================
; Delete file (command $21)
;
; DOS 2.0S quirks/bugs:
;	- Will happily delete a file that is open for write. Doing so corrupts
;	  the disk.
;	- Requires an open file slot (fortunately for us).
;
.proc DOSSpecialDelete
		;look for the file
		jsr		DOSParseFullFilename
		bmi		error
		jsr		DOSFindFirst
		bmi		error

		;check if file is locked
		tay
		lda		dos_dirbuffer,y
		and		#$20
		bne		is_locked

		;replace file entry flags with deleted flag
		lda		#$80
		sta		dos_dirbuffer,y

		;mark directory buffer as dirty
		sta		dos_secbuf_dirty

		;free sectors in file chain
		jsr		DOSFreeSectorChain
		bmi		error

		;write directory and VTOC
		jsr		DOSFlushMetadata
error:
		rts

is_locked:
		ldy		#CIOStatFileLocked
		rts
.endp

;==========================================================================
; Follow the sector chain for a file and mark all sectors free in the
; VTOC.
;
; Note that this routine does not clear the sectors -- it isn't possible
; to 'clean' the sector chain anyway as all values of the 6 bits file ID
; field could be valid file IDs. On the good side, this means that we don't
; need to rewrite each sector.
;
; We must not use sector buffer 0 here, because the OPEN path depends on
; that buffer being preserved across this call, and we can't use 1 because
; we need that for the VTOC. Therefore, we need an open file buffer.
;
.proc DOSFreeSectorChain
		;search for an open file buffer to use
		jsr		DOSFindOpenBuffer
		bne		have_buffer

		ldy		#CIOStatTooManyFiles
		rts

have_buffer:
		sta		buf

		;follow file chain and free all sectors
		ldx		dos_dsoff
		lda		dos_dirbuffer+3,x
		ldy		dos_dirbuffer+4,x
read_next:
		ldx		#0
buf = *-1
		jsr		DOSReadSector
		bmi		error

		ldy		bcb_linkoffset
		lda		(zsba),y
		and		#3
		sta		next_hi
		iny
		lda		(zsba),y
		sta		next_lo

		lda		dos_seclo
		ldy		dos_sechi
		jsr		DOSFreeSector

		lda		#0
next_lo = *-1
		ldy		#0
next_hi = *-1
		bne		read_next
		tax
		bne		read_next
		ldy		#1
error:
		rts
.endp

;==========================================================================
.proc DOSSpecialLock
		;look for the file
		jsr		DOSParseFullFilename
		bmi		error
		jsr		DOSFindFirst
		bmi		error

		;retrieve current flags and set lock flag
		tay
		lda		dos_dirbuffer,y

		;check if file is already locked
		and		#$20
		bne		already_locked

		;set lock flag
		lda		dos_dirbuffer,y
		ora		#$20
		sta		dos_dirbuffer,y

		;rewrite directory sector
		jsr		DOSWriteSector
		bmi		error

already_locked:
		;all done
		ldy		#1

error:
		rts
.endp

;==========================================================================
.proc DOSSpecialUnlock
		;look for the file
		jsr		DOSParseFullFilename
		bmi		error
		jsr		DOSFindFirst
		bmi		error

		;retrieve current flags and set lock flag
		tay
		lda		dos_dirbuffer,y

		;check if file is already locked
		and		#$20
		beq		already_unlocked

		;clear lock flag
		eor		dos_dirbuffer,y
		sta		dos_dirbuffer,y

		;rewrite directory sector
		jsr		DOSFlushMetadata
		bmi		error

already_unlocked:
		;all done
		ldy		#1

error:
		rts
.endp

;==========================================================================
; POINT (command $25)
;
; Errors:
;	171 - if mode 6 (dir) or 8 (write)
;
.proc DOSSpecialPoint
		ldy		#CIOStatNotOpen
		lda		dos_fcb_table[0].secbuf,x
		bmi		not_open

		;check if we allow seeking with the current mode
		lda		icax1z
		and		#$f7
		cmp		#4
		bne		invalid_point

		;load sector number and check if sector is in range
		ldy		icax4,x
		cpy		#>721
		bcc		sector_hi_ok
		bne		invalid_point
		lda		icax3,x
		cmp		#<721
		bcs		invalid_point
sector_hi_ok:
		sta		dos_fcb_table[0].sector,x
		sta		dos_seclo
		tya
		sta		dos_fcb_table[0].sector+1,x
		sta		dos_sechi

		;load and check sector offset
		lda		icax5,x
		cmp		bcb_linkoffset
		bcs		invalid_point
		sta		dos_fcb_table[0].offset,x

		;flush sector if dirty (possible in update mode)
		ldy		dos_fcb_table[0].secbuf,x
		lda		dos_secbuf_dirty,y
		bpl		sector_clean

		tya
		tax
		jsr		DOSWriteSector

sector_clean:
		;load new sector
		ldx		dos_iocb
		lda		dos_fcb_table[0].secbuf,x
		pha
		lda		icax3,x
		ldy		icax4,x
		pla
		tax
		jsr		DOSReadDataSector

		;all done
		ldy		#1
not_open:
		rts

invalid_point:
		ldy		#CIOStatInvPoint
		rts
.endp

;==========================================================================
; NOTE command ($26)
;
; DOS 2.0S notes:
;	- This command does not work on a directory mode channel. It always
;	  returns 0,0.
;
.proc DOSSpecialNote
		ldy		#CIOStatNotOpen
		lda		dos_fcb_table[0].secbuf,x
		bmi		not_open

		lda		dos_fcb_table[0].sector,x
		sta		icax3,x
		lda		dos_fcb_table[0].sector+1,x
		sta		icax4,x
		lda		dos_fcb_table[0].offset,x
		sta		icax5,x
		ldy		#1
not_open:
		rts
.endp

;==========================================================================
.proc DOSSpecialLoadEXE
		lda		#4
		sta		icax1,x

		;check if this IOCB is already open
		lda		dos_fcb_table[0].secbuf,x
		bpl		already_open

		;hack mode 4 (read) into the IOCB and attempt to open the EXE
		stx		dos_iocb
		lda		#CIOCmdOpen
		sta		iccmd,x
		jsr		ciov
		bmi		fail

already_open:
		;load the executable -- note that we are reentering CIO!
		jsr		DOSLoadExecutable
		bcs		close_and_exit

		;close the IOCB first, then call it
		jsr		close_and_exit
		jsr		do_run
		ldy		#1
		rts

close_and_exit:
		ldx		dos_iocb
		mva		#CIOCmdClose iccmd,x
		jsr		ciov

		ldy		#1
fail:
		rts

do_run:
		jmp		(runad)
.endp

;==========================================================================
; FORMAT DISK command (XIO 253)
;
.proc DOSSpecialFormat
		;check if it is a medium density format, which we don't support
		;yet
		lda		icax1z
		beq		mode_ok
error2:
		rts

mode_ok:
		;we're about to take over the VTOC buffer, so flush it
		jsr		DOSFlushMetadata
		bmi		error2

		;issue format command
		lda		icdnoz
		sta		dos_dunit
		ldx		#<dos_dirbuffer
		ldy		#>dos_dirbuffer
		lda		#$21
		jsr		DOSDoIOBufXY
		bmi		error2

		;zero VTOC and dir buffers together
		lda		#0
		tax
		sta:rne	dos_dirbuffer,x+

		;init signature and sector counts
		ldx		#4
		mva:rpl	vtoc_signature,x dos_vtocbuffer,x-

		;mark VTOC buffer as dirty
		stx		dos_secbuf_dirty+1

		;mark all sectors as free
		txa
		ldx		#89
		sta:rpl	dos_vtocbuffer+10,x-

		;mark sectors 0-3 as allocated
		mva		#$0f dos_vtocbuffer+10

		;mark sectors 360-368 as allocated
		mva		#$00 dos_vtocbuffer+10+45
		mva		#$7f dos_vtocbuffer+10+46

		mva		#<360 dos_secbuf_seclo+1
		lda		#>360
		sta		dos_secbuf_sechi+1
		sta		dos_secbuf_sechi

		;clear all 8 directory sectors
		mva		#<369 dos_secbuf_seclo
dirclear_loop:
		dec		dos_secbuf_seclo
		ldx		#$ff
		stx		dos_secbuf_dirty
		inx
		jsr		DOSWriteSector
		lda		dos_secbuf_seclo
		cmp		#<361
		bne		dirclear_loop

		jsr		DOSFlushMetadata
		bmi		error

		;write sectors 1-3 to disk
		ldx		#<$0800
		stx		daux2
		ldy		#>$0800
		lda		#3
		sta		daux1
bootsec_loop:
		lda		#'P'
		jsr		DOSDoIOBufXY
		bmi		error
		ldy		#$07
		lda		dbuflo
		eor		#$80
		tax
		dec		daux1
		bne		bootsec_loop

		ldy		#1
error:
		rts

vtoc_signature:
		dta		$02,a(707),a(707)
.endp

;==========================================================================
; Check if we can do burst I/O safely.
;
; Must be called directly from the entry point.
;
; Output:
;	dos_burstok = 0 if can't burst, $FF if can burst
;
; Modified:
;	X, Y
;
; Preserved:
;	A
;
.proc DOSCheckBurst
		tsx
		ldy		$0104,x
		ldx		#0
		cpy		#$c0
		bcc		no_burst
		ldy		iccomz
		cpy		#CIOCmdPutChars
		beq		burst_ok
		cpy		#CIOCmdGetChars
		bne		no_burst
burst_ok:
		dex
no_burst:
		stx		dos_burstok
		rts
.endp

;==========================================================================
; Search for a filename.
;
; Entry:
;	dos_filename	Filename pattern to search for
;
; Exit:
;	Y,P.N		CIO status
;	A			Base index of directory entry
;	dos_dsfidx	Low sector byte of first free directory entry, or 0 if full
;	dos_dsfoff	Offset of first free directory entry, if any
;
.proc DOSFindFirst
		;clear first free pointer
		lda		#0
		sta		dos_dsfsec

		;start with end of sector 360
		lda		#<360
		sta		dos_dssec
		lda		#$80
		sta		dos_dsoff

		mva		#$ff dos_dsfid
	
		;fall through to find next	
.def :DOSFindNext = *

dirent_loop:
		;advance to next entry
		inc		dos_dsfid
		lda		dos_dsoff
		clc
		adc		#16
		sta		dos_dsoff
		tay

		;check if we are at the end of the current directory sector
		bpl		not_dirsec_end

		;bump sector and check if we're at end
		inc		dos_dssec
		lda		dos_dssec
		cmp		#<369
		bcs		terminate_search

		;read next directory sector
		ldx		#0
		ldy		#>361
		jsr		DOSReadSector
		bmi		error

		ldy		#0
		sty		dos_dsoff

not_dirsec_end:
		;check if current entry is valid
		lda		dos_dirbuffer,y
		asl
		bmi		is_valid

		;mark first free if we haven't seen it already
		lda		dos_dsfsec
		bne		already_found_free

		mva		dos_dssec dos_dsfsec
		sty		dos_dsfoff

already_found_free:

		;check if this is the end of the directory
		lda		dos_dirbuffer,y
		beq		terminate_search
		bne		dirent_loop

is_valid:
		;it's valid -- compare the filename
		tya
		ora		#5
		tay
		ldx		#$f5
fntest_loop:
		lda		dos_filename-$f5,x
		cmp		#'?'
		beq		skip_wild
		cmp		dos_dirbuffer,y
		bne		dirent_loop
skip_wild:
		iny
		inx
		bne		fntest_loop
		
		lda		dos_dsoff
		ldy		#1
		rts
				
terminate_search:
		;not found
		lda		#0
		sta		dos_dssec
		ldy		#CIOStatFileNotFound
error:
		rts
.endp

;==========================================================================
; Entry:
;	ICBALZ = filename buffer
;	Y = starting index (DOSParseFilename only)
;
; Exit:
;	DOS_FILENAME = parsed filename
;	Y = CIO status
;
; DOS 2.0S quirks/bugs:
;	- Star fills the remainder of the filename or extension field with
;	  question marks. Nothing after the star in that field has effect.
;	- Any characters beyond the field limit (8/3) are silently discarded.
;	- Unrecognized characters stop parsing without error.
;
.proc DOSParseFullFilename
		ldy		#0
		lda		(icbalz),y
		cmp		#'D'
		beq		drvlet_ok
		ldy		#CIOStatUnkDevice
drvlet_ok:
		iny
		
		;validate optional digit
		lda		(icbalz),y
		cmp		#'1'
		bcc		not_digit
		cmp		#'9'+1
		bcs		not_digit
		iny
		lda		(icbalz),y
not_digit:

		;validate colon
		cmp		#':'
		bne		bad_filename
		iny

.def :DOSParseFilename = *
		ldx		#8
		stx		field_len
		ldx		#0
		lda		(icbalz),y
		jsr		DOSIsalphaWild
		bcs		bad_filename
		jsr		parse_field_firstchar

		;check for dot
		lda		(icbalz),y
		cmp		#'.'
		bne		no_dot
		iny
no_dot:
		;parse extension
		lda		#11
		sta		field_len
		jsr		parse_field
		ldy		#1
		rts

bad_filename:
		ldy		#CIOStatFileNameErr
		rts

filename_write_noinc:
		sta		dos_filename,x
		inx
parse_field:
filename_loop:
		lda		(icbalz),y
		jsr		DOSIsalnumWild
		bcs		filename_done
parse_field_firstchar:
		cmp		#'*'
		bne		filename_write
		lda		#'?'
		cpx		field_len
		bcc		filename_write_noinc
filename_write:
		iny
		cpx		#8
field_len = *-1
		bcs		filename_loop
		bcc		filename_write_noinc

		;pad out the rest of the field
fill_loop:
		lda		#' '
		sta		dos_filename,x
		inx
filename_done:
		cpx		field_len
		bcc		fill_loop
		rts

.endp

;==========================================================================
.proc DOSFormatLine
		;reset sector pointer
		ldx		dos_iocb
		lda		dos_fcb_table[0].secbuf,x
		tax
		jsr		DOSSetSectorPointer

		;check if we got an entry
		lda		dos_dssec
		bne		dirent_ok

		;we did -- do the free sector thing
		lda		#0
		sta		dos_fcb_table[0].secnxt,x

		jmp		DOSFormatFreeLine

dirent_ok:
		ldx		dos_dsoff
		jmp		DOSFormatDirLine
.endp

;==========================================================================
;
; Entry:
;	X = directory entry offset in directory sector buffer
;
; A directory line produced by DOS looks like the following:
;
; <two spaces> <8 char filename> <3 char ext> <space> <3 digit size>
;
; Thus, each line produced is exactly 18 bytes long including the
; EOL.
;
.proc DOSFormatDirLine
		;stash dirent offset
		stx		zbufp

		;begin with optional asterisk for lock, then space
		ldy		#$2a
		lda		dos_dirbuffer,x
		and		#$20
		sne:ldy	#$20
		tya
		ldy		#0
		sta		(zsba),y+
		lda		#$20
		sta		(zsba),y+

		;copy filename
		lda		#11
		sta		zbufp
copy_loop:
		lda		dos_dirbuffer+5,x+
		sta		(zsba),y+
		dec		zbufp
		bne		copy_loop

		;add another space
		lda		#$20
		sta		(zsba),y+

		;convert sector count
		lda		dos_dirbuffer+1-11,x
		sta		zbufp
		lda		dos_dirbuffer+2-11,x
		sta		zbufp+1
		jsr		DOSFormatSectorCount

		;finish with EOL
		lda		#$9B
		sta		(zsba),y+

update_fcb:
		ldx		dos_iocb
		tya
		sta		dos_fcb_table[0].secvlen,x
		lda		#0
		sta		dos_fcb_table[0].offset,x
		rts
.endp

;==========================================================================
; The final line of a directory listing has the following format:
;
; <3 digits> " FREE SECTORS" <EOL>
;
; It is 17 chars long, including EOL.
;
.proc DOSFormatFreeLine
		;read VTOC sector
		lda		zsba
		pha
		lda		zsba+1
		pha
		ldx		#0
		lda		#<360
		ldy		#>360
		jsr		DOSReadSector
		pla
		sta		zsba+1
		pla
		sta		zsba
		tya
		bpl		vtoc_read_ok
		rts

vtoc_read_ok:
		;copy free sector count from VTOC
		lda		dos_dirbuffer+3
		sta		zbufp
		lda		dos_dirbuffer+4
		sta		zbufp+1

		ldy		#0
		jsr		DOSFormatSectorCount

copy_loop:
		lda		freemsg_begin-3,y
		sta		(zsba),y+
		cpy		#17
		bne		copy_loop		
		jmp		DOSFormatDirLine.update_fcb

freemsg_begin:
		dta		' SECTORS FREE',$9B
freemsg_end:
.endp

;==========================================================================
.proc DOSFormatSectorCount
		lda		#0
		sta		zdrva
		sta		zdrva+1
		ldx		#16
bit_loop:
		sed
		asl		zbufp
		rol		zbufp+1
		lda		zdrva
		adc		zdrva
		sta		zdrva
		lda		zdrva+1
		adc		zdrva+1
		sta		zdrva+1
		dex
		bne		bit_loop
		cld

		;convert three BCD digits
		lda		zdrva+1
		and		#$0f
		ora		#$30
		sta		(zsba),y+

		lda		zdrva
		lsr
		lsr
		lsr
		lsr
		ora		#$30
		sta		(zsba),y+

		lda		zdrva
		and		#$0f
		ora		#$30
		sta		(zsba),y+
		rts
.endp

;==========================================================================
.proc DOSReadDataSector
		jsr		DOSReadSector
		bmi		error

.def :DOSReadSectorMetadata = *
		;copy next sector number
		ldx		dos_iocb
		ldy		bcb_linkoffset
		lda		(zsba),y
		and		#3
		sta		dos_fcb_table[0].secnxt+1,x		
		iny
		lda		(zsba),y
		sta		dos_fcb_table[0].secnxt,x		

		;copy valid sector length
		iny
		lda		(zsba),y
		sta		dos_fcb_table[0].secvlen,x

		;update current sector number
		mwa		dos_seclo dos_fcb_table[0].sector,x

		;all done
		ldy		#1
error:
		rts
.endp

;==========================================================================
; Entry:
;	Y:A		Sector number
;	X		Sector buffer index
;
.proc DOSReadSector
		sta		dos_seclo
		sty		dos_sechi
		jsr		DOSSetSectorPointer

		lda		dos_seclo
		ldy		dos_sechi
		cmp		dos_secbuf_seclo,x
		bne		wrong_sector
		tya
		cmp		dos_secbuf_sechi,x
		bne		wrong_sector
		lda		dos_secbuf_dunit,x
		cmp		dos_dunit
		bne		wrong_sector
		
		;we already have this sector
		ldy		#1
		rts

wrong_sector:
		;stash the sector buffer index
		stx		dos_secbidx

		;check if the current sector buffer is dirty - if so, write it
		;out first
		lda		dos_secbuf_dirty,x
		bpl		secbuf_clean
		jsr		DOSWriteSector		
secbuf_clean:

		;issue a disk sector read
		mwa		dos_seclo daux1
		lda		#'R'
		jsr		DOSDoIO
		bmi		error
		
		;update the cached sector index and drive
		ldx		dos_secbidx
		mva		dos_seclo dos_secbuf_seclo,x
		mva		dos_sechi dos_secbuf_sechi,x
		mva		dos_dunit dos_secbuf_dunit,x
		
		;all done
		tya
error:
		rts
.endp

;==========================================================================
; Write a sector buffer back to disk.
;
; Entry:
;	X		Sector buffer index
;
.proc DOSWriteSector
		stx		dos_secbidx

		;set zsba
		jsr		DOSSetSectorPointer
		ldx		dos_secbidx

		;mark sector buffer clean
		lsr		dos_secbuf_dirty,x

		;issue a disk sector buffer write and exit
		mva		dos_secbuf_seclo,x daux1
		mva		dos_secbuf_sechi,x daux2
		lda		#'P'
		jmp		DOSDoIO
.endp

;==========================================================================
.proc DOSDoIO
		ldx		zsba
		ldy		zsba+1

.def :DOSDoIOBufXY = *
		stx		dbuflo
		sty		dbufhi
.def :DOSDoIOCmd = *
		mvy		dos_dunit dunit
		sta		dcomnd
		jmp		dskinv
.endp

;==========================================================================
; Update CIO address and length values actor a sector sized burst.
;
.proc DOSBurstUpdateCIOPointers
		ldy		bcb_linkoffset
		dey
		tya
		clc
		adc		icbalz
		sta		icbalz
		scc:inc	icbahz
		tya
		eor		#$ff
		sec
		adc		icbllz
		sta		icbllz
		scs:dec	icblhz
		rts
.endp

;==========================================================================
; Set ZSBA to sector buffer.
;
; Entry:
;	X = sector buffer number
;
; Preserved:
;	Y
;
.proc DOSSetSectorPointer
		txa
		lsr
		pha
		lda		#0
		ror
		adc		#<dos_sectorbuffers
		sta		zsba
		pla
		adc		#>dos_sectorbuffers
		sta		zsba+1
		rts
.endp


;==========================================================================
; Invalidates all clean metadata buffers. Dirty metadata buffers are leftdoscp
; as-is.
;
; Modified:
;	A
;
; Preserved:
;	X, Y
;
.proc DOSInvalidateMetadataBuffers
		lda		#$ff
		bit		dos_secbuf_dirty
		smi:sta	dos_secbuf_dunit

		bit		dos_secbuf_dirty+1
		smi:sta	dos_secbuf_dunit+1
		rts
.endp

;==========================================================================
.proc DOSFlushMetadata
		;flush directory buffer
		lda		dos_secbuf_dirty
		bpl		dirbuf_clean
		ldx		#0
		jsr		DOSWriteSector
		bmi		error
dirbuf_clean:
		;flush VTOC buffer
		lda		dos_secbuf_dirty+1
		bpl		vtocbuf_clean
		ldx		#1
		jsr		DOSWriteSector
vtocbuf_clean:
error:
		rts
.endp

;==========================================================================
; Allocate sector.
;
; Finds a free sector by scanning the VTOC bitmap, allocates that sector
; in the bitmap, and returns the sector address.
;
; The sector count is updated but not used. DOS 2.0S will happily underflow
; this counter if it is out of sync with the bitmap.
;
.proc DOSAllocateSector
		;load VTOC
		ldx		#1
		lda		#<360
		ldy		#>360
		jsr		DOSReadSector
		bmi		xit

		;scan for a free sector (set bit in bitmap)
		ldy		#0
scan_loop:
		lda		dos_vtocbuffer+10,y
		bne		found_free_sector
		iny
		cpy		#90
		bne		scan_loop

		;no free sectors
		ldy		#CIOStatDiskFull
xit:
		rts

found_free_sector:
		;mark VTOC sector as dirty
		sec
		ror		dos_secbuf_dirty+1

		;stash the bitmap byte
		sta		zbufp

		;compute starting sector for this byte
		tya
		ldx		#0
		stx		zbufp+1
		ldx		#3
bitshift_loop:
		asl
		rol		zbufp+1
		dex
		bne		bitshift_loop
		tax

		;find first set bit starting from left
		lda		#$80
		dta		{bit $00}
bitscan_loop:
		lsr
		inx
		bit		zbufp
		beq		bitscan_loop
		dex

		;clear the bit in the VTOC
		eor		#$ff
		and		dos_vtocbuffer+10,y
		sta		dos_vtocbuffer+10,y

		;decrement the free sector count
		dew		dos_vtocbuffer+3

		;mark the VTOC dirty
		sec
		ror		dos_secbuf_dirty+1

		;stash the sector number
		stx		zbufp

		;all done
		ldy		#1
		rts
.endp

;==========================================================================
; Free sector.
;
; Entry:
;	Y:A = sector number
;
.proc DOSFreeSector
		;convert sector number to bitmap byte and offset
		tax
		sty		zbufp+1
		lsr		zbufp+1
		ror
		lsr		zbufp+1
		ror
		lsr		zbufp+1
		ror
		sta		zbufp+1
		txa
		and		#$07
		tax
		lda		bit_table,x
		sta		zbufp

		;load VTOC
		ldx		#1
		lda		#<360
		ldy		#>360
		jsr		DOSReadSector
		bmi		xit

		;mark VTOC sector as dirty
		sec
		ror		dos_secbuf_dirty+1

		;set the bit
		ldx		zbufp+1
		lda		dos_vtocbuffer+10,x
		ora		zbufp
		sta		dos_vtocbuffer+10,x

		;increment the free sector count
		inw		dos_vtocbuffer+3

		;all done
xit:
		rts

bit_table:
		dta		$80,$40,$20,$10,$08,$04,$02,$01
.endp

;==========================================================================
.proc DOSCloseIOCB1
		ldx		#$10
		mva		#CIOCmdClose iccmd+$10
		jmp		ciov
.endp

;==========================================================================
.proc DOSCPGetFilename
		;init dest counter
		ldy		#0

		;skip spaces
		ldx		dosvec_lnoff
space_loop:
		lda		dosvec_lnbuf,x
		inx
		cmp		#' '
		beq		space_loop
		dex

		;check if we've hit the end
		cmp		#$9b
		beq		fill_remainder

		;check if filename starts with x:
		lda		dosvec_lnbuf+1,x
		cmp		#':'
		beq		possibly_relative_prefix

		;check if filename starts with xn: -- we keep this
		sbc		#'1'-1
		cmp		#8
		bcs		no_drive_prefix
		lda		dosvec_lnbuf+2,x
		cmp		#':'
		bne		no_drive_prefix
		beq		has_drive_prefix

possibly_relative_prefix:
		;check if we had D:
		lda		dosvec_lnbuf,x
		cmp		#'D'

		;keep device specifier if it is for another device
		bne		has_drive_prefix

		;skip D:
		inx
		inx

no_drive_prefix:
		;add D1: (default drive specifier)
		lda		#'D'
		sta		dosvec_fnbuf
		lda		#'1'
		sta		dosvec_fnbuf+1
		lda		#':'
		sta		dosvec_fnbuf+2
		ldy		#3

has_drive_prefix:
		;copy up to 15 non-space, non-EOL chars
copy_loop:
		lda		dosvec_lnbuf,x
		cmp		#' '
		beq		xit
		cmp		#$9b
		beq		xit
		sta		dosvec_fnbuf,y
		inx
		iny
		cpy		#15
		bne		copy_loop
xit:

		stx		dosvec_lnoff

		;EOL fill the remainder of the filename (always at least one)
fill_remainder:
		lda		#$9b
fill_loop:
		sta		dosvec_fnbuf,y
		iny
		cpy		#16
		bne		fill_loop

		;return Z=0 if we have no filename, Z=1 if we do
		lda		dosvec_fnbuf
		cmp		#$9b
no_filename:
		rts
.endp

;==========================================================================
;CP

		icl		'cp.s'

;==========================================================================
dosvec_start:
		jmp		DOSRun				;$00 = regular (DOSVEC) entry
		jmp		DOSCPGetFilename	;$03 = JMP to get filename routine
		dta		$00
		dta		$00
		dta		$00
		dta		$00
dosvec_lnoff:
		dta		$00					;$0A = CP line offset (CPBUFP in OS/A+)
		dta		$00					;$0B = CP execute flags (CPEXFL in OS/A+)

dosvec_fnbuf = dosvec_start + $21	;$21 = CP filename buffer (28 bytes long per SDX manual)
dosvec_lnbuf = dosvec_start + $3F	;$3F = CP line buffer (64 bytes long per SDX manual)

dosvec_end = dosvec_lnbuf + $40

;==========================================================================

dos_sectorbuffers = dosvec_end
dos_dirbuffer = dos_sectorbuffers
dos_vtocbuffer = dos_sectorbuffers+$80

		.echo	'DOS base start: ', dosvec_start
		.echo	'DOS sector buffer start: ', dos_sectorbuffers
		.echo	'DOS sector buffer end (default): ', dos_sectorbuffers+$80*5

		.if dos_sectorbuffers+$80*5 > $1cfc
		.error	'DOS is too long -- exceeds DOS 2.0 limit of $1CFC'
		.endif

;==========================================================================

		end
