;	Altirra - Atari 800/800XL/5200 emulator
;	Modular Kernel ROM - Self Test Entry Trampoline
;	Copyright (C) 2008-2016 Avery Lee
;
;	Copying and distribution of this file, with or without modification,
;	are permitted in any medium without royalty provided the copyright
;	notice and this notice are preserved.  This file is offered as-is,
;	without any warranty.

.proc SelfTestEntry
	lda		portb
	and		#$7f
	sta		portb
	jmp		BootScreen
.endp
