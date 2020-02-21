;	Altirra - Atari 800/800XL/5200 emulator
;	Executable loader - normal speed version
;	Copyright (C) 2008-2015 Avery Lee
;
;	This program is free software; you can redistribute it and/or modify
;	it under the terms of the GNU General Public License as published by
;	the Free Software Foundation; either version 2 of the License, or
;	(at your option) any later version.
;
;	This program is distributed in the hope that it will be useful,
;	but WITHOUT ANY WARRANTY; without even the implied warranty of
;	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
;	GNU General Public License for more details.
;
;	You should have received a copy of the GNU General Public License
;	along with this program; if not, write to the Free Software
;	Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.

runad	equ		$02e0
initad	equ		$02e2
siov	equ		$e459

ddevic	equ		$0300
dunit	equ		$0301
dbuflo	equ		$0304
dstats	equ		$0303
daux1	equ		$030a

		org		BASEADDR
		opt		h-f+

base:
		dta		0
		dta		1
		dta		a(BASEADDR)
		dta		a($e4c0)
read_loop:
		ldx		#2
		mva:rpl	init_block,x ddevic,x-

		ldx		#8
		jsr		do_read

		lda		param_block2+3
		beq		doRun

		mwa		#dummy initad
		ldx		#17
		jsr		do_read

		jsr		doInit

		jmp		read_loop

do_read:
		ldy		#8
copy_loop:
		lda		param_block,x
		sta		dstats,y
		dex
		dey
		bpl		copy_loop

retry_loop:
		jsr		siov
		bmi		retry_loop

		inc		param_block + 7
dummy:
		rts

doInit:
		jmp		(initad)

doRun:
		jmp		(runad)

init_block:
		dta		$ff
		dta		$00
		dta		$52

param_block:
		dta		$40, a(param_block2+1), 8, 0, 8, 0, 0, 0
param_block2:
		dta		$40
		.ds		8

.if * > BASEADDR+$80
		.error "Loader too long"
.endif

.if * < BASEADDR+$80
		org		BASEADDR+$7F
		dta		0
.endif