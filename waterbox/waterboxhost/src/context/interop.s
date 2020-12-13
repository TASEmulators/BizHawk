bits 64
org 0x35f00000000
%define RVA(addr) (addr - 0x35f00000000)

struc Context
	.thread_area resq 1
	.host_rsp resq 1
	.guest_rsp resq 1
	.dispatch_syscall resq 1
	.host_ptr resq 1
	.extcall_slots resq 64
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
	push rbp ; this call might be suspended and cothreaded.  the guest knows to save nonvolatiles if it needs to, except rbp
	mov r10, [gs:0x18]
	mov [r10 + Context.guest_rsp], rsp
	mov rsp, [r10 + Context.host_rsp]

	; restore host TIB data
	mov r11, [rsp]
	mov [gs:0x10], r11
	mov r11, [rsp + 8]
	mov [gs:0x08], r11

	; save and then null out SubSystemTib
	push r10
	xor r11, r11
	mov [gs:0x18], r11

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

	; Restore SubSystemTib (aka context ptr)
	mov r10, [rsp + 16]
	mov [gs:0x18], r10

	mov rsp, [r10 + Context.guest_rsp]
	pop rbp
	ret
guest_syscall_end:

times 0x100-($-$$) int3 ; CALL_GUEST_SIMPLE_ADDR
; alternative to guest call thunks for functions with 0 args
; rdi - guest entry point
; rsi - address of context structure
call_guest_simple:
	mov r11, rdi
	mov r10, rsi
	jmp call_guest_impl

times 0x200-($-$$) int3 ; EXTCALL_THUNK_ADDR
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

	; save and then null out SubSystemTib
	push r10
	xor r11, r11
	mov [gs:0x18], r11

	mov r11, [r10 + Context.extcall_slots + rax * 8] ; get slot ptr
	call r11

	; set guest TIB data
	xor r10, r10
	mov [gs:0x10], r10
	sub r10, 1
	mov [gs:0x08], r10

	; Restore SubSystemTib (aka context ptr)
	mov r10, [rsp]
	mov [gs:0x18], r10

	mov rsp, [r10 + Context.guest_rsp]
	ret
guest_extcall_impl_end:

times 0x700-($-$$) int3 ; RUNTIME_TABLE_ADDR
runtime_function_table:
	; https://docs.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-runtime_function
	dd RVA(guest_syscall)
	dd RVA(guest_syscall_end)
	dd RVA(guest_syscall_unwind)

	dd RVA(guest_extcall_impl)
	dd RVA(guest_extcall_impl_end)
	dd RVA(guest_extcall_impl_unwind)
guest_syscall_unwind:
	; https://docs.microsoft.com/en-us/cpp/build/exception-handling-x64
	db 1
	db 5 ; fake prolog
	db 1
	db 0

	db 5 ; fake prolog offset
	db 0x42 ; 40 bytes of stack
	dw 0 ; unused entry
guest_extcall_impl_unwind:
	db 1
	db 5 ; fake prolog
	db 1
	db 0

	db 5 ; fake prolog offset
	db 0x22 ; 24 bytes of stack ;; WAS 0x22
	dw 0 ; unused entry
