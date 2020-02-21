;	Altirra - Atari 800/800XL/5200 emulator
;	Modular Kernel ROM - 65C816 Interrupt Handlers
;	Copyright (C) 2008-2018 Avery Lee
;
;	Copying and distribution of this file, with or without modification,
;	are permitted in any medium without royalty provided the copyright
;	notice and this notice are preserved.  This file is offered as-is,
;	without any warranty.

;==========================================================================
; Dispatched from INTINV. Used by SpartaDOS X.
;
.proc IntInitInterrupts
	mva		#$40 nmien
	
	;Required by XEGS carts to run since they have a clone of the XL/XE
	;OS in them.
	mva		trig3 gintlk
	rts
.endp

;==========================================================================
.proc IntDispatchNmi
		bit		nmist		;check nmi status
		bpl		not_dli		;skip if not a DLI
		jmp		(vdslst)	;jump to display list vector

not_dli:
		pha
		cld
		phx
		phy
		sta		nmires		;reset VBI interrupt
		jmp		(vvblki)	;jump through vblank immediate vector	
.endp

;==============================================================================
.proc IntDispatchNativeNmi
		jmp		[vnmin]
.endp

;==============================================================================
.proc IntNativeNmiHandler
		;push and zero DBR
		phb
		phk
		plb

		;switch to 8-bit M/A
		sep		#$20

		;check for DLI
		bit		nmist
		bpl		not_dli

		;call DLI handler
		php
		jsr		dispatch_dli

		;restore DBR and exit
		plb
		rti

dispatch_dli:
		jmp		(vdslst)

not_dli:
		;save X and Y
		rep		#$10
		phx
		phy

		;save and clear D
		phd
		pea		#0
		pld

		;switch to 8-bit registers
		phk
		pea		#xit
		sep		#$30

		;re-save P/A/X/Y
		pha
		pha
		lda		13,s
		sta		2,s
		phx
		phy

		;call VBI handler
		jmp		(vvblki)

xit:
		;restore registers and exit
		pld
		rep		#$10
		ply
		plx
		plb
		rti

		jsl		dispatch_vbi
dispatch_vbi:
.endp

;==========================================================================
.proc IntDispatchNativeIrq
		jmp		[virqn]
.endp

;==========================================================================
.proc IntNativeIrqHandler
		;save A/X/Y
		pha
		phx
		phy

		;save M/X mode
		php

		;save D and switch to page zero
		phd
		pea		#0
		pld

		;save B and switch to bank zero
		phb
		phk
		plb

		;switch to m8x8 mode
		sep		#$30

		;call regular IRQ handler
		phk
		pea		#xit
		php

.def :IntDispatchIrq
		jmp		(vimirq)

xit:
		;restore B and D
		plb
		pld

		;restore M/X
		plp
		ply
		plx
		pla
		rti
.endp

;==============================================================================
.proc IntDispatchAbort
		jmp		(vabte)
.endp

;==============================================================================
.proc IntDispatchNativeAbort
		jmp		[vabtn]
.endp

;==============================================================================
.proc IntDispatchCop
		jmp		(vcope)
.endp

;==============================================================================
.proc IntDispatchNativeCop
		jmp		[vcopn]
.endp

;==============================================================================
.proc IntCopHandler
		rti
.endp

;==============================================================================
; Native COP #n interrupt handler
;
; Conditions when VCOP0/VCOPU/VCOPC called (like 816os):
;	- D saved, D=0
;	- DBR=0
;	- A/X/Y 16-bit, but NOT saved
;	- [$18] points to COP argument
;
.proc IntNativeCopHandler
		;save and reset D
		phd
		pea		#0
		pld

		;save and reset DBR
		phb
		phk
		plb

		;switch to 16-bit registers
		rep		#$30

		;move return address to $18
		pha
		lda		7,s
		dec
		sta		$18

		sep		#$20
		lda		9,s
		sta		$1a

		lda		[$18]
		beq		dispatch_0
		bmi		dispatch_c

dispatch_0:
		rep		#$20
		pla
		phk
		pea		#xit-1
		jmp		[vcop0]

xit:
		;restore B/D and exit
		plb
		pld
		rti

dispatch_u:
		rep		#$20
		pla
		phk
		pea		#xit-1
		jmp		[vcopu]

dispatch_c:
		rep		#$20
		pla
		phk
		pea		#xit-1
		jmp		[vcopc]
.endp

;==============================================================================
.proc IntDispatchNativeBreak
		jmp		[vbrkn]
.endp

;==============================================================================
IntExitHandler_A = VBIProcess.exit_a
IntExitHandler_None = VBIProcess.exit_none
