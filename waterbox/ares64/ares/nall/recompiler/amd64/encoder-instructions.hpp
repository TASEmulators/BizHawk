#pragma once

//{
  alwaysinline auto clc()  { emit.byte(0xf8); }
  alwaysinline auto cmc()  { emit.byte(0xf5); }
  alwaysinline auto lahf() { emit.byte(0x9f); }
  alwaysinline auto sahf() { emit.byte(0x9e); }
  alwaysinline auto stc()  { emit.byte(0xf9); }
  alwaysinline auto ret()  { emit.byte(0xc3); }

  //call imm32
  alwaysinline auto call(imm32 it) {
    emit.byte(0xe8);
    emit.dword(it.data);
  }

  //jmp imm32
  alwaysinline auto jmp(imm32 it) {
    emit.byte(0xe9);
    emit.dword(it.data);
  }

  //call reg64
  alwaysinline auto call(reg64 rt) {
    emit.rex(0, 0, 0, rt & 8);
    emit.byte(0xff);
    emit.modrm(3, 2, rt & 7);
  }

  //lea reg64,[reg64+imm8]
  alwaysinline auto lea(reg64 rt, dis8 ds) {
    emit.rex(1, rt & 8, 0, ds.reg & 8);
    emit.byte(0x8d);
    emit.modrm(1, rt & 7, ds.reg & 7);
    if(ds.reg == rsp || ds.reg == r12) emit.sib(0, 4, 4);
    emit.byte(ds.imm);
  }

  //lea reg64,[reg64+imm32]
  alwaysinline auto lea(reg64 rt, dis32 ds) {
    emit.rex(1, rt & 8, 0, ds.reg & 8);
    emit.byte(0x8d);
    emit.modrm(2, rt & 7, ds.reg & 7);
    if(ds.reg == rsp || ds.reg == r12) emit.sib(0, 4, 4);
    emit.dword(ds.imm);
  }

  //mov reg8,imm8
  alwaysinline auto mov(reg8 rt, imm8 is) {
    emit.rex(0, 0, 0, rt & 8);
    emit.byte(0xb0 | rt & 7);
    emit.byte(is.data);
  }

  //mov reg32,imm32
  alwaysinline auto mov(reg32 rt, imm32 is) {
    emit.rex(0, 0, 0, rt & 8);
    emit.byte(0xb8 | rt & 7);
    emit.dword(is.data);
  }

  //mov reg64,imm32
  alwaysinline auto mov(reg64 rt, imm32 is) {
    emit.rex(1, 0, 0, rt & 8);
    emit.byte(0xc7);
    emit.modrm(3, 0, rt & 7);
    emit.dword(is.data);
  }

  //mov reg64,imm64
  alwaysinline auto mov(reg64 rt, imm64 is) {
    emit.rex(1, 0, 0, rt & 8);
    emit.byte(0xb8 | rt & 7);
    emit.qword(is.data);
  }

  //mov reg8,[mem64]
  alwaysinline auto mov(reg8 rt, mem64 ps) {
    if(unlikely(rt != al)) throw;
    emit.byte(0xa0);
    emit.qword(ps.data);
  }

  //mov reg16,[mem64]
  alwaysinline auto mov(reg16 rt, mem64 ps) {
    if(unlikely(rt != ax)) throw;
    emit.byte(0x66, 0xa1);
    emit.qword(ps.data);
  }

  //mov reg32,[mem64]
  alwaysinline auto mov(reg32 rt, mem64 ps) {
    if(unlikely(rt != eax)) throw;
    emit.byte(0xa1);
    emit.qword(ps.data);
  }

  //mov reg64,[mem64]
  alwaysinline auto mov(reg64 rt, mem64 ps) {
    if(unlikely(rt != rax)) throw;
    emit.rex(1, 0, 0, 0);
    emit.byte(0xa1);
    emit.qword(ps.data);
  }

  //mov [mem64],reg8
  alwaysinline auto mov(mem64 pt, reg8 rs) {
    if(unlikely(rs != al)) throw;
    emit.byte(0xa2);
    emit.qword(pt.data);
  }

  //mov [mem64+imm8],imm8
  alwaysinline auto movb(dis8 dt, imm8 is) {
    emit.rex(0, 0, 0, dt.reg & 8);
    emit.byte(0xc6);
    emit.modrm(1, 0, dt.reg & 7);
    if(dt.reg == rsp || dt.reg == r12) emit.sib(0, 4, 4);
    emit.byte(dt.imm);
    emit.byte(is.data);
  }

  //mov [mem64],reg16
  alwaysinline auto mov(mem64 pt, reg16 rs) {
    if(unlikely(rs != ax)) throw;
    emit.byte(0x66, 0xa3);
    emit.qword(pt.data);
  }

  //mov [mem64],reg32
  alwaysinline auto mov(mem64 pt, reg32 rs) {
    if(unlikely(rs != eax)) throw;
    emit.byte(0xa3);
    emit.qword(pt.data);
  }

  //mov [mem64],reg64
  alwaysinline auto mov(mem64 pt, reg64 rs) {
    if(unlikely(rs != rax)) throw;
    emit.rex(1, 0, 0, 0);
    emit.byte(0xa3);
    emit.qword(pt.data);
  }

  //op reg8,[reg64]
  #define op(code) \
    emit.rex(0, rt & 8, 0, ds.reg & 8); \
    emit.byte(code); \
    emit.modrm(0, rt & 7, ds.reg & 7); \
    if(ds.reg == rsp || ds.reg == r12) emit.sib(0, 4, 4);
  alwaysinline auto adc(reg8 rt, dis ds) { op(0x12); }
  alwaysinline auto add(reg8 rt, dis ds) { op(0x02); }
  alwaysinline auto and(reg8 rt, dis ds) { op(0x22); }
  alwaysinline auto cmp(reg8 rt, dis ds) { op(0x3a); }
  alwaysinline auto mov(reg8 rt, dis ds) { op(0x8a); }
  alwaysinline auto or (reg8 rt, dis ds) { op(0x0a); }
  alwaysinline auto sbb(reg8 rt, dis ds) { op(0x1a); }
  alwaysinline auto sub(reg8 rt, dis ds) { op(0x2a); }
  alwaysinline auto xor(reg8 rt, dis ds) { op(0x32); }
  #undef op

  //op reg8,[reg64+imm8]
  #define op(code) \
    emit.rex(0, rt & 8, 0, ds.reg & 8); \
    emit.byte(code); \
    emit.modrm(1, rt & 7, ds.reg & 7); \
    if(ds.reg == rsp || ds.reg == r12) emit.sib(0, 4, 4); \
    emit.byte(ds.imm);
  alwaysinline auto adc(reg8 rt, dis8 ds) { op(0x12); }
  alwaysinline auto add(reg8 rt, dis8 ds) { op(0x02); }
  alwaysinline auto and(reg8 rt, dis8 ds) { op(0x22); }
  alwaysinline auto cmp(reg8 rt, dis8 ds) { op(0x3a); }
  alwaysinline auto mov(reg8 rt, dis8 ds) { op(0x8a); }
  alwaysinline auto or (reg8 rt, dis8 ds) { op(0x0a); }
  alwaysinline auto sbb(reg8 rt, dis8 ds) { op(0x1a); }
  alwaysinline auto sub(reg8 rt, dis8 ds) { op(0x2a); }
  alwaysinline auto xor(reg8 rt, dis8 ds) { op(0x32); }
  #undef op

  //op reg32,[reg64]
  #define op(code) \
    emit.rex(0, rt & 8, 0, ds.reg & 8); \
    emit.byte(code); \
    emit.modrm(0, rt & 7, ds.reg & 7); \
    if(ds.reg == rsp || ds.reg == r12) emit.sib(0, 4, 4);
  alwaysinline auto adc(reg32 rt, dis ds) { op(0x13); }
  alwaysinline auto add(reg32 rt, dis ds) { op(0x03); }
  alwaysinline auto and(reg32 rt, dis ds) { op(0x23); }
  alwaysinline auto cmp(reg32 rt, dis ds) { op(0x3b); }
  alwaysinline auto mov(reg32 rt, dis ds) { op(0x8b); }
  alwaysinline auto or (reg32 rt, dis ds) { op(0x0b); }
  alwaysinline auto sbb(reg32 rt, dis ds) { op(0x1b); }
  alwaysinline auto sub(reg32 rt, dis ds) { op(0x2b); }
  alwaysinline auto xor(reg32 rt, dis ds) { op(0x33); }
  #undef op

  //op reg32,[reg64+imm8]
  #define op(code) \
    emit.rex(0, rt & 8, 0, ds.reg & 8); \
    emit.byte(code); \
    emit.modrm(1, rt & 7, ds.reg & 7); \
    if(ds.reg == rsp || ds.reg == r12) emit.sib(0, 4, 4); \
    emit.byte(ds.imm);
  alwaysinline auto adc(reg32 rt, dis8 ds) { op(0x13); }
  alwaysinline auto add(reg32 rt, dis8 ds) { op(0x03); }
  alwaysinline auto and(reg32 rt, dis8 ds) { op(0x23); }
  alwaysinline auto cmp(reg32 rt, dis8 ds) { op(0x3b); }
  alwaysinline auto mov(reg32 rt, dis8 ds) { op(0x8b); }
  alwaysinline auto or (reg32 rt, dis8 ds) { op(0x0b); }
  alwaysinline auto sbb(reg32 rt, dis8 ds) { op(0x1b); }
  alwaysinline auto sub(reg32 rt, dis8 ds) { op(0x2b); }
  alwaysinline auto xor(reg32 rt, dis8 ds) { op(0x33); }
  #undef op

  //op reg64,[reg64]
  #define op(code) \
    emit.rex(1, rt & 8, 0, ds.reg & 8); \
    emit.byte(code); \
    emit.modrm(0, rt & 7, ds.reg & 7); \
    if(ds.reg == rsp || ds.reg == r12) emit.sib(0, 4, 4);
  alwaysinline auto adc(reg64 rt, dis ds) { op(0x13); }
  alwaysinline auto add(reg64 rt, dis ds) { op(0x03); }
  alwaysinline auto and(reg64 rt, dis ds) { op(0x23); }
  alwaysinline auto cmp(reg64 rt, dis ds) { op(0x3b); }
  alwaysinline auto mov(reg64 rt, dis ds) { op(0x8b); }
  alwaysinline auto or (reg64 rt, dis ds) { op(0x0b); }
  alwaysinline auto sbb(reg64 rt, dis ds) { op(0x1b); }
  alwaysinline auto sub(reg64 rt, dis ds) { op(0x2b); }
  alwaysinline auto xor(reg64 rt, dis ds) { op(0x33); }
  #undef op

  //op reg64,[reg64+imm8]
  #define op(code) \
    emit.rex(1, rt & 8, 0, ds.reg & 8); \
    emit.byte(code); \
    emit.modrm(1, rt & 7, ds.reg & 7); \
    if(ds.reg == rsp || ds.reg == r12) emit.sib(0, 4, 4); \
    emit.byte(ds.imm);
  alwaysinline auto adc(reg64 rt, dis8 ds) { op(0x13); }
  alwaysinline auto add(reg64 rt, dis8 ds) { op(0x03); }
  alwaysinline auto and(reg64 rt, dis8 ds) { op(0x23); }
  alwaysinline auto cmp(reg64 rt, dis8 ds) { op(0x3b); }
  alwaysinline auto mov(reg64 rt, dis8 ds) { op(0x8b); }
  alwaysinline auto or (reg64 rt, dis8 ds) { op(0x0b); }
  alwaysinline auto sbb(reg64 rt, dis8 ds) { op(0x1b); }
  alwaysinline auto sub(reg64 rt, dis8 ds) { op(0x2b); }
  alwaysinline auto xor(reg64 rt, dis8 ds) { op(0x33); }
  #undef op

  //op reg64,[reg64+imm32]
  #define op(code) \
    emit.rex(1, rt & 8, 0, ds.reg & 8); \
    emit.byte(code); \
    emit.modrm(2, rt & 7, ds.reg & 7); \
    if(ds.reg == rsp || ds.reg == r12) emit.sib(0, 4, 4); \
    emit.dword(ds.imm);
  alwaysinline auto adc(reg64 rt, dis32 ds) { op(0x13); }
  alwaysinline auto add(reg64 rt, dis32 ds) { op(0x03); }
  alwaysinline auto and(reg64 rt, dis32 ds) { op(0x23); }
  alwaysinline auto cmp(reg64 rt, dis32 ds) { op(0x3b); }
  alwaysinline auto mov(reg64 rt, dis32 ds) { op(0x8b); }
  alwaysinline auto or (reg64 rt, dis32 ds) { op(0x0b); }
  alwaysinline auto sbb(reg64 rt, dis32 ds) { op(0x1b); }
  alwaysinline auto sub(reg64 rt, dis32 ds) { op(0x2b); }
  alwaysinline auto xor(reg64 rt, dis32 ds) { op(0x33); }
  #undef op

  //op [reg64+imm8],reg8
  #define op(code) \
    emit.rex(0, rs & 8, 0, dt.reg & 8); \
    emit.byte(code); \
    emit.modrm(1, rs & 7, dt.reg & 7); \
    if(dt.reg == rsp || dt.reg == r12) emit.sib(0, 4, 4); \
    emit.byte(dt.imm);
  alwaysinline auto adc(dis8 dt, reg8 rs) { op(0x10); }
  alwaysinline auto add(dis8 dt, reg8 rs) { op(0x00); }
  alwaysinline auto and(dis8 dt, reg8 rs) { op(0x20); }
  alwaysinline auto cmp(dis8 dt, reg8 rs) { op(0x38); }
  alwaysinline auto mov(dis8 dt, reg8 rs) { op(0x88); }
  alwaysinline auto or (dis8 dt, reg8 rs) { op(0x08); }
  alwaysinline auto sbb(dis8 dt, reg8 rs) { op(0x18); }
  alwaysinline auto sub(dis8 dt, reg8 rs) { op(0x28); }
  alwaysinline auto xor(dis8 dt, reg8 rs) { op(0x30); }
  #undef op

  //op reg64,imm32
  #define op(group) \
    emit.rex(1, 0, 0, rt & 8); \
    emit.byte(0x81); \
    emit.modrm(3, group, rt & 7); \
    emit.dword(is.data);
  alwaysinline auto add(reg64 rt, imm32 is) { op(0); }
  alwaysinline auto or (reg64 rt, imm32 is) { op(1); }
  alwaysinline auto adc(reg64 rt, imm32 is) { op(2); }
  alwaysinline auto sbb(reg64 rt, imm32 is) { op(3); }
  alwaysinline auto and(reg64 rt, imm32 is) { op(4); }
  alwaysinline auto sub(reg64 rt, imm32 is) { op(5); }
  alwaysinline auto xor(reg64 rt, imm32 is) { op(6); }
  alwaysinline auto cmp(reg64 rt, imm32 is) { op(7); }
  #undef op

  //op.d [reg64+imm8],imm8
  #define op(group) \
    emit.rex(0, 0, 0, dt.reg & 8); \
    emit.byte(0x83); \
    emit.modrm(1, group, dt.reg & 7); \
    if(dt.reg == rsp || dt.reg == r12) emit.sib(0, 4, 4); \
    emit.byte(dt.imm); \
    emit.byte(is.data);
  alwaysinline auto addd(dis8 dt, imm8 is) { op(0); }
  alwaysinline auto ord (dis8 dt, imm8 is) { op(1); }
  alwaysinline auto adcd(dis8 dt, imm8 is) { op(2); }
  alwaysinline auto sbbd(dis8 dt, imm8 is) { op(3); }
  alwaysinline auto andd(dis8 dt, imm8 is) { op(4); }
  alwaysinline auto subd(dis8 dt, imm8 is) { op(5); }
  alwaysinline auto xord(dis8 dt, imm8 is) { op(6); }
  alwaysinline auto cmpd(dis8 dt, imm8 is) { op(7); }
  #undef op

  //op [reg64],reg32
  #define op(code) \
    emit.rex(0, rs & 8, 0, dt.reg & 8); \
    emit.byte(code); \
    emit.modrm(0, rs & 7, dt.reg & 7); \
    if(dt.reg == rsp || dt.reg == r12) emit.sib(0, 4, 4);
  alwaysinline auto adc(dis dt, reg32 rs) { op(0x11); }
  alwaysinline auto add(dis dt, reg32 rs) { op(0x01); }
  alwaysinline auto and(dis dt, reg32 rs) { op(0x21); }
  alwaysinline auto cmp(dis dt, reg32 rs) { op(0x39); }
  alwaysinline auto mov(dis dt, reg32 rs) { op(0x89); }
  alwaysinline auto or (dis dt, reg32 rs) { op(0x09); }
  alwaysinline auto sbb(dis dt, reg32 rs) { op(0x19); }
  alwaysinline auto sub(dis dt, reg32 rs) { op(0x29); }
  alwaysinline auto xor(dis dt, reg32 rs) { op(0x31); }
  #undef op

  //op [reg64+imm8],reg32
  #define op(code) \
    emit.rex(0, rs & 8, 0, dt.reg & 8); \
    emit.byte(code); \
    emit.modrm(1, rs & 7, dt.reg & 7); \
    if(dt.reg == rsp || dt.reg == r12) emit.sib(0, 4, 4); \
    emit.byte(dt.imm);
  alwaysinline auto adc(dis8 dt, reg32 rs) { op(0x11); }
  alwaysinline auto add(dis8 dt, reg32 rs) { op(0x01); }
  alwaysinline auto and(dis8 dt, reg32 rs) { op(0x21); }
  alwaysinline auto cmp(dis8 dt, reg32 rs) { op(0x39); }
  alwaysinline auto mov(dis8 dt, reg32 rs) { op(0x89); }
  alwaysinline auto or (dis8 dt, reg32 rs) { op(0x09); }
  alwaysinline auto sbb(dis8 dt, reg32 rs) { op(0x19); }
  alwaysinline auto sub(dis8 dt, reg32 rs) { op(0x29); }
  alwaysinline auto xor(dis8 dt, reg32 rs) { op(0x31); }
  #undef op

  //op [reg64],reg64
  #define op(code) \
    emit.rex(0, rs & 8, 0, dt.reg & 8); \
    emit.byte(code); \
    emit.modrm(0, rs & 7, dt.reg & 7); \
    if(dt.reg == rsp || dt.reg == r12) emit.sib(0, 4, 4);
  alwaysinline auto adc(dis dt, reg64 rs) { op(0x11); }
  alwaysinline auto add(dis dt, reg64 rs) { op(0x01); }
  alwaysinline auto and(dis dt, reg64 rs) { op(0x21); }
  alwaysinline auto cmp(dis dt, reg64 rs) { op(0x39); }
  alwaysinline auto mov(dis dt, reg64 rs) { op(0x89); }
  alwaysinline auto or (dis dt, reg64 rs) { op(0x09); }
  alwaysinline auto sbb(dis dt, reg64 rs) { op(0x19); }
  alwaysinline auto sub(dis dt, reg64 rs) { op(0x29); }
  alwaysinline auto xor(dis dt, reg64 rs) { op(0x31); }
  #undef op

  //op [reg64+imm8],reg64
  #define op(code) \
    emit.rex(1, rs & 8, 0, dt.reg & 8); \
    emit.byte(code); \
    emit.modrm(1, rs & 7, dt.reg & 7); \
    if(dt.reg == rsp || dt.reg == r12) emit.sib(0, 4, 4); \
    emit.byte(dt.imm);
  alwaysinline auto adc(dis8 dt, reg64 rs) { op(0x11); }
  alwaysinline auto add(dis8 dt, reg64 rs) { op(0x01); }
  alwaysinline auto and(dis8 dt, reg64 rs) { op(0x21); }
  alwaysinline auto cmp(dis8 dt, reg64 rs) { op(0x39); }
  alwaysinline auto mov(dis8 dt, reg64 rs) { op(0x89); }
  alwaysinline auto or (dis8 dt, reg64 rs) { op(0x09); }
  alwaysinline auto sbb(dis8 dt, reg64 rs) { op(0x19); }
  alwaysinline auto sub(dis8 dt, reg64 rs) { op(0x29); }
  alwaysinline auto xor(dis8 dt, reg64 rs) { op(0x31); }
  #undef op

  //op [reg64+imm32],reg64
  #define op(code) \
    emit.rex(1, rs & 8, 0, dt.reg & 8); \
    emit.byte(code); \
    emit.modrm(2, rs & 7, dt.reg & 7); \
    if(dt.reg == rsp || dt.reg == r12) emit.sib(0, 4, 4); \
    emit.dword(dt.imm);
  alwaysinline auto adc(dis32 dt, reg64 rs) { op(0x11); }
  alwaysinline auto add(dis32 dt, reg64 rs) { op(0x01); }
  alwaysinline auto and(dis32 dt, reg64 rs) { op(0x21); }
  alwaysinline auto cmp(dis32 dt, reg64 rs) { op(0x39); }
  alwaysinline auto mov(dis32 dt, reg64 rs) { op(0x89); }
  alwaysinline auto or (dis32 dt, reg64 rs) { op(0x09); }
  alwaysinline auto sbb(dis32 dt, reg64 rs) { op(0x19); }
  alwaysinline auto sub(dis32 dt, reg64 rs) { op(0x29); }
  alwaysinline auto xor(dis32 dt, reg64 rs) { op(0x31); }
  #undef op

  //op reg32,reg8
  #define op(code) \
    emit.rex(0, rt & 8, 0, rs & 8); \
    emit.byte(0x0f, code); \
    emit.modrm(3, rt & 7, rs & 7);
  alwaysinline auto movsx(reg32 rt, reg8 rs) { op(0xbe); }
  alwaysinline auto movzx(reg32 rt, reg8 rs) { op(0xb6); }
  #undef op

  //op reg32,reg16
  #define op(code) \
    emit.rex(0, rt & 8, 0, rs & 8); \
    emit.byte(0x0f, code); \
    emit.modrm(3, rt & 7, rs & 7);
  alwaysinline auto movsx(reg32 rt, reg16 rs) { op(0xbf); }
  alwaysinline auto movzx(reg32 rt, reg16 rs) { op(0xb7); }
  #undef op

  alwaysinline auto movsxd(reg64 rt, reg32 rs) {
    emit.rex(1, rt & 8, 0, rs & 8);
    emit.byte(0x63);
    emit.modrm(3, rt & 7, rs & 7);
  }

  //incd [reg64+imm8]
  alwaysinline auto incd(dis8 dt) {
    emit.rex(0, 0, 0, dt.reg & 8);
    emit.byte(0xff);
    emit.modrm(1, 0, dt.reg & 7);
    if(dt.reg == rsp || dt.reg == r12) emit.sib(0, 4, 4);
    emit.byte(dt.imm);
  }

  //decd [reg64+imm8]
  alwaysinline auto decd(dis8 dt) {
    emit.rex(0, 0, 0, dt.reg & 8);
    emit.byte(0xff);
    emit.modrm(1, 1, dt.reg & 7);
    if(dt.reg == rsp || dt.reg == r12) emit.sib(0, 4, 4);
    emit.byte(dt.imm);
  }

  //inc reg32
  alwaysinline auto inc(reg32 rt) {
    emit.rex(0, 0, 0, rt & 8);
    emit.byte(0xff);
    emit.modrm(3, 0, rt & 7);
  }

  //dec reg32
  alwaysinline auto dec(reg32 rt) {
    emit.rex(0, 0, 0, rt & 8);
    emit.byte(0xff);
    emit.modrm(3, 1, rt & 7);
  }

  //inc reg64
  alwaysinline auto inc(reg64 rt) {
    emit.rex(1, 0, 0, rt & 8);
    emit.byte(0xff);
    emit.modrm(3, 0, rt & 7);
  }

  //dec reg64
  alwaysinline auto dec(reg64 rt) {
    emit.rex(1, 0, 0, rt & 8);
    emit.byte(0xff);
    emit.modrm(3, 1, rt & 7);
  }

  #define op(code) \
    emit.rex(0, 0, 0, rt & 8); \
    emit.byte(0xd0); \
    emit.modrm(3, code, rt & 7);
  alwaysinline auto rol(reg8 rt) { op(0); }
  alwaysinline auto ror(reg8 rt) { op(1); }
  alwaysinline auto rcl(reg8 rt) { op(2); }
  alwaysinline auto rcr(reg8 rt) { op(3); }
  alwaysinline auto shl(reg8 rt) { op(4); }
  alwaysinline auto shr(reg8 rt) { op(5); }
  alwaysinline auto sal(reg8 rt) { op(6); }
  alwaysinline auto sar(reg8 rt) { op(7); }
  #undef op

  #define op(code) \
    emit.rex(0, 0, 0, rt & 8); \
    emit.byte(0xd1); \
    emit.modrm(3, code, rt & 7);
  alwaysinline auto rol(reg32 rt) { op(0); }
  alwaysinline auto ror(reg32 rt) { op(1); }
  alwaysinline auto rcl(reg32 rt) { op(2); }
  alwaysinline auto rcr(reg32 rt) { op(3); }
  alwaysinline auto shl(reg32 rt) { op(4); }
  alwaysinline auto shr(reg32 rt) { op(5); }
  alwaysinline auto sal(reg32 rt) { op(6); }
  alwaysinline auto sar(reg32 rt) { op(7); }
  #undef op

  #define op(code) \
    emit.rex(0, 0, 0, rt & 8); \
    emit.byte(0xc1); \
    emit.modrm(3, code, rt & 7); \
    emit.byte(is.data);
  alwaysinline auto rol(reg32 rt, imm8 is) { op(0); }
  alwaysinline auto ror(reg32 rt, imm8 is) { op(1); }
  alwaysinline auto rcl(reg32 rt, imm8 is) { op(2); }
  alwaysinline auto rcr(reg32 rt, imm8 is) { op(3); }
  alwaysinline auto shl(reg32 rt, imm8 is) { op(4); }
  alwaysinline auto shr(reg32 rt, imm8 is) { op(5); }
  alwaysinline auto sal(reg32 rt, imm8 is) { op(6); }
  alwaysinline auto sar(reg32 rt, imm8 is) { op(7); }
  #undef op

  #define op(code) \
    if(unlikely(rs != cl)) throw; \
    emit.rex(0, 0, 0, rt & 8); \
    emit.byte(0xd3); \
    emit.modrm(3, code, rt & 7);
  alwaysinline auto rol(reg32 rt, reg8 rs) { op(0); }
  alwaysinline auto ror(reg32 rt, reg8 rs) { op(1); }
  alwaysinline auto rcl(reg32 rt, reg8 rs) { op(2); }
  alwaysinline auto rcr(reg32 rt, reg8 rs) { op(3); }
  alwaysinline auto shl(reg32 rt, reg8 rs) { op(4); }
  alwaysinline auto shr(reg32 rt, reg8 rs) { op(5); }
  alwaysinline auto sal(reg32 rt, reg8 rs) { op(6); }
  alwaysinline auto sar(reg32 rt, reg8 rs) { op(7); }
  #undef op

  #define op(code) \
    emit.rex(1, 0, 0, rt & 8); \
    emit.byte(0xc1); \
    emit.modrm(3, code, rt & 7); \
    emit.byte(is.data);
  alwaysinline auto rol(reg64 rt, imm8 is) { op(0); }
  alwaysinline auto ror(reg64 rt, imm8 is) { op(1); }
  alwaysinline auto rcl(reg64 rt, imm8 is) { op(2); }
  alwaysinline auto rcr(reg64 rt, imm8 is) { op(3); }
  alwaysinline auto shl(reg64 rt, imm8 is) { op(4); }
  alwaysinline auto shr(reg64 rt, imm8 is) { op(5); }
  alwaysinline auto sal(reg64 rt, imm8 is) { op(6); }
  alwaysinline auto sar(reg64 rt, imm8 is) { op(7); }
  #undef op

  #define op(code) \
    if(unlikely(rs != cl)) throw; \
    emit.rex(1, 0, 0, rt & 8); \
    emit.byte(0xd3); \
    emit.modrm(3, code, rt & 7);
  alwaysinline auto rol(reg64 rt, reg8 rs) { op(0); }
  alwaysinline auto ror(reg64 rt, reg8 rs) { op(1); }
  alwaysinline auto rcl(reg64 rt, reg8 rs) { op(2); }
  alwaysinline auto rcr(reg64 rt, reg8 rs) { op(3); }
  alwaysinline auto shl(reg64 rt, reg8 rs) { op(4); }
  alwaysinline auto shr(reg64 rt, reg8 rs) { op(5); }
  alwaysinline auto sal(reg64 rt, reg8 rs) { op(6); }
  alwaysinline auto sar(reg64 rt, reg8 rs) { op(7); }
  #undef op

  //push reg
  alwaysinline auto push(reg64 rt) {
    emit.rex(0, 0, 0, rt & 8);
    emit.byte(0x50 | rt & 7);
  }

  //pop reg
  alwaysinline auto pop(reg64 rt) {
    emit.rex(0, 0, 0, rt & 8);
    emit.byte(0x58 | rt & 7);
  }

  #define op(code) \
    emit.rex(0, rs & 8, 0, rt & 8); \
    emit.byte(code); \
    emit.modrm(3, rs & 7, rt & 7);
  alwaysinline auto adc (reg8 rt, reg8 rs) { op(0x10); }
  alwaysinline auto add (reg8 rt, reg8 rs) { op(0x00); }
  alwaysinline auto and (reg8 rt, reg8 rs) { op(0x20); }
  alwaysinline auto cmp (reg8 rt, reg8 rs) { op(0x38); }
  alwaysinline auto mov (reg8 rt, reg8 rs) { op(0x88); }
  alwaysinline auto or  (reg8 rt, reg8 rs) { op(0x08); }
  alwaysinline auto sbb (reg8 rt, reg8 rs) { op(0x18); }
  alwaysinline auto sub (reg8 rt, reg8 rs) { op(0x28); }
  alwaysinline auto test(reg8 rt, reg8 rs) { op(0x84); }
  alwaysinline auto xor (reg8 rt, reg8 rs) { op(0x30); }
  #undef op

  #define op(code) \
    emit.byte(0x66); \
    emit.rex(0, rs & 8, 0, rt & 8); \
    emit.byte(code); \
    emit.modrm(3, rs & 7, rt & 7);
  alwaysinline auto adc (reg16 rt, reg16 rs) { op(0x11); }
  alwaysinline auto add (reg16 rt, reg16 rs) { op(0x01); }
  alwaysinline auto and (reg16 rt, reg16 rs) { op(0x21); }
  alwaysinline auto cmp (reg16 rt, reg16 rs) { op(0x39); }
  alwaysinline auto mov (reg16 rt, reg16 rs) { op(0x89); }
  alwaysinline auto or  (reg16 rt, reg16 rs) { op(0x09); }
  alwaysinline auto sbb (reg16 rt, reg16 rs) { op(0x19); }
  alwaysinline auto sub (reg16 rt, reg16 rs) { op(0x29); }
  alwaysinline auto test(reg16 rt, reg16 rs) { op(0x85); }
  alwaysinline auto xor (reg16 rt, reg16 rs) { op(0x31); }
  #undef op

  #define op(code) \
    emit.rex(0, rs & 8, 0, rt & 8); \
    emit.byte(code); \
    emit.modrm(3, rs & 7, rt & 7);
  alwaysinline auto adc (reg32 rt, reg32 rs) { op(0x11); }
  alwaysinline auto add (reg32 rt, reg32 rs) { op(0x01); }
  alwaysinline auto and (reg32 rt, reg32 rs) { op(0x21); }
  alwaysinline auto cmp (reg32 rt, reg32 rs) { op(0x39); }
  alwaysinline auto mov (reg32 rt, reg32 rs) { op(0x89); }
  alwaysinline auto or  (reg32 rt, reg32 rs) { op(0x09); }
  alwaysinline auto sbb (reg32 rt, reg32 rs) { op(0x19); }
  alwaysinline auto sub (reg32 rt, reg32 rs) { op(0x29); }
  alwaysinline auto test(reg32 rt, reg32 rs) { op(0x85); }
  alwaysinline auto xor (reg32 rt, reg32 rs) { op(0x31); }
  #undef op

  #define op(code) \
    emit.rex(1, rs & 8, 0, rt & 8); \
    emit.byte(code); \
    emit.modrm(3, rs & 7, rt & 7);
  alwaysinline auto adc (reg64 rt, reg64 rs) { op(0x11); }
  alwaysinline auto add (reg64 rt, reg64 rs) { op(0x01); }
  alwaysinline auto and (reg64 rt, reg64 rs) { op(0x21); }
  alwaysinline auto cmp (reg64 rt, reg64 rs) { op(0x39); }
  alwaysinline auto mov (reg64 rt, reg64 rs) { op(0x89); }
  alwaysinline auto or  (reg64 rt, reg64 rs) { op(0x09); }
  alwaysinline auto sbb (reg64 rt, reg64 rs) { op(0x19); }
  alwaysinline auto sub (reg64 rt, reg64 rs) { op(0x29); }
  alwaysinline auto test(reg64 rt, reg64 rs) { op(0x85); }
  alwaysinline auto xor (reg64 rt, reg64 rs) { op(0x31); }
  #undef op

  #define op(code) \
    emit.rex(0, 0, 0, rt & 8); \
    emit.byte(0x83); \
    emit.modrm(3, code, rt & 7); \
    emit.byte(is.data);
  alwaysinline auto adc(reg32 rt, imm8 is) { op(2); }
  alwaysinline auto add(reg32 rt, imm8 is) { op(0); }
  alwaysinline auto and(reg32 rt, imm8 is) { op(4); }
  alwaysinline auto cmp(reg32 rt, imm8 is) { op(7); }
  alwaysinline auto or (reg32 rt, imm8 is) { op(1); }
  alwaysinline auto sbb(reg32 rt, imm8 is) { op(3); }
  alwaysinline auto sub(reg32 rt, imm8 is) { op(5); }
  alwaysinline auto xor(reg32 rt, imm8 is) { op(6); }
  #undef op

  #define op(code) \
    emit.rex(1, 0, 0, rt & 8); \
    emit.byte(0x83); \
    emit.modrm(3, code, rt & 7); \
    emit.byte(is.data);
  alwaysinline auto adc(reg64 rt, imm8 is) { op(2); }
  alwaysinline auto add(reg64 rt, imm8 is) { op(0); }
  alwaysinline auto and(reg64 rt, imm8 is) { op(4); }
  alwaysinline auto cmp(reg64 rt, imm8 is) { op(7); }
  alwaysinline auto or (reg64 rt, imm8 is) { op(1); }
  alwaysinline auto sbb(reg64 rt, imm8 is) { op(3); }
  alwaysinline auto sub(reg64 rt, imm8 is) { op(5); }
  alwaysinline auto xor(reg64 rt, imm8 is) { op(6); }
  #undef op

  #define op(code, group) \
    if(rt == al) { \
      emit.byte(code); \
      emit.byte(is.data); \
    } else { \
      emit.rex(0, 0, 0, rt & 8); \
      emit.byte(0x80); \
      emit.modrm(3, group, rt & 7); \
      emit.byte(is.data); \
    }
  alwaysinline auto adc(reg8 rt, imm8 is) { op(0x14, 2); }
  alwaysinline auto add(reg8 rt, imm8 is) { op(0x04, 0); }
  alwaysinline auto and(reg8 rt, imm8 is) { op(0x24, 4); }
  alwaysinline auto cmp(reg8 rt, imm8 is) { op(0x3c, 7); }
  alwaysinline auto or (reg8 rt, imm8 is) { op(0x0c, 1); }
  alwaysinline auto sbb(reg8 rt, imm8 is) { op(0x1c, 3); }
  alwaysinline auto sub(reg8 rt, imm8 is) { op(0x2c, 5); }
  alwaysinline auto xor(reg8 rt, imm8 is) { op(0x34, 6); }
  #undef op

  #define op(code, group) \
    if(rt == eax) { \
      emit.byte(code); \
      emit.dword(is.data); \
    } else { \
      emit.rex(0, 0, 0, rt & 8); \
      emit.byte(0x81); \
      emit.modrm(3, group, rt & 7); \
      emit.dword(is.data); \
    }
  alwaysinline auto adc(reg32 rt, imm32 is) { op(0x15, 2); }
  alwaysinline auto add(reg32 rt, imm32 is) { op(0x05, 0); }
  alwaysinline auto and(reg32 rt, imm32 is) { op(0x25, 4); }
  alwaysinline auto cmp(reg32 rt, imm32 is) { op(0x3d, 7); }
  alwaysinline auto or (reg32 rt, imm32 is) { op(0x0d, 1); }
  alwaysinline auto sbb(reg32 rt, imm32 is) { op(0x1d, 3); }
  alwaysinline auto sub(reg32 rt, imm32 is) { op(0x2d, 5); }
  alwaysinline auto xor(reg32 rt, imm32 is) { op(0x35, 6); }
  #undef op

  #define op(code) \
    emit.rex(0, 0, 0, rt & 8); \
    emit.byte(0xf7); \
    emit.modrm(3, code, rt & 7);
  alwaysinline auto not (reg32 rt) { op(2); }
  alwaysinline auto neg (reg32 rt) { op(3); }
  alwaysinline auto mul (reg32 rt) { op(4); }
  alwaysinline auto imul(reg32 rt) { op(5); }
  alwaysinline auto div (reg32 rt) { op(6); }
  alwaysinline auto idiv(reg32 rt) { op(7); }
  #undef op

  #define op(code) \
    emit.rex(1, 0, 0, rt & 8); \
    emit.byte(0xf7); \
    emit.modrm(3, code, rt & 7);
  alwaysinline auto not (reg64 rt) { op(2); }
  alwaysinline auto neg (reg64 rt) { op(3); }
  alwaysinline auto mul (reg64 rt) { op(4); }
  alwaysinline auto imul(reg64 rt) { op(5); }
  alwaysinline auto div (reg64 rt) { op(6); }
  alwaysinline auto idiv(reg64 rt) { op(7); }
  #undef op

  #define op(code) \
    emit.byte(code); \
    emit.byte(it.data);
  #define r imm8 it{resolve(l, 1, 1)}
  alwaysinline auto jmp (imm8 it) {    op(0xeb); }
  alwaysinline auto jmp8(label l) { r; op(0xeb); }
  alwaysinline auto jnz (imm8 it) {    op(0x75); }
  alwaysinline auto jnz8(label l) { r; op(0x75); }
  alwaysinline auto jz  (imm8 it) {    op(0x74); }
  alwaysinline auto jz8 (label l) { r; op(0x74); }
  #undef r
  #undef op

  #define op(code) \
    emit.byte(0x0f); \
    emit.byte(code); \
    emit.dword(it.data);
  #define r imm32 it{resolve(l, 2, 4)}
  alwaysinline auto jnz(imm32 it) {    op(0x85); }
  alwaysinline auto jnz(label l)  { r; op(0x85); }
  alwaysinline auto jz (imm32 it) {    op(0x84); }
  alwaysinline auto jz (label l)  { r; op(0x84); }
  #undef r
  #undef op

  //op reg8
  #define op(code) \
    emit.rex(0, 0, 0, rt & 8); \
    emit.byte(0x0f); \
    emit.byte(code); \
    emit.modrm(3, 0, rt & 7);
  alwaysinline auto seta (reg8 rt) { op(0x97); }
  alwaysinline auto setbe(reg8 rt) { op(0x96); }
  alwaysinline auto setb (reg8 rt) { op(0x92); }
  alwaysinline auto setc (reg8 rt) { op(0x92); }
  alwaysinline auto setg (reg8 rt) { op(0x9f); }
  alwaysinline auto setge(reg8 rt) { op(0x9d); }
  alwaysinline auto setl (reg8 rt) { op(0x9c); }
  alwaysinline auto setle(reg8 rt) { op(0x9e); }
  alwaysinline auto setnc(reg8 rt) { op(0x93); }
  alwaysinline auto setno(reg8 rt) { op(0x91); }
  alwaysinline auto setnp(reg8 rt) { op(0x9b); }
  alwaysinline auto setns(reg8 rt) { op(0x99); }
  alwaysinline auto setnz(reg8 rt) { op(0x95); }
  alwaysinline auto seto (reg8 rt) { op(0x90); }
  alwaysinline auto setp (reg8 rt) { op(0x9a); }
  alwaysinline auto sets (reg8 rt) { op(0x98); }
  alwaysinline auto setz (reg8 rt) { op(0x94); }
  #undef op

  //op [reg64]
  #define op(code) \
    emit.rex(0, 0, 0, dt.reg & 8); \
    emit.byte(0x0f); \
    emit.byte(code); \
    if(dt.reg == rsp || dt.reg == r12) { \
      emit.modrm(0, 0, dt.reg & 7); \
      emit.sib(0, 4, 4); \
    } else if(dt.reg == rbp || dt.reg == r13) { \
      emit.modrm(1, 0, dt.reg & 7); \
      emit.byte(0x00); \
    } else { \
      emit.modrm(0, 0, dt.reg & 7); \
    }
  alwaysinline auto seta (dis dt) { op(0x97); }
  alwaysinline auto setbe(dis dt) { op(0x96); }
  alwaysinline auto setb (dis dt) { op(0x92); }
  alwaysinline auto setc (dis dt) { op(0x92); }
  alwaysinline auto setg (dis dt) { op(0x9f); }
  alwaysinline auto setge(dis dt) { op(0x9d); }
  alwaysinline auto setl (dis dt) { op(0x9c); }
  alwaysinline auto setle(dis dt) { op(0x9e); }
  alwaysinline auto setnc(dis dt) { op(0x93); }
  alwaysinline auto setno(dis dt) { op(0x91); }
  alwaysinline auto setnp(dis dt) { op(0x9b); }
  alwaysinline auto setns(dis dt) { op(0x99); }
  alwaysinline auto setnz(dis dt) { op(0x95); }
  alwaysinline auto seto (dis dt) { op(0x90); }
  alwaysinline auto setp (dis dt) { op(0x9a); }
  alwaysinline auto sets (dis dt) { op(0x98); }
  alwaysinline auto setz (dis dt) { op(0x94); }
  #undef op

  //op [reg64+imm8]
  #define op(code) \
    emit.rex(0, 0, 0, dt.reg & 8); \
    emit.byte(0x0f); \
    emit.byte(code); \
    emit.modrm(1, 0, dt.reg & 7); \
    if(dt.reg == rsp || dt.reg == r12) { \
      emit.sib(0, 4, 4); \
    } \
    emit.byte(dt.imm);
  alwaysinline auto seta (dis8 dt) { op(0x97); }
  alwaysinline auto setbe(dis8 dt) { op(0x96); }
  alwaysinline auto setb (dis8 dt) { op(0x92); }
  alwaysinline auto setc (dis8 dt) { op(0x92); }
  alwaysinline auto setg (dis8 dt) { op(0x9f); }
  alwaysinline auto setge(dis8 dt) { op(0x9d); }
  alwaysinline auto setl (dis8 dt) { op(0x9c); }
  alwaysinline auto setle(dis8 dt) { op(0x9e); }
  alwaysinline auto setnc(dis8 dt) { op(0x93); }
  alwaysinline auto setno(dis8 dt) { op(0x91); }
  alwaysinline auto setnp(dis8 dt) { op(0x9b); }
  alwaysinline auto setns(dis8 dt) { op(0x99); }
  alwaysinline auto setnz(dis8 dt) { op(0x95); }
  alwaysinline auto seto (dis8 dt) { op(0x90); }
  alwaysinline auto setp (dis8 dt) { op(0x9a); }
  alwaysinline auto sets (dis8 dt) { op(0x98); }
  alwaysinline auto setz (dis8 dt) { op(0x94); }
  #undef op

  //call imm64 (pseudo-op)
  alwaysinline auto call(imm64 target, reg64 scratch) {
    s64 dist = distance(target.data) - 5;
    if(dist < INT32_MIN || dist > INT32_MAX) {
      mov(scratch, target);
      call(scratch);
    } else {
      call(imm32{dist});
    }
  }

  //jmp label (pseudo-op)
  alwaysinline auto jmp(label l) {
    jmp(imm32{resolve(l, 1, 4)});
  }
//};
