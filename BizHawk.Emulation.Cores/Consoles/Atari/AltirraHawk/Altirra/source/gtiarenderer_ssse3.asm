		section		.rdata
		align		64
window_table:
		dq			0, 0, -1, -1, 0, 0
color_table_preshuffle:
		db			08h, 04h, 05h, 05h, 06h, 06h, 06h, 06h
		db			07h, 07h, 07h, 07h, 07h, 07h, 07h, 07h
hires_splat_pf1:
		dq			0505050505050505h,0505050505050505h
hires_mask_1:
		dq			0f000f000f000f00h,0f000f000f000f00h
hires_mask_2:
		dq			0f0f00000f0f0000h,0f0f00000f0f0000h

		section		.code

;==========================================================================
;
; Inputs:
;	void *dst,
;	const uint8 *src,
;	uint32 n,
;	const uint8 *color_table
;
		global		_atasm_gtia_render_lores_fast_ssse3
_atasm_gtia_render_lores_fast_ssse3:
		push		ebp
		push		edi
		push		esi
		push		ebx

		mov			eax, [esp+16+16]
		mov			ecx, [esp+8+16]
		mov			edi, [esp+4+16]
		movdqa		xmm2, [eax]
		pshufb		xmm2, oword [color_table_preshuffle]

		;check if n==0
		mov			edx, [esp+12+16]
		or			edx, edx
		jz			.xit

		;compute srcEnd = src + n
		mov			ebx, ecx
		add			ebx, edx
		mov			[esp+12+16], ebx
		
		;check if we have a start offset
		mov			edx, ecx
		and			edx, 15
		
		;remove start offset
		sub			edi, edx
		sub			ecx, edx
		sub			edi, edx
		
		;check if we have overlapping start and stop masks
		mov			esi, ebx
		xor			esi, [esp+8+16]
		and			esi, 0fffffff0h
		jz			.dosingle

		;process start section
		or			edx, edx
		jz			.xstart

		xor			edx, 15
		inc			edx
		call		.domask
		
.xstart:
		mov			eax, ebx
		sub			eax, ecx
		sub			eax, 16
		js			.endcheck
.xloop:
		movdqa		xmm0, [ecx]
		add			ecx, 16
		movdqa		xmm1, xmm2
		pshufb		xmm1, xmm0
		movdqa		xmm0, xmm1
		punpcklbw	xmm0, xmm0
		punpckhbw	xmm1, xmm1
		movdqa		[edi], xmm0
		movdqa		[edi+16], xmm1
		add			edi, 32
		sub			eax, 16
		jns			.xloop
.endcheck:
		and			eax, 15
		jz			.xit
		
		mov			edx, 32
		sub			edx, eax
		call		.domask
		
.xit:
		sfence
		pop			ebx
		pop			esi
		pop			edi
		pop			ebp
		ret

.dosingle:
		xor			edx, 15
		and			ebx, 15
		xor			ebx, 15
		movq		xmm4, qword [window_table+edx+1]
		movq		xmm5, qword [window_table+edx+9]
		movq		xmm0, qword [window_table+ebx+17]
		movq		xmm1, qword [window_table+ebx+25]
		pand		xmm4, xmm0
		pand		xmm5, xmm1
		call		.domask2
		jmp			short .xit

.domask:
		movq		xmm4, qword [window_table+edx]
		movq		xmm5, qword [window_table+edx+8]
.domask2:
		punpcklbw	xmm4, xmm4
		punpcklbw	xmm5, xmm5
		movdqa		xmm0, [ecx]
		add			ecx, 16
		movdqa		xmm1, xmm2
		pshufb		xmm1, xmm0
		movdqa		xmm0, xmm1
		punpcklbw	xmm0, xmm0
		punpckhbw	xmm1, xmm1
		movdqa		xmm6, [edi]
		movdqa		xmm7, [edi+16]
		pand		xmm0, xmm4
		pand		xmm1, xmm5
		pandn		xmm4, xmm6
		pandn		xmm5, xmm7
		por			xmm0, xmm4
		por			xmm1, xmm5
		movdqa		[edi], xmm0
		movdqa		[edi+16], xmm1
		add			edi, 32
		ret

;==========================================================================
;
; Inputs:
;	void *dst,
;	const uint8 *src,
;	const uint8 *hiressrc,
;	uint32 n,
;	const uint8 *color_table
;
		global		_atasm_gtia_render_mode8_fast_ssse3
_atasm_gtia_render_mode8_fast_ssse3:
		push		ebp
		push		edi
		push		esi
		push		ebx

		mov			eax, [esp+20+16]
		mov			ebp, [esp+12+16]
		mov			ecx, [esp+8+16]
		mov			edi, [esp+4+16]
		movdqa		xmm0, [eax]
		movdqa		xmm4, xmm0
		pshufb		xmm0, oword [color_table_preshuffle]
		pshufb		xmm4, oword [hires_splat_pf1]
		movdqa		xmm5, xmm4
		movdqa		xmm1, xmm0
		movdqa		xmm2, [hires_mask_2]
		movdqa		xmm3, [hires_mask_1]
		pandn		xmm2, xmm0
		pandn		xmm3, xmm1
		pand		xmm4, [hires_mask_2]
		pand		xmm5, [hires_mask_1]
		por			xmm2, xmm4
		por			xmm3, xmm5

		;check if n==0
		mov			edx, [esp+16+16]
		or			edx, edx
		jz			.xit

		;compute srcEnd = src + n
		mov			ebx, ecx
		add			ebx, edx
		mov			[esp+16+16], ebx
		
		;check if we have a start offset
		mov			edx, ecx
		and			edx, 15
		
		;remove start offset
		sub			edi, edx
		sub			ecx, edx
		sub			edi, edx
		sub			ebp, edx
		
		;check if we have overlapping start and stop masks
		mov			esi, ebx
		xor			esi, [esp+8+16]
		and			esi, 0fffffff0h
		jz			.dosingle

		;process start section
		or			edx, edx
		jz			.xstart

		xor			edx, 15
		inc			edx
		call		.domask
		
.xstart:
		mov			eax, ebx
		sub			eax, ecx
		sub			eax, 16
		js			.endcheck
.xloop:
		movdqa		xmm0, xmm2
		movdqa		xmm1, xmm3
		movdqa		xmm6, [ecx]
		por			xmm6, [ebp]
		add			ecx, 16
		add			ebp, 16
		pshufb		xmm0, xmm6
		pshufb		xmm1, xmm6
		movdqa		xmm7, xmm0
		punpcklbw	xmm0, xmm1
		punpckhbw	xmm7, xmm1
		movdqa		[edi], xmm0
		movdqa		[edi+16], xmm7
		add			edi, 32
		sub			eax, 16
		jns			.xloop
.endcheck:
		and			eax, 15
		jz			.xit
		
		mov			edx, 32
		sub			edx, eax
		call		.domask
		
.xit:
		sfence
		pop			ebx
		pop			esi
		pop			edi
		pop			ebp
		ret

.dosingle:
		xor			edx, 15
		and			ebx, 15
		xor			ebx, 15
		movq		xmm4, qword [window_table+edx+1]
		movq		xmm5, qword [window_table+edx+9]
		movq		xmm0, qword [window_table+ebx+17]
		movq		xmm1, qword [window_table+ebx+25]
		pand		xmm4, xmm0
		pand		xmm5, xmm1
		call		.domask2
		jmp			short .xit

.domask:
		movq		xmm4, qword [window_table+edx]
		movq		xmm5, qword [window_table+edx+8]
.domask2:
		punpcklbw	xmm4, xmm4
		punpcklbw	xmm5, xmm5
		movdqa		xmm0, xmm2
		movdqa		xmm1, xmm3
		movdqa		xmm6, [ecx]
		por			xmm6, [ebp]
		add			ecx, 16
		add			ebp, 16
		pshufb		xmm0, xmm6
		pshufb		xmm1, xmm6
		movdqa		xmm7, xmm0
		punpcklbw	xmm0, xmm1
		punpckhbw	xmm7, xmm1
		movdqa		xmm6, [edi]
		movdqa		xmm1, [edi+16]
		pand		xmm0, xmm4
		pand		xmm7, xmm5
		pandn		xmm4, xmm6
		pandn		xmm5, xmm1
		por			xmm0, xmm4
		por			xmm7, xmm5
		movdqa		[edi], xmm0
		movdqa		[edi+16], xmm7
		add			edi, 32
		ret

		end
