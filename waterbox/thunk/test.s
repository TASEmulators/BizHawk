	.file	"test.c"
	.text
	.globl	Depart0
	.def	Depart0;	.scl	2;	.type	32;	.endef
Depart0:
	pushq	%rbp
	movq	%rsp, %rbp
	subq	$32, %rsp
	movabsq	$-2401053088335136050, %rax
	call	*%rax
	leave
	ret
	.globl	Depart1
	.def	Depart1;	.scl	2;	.type	32;	.endef
Depart1:
	pushq	%rbp
	movq	%rsp, %rbp
	subq	$48, %rsp
	movq	%rdi, -8(%rbp)
	movq	-8(%rbp), %rdx
	movabsq	$-2401053088335136050, %rax
	movq	%rdx, %rcx
	call	*%rax
	leave
	ret
	.globl	Depart2
	.def	Depart2;	.scl	2;	.type	32;	.endef
Depart2:
	pushq	%rbp
	movq	%rsp, %rbp
	subq	$48, %rsp
	movq	%rdi, -8(%rbp)
	movq	%rsi, -16(%rbp)
	movq	-16(%rbp), %rdx
	movq	-8(%rbp), %rcx
	movabsq	$-2401053088335136050, %rax
	call	*%rax
	leave
	ret
	.globl	Depart3
	.def	Depart3;	.scl	2;	.type	32;	.endef
Depart3:
	pushq	%rbp
	movq	%rsp, %rbp
	subq	$64, %rsp
	movq	%rdi, -8(%rbp)
	movq	%rsi, -16(%rbp)
	movq	%rdx, -24(%rbp)
	movq	-24(%rbp), %rsi
	movq	-16(%rbp), %rdx
	movq	-8(%rbp), %rcx
	movabsq	$-2401053088335136050, %rax
	movq	%rsi, %r8
	call	*%rax
	leave
	ret
	.globl	Depart4
	.def	Depart4;	.scl	2;	.type	32;	.endef
Depart4:
	pushq	%rbp
	movq	%rsp, %rbp
	subq	$64, %rsp
	movq	%rdi, -8(%rbp)
	movq	%rsi, -16(%rbp)
	movq	%rdx, -24(%rbp)
	movq	%rcx, -32(%rbp)
	movq	-32(%rbp), %rdi
	movq	-24(%rbp), %rsi
	movq	-16(%rbp), %rdx
	movq	-8(%rbp), %rcx
	movabsq	$-2401053088335136050, %rax
	movq	%rdi, %r9
	movq	%rsi, %r8
	call	*%rax
	leave
	ret
	.globl	Depart5
	.def	Depart5;	.scl	2;	.type	32;	.endef
Depart5:
	pushq	%rbp
	movq	%rsp, %rbp
	subq	$96, %rsp
	movq	%rdi, -8(%rbp)
	movq	%rsi, -16(%rbp)
	movq	%rdx, -24(%rbp)
	movq	%rcx, -32(%rbp)
	movq	%r8, -40(%rbp)
	movq	-32(%rbp), %rdi
	movq	-24(%rbp), %rsi
	movq	-16(%rbp), %rdx
	movq	-8(%rbp), %rcx
	movq	-40(%rbp), %rax
	movq	%rax, 32(%rsp)
	movabsq	$-2401053088335136050, %rax
	movq	%rdi, %r9
	movq	%rsi, %r8
	call	*%rax
	leave
	ret
	.globl	Depart6
	.def	Depart6;	.scl	2;	.type	32;	.endef
Depart6:
	pushq	%rbp
	movq	%rsp, %rbp
	subq	$96, %rsp
	movq	%rdi, -8(%rbp)
	movq	%rsi, -16(%rbp)
	movq	%rdx, -24(%rbp)
	movq	%rcx, -32(%rbp)
	movq	%r8, -40(%rbp)
	movq	%r9, -48(%rbp)
	movq	-32(%rbp), %rdi
	movq	-24(%rbp), %rsi
	movq	-16(%rbp), %rdx
	movq	-8(%rbp), %rcx
	movq	-48(%rbp), %rax
	movq	%rax, 40(%rsp)
	movq	-40(%rbp), %rax
	movq	%rax, 32(%rsp)
	movabsq	$-2401053088335136050, %rax
	movq	%rdi, %r9
	movq	%rsi, %r8
	call	*%rax
	leave
	ret
	.globl	Arrive0
	.def	Arrive0;	.scl	2;	.type	32;	.endef
	.seh_proc	Arrive0
Arrive0:
	pushq	%rbp
	.seh_pushreg	%rbp
	pushq	%rdi
	.seh_pushreg	%rdi
	pushq	%rsi
	.seh_pushreg	%rsi
	movq	%rsp, %rbp
	.seh_setframe	%rbp, 0
	subq	$160, %rsp
	.seh_stackalloc	160
	movaps	%xmm6, (%rsp)
	.seh_savexmm	%xmm6, 0
	movaps	%xmm7, 16(%rsp)
	.seh_savexmm	%xmm7, 16
	movaps	%xmm8, -128(%rbp)
	.seh_savexmm	%xmm8, 32
	movaps	%xmm9, -112(%rbp)
	.seh_savexmm	%xmm9, 48
	movaps	%xmm10, -96(%rbp)
	.seh_savexmm	%xmm10, 64
	movaps	%xmm11, -80(%rbp)
	.seh_savexmm	%xmm11, 80
	movaps	%xmm12, -64(%rbp)
	.seh_savexmm	%xmm12, 96
	movaps	%xmm13, -48(%rbp)
	.seh_savexmm	%xmm13, 112
	movaps	%xmm14, -32(%rbp)
	.seh_savexmm	%xmm14, 128
	movaps	%xmm15, -16(%rbp)
	.seh_savexmm	%xmm15, 144
	.seh_endprologue
	movabsq	$-2401053088335136050, %rax
	call	*%rax
	movaps	(%rsp), %xmm6
	movaps	16(%rsp), %xmm7
	movaps	-128(%rbp), %xmm8
	movaps	-112(%rbp), %xmm9
	movaps	-96(%rbp), %xmm10
	movaps	-80(%rbp), %xmm11
	movaps	-64(%rbp), %xmm12
	movaps	-48(%rbp), %xmm13
	movaps	-32(%rbp), %xmm14
	movaps	-16(%rbp), %xmm15
	addq	$160, %rsp
	popq	%rsi
	popq	%rdi
	popq	%rbp
	ret
	.seh_endproc
	.globl	Arrive1
	.def	Arrive1;	.scl	2;	.type	32;	.endef
	.seh_proc	Arrive1
Arrive1:
	pushq	%rbp
	.seh_pushreg	%rbp
	pushq	%rdi
	.seh_pushreg	%rdi
	pushq	%rsi
	.seh_pushreg	%rsi
	movq	%rsp, %rbp
	.seh_setframe	%rbp, 0
	subq	$160, %rsp
	.seh_stackalloc	160
	movaps	%xmm6, (%rsp)
	.seh_savexmm	%xmm6, 0
	movaps	%xmm7, 16(%rsp)
	.seh_savexmm	%xmm7, 16
	movaps	%xmm8, -128(%rbp)
	.seh_savexmm	%xmm8, 32
	movaps	%xmm9, -112(%rbp)
	.seh_savexmm	%xmm9, 48
	movaps	%xmm10, -96(%rbp)
	.seh_savexmm	%xmm10, 64
	movaps	%xmm11, -80(%rbp)
	.seh_savexmm	%xmm11, 80
	movaps	%xmm12, -64(%rbp)
	.seh_savexmm	%xmm12, 96
	movaps	%xmm13, -48(%rbp)
	.seh_savexmm	%xmm13, 112
	movaps	%xmm14, -32(%rbp)
	.seh_savexmm	%xmm14, 128
	movaps	%xmm15, -16(%rbp)
	.seh_savexmm	%xmm15, 144
	.seh_endprologue
	movq	%rcx, 32(%rbp)
	movabsq	$-2401053088335136050, %rax
	movq	32(%rbp), %rdi
	call	*%rax
	movaps	(%rsp), %xmm6
	movaps	16(%rsp), %xmm7
	movaps	-128(%rbp), %xmm8
	movaps	-112(%rbp), %xmm9
	movaps	-96(%rbp), %xmm10
	movaps	-80(%rbp), %xmm11
	movaps	-64(%rbp), %xmm12
	movaps	-48(%rbp), %xmm13
	movaps	-32(%rbp), %xmm14
	movaps	-16(%rbp), %xmm15
	addq	$160, %rsp
	popq	%rsi
	popq	%rdi
	popq	%rbp
	ret
	.seh_endproc
	.globl	Arrive2
	.def	Arrive2;	.scl	2;	.type	32;	.endef
	.seh_proc	Arrive2
Arrive2:
	pushq	%rbp
	.seh_pushreg	%rbp
	pushq	%rdi
	.seh_pushreg	%rdi
	pushq	%rsi
	.seh_pushreg	%rsi
	movq	%rsp, %rbp
	.seh_setframe	%rbp, 0
	subq	$160, %rsp
	.seh_stackalloc	160
	movaps	%xmm6, (%rsp)
	.seh_savexmm	%xmm6, 0
	movaps	%xmm7, 16(%rsp)
	.seh_savexmm	%xmm7, 16
	movaps	%xmm8, -128(%rbp)
	.seh_savexmm	%xmm8, 32
	movaps	%xmm9, -112(%rbp)
	.seh_savexmm	%xmm9, 48
	movaps	%xmm10, -96(%rbp)
	.seh_savexmm	%xmm10, 64
	movaps	%xmm11, -80(%rbp)
	.seh_savexmm	%xmm11, 80
	movaps	%xmm12, -64(%rbp)
	.seh_savexmm	%xmm12, 96
	movaps	%xmm13, -48(%rbp)
	.seh_savexmm	%xmm13, 112
	movaps	%xmm14, -32(%rbp)
	.seh_savexmm	%xmm14, 128
	movaps	%xmm15, -16(%rbp)
	.seh_savexmm	%xmm15, 144
	.seh_endprologue
	movq	%rcx, 32(%rbp)
	movq	%rdx, 40(%rbp)
	movq	40(%rbp), %rdx
	movabsq	$-2401053088335136050, %rax
	movq	%rdx, %rsi
	movq	32(%rbp), %rdi
	call	*%rax
	movaps	(%rsp), %xmm6
	movaps	16(%rsp), %xmm7
	movaps	-128(%rbp), %xmm8
	movaps	-112(%rbp), %xmm9
	movaps	-96(%rbp), %xmm10
	movaps	-80(%rbp), %xmm11
	movaps	-64(%rbp), %xmm12
	movaps	-48(%rbp), %xmm13
	movaps	-32(%rbp), %xmm14
	movaps	-16(%rbp), %xmm15
	addq	$160, %rsp
	popq	%rsi
	popq	%rdi
	popq	%rbp
	ret
	.seh_endproc
	.globl	Arrive3
	.def	Arrive3;	.scl	2;	.type	32;	.endef
	.seh_proc	Arrive3
Arrive3:
	pushq	%rbp
	.seh_pushreg	%rbp
	pushq	%rdi
	.seh_pushreg	%rdi
	pushq	%rsi
	.seh_pushreg	%rsi
	movq	%rsp, %rbp
	.seh_setframe	%rbp, 0
	subq	$160, %rsp
	.seh_stackalloc	160
	movaps	%xmm6, (%rsp)
	.seh_savexmm	%xmm6, 0
	movaps	%xmm7, 16(%rsp)
	.seh_savexmm	%xmm7, 16
	movaps	%xmm8, -128(%rbp)
	.seh_savexmm	%xmm8, 32
	movaps	%xmm9, -112(%rbp)
	.seh_savexmm	%xmm9, 48
	movaps	%xmm10, -96(%rbp)
	.seh_savexmm	%xmm10, 64
	movaps	%xmm11, -80(%rbp)
	.seh_savexmm	%xmm11, 80
	movaps	%xmm12, -64(%rbp)
	.seh_savexmm	%xmm12, 96
	movaps	%xmm13, -48(%rbp)
	.seh_savexmm	%xmm13, 112
	movaps	%xmm14, -32(%rbp)
	.seh_savexmm	%xmm14, 128
	movaps	%xmm15, -16(%rbp)
	.seh_savexmm	%xmm15, 144
	.seh_endprologue
	movq	%rcx, 32(%rbp)
	movq	%rdx, 40(%rbp)
	movq	%r8, 48(%rbp)
	movq	48(%rbp), %rdx
	movq	40(%rbp), %rcx
	movabsq	$-2401053088335136050, %rax
	movq	%rcx, %rsi
	movq	32(%rbp), %rdi
	call	*%rax
	movaps	(%rsp), %xmm6
	movaps	16(%rsp), %xmm7
	movaps	-128(%rbp), %xmm8
	movaps	-112(%rbp), %xmm9
	movaps	-96(%rbp), %xmm10
	movaps	-80(%rbp), %xmm11
	movaps	-64(%rbp), %xmm12
	movaps	-48(%rbp), %xmm13
	movaps	-32(%rbp), %xmm14
	movaps	-16(%rbp), %xmm15
	addq	$160, %rsp
	popq	%rsi
	popq	%rdi
	popq	%rbp
	ret
	.seh_endproc
	.globl	Arrive4
	.def	Arrive4;	.scl	2;	.type	32;	.endef
	.seh_proc	Arrive4
Arrive4:
	pushq	%rbp
	.seh_pushreg	%rbp
	pushq	%rdi
	.seh_pushreg	%rdi
	pushq	%rsi
	.seh_pushreg	%rsi
	movq	%rsp, %rbp
	.seh_setframe	%rbp, 0
	subq	$160, %rsp
	.seh_stackalloc	160
	movaps	%xmm6, (%rsp)
	.seh_savexmm	%xmm6, 0
	movaps	%xmm7, 16(%rsp)
	.seh_savexmm	%xmm7, 16
	movaps	%xmm8, -128(%rbp)
	.seh_savexmm	%xmm8, 32
	movaps	%xmm9, -112(%rbp)
	.seh_savexmm	%xmm9, 48
	movaps	%xmm10, -96(%rbp)
	.seh_savexmm	%xmm10, 64
	movaps	%xmm11, -80(%rbp)
	.seh_savexmm	%xmm11, 80
	movaps	%xmm12, -64(%rbp)
	.seh_savexmm	%xmm12, 96
	movaps	%xmm13, -48(%rbp)
	.seh_savexmm	%xmm13, 112
	movaps	%xmm14, -32(%rbp)
	.seh_savexmm	%xmm14, 128
	movaps	%xmm15, -16(%rbp)
	.seh_savexmm	%xmm15, 144
	.seh_endprologue
	movq	%rcx, 32(%rbp)
	movq	%rdx, 40(%rbp)
	movq	%r8, 48(%rbp)
	movq	%r9, 56(%rbp)
	movq	56(%rbp), %rcx
	movq	48(%rbp), %rdx
	movq	40(%rbp), %r8
	movabsq	$-2401053088335136050, %rax
	movq	%r8, %rsi
	movq	32(%rbp), %rdi
	call	*%rax
	movaps	(%rsp), %xmm6
	movaps	16(%rsp), %xmm7
	movaps	-128(%rbp), %xmm8
	movaps	-112(%rbp), %xmm9
	movaps	-96(%rbp), %xmm10
	movaps	-80(%rbp), %xmm11
	movaps	-64(%rbp), %xmm12
	movaps	-48(%rbp), %xmm13
	movaps	-32(%rbp), %xmm14
	movaps	-16(%rbp), %xmm15
	addq	$160, %rsp
	popq	%rsi
	popq	%rdi
	popq	%rbp
	ret
	.seh_endproc
	.globl	Arrive5
	.def	Arrive5;	.scl	2;	.type	32;	.endef
	.seh_proc	Arrive5
Arrive5:
	pushq	%rbp
	.seh_pushreg	%rbp
	pushq	%rdi
	.seh_pushreg	%rdi
	pushq	%rsi
	.seh_pushreg	%rsi
	movq	%rsp, %rbp
	.seh_setframe	%rbp, 0
	subq	$160, %rsp
	.seh_stackalloc	160
	movaps	%xmm6, (%rsp)
	.seh_savexmm	%xmm6, 0
	movaps	%xmm7, 16(%rsp)
	.seh_savexmm	%xmm7, 16
	movaps	%xmm8, -128(%rbp)
	.seh_savexmm	%xmm8, 32
	movaps	%xmm9, -112(%rbp)
	.seh_savexmm	%xmm9, 48
	movaps	%xmm10, -96(%rbp)
	.seh_savexmm	%xmm10, 64
	movaps	%xmm11, -80(%rbp)
	.seh_savexmm	%xmm11, 80
	movaps	%xmm12, -64(%rbp)
	.seh_savexmm	%xmm12, 96
	movaps	%xmm13, -48(%rbp)
	.seh_savexmm	%xmm13, 112
	movaps	%xmm14, -32(%rbp)
	.seh_savexmm	%xmm14, 128
	movaps	%xmm15, -16(%rbp)
	.seh_savexmm	%xmm15, 144
	.seh_endprologue
	movq	%rcx, 32(%rbp)
	movq	%rdx, 40(%rbp)
	movq	%r8, 48(%rbp)
	movq	%r9, 56(%rbp)
	movq	64(%rbp), %r8
	movq	56(%rbp), %rcx
	movq	48(%rbp), %rdx
	movq	40(%rbp), %r9
	movabsq	$-2401053088335136050, %rax
	movq	%r9, %rsi
	movq	32(%rbp), %rdi
	call	*%rax
	movaps	(%rsp), %xmm6
	movaps	16(%rsp), %xmm7
	movaps	-128(%rbp), %xmm8
	movaps	-112(%rbp), %xmm9
	movaps	-96(%rbp), %xmm10
	movaps	-80(%rbp), %xmm11
	movaps	-64(%rbp), %xmm12
	movaps	-48(%rbp), %xmm13
	movaps	-32(%rbp), %xmm14
	movaps	-16(%rbp), %xmm15
	addq	$160, %rsp
	popq	%rsi
	popq	%rdi
	popq	%rbp
	ret
	.seh_endproc
	.globl	Arrive6
	.def	Arrive6;	.scl	2;	.type	32;	.endef
	.seh_proc	Arrive6
Arrive6:
	pushq	%rbp
	.seh_pushreg	%rbp
	pushq	%rdi
	.seh_pushreg	%rdi
	pushq	%rsi
	.seh_pushreg	%rsi
	movq	%rsp, %rbp
	.seh_setframe	%rbp, 0
	subq	$160, %rsp
	.seh_stackalloc	160
	movaps	%xmm6, (%rsp)
	.seh_savexmm	%xmm6, 0
	movaps	%xmm7, 16(%rsp)
	.seh_savexmm	%xmm7, 16
	movaps	%xmm8, -128(%rbp)
	.seh_savexmm	%xmm8, 32
	movaps	%xmm9, -112(%rbp)
	.seh_savexmm	%xmm9, 48
	movaps	%xmm10, -96(%rbp)
	.seh_savexmm	%xmm10, 64
	movaps	%xmm11, -80(%rbp)
	.seh_savexmm	%xmm11, 80
	movaps	%xmm12, -64(%rbp)
	.seh_savexmm	%xmm12, 96
	movaps	%xmm13, -48(%rbp)
	.seh_savexmm	%xmm13, 112
	movaps	%xmm14, -32(%rbp)
	.seh_savexmm	%xmm14, 128
	movaps	%xmm15, -16(%rbp)
	.seh_savexmm	%xmm15, 144
	.seh_endprologue
	movq	%rcx, 32(%rbp)
	movq	%rdx, 40(%rbp)
	movq	%r8, 48(%rbp)
	movq	%r9, 56(%rbp)
	movq	72(%rbp), %r9
	movq	64(%rbp), %r8
	movq	56(%rbp), %rcx
	movq	48(%rbp), %rdx
	movq	40(%rbp), %r10
	movabsq	$-2401053088335136050, %rax
	movq	%r10, %rsi
	movq	32(%rbp), %rdi
	call	*%rax
	movaps	(%rsp), %xmm6
	movaps	16(%rsp), %xmm7
	movaps	-128(%rbp), %xmm8
	movaps	-112(%rbp), %xmm9
	movaps	-96(%rbp), %xmm10
	movaps	-80(%rbp), %xmm11
	movaps	-64(%rbp), %xmm12
	movaps	-48(%rbp), %xmm13
	movaps	-32(%rbp), %xmm14
	movaps	-16(%rbp), %xmm15
	addq	$160, %rsp
	popq	%rsi
	popq	%rdi
	popq	%rbp
	ret
	.seh_endproc
	.globl	End
	.def	End;	.scl	2;	.type	32;	.endef
	.seh_proc	End
End:
	pushq	%rbp
	.seh_pushreg	%rbp
	movq	%rsp, %rbp
	.seh_setframe	%rbp, 0
	.seh_endprologue
	nop
	popq	%rbp
	ret
	.seh_endproc
	.globl	ptrs
	.data
	.align 32
ptrs:
	.quad	Depart0
	.quad	Depart1
	.quad	Depart2
	.quad	Depart3
	.quad	Depart4
	.quad	Depart5
	.quad	Depart6
	.quad	Arrive0
	.quad	Arrive1
	.quad	Arrive2
	.quad	Arrive3
	.quad	Arrive4
	.quad	Arrive5
	.quad	Arrive6
	.quad	End
	.section .rdata,"dr"
	.align 8
.LC0:
	.ascii "private static readonly byte[][] %s =\12{\12\0"
.LC1:
	.ascii "\11new byte[] { \0"
.LC2:
	.ascii "0x%02x, \0"
.LC3:
	.ascii "},\0"
.LC4:
	.ascii "};\0"
	.text
	.globl	print
	.def	print;	.scl	2;	.type	32;	.endef
	.seh_proc	print
print:
	pushq	%rbp
	.seh_pushreg	%rbp
	movq	%rsp, %rbp
	.seh_setframe	%rbp, 0
	subq	$64, %rsp
	.seh_stackalloc	64
	.seh_endprologue
	movq	%rcx, 16(%rbp)
	movl	%edx, 24(%rbp)
	movq	16(%rbp), %rdx
	leaq	.LC0(%rip), %rcx
	call	printf
	movl	24(%rbp), %eax
	movl	%eax, -4(%rbp)
	jmp	.L31
.L34:
	leaq	.LC1(%rip), %rcx
	call	printf
	movl	-4(%rbp), %eax
	cltq
	leaq	0(,%rax,8), %rdx
	leaq	ptrs(%rip), %rax
	movq	(%rdx,%rax), %rax
	movq	%rax, -16(%rbp)
	movl	-4(%rbp), %eax
	addl	$1, %eax
	cltq
	leaq	0(,%rax,8), %rdx
	leaq	ptrs(%rip), %rax
	movq	(%rdx,%rax), %rax
	movq	%rax, -24(%rbp)
	jmp	.L32
.L33:
	movq	-16(%rbp), %rax
	leaq	1(%rax), %rdx
	movq	%rdx, -16(%rbp)
	movzbl	(%rax), %eax
	movzbl	%al, %eax
	movl	%eax, %edx
	leaq	.LC2(%rip), %rcx
	call	printf
.L32:
	movq	-16(%rbp), %rax
	cmpq	-24(%rbp), %rax
	jb	.L33
	leaq	.LC3(%rip), %rcx
	call	puts
	addl	$1, -4(%rbp)
.L31:
	movl	24(%rbp), %eax
	addl	$7, %eax
	cmpl	-4(%rbp), %eax
	jg	.L34
	leaq	.LC4(%rip), %rcx
	call	puts
	nop
	addq	$64, %rsp
	popq	%rbp
	ret
	.seh_endproc
	.def	__main;	.scl	2;	.type	32;	.endef
	.section .rdata,"dr"
.LC5:
	.ascii "Depart\0"
.LC6:
	.ascii "Arrive\0"
	.text
	.globl	main
	.def	main;	.scl	2;	.type	32;	.endef
	.seh_proc	main
main:
	pushq	%rbp
	.seh_pushreg	%rbp
	movq	%rsp, %rbp
	.seh_setframe	%rbp, 0
	subq	$32, %rsp
	.seh_stackalloc	32
	.seh_endprologue
	call	__main
	movl	$0, %edx
	leaq	.LC5(%rip), %rcx
	call	print
	movl	$0, %edx
	leaq	.LC6(%rip), %rcx
	call	print
	movl	$0, %eax
	addq	$32, %rsp
	popq	%rbp
	ret
	.seh_endproc
	.ident	"GCC: (Rev2, Built by MSYS2 project) 5.3.0"
	.def	printf;	.scl	2;	.type	32;	.endef
	.def	puts;	.scl	2;	.type	32;	.endef
