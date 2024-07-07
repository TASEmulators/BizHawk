
                     SLJIT - Stack Less JIT Compiler

Purpose:
  A low-level, machine independent JIT compiler, which is suitable for
  translating interpreted byte code into machine code. The sljitLir.h
  describes the LIR (low-level intermediate representation) of SLJIT.

Key features:
  - Supports several target architectures:
    x86 32/64, ARM 32/64, RiscV 32/64, s390x 64,
    PowerPC 32/64, LoongArch 64, MIPS 32/64
  - Supports a large number of operations
    - Supports self-modifying code
    - Supports tail calls
    - Support fast calls (non-ABI compatible)
    - Supports byte order reverse (endianness switching)
    - Supports unaligned memory accesses
    - Supports SIMD / atomic operations on certain CPUs
  - Direct register access, both integer and floating point
  - Stack space allocated for function local variables can be
    accessed as a linear memory area
  - All-in-one compilation is supported
  - When sljitLir.c is directly included by a C source file,
    the jit compiler API can be completely hidden from
    external use (see SLJIT_CONFIG_STATIC macro)
    - Code can be generated for multiple target cpus
      by including sljitLir.c in different C files, where
      each compiler instance is configured to target a
      different architecture
  - The compiler can be serialized into a byte buffer
    - Useful for ahead-of-time compiling
    - Code generation can be resumed after deserialization
      (partial ahead-of-time compiling)

Compatible:
  C99 (C++) compilers.

Using sljit:
  Copy the content of sljit_src directory into your project source directory.
  Add sljitLir.c source file to your build environment. All other files are
  included by sljitLir.c (if required). Define the machine by SLJIT_CONFIG_*
  selector. See sljitConfigCPU.h for all possible values. For C++ compilers,
  rename sljitLir.c to sljitLir.cpp.

More info:
  https://zherczeg.github.io/sljit/

Contact:
  hzmester@freemail.hu

Special thanks:
  Alexander Nasonov
  Carlo Marcelo Arenas Belón
  Christian Persch
  Daniel Richard G.
  Giuseppe D'Angelo
  H.J. Lu
  James Cowgill
  Jason Hood
  Jiong Wang (TileGX support)
  Marc Mutz
  Martin Storsjö
  Michael McConville
  Mingtao Zhou (LoongArch support)
  Walter Lee
  Wen Xichang
  YunQiang Su
