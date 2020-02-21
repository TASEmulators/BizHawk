;	Altirra - Atari 800/800XL/5200 emulator
;	Executable loader - on-board BASIC disable fragment
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

		icl		'kerneldb.inc'
		icl		'hardware.inc'
		icl		'cio.inc'

ciov = $e456

		org		$0680

main:
		;save CRB and switch to DDRB
		lda		pbctl
		pha
		and		#$fc
		sta		pbctl

		;check if port B is in output mode
		lda		portb
		and		#$02

		;bail if input
		bne		done

		;switch to ORB
		pla
		pha
		ora		#$04
		sta		pbctl

		;disable onboard BASIC
		lda		portb
		ora		#$02
		sta		portb

		;update BASICF flag to match
		lda		#$ff
		sta		basicf

		;check if $A000-BFFF is RAM and bail if not
		lda		$a000
		tax
		eor		#$ff
		sta		$a000
		cmp		$a000
		stx		$a000
		bne		done

		;reset memory top to $C000
		lda		#$c0
		sta		ramtop

		;reopen E:
		ldx		#0
		lda		#CIOCmdClose
		sta		iccmd
		jsr		ciov
		lda		#CIOCmdOpen
		sta		iccmd
		mwa		#e_dev icbal
		jsr		ciov

		;wait for stage 2 VBLANK to run once
		sei
		stx		consol
		cli
		lda		#8
		bit:rne		consol

done:
		;restore CRB
		pla
		sta		pbctl
		rts

e_dev:
		dta		'E',$9B

		ini		main
