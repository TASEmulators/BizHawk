;	Altirra - Atari 800/800XL/5200 emulator
;	Modular Kernel ROM - 65C816 IRQ routines
;	Copyright (C) 2008-2018 Avery Lee
;
;	Copying and distribution of this file, with or without modification,
;	are permitted in any medium without royalty provided the copyright
;	notice and this notice are preserved.  This file is offered as-is,
;	without any warranty.

;==========================================================================
; _KERNEL_FAST_IRQ
;
; If set, expands the IRQ module slightly to save 12 cycles when ack'ing
; POKEY IRQs. This is still faster than the stock XL/XE OS. If cleared,
; an additional subroutine call is used to reduce code size.
;
.ifndef _KERNEL_FAST_IRQ
	_KERNEL_FAST_IRQ = 0
.endif

;==========================================================================
.if _KERNEL_FAST_IRQ
_ACK_IRQ .macro
	sta		irqen
	lda		pokmsk
	sta		irqen
.endm
.else
_ACK_IRQ .macro
	jsr		IrqAcknowledge
.endm
.endif

;==========================================================================
; The canonical IRQ priority order for the XL/XE is:
;	- Serial input ready ($20)
;	- PBI devices
;	- Serial output ready ($10)
;	- Serial output complete ($08)
;	- Timer 1 ($01)
;	- Timer 2 ($02)
;	- Timer 4 ($04)
;	- Keyboard ($80)
;	- Break ($40)
;	- PIA proceed
;	- PIA interrupt
;	- BRK instruction
;
IRQHandler = _IRQHandler._entry
.proc _IRQHandler
.if _KERNEL_PBI_SUPPORT
check_pbi:
	;check if a device interrupt is active
	and		$d1ff
	beq		no_pbi_interrupt

	;save X
	phx
	
	;jump through PBI interrupt vector
	jmp		(vpirq)
.endif

dispatch_serout:
	lda		#$ef
	_ACK_IRQ
	jmp		(vseror)

check_seroc:
	bit		irqst
	bne		not_seroc
dispatch_seroc:
	jmp		(vseroc)

dispatch_timer1:
	lda		#$fe
	_ACK_IRQ
	jmp		(vtimr1)

dispatch_timer2:
	lda		#$fd
	_ACK_IRQ
	jmp		(vtimr2)

dispatch_timer4:
	lda		#$fb
	_ACK_IRQ
	jmp		(vtimr4)

_entry:
	pha
	
	;check for serial input ready IRQ
	lda		#$20
	bit		irqst
	bne		not_serin
	lda		#$df
	sta		irqen
	lda		pokmsk
	sta		irqen
	jmp		(vserin)
not_serin:

	.if _KERNEL_PBI_SUPPORT
	;check for PBI devices requiring interrupt handling
	lda		pdmsk
	bne		check_pbi
no_pbi_interrupt:
	.endif

	;check for serial output ready IRQ
	lda		#$10
	bit		irqst
	beq		dispatch_serout

	;check for serial output complete (not a latch, so must mask)
	lsr
	bit		pokmsk
	bne		check_seroc
not_seroc:

	lda		irqst
	lsr
	bcc		dispatch_timer1
	lsr
	bcc		dispatch_timer2
	lsr
	bcc		dispatch_timer4
	bit		irqst
	bvc		dispatch_keyboard
	bpl		dispatch_break

	;check for serial bus proceed line
	bit		pactl
	bmi		dispatch_pia_irqa

	;check for serial bus interrupt line
	bit		pbctl
	bmi		dispatch_pia_irqb

	;check for break instruction
	lda		2,s
	and		#$10
	beq		not_brk
	jmp		(vbreak)
not_brk:
	pla
	rti
	

dispatch_keyboard:
	lda		#$bf
	_ACK_IRQ
	jmp		(vkeybd)

dispatch_break:
	lda		#$7f
	_ACK_IRQ
	jmp		(brkky)

dispatch_pia_irqa:
	;clear serial bus proceed interrupt
	lda		porta
	jmp		(vprced)

dispatch_pia_irqb:
	;clear serial bus interrupt interrupt
	lda		portb
	jmp		(vinter)
		
.endp

;==========================================================================
.if !_KERNEL_FAST_IRQ
.proc IrqAcknowledge
	sta		irqen
	lda		pokmsk
	sta		irqen
	rts
.endp
.endif
