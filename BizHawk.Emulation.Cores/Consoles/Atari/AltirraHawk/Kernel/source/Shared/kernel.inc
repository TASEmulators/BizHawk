;	Altirra - Atari 800/800XL/5200 emulator
;	Modular Kernel ROM - Kernel vectors
;	Copyright (C) 2008-2016 Avery Lee
;
;	Copying and distribution of this file, with or without modification,
;	are permitted in any medium without royalty provided the copyright
;	notice and this notice are preserved.  This file is offered as-is,
;	without any warranty.

.ifndef f_KERNEL_INC
.def f_KERNEL_INC

;==========================================================================
; Math pack
;
afp		= $d800
fasc	= $d8e6
ipf		= $d9aa
fpi		= $d9d2
zfr0	= $da44
zf1		= $da46
zfl		= $da48
fsub	= $da60
fadd	= $da66
fmul	= $dadb
fdiv	= $db28
plyevl	= $dd40
fld0r	= $dd89
fld0p	= $dd8d
fld1r	= $dd98
fld1p	= $dd9c
fst0r	= $dda7
fst0p	= $ddab
fmove	= $ddb6
exp		= $ddc0
exp10	= $ddcc
log		= $decd
log10	= $ded1

;==========================================================================
; Main kernel ROM
;
editrv	= $e400
screnv	= $e410
keybdv	= $e420
printv	= $e430
casetv	= $e440
diskiv	= $e450
dskinv	= $e453
ciov	= $e456
siov	= $e459
setvbv	= $e45c
sysvbv	= $e45f
xitvbv	= $e462
sioinv	= $e465
sendev	= $e468
intinv	= $e46b
cioinv	= $e46e
blkbdv	= $e471
warmsv	= $e474
coldsv	= $e477
rblokv	= $e47a
csopiv	= $e47d
pupdiv	= $e480		;XL/XE: power-up display
slftsv	= $e483		;XL/XE: self-test entry
pentv	= $e486		;XL/XE: peripheral handler add
phunlv	= $e489		;XL/XE: peripheral handler remove
phiniv	= $e48c		;XL/XE: peripheral handler init
gpdvv	= $e48f		;XL/XE: Generic device vector

.endif
