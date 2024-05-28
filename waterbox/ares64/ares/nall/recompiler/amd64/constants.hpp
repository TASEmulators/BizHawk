#pragma once

//{
  struct imm8 {
    explicit imm8(u8 data) : data(data) {}
    u8 data;
  };

  struct imm16 {
    explicit imm16(u16 data) : data(data) {}
    u16 data;
  };

  struct imm32 {
    explicit imm32(u32 data) : data(data) {}
    u32 data;
  };

  struct imm64 {
    explicit imm64(u64 data) : data(data) {}
    template<typename T> explicit imm64(T* pointer) : data((u64)pointer) {}
    template<typename C, typename R, typename... P> explicit imm64(auto (C::*function)(P...) -> R) {
      union force_cast_ub {
        auto (C::*function)(P...) -> R;
        u64 pointer;
      } cast{function};
      data = cast.pointer;
    }
    template<typename C, typename R, typename... P> explicit imm64(auto (C::*function)(P...) const -> R) {
      union force_cast_ub {
        auto (C::*function)(P...) const -> R;
        u64 pointer;
      } cast{function};
      data = cast.pointer;
    }
    u64 data;
  };

  struct mem32 {
    explicit mem32(u64 data) : data(data) {}
    template<typename T> explicit mem32(T* pointer) : data((u64)pointer) {}
    template<typename T, typename C> explicit mem32(T C::*variable, C* object) {
      union force_cast_ub {
        T C::*variable;
        u64 pointer;
      } cast{variable};
      data = cast.pointer + u64(object);
    }
    u64 data;
  };

  struct mem64 {
    explicit mem64(u64 data) : data(data) {}
    template<typename T> explicit mem64(T* pointer) : data((u64)pointer) {}
    template<typename T, typename C> explicit mem64(T C::*variable, C* object) {
      union force_cast_ub {
        T C::*variable;
        u64 pointer;
      } cast{variable};
      data = cast.pointer + u64(object);
    }
    u64 data;
  };

  enum class reg8 : u32 {
    al, cl, dl, bl, ah, ch, dh, bh, r8b, r9b, r10b, r11b, r12b, r13b, r14b, r15b,
  };
  friend auto operator&(reg8 r, u32 m) -> u32 {
    return (u32)r & m;
  }
  static constexpr reg8 al   = reg8::al;
  static constexpr reg8 cl   = reg8::cl;
  static constexpr reg8 dl   = reg8::dl;
  static constexpr reg8 bl   = reg8::bl;
  static constexpr reg8 ah   = reg8::ah;
  static constexpr reg8 ch   = reg8::ch;
  static constexpr reg8 dh   = reg8::dh;
  static constexpr reg8 bh   = reg8::bh;
  static constexpr reg8 r8b  = reg8::r8b;
  static constexpr reg8 r9b  = reg8::r9b;
  static constexpr reg8 r10b = reg8::r10b;
  static constexpr reg8 r11b = reg8::r11b;
  static constexpr reg8 r12b = reg8::r12b;
  static constexpr reg8 r13b = reg8::r13b;
  static constexpr reg8 r14b = reg8::r14b;
  static constexpr reg8 r15b = reg8::r15b;

  enum class reg16 : u32 {
    ax, cx, dx, bx, sp, bp, si, di, r8w, r9w, r10w, r11w, r12w, r13w, r14w, r15w,
  };
  friend auto operator&(reg16 r, u32 m) -> u32 {
    return (u32)r & m;
  }
  static constexpr reg16 ax   = reg16::ax;
  static constexpr reg16 cx   = reg16::cx;
  static constexpr reg16 dx   = reg16::dx;
  static constexpr reg16 bx   = reg16::bx;
  static constexpr reg16 sp   = reg16::sp;
  static constexpr reg16 bp   = reg16::bp;
  static constexpr reg16 si   = reg16::si;
  static constexpr reg16 di   = reg16::di;
  static constexpr reg16 r8w  = reg16::r8w;
  static constexpr reg16 r9w  = reg16::r9w;
  static constexpr reg16 r10w = reg16::r10w;
  static constexpr reg16 r11w = reg16::r11w;
  static constexpr reg16 r12w = reg16::r12w;
  static constexpr reg16 r13w = reg16::r13w;
  static constexpr reg16 r14w = reg16::r14w;
  static constexpr reg16 r15w = reg16::r15w;

  enum class reg32 : u32 {
    eax, ecx, edx, ebx, esp, ebp, esi, edi, r8d, r9d, r10d, r11d, r12d, r13d, r14d, r15d,
  };
  friend auto operator&(reg32 r, u32 m) -> u32 {
    return (u32)r & m;
  }
  static constexpr reg32 eax  = reg32::eax;
  static constexpr reg32 ecx  = reg32::ecx;
  static constexpr reg32 edx  = reg32::edx;
  static constexpr reg32 ebx  = reg32::ebx;
  static constexpr reg32 esp  = reg32::esp;
  static constexpr reg32 ebp  = reg32::ebp;
  static constexpr reg32 esi  = reg32::esi;
  static constexpr reg32 edi  = reg32::edi;
  static constexpr reg32 r8d  = reg32::r8d;
  static constexpr reg32 r9d  = reg32::r9d;
  static constexpr reg32 r10d = reg32::r10d;
  static constexpr reg32 r11d = reg32::r11d;
  static constexpr reg32 r12d = reg32::r12d;
  static constexpr reg32 r13d = reg32::r13d;
  static constexpr reg32 r14d = reg32::r14d;
  static constexpr reg32 r15d = reg32::r15d;

  enum class reg64 : u32 {
    rax, rcx, rdx, rbx, rsp, rbp, rsi, rdi, r8, r9, r10, r11, r12, r13, r14, r15,
  };
  friend auto operator&(reg64 r, u32 m) -> u32 {
    return (u32)r & m;
  }
  static constexpr reg64 rax = reg64::rax;
  static constexpr reg64 rcx = reg64::rcx;
  static constexpr reg64 rdx = reg64::rdx;
  static constexpr reg64 rbx = reg64::rbx;
  static constexpr reg64 rsp = reg64::rsp;
  static constexpr reg64 rbp = reg64::rbp;
  static constexpr reg64 rsi = reg64::rsi;
  static constexpr reg64 rdi = reg64::rdi;
  static constexpr reg64 r8  = reg64::r8;
  static constexpr reg64 r9  = reg64::r9;
  static constexpr reg64 r10 = reg64::r10;
  static constexpr reg64 r11 = reg64::r11;
  static constexpr reg64 r12 = reg64::r12;
  static constexpr reg64 r13 = reg64::r13;
  static constexpr reg64 r14 = reg64::r14;
  static constexpr reg64 r15 = reg64::r15;

  struct dis {
    explicit dis(reg64 reg) : reg(reg) {}
    reg64 reg;
  };

  struct dis8 {
    explicit dis8(reg64 reg, s8 imm) : reg(reg), imm(imm) {}
    reg64 reg;
    s8 imm;
  };

  struct dis32 {
    explicit dis32(reg64 reg, s32 imm) : reg(reg), imm(imm) {}
    reg64 reg;
    s32 imm;
  };
//};
