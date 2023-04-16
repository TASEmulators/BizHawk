#pragma once

#include <nall/platform.hpp>

//the c/c++ standard library fenv.h has numerous design and implementation flaws:
//- forces updates to both x87 and sse state on amd64
//- 'set' operations require a register read + modify + write
//- some implementations define api flags differently from native registers (msvc)
//- some implementations are so buggy they don't even use the correct registers (mingw/arm64)

//here we provide our own thin abstraction that falls back on fenv.h as a last resort.
//note: the state of the control register is cached, so changes made via external means
//will be reverted the next time it is modified by this wrapper.

namespace nall {

struct float_env {
#if defined(ARCHITECTURE_AMD64)
  static constexpr u32 allExcept  = _MM_EXCEPT_MASK;
  static constexpr u32 denormal   = _MM_EXCEPT_DENORM;
  static constexpr u32 inexact    = _MM_EXCEPT_INEXACT;
  static constexpr u32 underflow  = _MM_EXCEPT_UNDERFLOW;
  static constexpr u32 overflow   = _MM_EXCEPT_OVERFLOW;
  static constexpr u32 divByZero  = _MM_EXCEPT_DIV_ZERO;
  static constexpr u32 invalid    = _MM_EXCEPT_INVALID;
  static constexpr u32 downward   = _MM_ROUND_DOWN;
  static constexpr u32 toNearest  = _MM_ROUND_NEAREST;
  static constexpr u32 towardZero = _MM_ROUND_TOWARD_ZERO;
  static constexpr u32 upward     = _MM_ROUND_UP;
#elif defined(ARCHITECTURE_ARM64)
  static constexpr u32 allExcept  = 0x3f;
  static constexpr u32 denormal   = 0x20;
  static constexpr u32 inexact    = 0x10;
  static constexpr u32 underflow  = 0x08;
  static constexpr u32 overflow   = 0x04;
  static constexpr u32 divByZero  = 0x02;
  static constexpr u32 invalid    = 0x01;
  static constexpr u32 downward   = 2 << 22;
  static constexpr u32 toNearest  = 0 << 22;
  static constexpr u32 towardZero = 3 << 22;
  static constexpr u32 upward     = 1 << 22;
#else
  static constexpr u32 allExcept  = FE_ALL_EXCEPT;
#if defined(FE_DENORMAL)
  static constexpr u32 denormal   = FE_DENORMAL;
#else
  static constexpr u32 denormal   = 0;
#endif
  static constexpr u32 inexact    = FE_INEXACT;
  static constexpr u32 underflow  = FE_UNDERFLOW;
  static constexpr u32 overflow   = FE_OVERFLOW;
  static constexpr u32 divByZero  = FE_DIVBYZERO;
  static constexpr u32 invalid    = FE_INVALID;
  static constexpr u32 downward   = FE_DOWNWARD;
  static constexpr u32 toNearest  = FE_TONEAREST;
  static constexpr u32 towardZero = FE_TOWARDZERO;
  static constexpr u32 upward     = FE_UPWARD;
#endif
  static constexpr u32 roundMask = downward | toNearest | towardZero | upward;

  u32 control = 0;

  float_env() {
    control = getControl();
  }

  auto setRound(u32 mode) -> void {
    control &= ~roundMask;
    control |= mode & roundMask;
    setControl();
  }

  auto getRound() -> u32 {
    return control & roundMask;
  }

  auto testExcept(u32 mask) -> u32 {
    return getStatus() & mask & allExcept;
  }

  auto clearExcept() -> void {
    clearStatus();
  }

private:
  auto getControl() -> u32 {
#if defined(ARCHITECTURE_AMD64)
    return _mm_getcsr() & ~allExcept;
#elif defined(ARCHITECTURE_ARM64)
  #if defined(COMPILER_MICROSOFT)
    return _ReadStatusReg(ARM64_FPCR);
  #else
    u64 value;
    __asm__ __volatile__("mrs %0, FPCR" : "=r"(value));
    return value;
  #endif
#else
    return fegetround();
#endif
  }

  auto getStatus() -> u32 {
#if defined(ARCHITECTURE_AMD64)
    return _mm_getcsr() & allExcept;
#elif defined(ARCHITECTURE_ARM64)
  #if defined(COMPILER_MICROSOFT)
    return _ReadStatusReg(ARM64_FPSR);
  #else
    u64 value;
    __asm__ __volatile__("mrs %0, FPSR" : "=r"(value));
    return value;
  #endif
#else
    return fetestexcept(allExcept);
#endif
  }

  auto setControl() -> void {
#if defined(ARCHITECTURE_AMD64)
    _mm_setcsr(control | getStatus());
#elif defined(ARCHITECTURE_ARM64)
  #if defined(COMPILER_MICROSOFT)
    _WriteStatusReg(ARM64_FPCR, control);
  #else
    u64 value = control;
    __asm__ __volatile__("msr FPCR, %0" : : "r"(value));
  #endif
#else
    fesetround(control & roundMask);
#endif
  }

  auto clearStatus() -> void {
#if defined(ARCHITECTURE_AMD64)
    _mm_setcsr(control);
#elif defined(ARCHITECTURE_ARM64)
  #if defined(COMPILER_MICROSOFT)
    _WriteStatusReg(ARM64_FPSR, 0);
  #else
    u64 value = 0;
    __asm__ __volatile__("msr FPSR, %0" : : "r"(value));
  #endif
#else
    feclearexcept(allExcept);
#endif
  }
};

}
