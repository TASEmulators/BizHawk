		org		$c000
		opt		f+

;==========================================================================
.proc bios_nmi
		rti
.endp

;==========================================================================
.proc bios_reset
		sei
		cld

		;check if the hardware cold boot flag is set
		lda		u_coldf
		bmi		cold_boot
		
		;nope, it's a warm boot... transfer to the OS kernel
		jmp		bios_os_boot

cold_boot:
		;yup, it's a cold boot... clear the cold boot flag
		mva		#0 u_coldf
		
		;transfer control to OS
		jmp		bios_os_boot
.endp

;==========================================================================
.proc bios_irq
		rti
.endp

;==========================================================================
.proc bios_os_boot
		;disable VBXE, SB, and flash writes
		mva		#$00 u_aux
		
		;turn on SDX cart -- we need it enabled to reserve cart memory so
		;the flasher doesn't barf
		mva		#$00 u_sdx

		ldx		#bios_reset_thunk.end-bios_reset_thunk-1
warm_copy_loop:
		mva		bios_reset_thunk,x $01fa,x
		dex
		bpl		warm_copy_loop
		
		;boot kernel ROM with 320K memory, SDX enabled, IORAM disabled, config locked
		ldx		#$81
		jmp		$01fa
.endp

;==========================================================================
.proc bios_reset_thunk
		stx		u_ctl
		jmp		(resvec)
end:
.endp

;==========================================================================

		org		$fffa
		dta		a(bios_nmi)
		dta		a(bios_reset)
		dta		a(bios_irq)

		opt		f-

