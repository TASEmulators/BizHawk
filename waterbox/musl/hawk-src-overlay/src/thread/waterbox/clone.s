.text
.global __clone
.hidden __clone
.type   __clone,@function
__clone: /* (entry_point, stack, flags, arg, ptid, tls, ctid) */
	/* syscall number - NR_WBX_CLONE */
	xor %eax,%eax
	mov $2000,%ax

	/* set up information on child stack */
	and $-16,%rsi /* align */
	sub $16,%rsi
	mov %rdi,8(%rsi) /* thread entry point */
	mov %rcx,0(%rsi) /* thread entry argument */

	mov %r9,%rdi /* tls */
	mov 8(%rsp),%rcx /* child_tid */
	lea child_thread_start(%rip),%rdx /* child_rip */

	/* syscall NR_WBX_CLONE (tls, child_rsp, child_rip, child_tid, parent_tid) */
	sub $8,%rsp
	mov $0x35f00000080,%r10
	call *%r10
	add $8,%rsp
	ret

child_thread_start:
	pop %rdi /* thread entry argument */
	pop %rax /* thread entry point */
	call *%rax /* run thread */

	/* syscall exit */
	mov $60,%al
	mov $0x35f00000080,%r10
	call *%r10
	int3
