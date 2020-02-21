;	Altirra - Atari 800/800XL/5200 emulator
;	Parallel Bus Interface (PBI) disk hook firmware
;	Copyright (C) 2008-2016 Avery Lee
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

		org		$D800
		opt		h-f+

		dta		a(0)			;checksum (unused)
		dta		0				;revision (unused)
		dta		$80				;ID byte
		dta		0				;device type (unused)
		jmp		SIOHandler		;SIO vector
		jmp		IRQHandler		;IRQ vector
		dta		$91				;ID byte
		dta		0				;device name (unused)
		dta		a(CIOOpen)
		dta		a(CIOClose)
		dta		a(CIOGetByte)
		dta		a(CIOPutByte)
		dta		a(CIOGetStatus)
		dta		a(CIOSpecial)
		jmp		Init

;==========================================================================
.proc Init
		lda		shpdvs			;get current mask
		ora		pdvmsk			;merge into enabled mask
		sta		pdvmsk
		rts
.endp

;==========================================================================
.proc SIOHandler
		;Poke into magic register. Actual contents don't matter. Emulator
		;will magically read registers and memory and write registers and
		;memory.
		sta		$dcef
		rts
.endp

;==========================================================================
.proc IRQHandler
		rts
.endp

;==========================================================================
CIOOpen:
CIOClose:
CIOGetByte:
CIOPutByte:
CIOGetStatus:
CIOSpecial:
		rts

;==========================================================================

		org		$DBFF
		dta		0
