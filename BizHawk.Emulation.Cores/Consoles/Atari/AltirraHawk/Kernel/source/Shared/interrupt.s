;	Altirra - Atari 800/800XL/5200 emulator
;	Modular Kernel ROM - Interrupt Handlers
;	Copyright (C) 2008-2016 Avery Lee
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
	
.if _KERNEL_XLXE
	;Required by XEGS carts to run since they have a clone of the XL/XE
	;OS in them.
	mva		trig3 gintlk
.endif

	rts
.endp

;==========================================================================
.proc IntDispatchNMI
	bit		nmist		;check nmi status
	bpl		not_dli		;skip if not a DLI
	jmp		(vdslst)	;jump to display list vector

.if !_KERNEL_XLXE
is_system_reset:
	jmp		warmsv
.endif

not_dli:
	pha
	
.if _KERNEL_XLXE
	;Only XL/XE OSes cleared the decimal bit.
	cld
.else
	;The stock OS treats 'not RNMI' as VBI. We'd best follow its example.
	lda		#$20
	bit		nmist
	bne		is_system_reset
.endif

	txa
	pha
	tya
	pha
	sta		nmires		;reset VBI interrupt
	jmp		(vvblki)	;jump through vblank immediate vector	
.endp

.proc IntDispatchIRQ
.if _KERNEL_XLXE
	cld
.endif
	jmp		(vimirq)
.endp

;==============================================================================
IntExitHandler_A = VBIProcess.exit_a
IntExitHandler_None = VBIProcess.exit_none
