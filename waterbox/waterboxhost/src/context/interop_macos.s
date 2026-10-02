bits 64
org 0x35f00000000
%define RVA(addr) (addr - 0x35f00000000)

; macOS variant of interop.s. See interop.s for the canonical/commented version.
;
; Difference from Linux/Windows: on macOS x86-64 (incl. under Rosetta 2) the %gs base
; is the OS thread-self-data (TSD) and CANNOT be repointed (no arch_prctl; rdgsbase/
; wrgsbase #UD under Rosetta). The guest musl only reads its Context pointer from
; gs:0x18 (= TSD slot 3, mach_thread_self). gs:0x08/0x10 are macOS errno/mig_reply and
; must NOT be touched (host C code uses errno). So here we:
;   * never write gs:0x08/0x10 (the "TIB stack base/limit" dance is dropped),
;   * stash the real mach_thread_self in a free TSD slot (gs:REAL_TLS) on guest entry,
;   * set gs:0x18 = Context while guest runs, and restore gs:0x18 = real at every
;     guest->host boundary (and on final exit) so host/macOS code sees a valid TSD.
%define REAL_TLS 0x60

struc Context
	.thread_area resq 1
	.host_rsp resq 1
	.guest_rsp resq 1
	.host_rsp_alt resq 1
	.guest_rsp_alt resq 1
	.dispatch_syscall resq 1
	.host_ptr resq 1
	.extcall_slots resq 64
endstruc

times 0x80-($-$$) int3
; called by guest when it wishes to make a syscall
guest_syscall:
	push rbp
	mov r10, [gs:0x18]
	mov [r10 + Context.guest_rsp], rsp
	mov rsp, [r10 + Context.host_rsp]

	; save context, restore real mach_thread_self for the host call
	push r10
	mov r11, [gs:REAL_TLS]
	mov [gs:0x18], r11

	mov r11, [r10 + Context.host_ptr]
	push r11 ; arg 8 to dispatch_syscall: host
	push rax ; arg 7 to dispatch_syscall: nr
	mov rax, [r10 + Context.dispatch_syscall]
	call rax

	; Restore Context ptr for the guest
	mov r10, [rsp + 16]
	mov [gs:0x18], r10

	mov rsp, [r10 + Context.guest_rsp]
	pop rbp
	ret
guest_syscall_end:

times 0x100-($-$$) int3 ; CALL_GUEST_SIMPLE_ADDR
call_guest_simple:
	mov r11, rdi
	mov r10, rsi
	jmp call_guest_impl

times 0x200-($-$$) int3 ; CALL_GUEST_IMPL_ADDR
call_guest_impl:
	; check if we need to swap stacks for a reentrant call
	mov rax, [r10 + Context.host_rsp]
	test rax, rax
	je do_tib
	mov rax, [r10 + Context.host_rsp_alt]
	test rax, rax
	je do_swap
	int3 ; both stacks exhausted

do_swap:
	mov rax, [r10 + Context.host_rsp]
	mov [r10 + Context.host_rsp_alt], rax
	mov rax, [r10 + Context.guest_rsp]
	xchg rax, [r10 + Context.guest_rsp_alt]
	mov [r10 + Context.guest_rsp], rax

do_tib:
	; keep two pushes so host_rsp layout + stack balance match interop.s exactly
	; (values are unused on macOS; we do not touch gs:0x08/0x10)
	push rax
	push rax

	; stash real mach_thread_self, then set Context ptr for the guest
	mov rax, [gs:0x18]
	mov [gs:REAL_TLS], rax
	mov [gs:0x18], r10

	mov [r10 + Context.host_rsp], rsp
	mov rsp, [r10 + Context.guest_rsp]
	call r11 ; stack hygiene note - this host address is saved on the guest stack
	mov r10, [gs:0x18]
	mov [r10 + Context.guest_rsp], rsp ; restore stack so next call using same Context will work
	mov rsp, [r10 + Context.host_rsp]
	mov r11, 0
	mov [r10 + Context.host_rsp], r11 ; zero out host_rsp so we'll know this callstack is no longer in use

	; restore real mach_thread_self now that we are back in host land
	; (use r11, NOT rax: rax holds the guest function's return value)
	mov r11, [gs:REAL_TLS]
	mov [gs:0x18], r11

	; check to see if we need to swap back stacks
	mov r11, [r10 + Context.host_rsp_alt]
	test r11, r11
	je do_restore_tib

	mov [r10 + Context.host_rsp], r11
	mov r11, 0
	mov [r10 + Context.host_rsp_alt], r11
	mov r11, [r10 + Context.guest_rsp_alt]
	xchg r11, [r10 + Context.guest_rsp]
	mov [r10 + Context.guest_rsp_alt], r11

do_restore_tib:
	; discard the two pushed qwords (balance); do NOT touch gs:0x08/0x10
	pop r10
	pop r10
	ret

times 0x300-($-$$) int3 ; EXTCALL_THUNK_ADDR
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
guest_extcall_impl:
	mov r10, [gs:0x18]
	mov [r10 + Context.guest_rsp], rsp
	mov rsp, [r10 + Context.host_rsp]

	; save context, restore real mach_thread_self for the host call
	push r10
	mov r11, [gs:REAL_TLS]
	mov [gs:0x18], r11

	mov r11, [r10 + Context.extcall_slots + rax * 8] ; get slot ptr
	call r11

	; Restore Context ptr for the guest
	mov r10, [rsp]
	mov [gs:0x18], r10

	mov rsp, [r10 + Context.guest_rsp]
	ret
guest_extcall_impl_end:

times 0x800-($-$$) int3 ; RUNTIME_TABLE_ADDR
; (Windows-only SEH table; unused on macOS but kept for identical byte layout)
runtime_function_table:
	dd RVA(guest_syscall)
	dd RVA(guest_syscall_end)
	dd RVA(guest_syscall_unwind)

	dd RVA(guest_extcall_impl)
	dd RVA(guest_extcall_impl_end)
	dd RVA(guest_extcall_impl_unwind)
guest_syscall_unwind:
	db 1
	db 5
	db 1
	db 0

	db 5
	db 0x42
	dw 0
guest_extcall_impl_unwind:
	db 1
	db 5
	db 1
	db 0

	db 5
	db 0x22
	dw 0
