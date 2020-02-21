;	Altirra - Atari 800/800XL emulator
;	Rapidus OS placeholder
;	Copyright (C) 2008-2017 Avery Lee
;
;	Copying and distribution of this file, with or without modification,
;	are permitted in any medium without royalty provided the copyright
;	notice and this notice are preserved.  This file is offered as-is,
;	without any warranty.

		icl		'hardware.inc'

		opt		h-o-f-

		org		$c000
		opt		o+f+
		dta		$ff
	
;==========================================================================
		org		$E000

		ins		'atarifont.bin'

		org		$E450
		jmp		dummy
		jmp		dummy
		jmp		dummy
		jmp		dummy
		jmp		dummy
		jmp		dummy
		jmp		dummy
		jmp		dummy
		jmp		dummy
		jmp		dummy
		jmp		dummy
		jmp		dummy

		org		$E474
warmsv	jmp		main

		org		$E477
coldsv	jmp		main

		org		$E4C0
		rts

		;Make sure we clear ALL standard OS vectors.
		org		$E4C1

.proc	dummy
		rts
.endp

.proc	main
		cld
		ldx		#$ff
		txs

		;reset chipset
		ldx		#0
		txa
reset_loop:
		sta		$D000,x
		sta		$D200,x
		sta		$D400,x
		inx
		bne		reset_loop
		
		;wait for vertical blank
		lda		#124
		cmp:rne	vcount
	
		;set up display list
		mwa		#display_list dlistl
	
		;set up character set and colors
		mva		#$e0 chbase
		mva		#$02 chactl
		mva		#$ca colpf1
		mva		#$94 colpf2
		mva		#$00 colbk

		;turn on display DMA, normal playfield width
		mva		#$22 dmactl
	
		;jam the system
		jmp		*
.endp
	
;==========================================================================

		org		$f000

display_list:
		dta		$70
		dta		$70
		dta		$70
		dta		$42,a(message)
		dta		$30
		dta		$02
		dta		$02
		dta		$02
		dta		$30
		dta		$02
		dta		$41,a(display_list)
	
message:
		;		 0123456789012345678901234567890123456789
		dta		"Altirra Rapidus OS ROM                  "
		dta		"This is a placeholder for the normal    "
		dta		"Rapidus OS ROM. Run a flash updater to  "
		dta		"install a real 65C816 OS in this place. "
		dta		"System halted                           "

;==========================================================================
	
		org		$fffa
		dta		a(0)
		dta		a(main)
		dta		a(0)

		end
