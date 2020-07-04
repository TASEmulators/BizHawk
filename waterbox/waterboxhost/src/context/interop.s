bits 64
org 0x36a00000000

%macro save_ctx 1
	mov [%1], rsp
	mov [%1 + 0x08], rbp
	mov [%1 + 0x10], rbx
	mov [%1 + 0x18], r12
	mov [%1 + 0x20], r13
	mov [%1 + 0x28], r14
	mov [%1 + 0x30], r15
%endmacro

%macro load_ctx 1
	mov rsp, [%1]
	mov rbp, [%1 + 0x08]
	mov rbx, [%1 + 0x10]
	mov r12, [%1 + 0x18]
	mov r13, [%1 + 0x20]
	mov r14, [%1 + 0x28]
	mov r15, [%1 + 0x30]
%endmacro

%macro save_args 2
	%if %2 > 0
		mov [%1 + 0x08], rdi
	%endif
	%if %2 > 1
		mov [%1 + 0x10], rsi
	%endif
	%if %2 > 2
		mov [%1 + 0x18], rdx
	%endif
	%if %2 > 3
		mov [%1 + 0x20], rcx
	%endif
	%if %2 > 4
		mov [%1 + 0x28], r8
	%endif
	%if %2 > 5
		mov [%1 + 0x30], r9
	%endif
%endmacro

%macro load_args 2
	%if %2 > 0
		mov rdi, [%1 + 0x08]
	%endif
	%if %2 > 1
		mov rsi, [%1 + 0x10]
	%endif
	%if %2 > 2
		mov rdx, [%1 + 0x18]
	%endif
	%if %2 > 3
		mov rcx, [%1 + 0x20]
	%endif
	%if %2 > 4
		mov r8, [%1 + 0x28]
	%endif
	%if %2 > 5
		mov r9, [%1 + 0x30]
	%endif
%endmacro

%macro guest_syscall 1
	; guest initiates syscall
	; NR in rax, 0..6 args

	mov r10, [gs:0x18] ; context, context.host

	mov r11, r10
	add r11, 0x38 ; context.guest
	save_ctx r11

	add r11, 0x38 ; context.call
	mov [r11], rax ; syscall number
	save_args r11, %1

	load_ctx r10

	ret
%endmacro

syscall0:
	guest_syscall 0
align 256, int3

syscall1:
	guest_syscall 1
align 256, int3

syscall2:
	guest_syscall 2
align 256, int3

syscall3:
	guest_syscall 3
align 256, int3

syscall4:
	guest_syscall 4
align 256, int3

syscall5:
	guest_syscall 5
align 256, int3

syscall6:
	guest_syscall 6
align 256, int3

depart:
	; host starts new guest thread
	; context.guest.rsp set to stack limit - 0x10
	; [context.guest.rsp] set to entry point
	; guest args in args

	mov r10, [gs:0x18] ; context, context.host
	save_ctx r10
	; TODO: Useful to do a full load_ctx with zeroed values here?
	mov rsp, [r10 + 0x38]
	mov r11, arrive
	mov [rsp + 8], r11
	ret
align 32, int3

arrive:
	; guest returns from depart
	; return to host value in rax
	mov r10, [gs:0x18] ; context, context.host
	mov r11, -1 ; Fake syscall number
	mov [r10 + 0x70], r11

	load_ctx r10
	ret
align 32, int3

anyret:
	; host returns the results of a callback, extcall or guest_syscall, to guest
	; one argument, the return value

	mov r10, [gs:0x18] ; context, context.host
	save_ctx r10

	mov r11, r10
	add r11, 0x38 ; context.guest
	load_ctx r11
	mov rax, rdi
	ret
align 256, int3

%macro extcall 2
	; guest initiates external call
	; call number is %1 (constant), 0..6 args
	mov rax, 0x8000000000000000 + %1
	jmp syscall%2
%endmacro

%macro extcall_group 1
	%assign i 0
	%rep 7
		extcall %1, i
		align 16, int3
		%assign i i+1
	%endrep
	align 128, int3
%endmacro

%assign j 0
%rep 64
	extcall_group j
	%assign j j+1
%endrep
