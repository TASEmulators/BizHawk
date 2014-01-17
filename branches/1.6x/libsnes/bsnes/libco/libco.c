/*
  libco
  auto-selection module
  license: public domain
*/

#if (defined(__ARM_EABI__) || defined(__ARMCC_VERSION) )
  #include "sjlj-multi.c"
#elif defined(__GNUC__) && defined(__i386__)
  #if !defined(LIBCO_MSVC)
    #include "x86.c"
  #endif
#elif defined(__GNUC__) && defined(__amd64__)
  #include "amd64.c"
#elif defined(__GNUC__) && defined(_ARCH_PPC)
  #include "ppc.c"
#elif defined(__GNUC__)
  #include "sjlj.c"
#elif defined(_MSC_VER) && defined(_M_IX86)
  #include "x86.c"
#elif defined(_MSC_VER) && defined(_M_AMD64)
  #include "amd64.c"
#elif defined(_MSC_VER)
  #include "fiber.c"
#else
  #error "libco: unsupported processor, compiler or operating system"
#endif
