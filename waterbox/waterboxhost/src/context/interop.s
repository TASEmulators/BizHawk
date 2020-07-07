bits 64
org 0x35f00000000

struc Context
	.host_rsp resq 1
	.guest_rsp resq 1
	.dispatch_syscall resq 1
	.host_ptr resq 1
	.extcall_slots resq 64
endstruc

; sets up guest stack and calls a function
; r11 - guest entry point
; r10 - address of context structure
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
align 64, int3

; alternative to guest call thunks for functions with 0 args
; rdi - guest entry point
; rsi - address of context structure
call_guest_simple:
	mov r11, rdi
	mov r10, rsi
	jmp call_guest_impl
align 64, int3

; called by guest when it wishes to make a syscall
; must be loaded at fixed address, as that address is burned into guest executables
; rax - syscall number
; regular arg registers are 0..6 args to the syscall
guest_syscall:
	mov r10, [gs:0x18]
	mov [r10 + Context.guest_rsp], rsp
	mov rsp, [r10 + Context.host_rsp]
	sub rsp, 8 ; align
	mov r11, [r10 + Context.host_ptr]
	push r11 ; arg 8 to dispatch_syscall: host
	push rax ; arg 7 to dispatch_syscall: nr
	mov rax, [r10 + Context.dispatch_syscall]
	call rax
	mov r10, [gs:0x18]
	mov rsp, [r10 + Context.guest_rsp]
	ret
align 64, int3

; called by individual extcall thunks when the guest wishes to make an external call
; (very similar to guest_syscall)
; rax - slot number
; regular arg registers are 0..6 args to the extcall
guest_extcall_impl:
	mov r10, [gs:0x18]
	mov [r10 + Context.guest_rsp], rsp
	mov rsp, [r10 + Context.host_rsp]
	mov r11, [r10 + Context.extcall_slots + rax * 8] ; get slot ptr
	sub rsp, 8 ; align
	call r11
	mov r10, [gs:0x18]
	mov rsp, [r10 + Context.guest_rsp]
	ret
align 64, int3

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
