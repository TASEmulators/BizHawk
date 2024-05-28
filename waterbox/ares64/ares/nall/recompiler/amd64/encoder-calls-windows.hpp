#pragma once

//{
  //register aliases for function arguments
  static constexpr reg32 ra0d = reg32::ecx;
  static constexpr reg32 ra1d = reg32::edx;
  static constexpr reg32 ra2d = reg32::r8d;
  static constexpr reg32 ra3d = reg32::r9d;
  static constexpr reg32 ra4d = reg32::r10d;  //actually passed on stack
  static constexpr reg32 ra5d = reg32::r11d;  //actually passed on stack

  static constexpr reg64 ra0 = reg64::rcx;
  static constexpr reg64 ra1 = reg64::rdx;
  static constexpr reg64 ra2 = reg64::r8;
  static constexpr reg64 ra3 = reg64::r9;
  static constexpr reg64 ra4 = reg64::r10;  //actually passed on stack
  static constexpr reg64 ra5 = reg64::r11;  //actually passed on stack

  //virtual instructions to call member functions
  template<typename C, typename R, typename... P>
  alwaysinline auto call(auto (C::*function)(P...) -> R, C* object) {
    sub(rsp, imm8{0x28});
    mov(rcx, imm64{object});
    call(imm64{function}, rax);
    add(rsp, imm8{0x28});
  }

  template<typename C, typename R, typename... P, typename P0>
  alwaysinline auto call(auto (C::*function)(P...) -> R, C* object, P0 p0) {
    sub(rsp, imm8{0x28});
    mov(rcx, imm64{object});
    mov(rdx, imm64{p0});
    call(imm64{function}, rax);
    add(rsp, imm8{0x28});
  }

  template<typename C, typename R, typename... P, typename P0, typename P1>
  alwaysinline auto call(auto (C::*function)(P...) -> R, C* object, P0 p0, P1 p1) {
    sub(rsp, imm8{0x28});
    mov(rcx, imm64{object});
    mov(rdx, imm64{p0});
    mov(r8, imm64{p1});
    call(imm64{function}, rax);
    add(rsp, imm8{0x28});
  }

  template<typename C, typename R, typename... P, typename P0, typename P1, typename P2>
  alwaysinline auto call(auto (C::*function)(P...) -> R, C* object, P0 p0, P1 p1, P2 p2) {
    sub(rsp, imm8{0x28});
    mov(rcx, imm64{object});
    mov(rdx, imm64{p0});
    mov(r8, imm64{p1});
    mov(r9, imm64{p2});
    call(imm64{function}, rax);
    add(rsp, imm8{0x28});
  }

  template<typename C, typename R, typename... P, typename P0, typename P1, typename P2, typename P3>
  alwaysinline auto call(auto (C::*function)(P...) -> R, C* object, P0 p0, P1 p1, P2 p2, P3 p3) {
    sub(rsp, imm8{0x38});
    mov(rcx, imm64{object});
    mov(rdx, imm64{p0});
    mov(r8, imm64{p1});
    mov(r9, imm64{p2});
    mov(rax, imm64{p3});
    mov(dis8{rsp, 0x20}, rax);
    call(imm64{function}, rax);
    add(rsp, imm8{0x38});
  }

  template<typename C, typename R, typename... P, typename P0, typename P1, typename P2, typename P3, typename P4>
  alwaysinline auto call(auto (C::*function)(P...) -> R, C* object, P0 p0, P1 p1, P2 p2, P3 p3, P4 p4) {
    sub(rsp, imm8{0x38});
    mov(rcx, imm64{object});
    mov(rdx, imm64{p0});
    mov(r8, imm64{p1});
    mov(r9, imm64{p2});
    mov(rax, imm64{p3});
    mov(dis8{rsp, 0x20}, rax);
    mov(rax, imm64{p4});
    mov(dis8{rsp, 0x28}, rax);
    call(imm64{function}, rax);
    add(rsp, imm8{0x38});
  }
//};
