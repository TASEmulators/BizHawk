/*
 * ====================================================
 * Copyright (C) 2007 by Ellips BV. All rights reserved.
 *
 * Permission to use, copy, modify, and distribute this
 * software is freely granted, provided that this notice
 * is preserved.
 * ====================================================
 */

 /*
 **  jmp_buf:
 **   rbx rbp r12 r13 r14 r15 rsp rip
 **   0   8   16  24  32  40  48  56
 */
.global setjmp
.global longjmp
.type setjmp,@function
.type longjmp,@function

setjmp:
  movq    %rbx,  0 (%rdi)
  movq    %rbp,  8 (%rdi)
  movq    %r12, 16 (%rdi)
  movq    %r13, 24 (%rdi)
  movq    %r14, 32 (%rdi)
  movq    %r15, 40 (%rdi)
  leaq    8 (%rsp), %rax
  movq    %rax, 48 (%rdi)
  movq    (%rsp), %rax
  movq    %rax, 56 (%rdi)
  movq    $0, %rax
  ret

longjmp:
  movq    %rsi, %rax        /* Return value */
  test    %rax, %rax
  jnz L0
  inc     %rax              /* if 0 was passed, send back 1 instead */
L0:
  movq     8 (%rdi), %rbp

  cli
  movq    48 (%rdi), %rsp
  pushq   56 (%rdi)
  movq     0 (%rdi), %rbx
  movq    16 (%rdi), %r12
  movq    24 (%rdi), %r13
  movq    32 (%rdi), %r14
  movq    40 (%rdi), %r15
  sti

  ret
