bits 64
org 0x35f00000000

struc Context
	.tid resd 1
	.__padding resd 1
	; thread pointer as set by guest libc (pthread_self, etc)
	.thread_area resq 1
	; used by set_tid_address
	.clear_child_tid resq 1
	; a lock that this thread is waiting on
	.park_addr resq 1
	; Data structure shared between all threads that describes how to call out in this guest
	.context_call_info resq 1
	; Used internally to track the host's most recent rsp when transitioned to Waterbox code.
	.host_rsp resq 1
	; Sets the guest's starting rsp, and used internally to track the guest's most recent rsp when transitioned to extcall or syscall
	.guest_rsp resq 1

	; things only relevant to guest threads 
	; saved guest call data
	.rax resq 1
	.rdi resq 1
	.rsi resq 1
	.rdx resq 1
	.rcx resq 1
	.r8 resq 1
	.r9 resq 1
	; saved guest nonvolatiles (besides rsp, which is above)
	.rbx resq 1
	.rbp resq 1
	.r12 resq 1
	.r13 resq 1
	.r14 resq 1
	.r15 resq 1
endstruc

struc ContextCallInfo
	.dispatch_syscall resq 1
	.host_ptr resq 1
	.extcall_slots resq 64
endstruc


times 0-($-$$) int3
; sets up guest stack and calls a function for the main guest thread
; r11 - guest entry point
; r10 - address of context structure.  tid should be 1, because this is the main thread
; regular arg registers are 0..6 args passed through to guest
call_guest_impl:
	mov [gs:0x18], r10
	mov [r10 + Context.host_rsp], rsp
	mov rsp, [r10 + Context.guest_rsp]
	call r11 ; stack hygiene note - this host address is saved on the guest stack
	mov r10, [gs:0x18]
	mov [r10 + Context.guest_rsp], rsp ; restore stack so next call using same Context will work
	mov rsp, [r10 + Context.host_rsp]
	mov r11, 0
	mov [gs:0x18], r11
	ret

times 0x40-($-$$) int3
; alternative to call_guest_impl+thunks for functions with 0 args
; rdi - guest entry point
; rsi - address of context structure.  tid should be 1, because this is the main thread
call_guest_simple:
	mov r11, rdi
	mov r10, rsi
	jmp call_guest_impl

times 0x80-($-$$) int3
; called by guest when it wishes to make a syscall
; must be loaded at fixed address, as that address is burned into guest executables
; rax - syscall number
; regular arg registers are 0..6 args to the syscall
guest_syscall:
	mov r10, [gs:0x18]
	mov [r10 + Context.guest_rsp], rsp
	mov rsp, [r10 + Context.host_rsp]
	mov r11d, [r10 + Context.tid]
	cmp r11d, 1
	je guest_syscall_main_thread
	guest_syscall_guest_thread:
		mov r11, r10
		add r11, Context.rax
		; NR
		mov [r11 + Context.rax - Context.rax], rax
		; args
		mov [r11 + Context.rdi - Context.rax], rdi
		mov [r11 + Context.rsi - Context.rax], rsi
		mov [r11 + Context.rdx - Context.rax], rdx
		mov [r11 + Context.rcx - Context.rax], rcx
		mov [r11 + Context.r8 - Context.rax], r8
		mov [r11 + Context.r9 - Context.rax], r9
		; nonvolatiles
		mov [r11 + Context.rbx - Context.rax], rbx
		mov [r11 + Context.rbp - Context.rax], rbp
		mov [r11 + Context.r12 - Context.rax], r12
		mov [r11 + Context.r13 - Context.rax], r13
		mov [r11 + Context.r14 - Context.rax], r14
		mov [r11 + Context.r15 - Context.rax], r15
		pop r15
		pop r14
		pop r13
		pop r12
		pop rbp
		pop rbx
		ret
	guest_syscall_main_thread:
		sub rsp, 8 ; align
		mov r10, [r10 + Context.context_call_info]
		mov r11, [r10 + ContextCallInfo.host_ptr]
		push r11 ; arg 8 to dispatch_syscall: host
		push rax ; arg 7 to dispatch_syscall: nr
		mov rax, [r10 + ContextCallInfo.dispatch_syscall]
		call rax
		mov r10, [gs:0x18]
		mov rsp, [r10 + Context.guest_rsp]
		ret

times 0x120-($-$$) int3
; run some code in a guest thread
; rdi - address of context structure.  tid should not be 1, because this is not the main thread
enter_guest_thread:
	mov r10, rdi
	mov [gs:0x18], r10
	push rbx
	push rbp
	push r12
	push r13
	push r14
	push r15
	mov [r10 + Context.host_rsp], rsp
	mov rsp, [r10 + Context.guest_rsp]
	mov r11, r10
	add r11, Context.rax
	; return from syscall
	mov rax, [r11 + Context.rax - Context.rax]
	; arg restore
	; could do this limit entropy
	; mov rdi, [r11 + Context.rdi - Context.rax]
	; mov rsi, [r11 + Context.rsi - Context.rax]
	; mov rdx, [r11 + Context.rdx - Context.rax]
	; mov rcx, [r11 + Context.rcx - Context.rax]
	; mov r8, [r11 + Context.r8 - Context.rax]
	; mov r9, [r11 + Context.r9 - Context.rax]
	; nonvolatiles
	mov rbx, [r11 + Context.rbx - Context.rax]
	mov rbp, [r11 + Context.rbp - Context.rax]
	mov r12, [r11 + Context.r12 - Context.rax]
	mov r13, [r11 + Context.r13 - Context.rax]
	mov r14, [r11 + Context.r14 - Context.rax]
	mov r15, [r11 + Context.r15 - Context.rax]
	ret

; individual thunks to each of 64 call slots
; should be in fixed locations for memory hygiene in the core, since they may be stored there for some time

%macro guest_extcall_thunk 1
	mov rax, %1
	jmp guest_extcall_impl
	align 16, int3
%endmacro

times 0x200-($-$$) int3
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
	mov r11, [r10 + Context.context_call_info]
	mov r11, [r11 + ContextCallInfo.extcall_slots + rax * 8] ; get slot ptr
	sub rsp, 8 ; align
	call r11
	mov r10, [gs:0x18]
	mov rsp, [r10 + Context.guest_rsp]
	ret
