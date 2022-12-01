global __start

%macro EXPORT_FUNCS 1
	global depart%1
	global arrive%1
	export depart%1
	export arrive%1
%endmacro

EXPORT_FUNCS 0
EXPORT_FUNCS 1
EXPORT_FUNCS 2
EXPORT_FUNCS 3
EXPORT_FUNCS 4
EXPORT_FUNCS 5
EXPORT_FUNCS 6

; save msabi nonvolatiles which are volatile under sysv
%macro START_DEPART 0
	push rsi
	push rdi
	sub rsp, 0xA8
	movaps [rsp + 0x90], xmm15
	movaps [rsp + 0x80], xmm14
	movaps [rsp + 0x70], xmm13
	movaps [rsp + 0x60], xmm12
	movaps [rsp + 0x50], xmm11
	movaps [rsp + 0x40], xmm10
	movaps [rsp + 0x30], xmm9
	movaps [rsp + 0x20], xmm8
	movaps [rsp + 0x10], xmm7
	movaps [rsp], xmm6
%endmacro

; restore the saved msabi nonvolatiles which were volatile under sysv, then return
%macro END_DEPART 0
	movaps xmm6, [rsp]
	movaps xmm7, [rsp + 0x10]
	movaps xmm8, [rsp + 0x20]
	movaps xmm9, [rsp + 0x30]
	movaps xmm10, [rsp + 0x40]
	movaps xmm11, [rsp + 0x50]
	movaps xmm12, [rsp + 0x60]
	movaps xmm13, [rsp + 0x70]
	movaps xmm14, [rsp + 0x80]
	movaps xmm15, [rsp + 0x90]
	add rsp, 0xA8
	pop rdi
	pop rsi
	ret
%endmacro

; https://docs.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-runtime_function

%define RVA(sym) sym wrt ..imagebase

%macro DEPART_UNWIND_ENTRY 1
	dd RVA(depart%1)
	dd RVA(depart%1_end)
	dd RVA(xdepart)
%endmacro

%macro ARRIVE_UNWIND_ENTRY 1
	dd RVA(arrive%1)
	dd RVA(arrive%1_end)
	dd RVA(xarrive)
%endmacro

; https://docs.microsoft.com/en-us/cpp/build/exception-handling-x64

%define UWOP_PUSH_NONVOL 0
%define UWOP_ALLOC_LARGE 1
%define UWOP_ALLOC_SMALL 2
%define UWOP_SAVE_XMM128 8

section .pdata rdata align=4

	DEPART_UNWIND_ENTRY 0
	DEPART_UNWIND_ENTRY 1
	DEPART_UNWIND_ENTRY 2
	DEPART_UNWIND_ENTRY 3
	DEPART_UNWIND_ENTRY 4
	DEPART_UNWIND_ENTRY 5
	DEPART_UNWIND_ENTRY 6

	ARRIVE_UNWIND_ENTRY 0
	ARRIVE_UNWIND_ENTRY 1
	ARRIVE_UNWIND_ENTRY 2
	ARRIVE_UNWIND_ENTRY 3
	ARRIVE_UNWIND_ENTRY 4
	ARRIVE_UNWIND_ENTRY 5

	dd RVA(arrive6)
	dd RVA(arrive6_end)
	dd RVA(xarrive6)

section .xdata rdata align=8

	xdepart:
		db 1, 72, 24, 0
		db 68, (0x6 << 4) | UWOP_SAVE_XMM128
		dw 0
		db 63, (0x7 << 4) | UWOP_SAVE_XMM128
		dw 1
		db 57, (0x8 << 4) | UWOP_SAVE_XMM128
		dw 2
		db 51, (0x9 << 4) | UWOP_SAVE_XMM128
		dw 3
		db 45, (0xA << 4) | UWOP_SAVE_XMM128
		dw 4
		db 39, (0xB << 4) | UWOP_SAVE_XMM128
		dw 5
		db 33, (0xC << 4) | UWOP_SAVE_XMM128
		dw 6
		db 27, (0xD << 4) | UWOP_SAVE_XMM128
		dw 7
		db 18, (0xE << 4) | UWOP_SAVE_XMM128
		dw 8
		db 9, (0xF << 4) | UWOP_SAVE_XMM128
		dw 9
		db 2, (0x0 << 4) | UWOP_ALLOC_LARGE
		dw 0x15
		db 1, (0x7 << 4) | UWOP_PUSH_NONVOL
		db 0, (0x6 << 4) | UWOP_PUSH_NONVOL

	xarrive:
		db 1, 4, 1, 0
		db 0, (0x4 << 4) | UWOP_ALLOC_SMALL
		dw 0

	xarrive6:
		db 1, 4, 1, 0
		db 0, (0x6 << 4) | UWOP_ALLOC_SMALL
		dw 0

section .text

	; DllMain, just a stub here
	__start:
		mov eax, 1
		ret

	; departX are msabi functions that call a sysv function and returns its result.
	; arriveX are sysv functions that call a msabi function and returns its result. 
	; The function is passed as a hidden parameter in rax, and should take X pointer or integer type arguments.
	; If the function contains no pointer or integer type arguments, then it may instead have, at most, 4 floating point arguments.

	depart0:
		START_DEPART
		call rax
		END_DEPART
	depart0_end:

	depart1:
		START_DEPART
		mov rdi, rcx
		call rax
		END_DEPART
	depart1_end:

	depart2:
		START_DEPART
		mov rdi, rcx
		mov rsi, rdx
		call rax
		END_DEPART
	depart2_end:

	depart3:
		START_DEPART
		mov rdi, rcx
		mov rsi, rdx
		mov rdx, r8
		call rax
		END_DEPART
	depart3_end:

	depart4:
		START_DEPART
		mov rdi, rcx
		mov rsi, rdx
		mov rdx, r8
		mov rcx, r9
		call rax
		END_DEPART
	depart4_end:

	depart5:
		START_DEPART
		mov r10, r8
		mov r8, qword [rsp + 0xE0]
		mov rdi, rcx
		mov rsi, rdx
		mov rdx, r10
		mov rcx, r9
		call rax
		END_DEPART
	depart5_end:

	depart6:
		START_DEPART
		mov r11, r9
		mov r10, r8
		mov r8, qword [rsp + 0xE0]
		mov r9, qword [rsp + 0xE8]
		mov rdi, rcx
		mov rsi, rdx
		mov rdx, r10
		mov rcx, r11
		call rax
		END_DEPART
	depart6_end:

	arrive0:
		sub rsp, 0x28
		call rax
		add rsp, 0x28
		ret
	arrive0_end:

	arrive1:
		sub rsp, 0x28
		mov rcx, rdi
		call rax
		add rsp, 0x28
		ret
	arrive1_end:

	arrive2:
		sub rsp, 0x28
		mov rdx, rsi
		mov rcx, rdi
		call rax
		add rsp, 0x28
		ret
	arrive2_end:

	arrive3:
		sub rsp, 0x28
		mov r8, rdx
		mov rdx, rsi
		mov rcx, rdi
		call rax
		add rsp, 0x28
		ret
	arrive3_end:

	arrive4:
		sub rsp, 0x28
		mov r9, rcx
		mov r8, rdx
		mov rdx, rsi
		mov rcx, rdi
		call rax
		add rsp, 0x28
		ret
	arrive4_end:

	arrive5:
		sub rsp, 0x28
		mov r9, rcx
		mov r10, rdx
		mov rdx, rsi
		mov rcx, rdi
		mov qword [rsp + 0x20], r8
		mov r8, r10
		call rax
		add rsp, 0x28
		ret
	arrive5_end:

	arrive6:
		sub rsp, 0x38
		mov r10, rcx
		mov r11, rdx
		mov rdx, rsi
		mov rcx, rdi
		mov qword [rsp + 0x28], r9
		mov qword [rsp + 0x20], r8
		mov r8, r11
		mov r9, r10
		call rax
		add  rsp, 0x38
		ret
	arrive6_end:
