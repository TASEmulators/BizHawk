;	Altirra - Atari 800/800XL/5200 emulator
;	Modular Kernel ROM - Parallel Bus Interface routines
;	Copyright (C) 2008-2016 Avery Lee
;
;	Copying and distribution of this file, with or without modification,
;	are permitted in any medium without royalty provided the copyright
;	notice and this notice are preserved.  This file is offered as-is,
;	without any warranty.

.if _KERNEL_PBI_SUPPORT

		icl		'cio.inc'

;==========================================================================
; PBI device ROM entry points:
;	$D800	Checksum low (unused)
;	$D801	Checksum high (unused)
;	$D802	Revision (unused)
;	$D803	ID byte; must be $80.
;	$D804	Device type (unused)
;	$D805	JMP sio_vector
;	$D806		(cont.)
;	$D807		(cont.)
;	$D808	JMP irq_vector
;	$D809		(cont.)
;	$D80A		(cont.)
;	$D80B	ID byte; must be $91
;	$D80C	Device name (unused)
;	$D80D	CIO open vector - 1
;	$D80E		(cont.)
;	$D80F	CIO close vector - 1
;	$D810		(cont.)
;	$D811	CIO get byte vector - 1
;	$D812		(cont.)
;	$D813	CIO put byte vector - 1
;	$D814		(cont.)
;	$D815	CIO get status vector - 1
;	$D816		(cont.)
;	$D817	CIO special vector - 1
;	$D818		(cont.)
;	$D819	JMP init_vector
;	$D81A		(cont.)
;	$D81B		(cont.)
;

;==========================================================================
; Scan for PBI devices.
;
; The details:
;	- $D1FF is the PBI device select register; 1 selects a device.
;	- A selected device, if it exists, maps its ROM into $D800-DFFF, on
;	  top of the math pack.
;	- ID bytes are checked to ensure that a device ROM is actually present.
;	  (This means that the math pack must not match those bytes!)
;	- SHPDVS is the device selection shadow variable. It must match the
;	  value written to $D1FF whenever the PBI ROM is invoked so that the
;	  PBI device code can detect its ID.
;	- PDVMSK is the device enable mask and is used by SIO to call into
;	  PBI devices. Each bit in this mask indicates that a PBI device is
;	  available to possibly service requests. It is NOT set by this
;	  routine, but by the PBI device init code.
;
.proc PBIScan
		;set up interrupt handler
		mwa		#PBIHandleIRQ vpirq

		;begin scan
		lda		#$01
loop:
		;select next device
		sta		shpdvs
		sta		$d1ff
		
		;check ID bytes
		ldy		$d803
		cpy		#$80
		bne		invalid

		ldy		$d80b
		cpy		#$91
		bne		invalid
				
		;init PBI device
		jsr		$d819		
invalid:

		;next device
		lda		shpdvs
		asl
		bne		loop
		
		;deselect last device
		sta		shpdvs
		sta		$d1ff
		rts
.endp

;==========================================================================
; Entry:
;	A, X saved on stack
;	A = active PBI IRQ mask ($D1FF AND PDMSK)
;
; Exit:
;	A, X restored
;	Interrupt exit
;
.proc	PBIHandleIRQ
		;isolate lowest bit (ID of highest priority device)
		sta		jveck
		eor		#$ff
		clc
		adc		#1
		and		jveck

		;save off the current device mask and select interrupting device
		ldx		shpdvs
		sta		shpdvs
		sta		$d1ff		
		txa
		pha
		
		;call through interrupt vector
		jsr		$d808
		
		;restore previously (un)selected device
		pla
		sta		shpdvs
		sta		$d1ff
		
		;restore X, A and exit
		pla
		tax
		pla
		rti
.endp

;==========================================================================
.proc	PBIAttemptSIO
		lda		pdvmsk
		bne		begin_scan
fail:
		sta		shpdvs
		sta		$d1ff
		clc
		rts

begin_scan:
		lda		#0
		sec
loop:
		;advance to next device bit
		rol
		
		;check if we've scanned all 8 IDs
		bcs		fail
		
		;check if device exists
		bit		pdvmsk
		
		;keep scanning if not
		beq		loop
		
		;select the device
		sta		shpdvs
		sta		$d1ff
		
		;attempt I/O
		pha
		jsr		$d805
		pla
		
		;loop back if PBI handler didn't claim the I/O
		bcc		loop
		
		;deselect last device
		lda		#0
		sta		shpdvs
		sta		$d1ff
		
		;all done
		rts
.endp

;==========================================================================
.proc PBIGenericDeviceOpen
		ldy		#0
		bpl		PBIGenericDevicePutByte.vector_entry
.endp

;==========================================================================
.proc PBIGenericDeviceClose
		ldy		#2
		bpl		PBIGenericDevicePutByte.vector_entry
.endp

;==========================================================================
.proc PBIGenericDeviceGetByte
		ldy		#4
		bpl		PBIGenericDevicePutByte.vector_entry
.endp

;==========================================================================
.proc PBIGenericDevicePutByte
		ldy		#6
vector_entry:
		sty		reladr
		sta		reladr+1
		lda		#0
		sec
loop:
		;advance to next device bit
		rol
		
		;check if we've scanned all 8 IDs
		bcs		fail
		
		;check if device exists and keep scanning if not
		bit		pdvmsk
		beq		loop
		
		;select the device -- note that the write to SHPDVS *MUST* come
		;first, in case a PBI IRQ happens, since we do *NOT* have IRQs
		;masked here!
		sta		shpdvs
		sta		$d1ff
		
		;attempt I/O
		lda		ciochr
		jsr		dispatch
		
		;loop back if PBI handler didn't claim the I/O
		bcc		loop
		
		;all done
done:
		ldx		#0
		stx		shpdvs
		stx		$d1ff
		rts
		
fail:	
		ldy		#CIOStatUnkDevice
		bne		done
				
dispatch:
		ldy		reladr
		lda		$d80e,y
		pha
		lda		$d80d,y
		pha
		lda		reladr+1
		ldy		#CIOStatNotSupported
		rts

.endp

;==========================================================================
.proc PBIGenericDeviceGetStatus
		ldy		#8
		bpl		PBIGenericDevicePutByte.vector_entry
.endp

;==========================================================================
.proc PBIGenericDeviceSpecial
		ldy		#10
		bpl		PBIGenericDevicePutByte.vector_entry
.endp

;==========================================================================
.macro PBI_VECTOR_TABLE
		dta		a(PBIGenericDeviceOpen-1)
		dta		a(PBIGenericDeviceClose-1)
		dta		a(PBIGenericDeviceGetByte-1)
		dta		a(PBIGenericDevicePutByte-1)
		dta		a(PBIGenericDeviceGetStatus-1)
		dta		a(PBIGenericDeviceSpecial-1)
.endm

.endif
