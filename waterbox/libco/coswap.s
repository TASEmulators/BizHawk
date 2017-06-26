section .text
	global co_swap
	global __imp_co_swap

; TODO: how to tell GCC it doesn't need this?
align 16
__imp_co_swap:
	dq co_swap

align 16
co_swap:
	mov [rdx],rsp
	mov rsp,[rcx]
	pop rax
	mov [rdx+ 8],rbp
	mov [rdx+16],rsi
	mov [rdx+24],rdi
	mov [rdx+32],rbx
	mov [rdx+40],r12
	mov [rdx+48],r13
	mov [rdx+56],r14
	mov [rdx+64],r15
;#if !defined(LIBCO_NO_SSE)
	movaps [rdx+ 80],xmm6
	movaps [rdx+ 96],xmm7
	movaps [rdx+112],xmm8
	add rdx,112
	movaps [rdx+ 16],xmm9
	movaps [rdx+ 32],xmm10
	movaps [rdx+ 48],xmm11
	movaps [rdx+ 64],xmm12
	movaps [rdx+ 80],xmm13
	movaps [rdx+ 96],xmm14
	movaps [rdx+112],xmm15
;#endif
	mov rbp,[rcx+ 8]
	mov rsi,[rcx+16]
	mov rdi,[rcx+24]
	mov rbx,[rcx+32]
	mov r12,[rcx+40]
	mov r13,[rcx+48]
	mov r14,[rcx+56]
	mov r15,[rcx+64]
;#if !defined(LIBCO_NO_SSE)
	movaps xmm6, [rcx+ 80]
	movaps xmm7, [rcx+ 96]
	movaps xmm8, [rcx+112]
	add rcx,112
	movaps xmm9, [rcx+ 16]
	movaps xmm10,[rcx+ 32]
	movaps xmm11,[rcx+ 48]
	movaps xmm12,[rcx+ 64]
	movaps xmm13,[rcx+ 80]
	movaps xmm14,[rcx+ 96]
	movaps xmm15,[rcx+112]
;#endif
	jmp rax
