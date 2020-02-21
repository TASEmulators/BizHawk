;	Altirra - Atari 800/800XL/5200 emulator
;	Modular Kernel ROM - Hardware register definitions
;	Copyright (C) 2008-2016 Avery Lee
;
;	Copying and distribution of this file, with or without modification,
;	are permitted in any medium without royalty provided the copyright
;	notice and this notice are preserved.  This file is offered as-is,
;	without any warranty.

.ifndef f_HARDWARE_INC
.def f_HARDWARE_INC

;==========================================================================
; GTIA
;
m0pf	equ		$d000
m1pf	equ		$d001
m2pf	equ		$d002
m3pf	equ		$d003
p0pf	equ		$d004
p1pf	equ		$d005
p2pf	equ		$d006
p3pf	equ		$d007
m0pl	equ		$d008
m1pl	equ		$d009
m2pl	equ		$d00a
m3pl	equ		$d00b
p0pl	equ		$d00c
p1pl	equ		$d00d
p2pl	equ		$d00e
p3pl	equ		$d00f

hposp0	equ		$d000
hposp1	equ		$d001
hposp2	equ		$d002
hposp3	equ		$d003
hposm0	equ		$d004
hposm1	equ		$d005
hposm2	equ		$d006
hposm3	equ		$d007
sizep0	equ		$d008
sizep1	equ		$d009
sizep2	equ		$d00a
sizep3	equ		$d00b
sizem	equ		$d00c
grafp0	equ		$d00d
grafp1	equ		$d00e
grafp2	equ		$d00f
grafp3	equ		$d010
grafm	equ		$d011
trig0	equ		$d010
trig1	equ		$d011
trig2	equ		$d012
colpm0	equ		$d012
trig3	equ		$d013
colpm1	equ		$d013
pal		equ		$d014
colpm2	equ		$d014
colpm3	equ		$d015
colpf0	equ		$d016
colpf1	equ		$d017
colpf2	equ		$d018
colpf3	equ		$d019
colbk	equ		$d01a
prior	equ		$d01b
vdelay	equ		$d01c
gractl	equ		$d01d
hitclr	equ		$d01e
consol	equ		$d01f

;==========================================================================
; POKEY
;
pot0	equ	$d200
audf1	equ	$d200
pot1	equ	$d201
audc1	equ	$d201
pot2	equ	$d202
audf2	equ	$d202
pot3	equ	$d203
audc2	equ	$d203
pot4	equ	$d204
audf3	equ	$d204
pot5	equ	$d205
audc3	equ	$d205
pot6	equ	$d206
audf4	equ	$d206
pot7	equ	$d207
audc4	equ	$d207
audctl	equ	$d208
kbcode	equ	$d209
skres	equ	$d20a
potgo	equ	$d20b
serin	equ	$d20d
serout	equ	$d20d
irqen	equ	$d20e
irqst	equ	$d20e
skctl	equ	$d20f
skstat	equ	$d20f

;==========================================================================
; PIA
;
porta	equ	$d300
portb	equ	$d301
pactl	equ	$d302
pbctl	equ	$d303

;==========================================================================
; ANTIC
;
dmactl	equ	$d400
chactl	equ	$d401
dlistl	equ	$d402
dlisth	equ	$d403
hscrol	equ	$d404
vscrol	equ	$d405
pmbase	equ	$d407
chbase	equ	$d409
wsync	equ	$d40a
vcount	equ	$d40b
penh	equ	$d40c
penv	equ	$d40d
nmien	equ	$d40e
nmist	equ	$d40f
nmires	equ	$d40f

;==========================================================================
; 6502
;
nmivec	equ	$fffa
resvec	equ	$fffc
irqvec	equ	$fffe

.endif
