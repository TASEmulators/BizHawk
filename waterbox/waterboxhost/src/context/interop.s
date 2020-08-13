bits 64
org 0x35f00000000

struc Context
	.thread_area resq 1
	.host_rsp resq 1
	.guest_rsp resq 1
	.dispatch_syscall resq 1
	.host_ptr resq 1
	.extcall_slots resq 64
endstruc

struc SavedRegs
	.xmm resq 32
	.rbx resq 1
	.rbp resq 1
	.r12 resq 1
	.r13 resq 1
	.r14 resq 1
	.r15 resq 1
	.fenv resd 7
	.mxcsr resd 1
	.sizeof resq 0
endstruc

times 0-($-$$) int3 ; CALL_GUEST_IMPL_ADDR
; sets up guest stack and calls a function
; r11 - guest entry point
; r10 - address of context structure
; regular arg registers are 0..6 args passed through to guest
call_guest_impl:
	; save host TIB data
	mov rax, [gs:0x08]
	push rax
	mov rax, [gs:0x10]
	push rax

	; set guest TIB data
	xor rax, rax
	mov [gs:0x10], rax
	sub rax, 1
	mov [gs:0x08], rax

	mov [gs:0x18], r10
	mov [r10 + Context.host_rsp], rsp
	mov rsp, [r10 + Context.guest_rsp]
	call r11 ; stack hygiene note - this host address is saved on the guest stack
	mov r10, [gs:0x18]
	mov [r10 + Context.guest_rsp], rsp ; restore stack so next call using same Context will work
	mov rsp, [r10 + Context.host_rsp]
	mov r11, 0
	mov [gs:0x18], r11

	; restore host TIB data
	pop r10
	mov [gs:0x10], r10
	pop r10
	mov [gs:0x08], r10

	ret

times 0x80-($-$$) int3
; called by guest when it wishes to make a syscall
; must be loaded at fixed address, as that address is burned into guest executables
; rax - syscall number
; regular arg registers are 0..6 args to the syscall
guest_syscall:
	; save on the guest side all nonvolatiles, and volatiles we're suspicious about
	sub rsp, SavedRegs.sizeof + 8 ; +8 to align
	movdqa [rsp + SavedRegs.xmm + 0x00], xmm0
	movdqa [rsp + SavedRegs.xmm + 0x10], xmm1
	movdqa [rsp + SavedRegs.xmm + 0x20], xmm2
	movdqa [rsp + SavedRegs.xmm + 0x30], xmm3
	movdqa [rsp + SavedRegs.xmm + 0x40], xmm4
	movdqa [rsp + SavedRegs.xmm + 0x50], xmm5
	movdqa [rsp + SavedRegs.xmm + 0x60], xmm6
	movdqa [rsp + SavedRegs.xmm + 0x70], xmm7
	movdqa [rsp + SavedRegs.xmm + 0x80], xmm8
	movdqa [rsp + SavedRegs.xmm + 0x90], xmm9
	movdqa [rsp + SavedRegs.xmm + 0xa0], xmm10
	movdqa [rsp + SavedRegs.xmm + 0xb0], xmm11
	movdqa [rsp + SavedRegs.xmm + 0xc0], xmm12
	movdqa [rsp + SavedRegs.xmm + 0xd0], xmm13
	movdqa [rsp + SavedRegs.xmm + 0xe0], xmm14
	movdqa [rsp + SavedRegs.xmm + 0xf0], xmm15
	mov [rsp + SavedRegs.rbx], rbx
	mov [rsp + SavedRegs.rbp], rbp
	mov [rsp + SavedRegs.r12], r12
	mov [rsp + SavedRegs.r13], r13
	mov [rsp + SavedRegs.r14], r14
	mov [rsp + SavedRegs.r15], r15
	fnstenv [rsp + SavedRegs.fenv]
	stmxcsr [rsp + SavedRegs.mxcsr]

	; swap stacks
	mov r10, [gs:0x18]
	mov [r10 + Context.guest_rsp], rsp
	mov rsp, [r10 + Context.host_rsp]

	; restore host TIB data
	mov r11, [rsp]
	mov [gs:0x10], r11
	mov r11, [rsp + 8]
	mov [gs:0x08], r11

	sub rsp, 8 ; align
	mov r11, [r10 + Context.host_ptr]
	push r11 ; arg 8 to dispatch_syscall: host
	push rax ; arg 7 to dispatch_syscall: nr
	mov rax, [r10 + Context.dispatch_syscall]
	call rax

	; set guest TIB data
	xor r10, r10
	mov [gs:0x10], r10
	sub r10, 1
	mov [gs:0x08], r10

	; swap stacks
	mov r10, [gs:0x18]
	mov rsp, [r10 + Context.guest_rsp]

	; restore guest nonvolatiles
	movdqa xmm0, [rsp + SavedRegs.xmm + 0x00]
	movdqa xmm1, [rsp + SavedRegs.xmm + 0x10]
	movdqa xmm2, [rsp + SavedRegs.xmm + 0x20]
	movdqa xmm3, [rsp + SavedRegs.xmm + 0x30]
	movdqa xmm4, [rsp + SavedRegs.xmm + 0x40]
	movdqa xmm5, [rsp + SavedRegs.xmm + 0x50]
	movdqa xmm6, [rsp + SavedRegs.xmm + 0x60]
	movdqa xmm7, [rsp + SavedRegs.xmm + 0x70]
	movdqa xmm8, [rsp + SavedRegs.xmm + 0x80]
	movdqa xmm9, [rsp + SavedRegs.xmm + 0x90]
	movdqa xmm10, [rsp + SavedRegs.xmm + 0xa0]
	movdqa xmm11, [rsp + SavedRegs.xmm + 0xb0]
	movdqa xmm12, [rsp + SavedRegs.xmm + 0xc0]
	movdqa xmm13, [rsp + SavedRegs.xmm + 0xd0]
	movdqa xmm14, [rsp + SavedRegs.xmm + 0xe0]
	movdqa xmm15, [rsp + SavedRegs.xmm + 0xf0]
	mov rbx, [rsp + SavedRegs.rbx]
	mov rbp, [rsp + SavedRegs.rbp]
	mov r12, [rsp + SavedRegs.r12]
	mov r13, [rsp + SavedRegs.r13]
	mov r14, [rsp + SavedRegs.r14]
	mov r15, [rsp + SavedRegs.r15]
	fldenv [rsp + SavedRegs.fenv]
	ldmxcsr [rsp + SavedRegs.mxcsr]
	add rsp, SavedRegs.sizeof + 8 ; +8 to align

	ret

times 0x300-($-$$) int3 ; SETUP_CALL_FRAME_ADDR
; Set up a stack frame for a new thread, so that a syscall return can happen
; from it for the first time
; rdi - guest's desired rsp
; rsi - guest's desired entry point
; rax - adjusted guest rsp, should be set up in context when we want to start
; execution on this guest 
setup_call_frame:
	; assume that rdi is 16 byte aligned, which our musl code provides
	sub rdi, 16
	mov [rdi + 8], rsi

	sub rdi, SavedRegs.sizeof
	mov rcx, SavedRegs.sizeof / 8
	xor rax, rax
	rep stosq

	sub rdi, SavedRegs.sizeof
	mov rax, 0x37f
	mov [rdi + SavedRegs.fenv], rax
	mov rax, 0xffff
	mov [rdi + SavedRegs.fenv + 8], rax
	mov eax, 0x1f80
	mov [rdi + SavedRegs.mxcsr], eax

	mov rax, rdi
	ret

times 0x380-($-$$) int3 ; CALL_GUEST_SIMPLE_ADDR
; alternative to guest call thunks for functions with 0 args
; rdi - guest entry point
; rsi - address of context structure
call_guest_simple:
	mov r11, rdi
	mov r10, rsi
	jmp call_guest_impl

times 0x400-($-$$) int3 ; EXTCALL_THUNK_ADDR
; individual thunks to each of 64 call slots
; should be in fixed locations for memory hygiene in the core, since they may be stored there for some time
%macro guest_extcall_thunk 1
	mov rax, %1
	jmp guest_extcall_impl
	align 16, int3
%endmacro
%assign j 0
%rep 64
	guest_extcall_thunk j
	%assign j j+1
%endrep

; called by individual extcall thunks when the guest wishes to make an external call
; (very similar to guest_syscall)
; rax - slot number
; regular arg registers are 0..6 args to the extcall
guest_extcall_impl:
	mov r10, [gs:0x18]
	mov [r10 + Context.guest_rsp], rsp
	mov rsp, [r10 + Context.host_rsp]

	; restore host TIB data
	mov r11, [rsp]
	mov [gs:0x10], r11
	mov r11, [rsp + 8]
	mov [gs:0x08], r11

	mov r11, [r10 + Context.extcall_slots + rax * 8] ; get slot ptr
	sub rsp, 8 ; align
	call r11

	; set guest TIB data
	xor r10, r10
	mov [gs:0x10], r10
	sub r10, 1
	mov [gs:0x08], r10

	mov r10, [gs:0x18]
	mov rsp, [r10 + Context.guest_rsp]
	ret
