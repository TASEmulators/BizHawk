section .text
	global co_swap

align 16
co_swap:
	mov [rsi],rsp
	mov rsp,[rdi]
	pop rax
	mov [rsi+ 8],rbp
	mov [rsi+16],rbx
	mov [rsi+24],r12
	mov [rsi+32],r13
	mov [rsi+40],r14
	mov [rsi+48],r15
	mov rbp,[rdi+ 8]
	mov rbx,[rdi+16]
	mov r12,[rdi+24]
	mov r13,[rdi+32]
	mov r14,[rdi+40]
	mov r15,[rdi+48]
	jmp rax
