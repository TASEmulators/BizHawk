/* Copyright 2011-2012 Nicholas J. Kain, licensed under standard MIT license */
.text
.global __unmapself
.type   __unmapself,@function
__unmapself:
	sub $8,%rsp
	movl $11,%eax   /* SYS_munmap */
	mov $0x35f00000080,%r10
	call *%r10      /* munmap(arg2,arg3) */
	xor %rdi,%rdi   /* exit() args: always return success */
	movl $60,%eax   /* SYS_exit */
	mov $0x35f00000080,%r10
	call *%r10      /* exit(0) */
	int3
