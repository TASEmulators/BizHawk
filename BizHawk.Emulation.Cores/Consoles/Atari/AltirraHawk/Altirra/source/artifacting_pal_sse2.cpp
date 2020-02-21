//	Altirra - Atari 800/800XL/5200 emulator
//	PAL artifacting acceleration - x86 SSE2
//	Copyright (C) 2009-2011 Avery Lee
//
//	This program is free software; you can redistribute it and/or modify
//	it under the terms of the GNU General Public License as published by
//	the Free Software Foundation; either version 2 of the License, or
//	(at your option) any later version.
//
//	This program is distributed in the hope that it will be useful,
//	but WITHOUT ANY WARRANTY; without even the implied warranty of
//	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//	GNU General Public License for more details.
//
//	You should have received a copy of the GNU General Public License
//	along with this program; if not, write to the Free Software
//	Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.

#include <stdafx.h>

#ifdef VD_COMPILER_MSVC
	#pragma warning(disable: 4733)	// warning C4733: Inline asm assigning to 'FS:0' : handler not registered as safe handler
#endif

#ifdef VD_CPU_X86

void __declspec(naked) __stdcall ATArtifactPALLuma_SSE2(uint32 *dst, const uint8 *src, uint32 n, const uint32 *kernels) {
	__asm {
		push	ebp
		push	edi
		push	esi
		push	ebx

		push	0
		push	fs:dword ptr [0]
		mov		fs:dword ptr [0], esp

		mov		esi, [esp+12+24]
		shr		esi, 3
		mov		edi, [esp+16+24]
		mov		ebp, [esp+4+24]
		mov		esp, [esp+8+24]
		pxor	xmm0, xmm0
xloop:
		movzx	eax, byte ptr [esp]
		movzx	ebx, byte ptr [esp+1]
		movzx	ecx, byte ptr [esp+2]
		movzx	edx, byte ptr [esp+3]

		shl		eax, 8
		shl		ebx, 8
		shl		ecx, 8
		shl		edx, 8

		paddw	xmm0, [edi+eax]
		movdqa	xmm1, [edi+eax+16]
		paddw	xmm0, [edi+ebx+32]
		paddw	xmm1, [edi+ebx+48]
		paddw	xmm0, [edi+ecx+64]
		paddw	xmm1, [edi+ecx+80]
		paddw	xmm0, [edi+edx+96]
		paddw	xmm1, [edi+edx+112]
		movdqa	[ebp], xmm0

		movzx	eax, byte ptr [esp+4]
		movzx	ebx, byte ptr [esp+5]
		movzx	ecx, byte ptr [esp+6]
		movzx	edx, byte ptr [esp+7]
		add		esp, 8
		shl		eax, 8
		shl		ebx, 8
		shl		ecx, 8
		shl		edx, 8

		paddw	xmm1, [edi+eax+128]
		movdqa	xmm0, [edi+eax+144]
		paddw	xmm1, [edi+ebx+160]
		paddw	xmm0, [edi+ebx+176]
		paddw	xmm1, [edi+ecx+192]
		paddw	xmm0, [edi+ecx+208]
		paddw	xmm1, [edi+edx+224]
		paddw	xmm0, [edi+edx+240]

		movdqa	[ebp+16], xmm1
		add		ebp, 32

		dec		esi
		jne		xloop

		movdqa	[ebp], xmm0

		mov		esp, fs:dword ptr [0]
		pop		eax
		pop		ecx

		pop		ebx
		pop		esi
		pop		edi
		pop		ebp
		ret		16
	}
}

void __declspec(naked) __stdcall ATArtifactPALLumaTwin_SSE2(uint32 *dst, const uint8 *src, uint32 n, const uint32 *kernels) {
	__asm {
		push	ebp
		push	edi
		push	esi
		push	ebx

		push	0
		push	fs:dword ptr [0]
		mov		fs:dword ptr [0], esp

		mov		esi, [esp+12+24]
		shr		esi, 3
		mov		edi, [esp+16+24]
		mov		ebp, [esp+4+24]
		mov		esp, [esp+8+24]
		pxor	xmm0, xmm0
xloop:
		movzx	eax, byte ptr [esp]
		movzx	ecx, byte ptr [esp+2]

		shl		eax, 7
		shl		ecx, 7

		paddw	xmm0, [edi+eax]
		movdqa	xmm1, [edi+eax+16]
		paddw	xmm0, [edi+ecx+32]
		paddw	xmm1, [edi+ecx+48]
		movdqa	[ebp], xmm0

		movzx	eax, byte ptr [esp+4]
		movzx	ecx, byte ptr [esp+6]
		add		esp, 8
		shl		eax, 7
		shl		ecx, 7

		paddw	xmm1, [edi+eax+64]
		movdqa	xmm0, [edi+eax+80]
		paddw	xmm1, [edi+ecx+96]
		paddw	xmm0, [edi+ecx+112]

		movdqa	[ebp+16], xmm1
		add		ebp, 32

		dec		esi
		jne		xloop

		movdqa	[ebp], xmm0

		mov		esp, fs:dword ptr [0]
		pop		eax
		pop		ecx

		pop		ebx
		pop		esi
		pop		edi
		pop		ebp
		ret		16
	}
}

void __declspec(naked) __stdcall ATArtifactPALChroma_SSE2(uint32 *dst, const uint8 *src, uint32 n, const uint32 *kernels) {
	__asm {
		push	ebp
		push	edi
		push	esi
		push	ebx

		push	0
		push	fs:dword ptr [0]
		mov		fs:dword ptr [0], esp

		mov		esi, [esp+12+24]
		mov		edi, [esp+16+24]
		mov		ebp, [esp+4+24]
		mov		esp, [esp+8+24]
		pxor	xmm0, xmm0
		pxor	xmm1, xmm1
		pxor	xmm2, xmm2
		jmp		entry

		align	16
xloop:
		movzx	eax, byte ptr [esp]
		movzx	ebx, byte ptr [esp+1]
		movzx	ecx, byte ptr [esp+2]
		movzx	edx, byte ptr [esp+3]
		shl		eax, 9
		shl		ebx, 9
		shl		ecx, 9
		shl		edx, 9
		paddw	xmm0, [edi+eax+0*64]
		paddw	xmm1, [edi+eax+0*64+16]
		paddw	xmm2, [edi+eax+0*64+32]
		movdqa	xmm3, [edi+eax+0*64+48]
		paddw	xmm0, [edi+ebx+1*64]
		paddw	xmm1, [edi+ebx+1*64+16]
		paddw	xmm2, [edi+ebx+1*64+32]
		paddw	xmm3, [edi+ebx+1*64+48]
		paddw	xmm0, [edi+ecx+2*64]
		paddw	xmm1, [edi+ecx+2*64+16]
		paddw	xmm2, [edi+ecx+2*64+32]
		paddw	xmm3, [edi+ecx+2*64+48]
		paddw	xmm0, [edi+edx+3*64]
		paddw	xmm1, [edi+edx+3*64+16]
		paddw	xmm2, [edi+edx+3*64+32]
		paddw	xmm3, [edi+edx+3*64+48]
		movdqa	[ebp], xmm0

		movzx	eax, byte ptr [esp+4]
		movzx	ebx, byte ptr [esp+5]
		movzx	ecx, byte ptr [esp+6]
		movzx	edx, byte ptr [esp+7]
		shl		eax, 9
		shl		ebx, 9
		shl		ecx, 9
		shl		edx, 9
		paddw	xmm1, [edi+eax+4*64]
		paddw	xmm2, [edi+eax+4*64+16]
		paddw	xmm3, [edi+eax+4*64+32]
		movdqa	xmm0, [edi+eax+4*64+48]
		paddw	xmm1, [edi+ebx+5*64]
		paddw	xmm2, [edi+ebx+5*64+16]
		paddw	xmm3, [edi+ebx+5*64+32]
		paddw	xmm0, [edi+ebx+5*64+48]
		paddw	xmm1, [edi+ecx+6*64]
		paddw	xmm2, [edi+ecx+6*64+16]
		paddw	xmm3, [edi+ecx+6*64+32]
		paddw	xmm0, [edi+ecx+6*64+48]
		paddw	xmm1, [edi+edx+7*64]
		paddw	xmm2, [edi+edx+7*64+16]
		paddw	xmm3, [edi+edx+7*64+32]
		paddw	xmm0, [edi+edx+7*64+48]
		movdqa	[ebp+16], xmm1

		movzx	eax, byte ptr [esp+8]
		movzx	ebx, byte ptr [esp+9]
		movzx	ecx, byte ptr [esp+10]
		movzx	edx, byte ptr [esp+11]
		shl		eax, 9
		shl		ebx, 9
		shl		ecx, 9
		shl		edx, 9
		paddw	xmm2, [edi+eax+0*64]
		paddw	xmm3, [edi+eax+0*64+16]
		paddw	xmm0, [edi+eax+0*64+32]
		movdqa	xmm1, [edi+eax+0*64+48]
		paddw	xmm2, [edi+ebx+1*64]
		paddw	xmm3, [edi+ebx+1*64+16]
		paddw	xmm0, [edi+ebx+1*64+32]
		paddw	xmm1, [edi+ebx+1*64+48]
		paddw	xmm2, [edi+ecx+2*64]
		paddw	xmm3, [edi+ecx+2*64+16]
		paddw	xmm0, [edi+ecx+2*64+32]
		paddw	xmm1, [edi+ecx+2*64+48]
		paddw	xmm2, [edi+edx+3*64]
		paddw	xmm3, [edi+edx+3*64+16]
		paddw	xmm0, [edi+edx+3*64+32]
		paddw	xmm1, [edi+edx+3*64+48]
		movdqa	[ebp+32], xmm2

		movzx	eax, byte ptr [esp+12]
		movzx	ebx, byte ptr [esp+13]
		movzx	ecx, byte ptr [esp+14]
		movzx	edx, byte ptr [esp+15]
		shl		eax, 9
		add		esp, 16
		shl		ebx, 9
		shl		ecx, 9
		shl		edx, 9
		paddw	xmm3, [edi+eax+4*64]
		paddw	xmm0, [edi+eax+4*64+16]
		paddw	xmm1, [edi+eax+4*64+32]
		movdqa	xmm2, [edi+eax+4*64+48]
		paddw	xmm3, [edi+ebx+5*64]
		paddw	xmm0, [edi+ebx+5*64+16]
		paddw	xmm1, [edi+ebx+5*64+32]
		paddw	xmm2, [edi+ebx+5*64+48]
		paddw	xmm3, [edi+ecx+6*64]
		paddw	xmm0, [edi+ecx+6*64+16]
		paddw	xmm1, [edi+ecx+6*64+32]
		paddw	xmm2, [edi+ecx+6*64+48]
		paddw	xmm3, [edi+edx+7*64]
		paddw	xmm0, [edi+edx+7*64+16]
		paddw	xmm1, [edi+edx+7*64+32]
		paddw	xmm2, [edi+edx+7*64+48]
		movdqa	[ebp+48], xmm3
		add		ebp, 64

entry:
		sub		esi, 16
		jns		xloop

		test	esi, 8
		jmp		noodd

		movzx	eax, byte ptr [esp]
		movzx	ebx, byte ptr [esp+1]
		movzx	ecx, byte ptr [esp+2]
		movzx	edx, byte ptr [esp+3]
		shl		eax, 9
		shl		ebx, 9
		shl		ecx, 9
		shl		edx, 9
		paddw	xmm0, [edi+eax+0*64]
		paddw	xmm1, [edi+eax+0*64+16]
		paddw	xmm2, [edi+eax+0*64+32]
		movdqa	xmm3, [edi+eax+0*64+48]
		paddw	xmm0, [edi+ebx+1*64]
		paddw	xmm1, [edi+ebx+1*64+16]
		paddw	xmm2, [edi+ebx+1*64+32]
		paddw	xmm3, [edi+ebx+1*64+48]
		paddw	xmm0, [edi+ecx+2*64]
		paddw	xmm1, [edi+ecx+2*64+16]
		paddw	xmm2, [edi+ecx+2*64+32]
		paddw	xmm3, [edi+ecx+2*64+48]
		paddw	xmm0, [edi+edx+3*64]
		paddw	xmm1, [edi+edx+3*64+16]
		paddw	xmm2, [edi+edx+3*64+32]
		paddw	xmm3, [edi+edx+3*64+48]
		movdqa	[ebp], xmm0

		movzx	eax, byte ptr [esp+4]
		movzx	ebx, byte ptr [esp+5]
		movzx	ecx, byte ptr [esp+6]
		movzx	edx, byte ptr [esp+7]
		shl		eax, 9
		shl		ebx, 9
		shl		ecx, 9
		shl		edx, 9
		paddw	xmm1, [edi+eax+4*64]
		paddw	xmm2, [edi+eax+4*64+16]
		paddw	xmm3, [edi+eax+4*64+32]
		movdqa	xmm0, [edi+eax+4*64+48]
		paddw	xmm1, [edi+ebx+5*64]
		paddw	xmm2, [edi+ebx+5*64+16]
		paddw	xmm3, [edi+ebx+5*64+32]
		paddw	xmm0, [edi+ebx+5*64+48]
		paddw	xmm1, [edi+ecx+6*64]
		paddw	xmm2, [edi+ecx+6*64+16]
		paddw	xmm3, [edi+ecx+6*64+32]
		paddw	xmm0, [edi+ecx+6*64+48]
		paddw	xmm1, [edi+edx+7*64]
		paddw	xmm2, [edi+edx+7*64+16]
		paddw	xmm3, [edi+edx+7*64+32]
		paddw	xmm0, [edi+edx+7*64+48]
		movdqa	[ebp+16], xmm1
		movdqa	[ebp+32], xmm2
		jmp		short xit

noodd:
		movdqa	[ebp], xmm0

xit:
		mov		esp, fs:dword ptr [0]
		pop		eax
		pop		ecx

		pop		ebx
		pop		esi
		pop		edi
		pop		ebp
		ret		16
	}
}

void __declspec(naked) __stdcall ATArtifactPALChromaTwin_SSE2(uint32 *dst, const uint8 *src, uint32 n, const uint32 *kernels) {
	__asm {
		push	ebp
		push	edi
		push	esi
		push	ebx

		push	0
		push	fs:dword ptr [0]
		mov		fs:dword ptr [0], esp

		mov		esi, [esp+12+24]
		mov		edi, [esp+16+24]
		mov		ebp, [esp+4+24]
		mov		esp, [esp+8+24]
		pxor	xmm0, xmm0
		pxor	xmm1, xmm1
		pxor	xmm2, xmm2
		jmp		entry

		align	16
xloop:
		movzx	eax, byte ptr [esp]
		movzx	ecx, byte ptr [esp+2]
		shl		eax, 8
		shl		ecx, 8
		paddw	xmm0, [edi+eax+0*64]
		paddw	xmm1, [edi+eax+0*64+16]
		paddw	xmm2, [edi+eax+0*64+32]
		movdqa	xmm3, [edi+eax+0*64+48]
		paddw	xmm0, [edi+ecx+1*64]
		paddw	xmm1, [edi+ecx+1*64+16]
		paddw	xmm2, [edi+ecx+1*64+32]
		paddw	xmm3, [edi+ecx+1*64+48]
		movdqa	[ebp], xmm0

		movzx	eax, byte ptr [esp+4]
		movzx	ecx, byte ptr [esp+6]
		shl		eax, 8
		shl		ecx, 8
		paddw	xmm1, [edi+eax+2*64]
		paddw	xmm2, [edi+eax+2*64+16]
		paddw	xmm3, [edi+eax+2*64+32]
		movdqa	xmm0, [edi+eax+2*64+48]
		paddw	xmm1, [edi+ecx+3*64]
		paddw	xmm2, [edi+ecx+3*64+16]
		paddw	xmm3, [edi+ecx+3*64+32]
		paddw	xmm0, [edi+ecx+3*64+48]
		movdqa	[ebp+16], xmm1

		movzx	eax, byte ptr [esp+8]
		movzx	ecx, byte ptr [esp+10]
		shl		eax, 8
		shl		ecx, 8
		paddw	xmm2, [edi+eax+0*64]
		paddw	xmm3, [edi+eax+0*64+16]
		paddw	xmm0, [edi+eax+0*64+32]
		movdqa	xmm1, [edi+eax+0*64+48]
		paddw	xmm2, [edi+ecx+1*64]
		paddw	xmm3, [edi+ecx+1*64+16]
		paddw	xmm0, [edi+ecx+1*64+32]
		paddw	xmm1, [edi+ecx+1*64+48]
		movdqa	[ebp+32], xmm2

		movzx	eax, byte ptr [esp+12]
		movzx	ecx, byte ptr [esp+14]
		shl		eax, 8
		add		esp, 16
		shl		ecx, 8
		paddw	xmm3, [edi+eax+2*64]
		paddw	xmm0, [edi+eax+2*64+16]
		paddw	xmm1, [edi+eax+2*64+32]
		movdqa	xmm2, [edi+eax+2*64+48]
		paddw	xmm3, [edi+ecx+3*64]
		paddw	xmm0, [edi+ecx+3*64+16]
		paddw	xmm1, [edi+ecx+3*64+32]
		paddw	xmm2, [edi+ecx+3*64+48]
		movdqa	[ebp+48], xmm3
		add		ebp, 64
entry:
		sub		esi, 16
		jns		xloop

		test	esi, 8
		jz		noodd
		
		movzx	eax, byte ptr [esp]
		movzx	ecx, byte ptr [esp+2]
		shl		eax, 8
		shl		ecx, 8
		paddw	xmm0, [edi+eax+0*64]
		paddw	xmm1, [edi+eax+0*64+16]
		paddw	xmm2, [edi+eax+0*64+32]
		movdqa	xmm3, [edi+eax+0*64+48]
		paddw	xmm0, [edi+ecx+1*64]
		paddw	xmm1, [edi+ecx+1*64+16]
		paddw	xmm2, [edi+ecx+1*64+32]
		paddw	xmm3, [edi+ecx+1*64+48]
		movdqa	[ebp], xmm0

		movzx	eax, byte ptr [esp+4]
		movzx	ecx, byte ptr [esp+6]
		shl		eax, 8
		shl		ecx, 8
		paddw	xmm1, [edi+eax+2*64]
		paddw	xmm2, [edi+eax+2*64+16]
		paddw	xmm3, [edi+eax+2*64+32]
		movdqa	xmm0, [edi+eax+2*64+48]
		paddw	xmm1, [edi+ecx+3*64]
		paddw	xmm2, [edi+ecx+3*64+16]
		paddw	xmm3, [edi+ecx+3*64+32]
		paddw	xmm0, [edi+ecx+3*64+48]
		movdqa	[ebp+16], xmm1
		movdqa	[ebp+32], xmm2

		jmp		short xit

noodd:
		movdqa	[ebp], xmm0

xit:
		mov		esp, fs:dword ptr [0]
		pop		eax
		pop		ecx

		pop		ebx
		pop		esi
		pop		edi
		pop		ebp
		ret		16
	}
}

void __declspec(naked) __stdcall ATArtifactPALFinal_SSE2(uint32 *dst, const uint32 *ybuf, const uint32 *ubuf, const uint32 *vbuf, uint32 *ulbuf, uint32 *vlbuf, uint32 n) {
	static const __declspec(align(8)) sint16 kCoeffs[4]={
		-3182*4, -3182*4,	// -co_ug / co_ub * 16384 * 4
		-8346*4+0x10000, -8346*4+0x10000	// -co_vg / co_vr * 16384 * 4, wrapped around
	};

	__asm {
		push	ebp
		push	edi
		push	esi
		push	ebx

		mov		eax, [esp+12+16]	;ubuf
		mov		ebx, [esp+16+16]	;vbuf
		mov		ecx, [esp+20+16]	;ulbuf
		mov		edx, [esp+24+16]	;vlbuf
		mov		esi, [esp+28+16]	;n
		mov		edi, [esp+4+16]		;dst
		mov		ebp, [esp+8+16]		;ybuf

		shr		esi, 2

		movq	xmm6, qword ptr kCoeffs
		pshufd	xmm7, xmm6, 01010101b
		pshufd	xmm6, xmm6, 0
xloop:
		movdqa	xmm0, [ecx]			;read prev U
		movdqa	xmm1, [eax+16]		;read current U
		add		eax, 16
		paddw	xmm0, xmm1			;add (average) U
		movdqa	[ecx], xmm1			;update prev U
		add		ecx, 16

		movdqa	xmm2, [edx]			;read prev V
		movdqa	xmm3, [ebx+16]		;read current V
		add		ebx, 16
		paddw	xmm2, xmm3			;add (average) V
		movdqa	[edx], xmm3			;update prev V
		add		edx, 16

		movdqa	xmm4, [ebp]			;read current Y
		add		ebp, 16

		movdqa	xmm1, xmm0
		movdqa	xmm3, xmm2
		pmulhw	xmm0, xmm6			;compute U impact on green
		pmulhw	xmm2, xmm7			;compute V impact on green
		psubsw	xmm2, xmm3

		paddw	xmm0, xmm2
		paddw	xmm1, xmm4			;U + Y = blue
		paddw	xmm0, xmm4			;green
		paddw	xmm3, xmm4			;V + Y = red

		psraw	xmm0, 6
		psraw	xmm1, 6
		psraw	xmm3, 6

		packuswb	xmm0, xmm0
		packuswb	xmm1, xmm1
		packuswb	xmm3, xmm3
		punpcklbw	xmm1, xmm3
		punpcklbw	xmm0, xmm0
		movdqa		xmm3, xmm1
		punpcklbw	xmm1, xmm0
		punpckhbw	xmm3, xmm0

		movdqa	[edi], xmm1
		movdqa	[edi+16], xmm3
		add		edi, 32

		dec		esi
		jne		xloop

		pop		ebx
		pop		esi
		pop		edi
		pop		ebp
		ret		28
	}
}

#endif
