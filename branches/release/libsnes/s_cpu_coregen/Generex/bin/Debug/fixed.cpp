//##IMPL
//   case 0x00: return op_nop();
void SMPcore::op_nop() {
  op_io();
}
//##IMPL
//   case 0x01: return op_jst();
void SMPcore::op_jst() {
  dp = 0xffde - ((opcode >> 4) << 1);
  rd.l = op_read(dp++);
  rd.h = op_read(dp++);
  op_io();
  op_io();
  op_io();
  op_writesp(regs.pc.h);
  op_writesp(regs.pc.l);
  regs.pc = rd;
}
//##IMPL
//   case 0x02: return op_set_bit();
void SMPcore::op_set_bit() {
  dp = op_readpc();
  rd = op_readdp(dp) & ~(1 << (opcode >> 5));
  op_writedp(dp, rd | (!(opcode & 0x10) << (opcode >> 5)));
}
//##IMPL
//   case 0x03: return op_branch_bit();
void SMPcore::op_branch_bit() {
  dp = op_readpc();
  sp = op_readdp(dp);
  rd = op_readpc();
  op_io();
  if((bool)(sp & (1 << (opcode >> 5))) == (bool)(opcode & 0x10)) return;
  op_io();
  op_io();
  regs.pc += (int8)rd;
}
//##IMPL
//   case 0x04: return op_read_dp<&SMPcore::op_or>(regs.a);
void SMPcore::op_read_dp(uint8 &r) {
  dp = op_readpc();
  rd = op_readdp(dp);
  regs.a = op_or(regs.a, rd);
}
//##IMPL
//   case 0x05: return op_read_addr<&SMPcore::op_or>(regs.a);
void SMPcore::op_read_addr(uint8 &r) {
  dp.l = op_readpc();
  dp.h = op_readpc();
  rd = op_read(dp);
  regs.a = op_or(regs.a, rd);
}
//##IMPL
//   case 0x06: return op_read_ix<&SMPcore::op_or>();
void SMPcore::op_read_ix() {
  op_io();
  rd = op_readdp(regs.x);
  regs.a = op_or(regs.a, rd);
}
//##IMPL
//   case 0x07: return op_read_idpx<&SMPcore::op_or>();
void SMPcore::op_read_idpx() {
  dp = op_readpc() + regs.x;
  op_io();
  sp.l = op_readdp(dp++);
  sp.h = op_readdp(dp++);
  rd = op_read(sp);
  regs.a = op_or(regs.a, rd);
}
//##IMPL
//   case 0x08: return op_read_const<&SMPcore::op_or>(regs.a);
void SMPcore::op_read_const(uint8 &r) {
  rd = op_readpc();
  regs.a = op_or(regs.a, rd);
}
//##IMPL
//   case 0x09: return op_write_dp_dp<&SMPcore::op_or>();
void SMPcore::op_write_dp_dp() {
  sp = op_readpc();
  rd = op_readdp(sp);
  dp = op_readpc();
  wr = op_readdp(dp);
  wr = op_or(wr, rd);
  op_writedp(dp, wr);
}
//##IMPL
//   case 0x0a: return op_set_addr_bit();
void SMPcore::op_set_addr_bit() {
  dp.l = op_readpc();
  dp.h = op_readpc();
  bit = dp >> 13;
  dp &= 0x1fff;
  rd = op_read(dp);
  op_io();
  regs.p.c |= (rd & (1 << bit)) ^ 0;
}
//##IMPL
//   case 0x0b: return op_adjust_dp<&SMPcore::op_asl>();
void SMPcore::op_adjust_dp() {
  dp = op_readpc();
  rd = op_readdp(dp);
  rd = op_asl(rd);
  op_writedp(dp, rd);
}
//##IMPL
//   case 0x0c: return op_adjust_addr<&SMPcore::op_asl>();
void SMPcore::op_adjust_addr() {
  dp.l = op_readpc();
  dp.h = op_readpc();
  rd = op_read(dp);
  rd = op_asl(rd);
  op_write(dp, rd);
}
//##IMPL
//   case 0x0d: return op_push(regs.p);
void SMPcore::op_push(uint8 r) {
  op_io();
  op_io();
  op_writesp(regs.p);
}
//##IMPL
//   case 0x0e: return op_test_addr(1);
void SMPcore::op_test_addr(bool set) {
  dp.l = op_readpc();
  dp.h = op_readpc();
  rd = op_read(dp);
  regs.p.n = (regs.a - rd) & 0x80;
  regs.p.z = (regs.a - rd) == 0;
  op_read(dp);
  op_write(dp, rd | regs.a);
}
//##IMPL
//   case 0x0f: return op_brk();
void SMPcore::op_brk() {
  rd.l = op_read(0xffde);
  rd.h = op_read(0xffdf);
  op_io();
  op_io();
  op_writesp(regs.pc.h);
  op_writesp(regs.pc.l);
  op_writesp(regs.p);
  regs.pc = rd;
  regs.p.b = 1;
  regs.p.i = 0;
}
//##IMPL
//   case 0x10: return op_branch(regs.p.n == 0);
void SMPcore::op_branch(bool condition) {
  rd = op_readpc();
  if(regs.p.n != 0) return;
  op_io();
  op_io();
  regs.pc += (int8)rd;
}
//##IMPL
//   case 0x11: return op_jst();
void SMPcore::op_jst() {
  dp = 0xffde - ((opcode >> 4) << 1);
  rd.l = op_read(dp++);
  rd.h = op_read(dp++);
  op_io();
  op_io();
  op_io();
  op_writesp(regs.pc.h);
  op_writesp(regs.pc.l);
  regs.pc = rd;
}
//##IMPL
//   case 0x12: return op_set_bit();
void SMPcore::op_set_bit() {
  dp = op_readpc();
  rd = op_readdp(dp) & ~(1 << (opcode >> 5));
  op_writedp(dp, rd | (!(opcode & 0x10) << (opcode >> 5)));
}
//##IMPL
//   case 0x13: return op_branch_bit();
void SMPcore::op_branch_bit() {
  dp = op_readpc();
  sp = op_readdp(dp);
  rd = op_readpc();
  op_io();
  if((bool)(sp & (1 << (opcode >> 5))) == (bool)(opcode & 0x10)) return;
  op_io();
  op_io();
  regs.pc += (int8)rd;
}
//##IMPL
//   case 0x14: return op_read_dpi<&SMPcore::op_or>(regs.a, regs.x);
void SMPcore::op_read_dpi(uint8 &r, uint8 &i) {
  dp = op_readpc();
  op_io();
  rd = op_readdp(dp + regs.x);
  regs.a = op_or(regs.a, rd);
}
//##IMPL
//   case 0x15: return op_read_addri<&SMPcore::op_or>(regs.x);
void SMPcore::op_read_addri(uint8 &r) {
  dp.l = op_readpc();
  dp.h = op_readpc();
  op_io();
  rd = op_read(dp + regs.x);
  regs.a = op_or(regs.a, rd);
}
//##IMPL
//   case 0x16: return op_read_addri<&SMPcore::op_or>(regs.y);
void SMPcore::op_read_addri(uint8 &r) {
  dp.l = op_readpc();
  dp.h = op_readpc();
  op_io();
  rd = op_read(dp + regs.y);
  regs.a = op_or(regs.a, rd);
}
//##IMPL
//   case 0x17: return op_read_idpy<&SMPcore::op_or>();
void SMPcore::op_read_idpy() {
  dp = op_readpc();
  op_io();
  sp.l = op_readdp(dp++);
  sp.h = op_readdp(dp++);
  rd = op_read(sp + regs.y);
  regs.a = op_or(regs.a, rd);
}
//##IMPL
//   case 0x18: return op_write_dp_const<&SMPcore::op_or>();
void SMPcore::op_write_dp_const() {
  rd = op_readpc();
  dp = op_readpc();
  wr = op_readdp(dp);
  wr = op_or(wr, rd);
  op_writedp(dp, wr);
}
//##IMPL
//   case 0x19: return op_write_ix_iy<&SMPcore::op_or>();
void SMPcore::op_write_ix_iy() {
  op_io();
  rd = op_readdp(regs.y);
  wr = op_readdp(regs.x);
  wr = op_or(wr, rd);
  op_writedp(regs.x, wr);
}
//##IMPL
//   case 0x1a: return op_adjust_dpw(-1);
void SMPcore::op_adjust_dpw(signed n) {
  dp = op_readpc();
  rd.w = op_readdp(dp) - 1;
  op_writedp(dp++, rd.l);
  rd.h += op_readdp(dp);
  op_writedp(dp++, rd.h);
  regs.p.n = rd & 0x8000;
  regs.p.z = rd == 0;
}
//##IMPL
//   case 0x1b: return op_adjust_dpx<&SMPcore::op_asl>();
void SMPcore::op_adjust_dpx() {
  dp = op_readpc();
  op_io();
  rd = op_readdp(dp + regs.x);
  rd = op_asl(rd);
  op_writedp(dp + regs.x, rd);
}
//##IMPL
//   case 0x1c: return op_adjust<&SMPcore::op_asl>(regs.a);
void SMPcore::op_adjust(uint8 &r) {
  op_io();
  regs.a = op_asl(regs.a);
}
//##IMPL
//   case 0x1d: return op_adjust<&SMPcore::op_dec>(regs.x);
void SMPcore::op_adjust(uint8 &r) {
  op_io();
  regs.x = op_dec(regs.x);
}
//##IMPL
//   case 0x1e: return op_read_addr<&SMPcore::op_cmp>(regs.x);
void SMPcore::op_read_addr(uint8 &r) {
  dp.l = op_readpc();
  dp.h = op_readpc();
  rd = op_read(dp);
  regs.x = op_cmp(regs.x, rd);
}
//##IMPL
//   case 0x1f: return op_jmp_iaddrx();
void SMPcore::op_jmp_iaddrx() {
  dp.l = op_readpc();
  dp.h = op_readpc();
  op_io();
  dp += regs.x;
  rd.l = op_read(dp++);
  rd.h = op_read(dp++);
  regs.pc = rd;
}
//##IMPL
//   case 0x20: return op_set_flag(regs.p.p, 0);
void SMPcore::op_set_flag(bool &flag, bool data) {
  op_io();
  regs.p.p = 0;
}
//##IMPL
//   case 0x21: return op_jst();
void SMPcore::op_jst() {
  dp = 0xffde - ((opcode >> 4) << 1);
  rd.l = op_read(dp++);
  rd.h = op_read(dp++);
  op_io();
  op_io();
  op_io();
  op_writesp(regs.pc.h);
  op_writesp(regs.pc.l);
  regs.pc = rd;
}
//##IMPL
//   case 0x22: return op_set_bit();
void SMPcore::op_set_bit() {
  dp = op_readpc();
  rd = op_readdp(dp) & ~(1 << (opcode >> 5));
  op_writedp(dp, rd | (!(opcode & 0x10) << (opcode >> 5)));
}
//##IMPL
//   case 0x23: return op_branch_bit();
void SMPcore::op_branch_bit() {
  dp = op_readpc();
  sp = op_readdp(dp);
  rd = op_readpc();
  op_io();
  if((bool)(sp & (1 << (opcode >> 5))) == (bool)(opcode & 0x10)) return;
  op_io();
  op_io();
  regs.pc += (int8)rd;
}
//##IMPL
//   case 0x24: return op_read_dp<&SMPcore::op_and>(regs.a);
void SMPcore::op_read_dp(uint8 &r) {
  dp = op_readpc();
  rd = op_readdp(dp);
  regs.a = op_and(regs.a, rd);
}
//##IMPL
//   case 0x25: return op_read_addr<&SMPcore::op_and>(regs.a);
void SMPcore::op_read_addr(uint8 &r) {
  dp.l = op_readpc();
  dp.h = op_readpc();
  rd = op_read(dp);
  regs.a = op_and(regs.a, rd);
}
//##IMPL
//   case 0x26: return op_read_ix<&SMPcore::op_and>();
void SMPcore::op_read_ix() {
  op_io();
  rd = op_readdp(regs.x);
  regs.a = op_and(regs.a, rd);
}
//##IMPL
//   case 0x27: return op_read_idpx<&SMPcore::op_and>();
void SMPcore::op_read_idpx() {
  dp = op_readpc() + regs.x;
  op_io();
  sp.l = op_readdp(dp++);
  sp.h = op_readdp(dp++);
  rd = op_read(sp);
  regs.a = op_and(regs.a, rd);
}
//##IMPL
//   case 0x28: return op_read_const<&SMPcore::op_and>(regs.a);
void SMPcore::op_read_const(uint8 &r) {
  rd = op_readpc();
  regs.a = op_and(regs.a, rd);
}
//##IMPL
//   case 0x29: return op_write_dp_dp<&SMPcore::op_and>();
void SMPcore::op_write_dp_dp() {
  sp = op_readpc();
  rd = op_readdp(sp);
  dp = op_readpc();
  wr = op_readdp(dp);
  wr = op_and(wr, rd);
  op_writedp(dp, wr);
}
//##IMPL
//   case 0x2a: return op_set_addr_bit();
void SMPcore::op_set_addr_bit() {
  dp.l = op_readpc();
  dp.h = op_readpc();
  bit = dp >> 13;
  dp &= 0x1fff;
  rd = op_read(dp);
  op_io();
  regs.p.c |= (rd & (1 << bit)) ^ 1;
}
//##IMPL
//   case 0x2b: return op_adjust_dp<&SMPcore::op_rol>();
void SMPcore::op_adjust_dp() {
  dp = op_readpc();
  rd = op_readdp(dp);
  rd = op_rol(rd);
  op_writedp(dp, rd);
}
//##IMPL
//   case 0x2c: return op_adjust_addr<&SMPcore::op_rol>();
void SMPcore::op_adjust_addr() {
  dp.l = op_readpc();
  dp.h = op_readpc();
  rd = op_read(dp);
  rd = op_rol(rd);
  op_write(dp, rd);
}
//##IMPL
//   case 0x2d: return op_push(regs.a);
void SMPcore::op_push(uint8 r) {
  op_io();
  op_io();
  op_writesp(regs.a);
}
//##IMPL
//   case 0x2e: return op_bne_dp();
void SMPcore::op_bne_dp() {
  dp = op_readpc();
  sp = op_readdp(dp);
  rd = op_readpc();
  op_io();
  if(regs.a == sp) return;
  op_io();
  op_io();
  regs.pc += (int8)rd;
}
//##IMPL
//   case 0x2f: return op_branch(true);
void SMPcore::op_branch(bool condition) {
  rd = op_readpc();
  op_io();
  op_io();
  regs.pc += (int8)rd;
}
//##IMPL
//   case 0x30: return op_branch(regs.p.n == 1);
void SMPcore::op_branch(bool condition) {
  rd = op_readpc();
  if(regs.p.n != 1) return;
  op_io();
  op_io();
  regs.pc += (int8)rd;
}
//##IMPL
//   case 0x31: return op_jst();
void SMPcore::op_jst() {
  dp = 0xffde - ((opcode >> 4) << 1);
  rd.l = op_read(dp++);
  rd.h = op_read(dp++);
  op_io();
  op_io();
  op_io();
  op_writesp(regs.pc.h);
  op_writesp(regs.pc.l);
  regs.pc = rd;
}
//##IMPL
//   case 0x32: return op_set_bit();
void SMPcore::op_set_bit() {
  dp = op_readpc();
  rd = op_readdp(dp) & ~(1 << (opcode >> 5));
  op_writedp(dp, rd | (!(opcode & 0x10) << (opcode >> 5)));
}
//##IMPL
//   case 0x33: return op_branch_bit();
void SMPcore::op_branch_bit() {
  dp = op_readpc();
  sp = op_readdp(dp);
  rd = op_readpc();
  op_io();
  if((bool)(sp & (1 << (opcode >> 5))) == (bool)(opcode & 0x10)) return;
  op_io();
  op_io();
  regs.pc += (int8)rd;
}
//##IMPL
//   case 0x34: return op_read_dpi<&SMPcore::op_and>(regs.a, regs.x);
void SMPcore::op_read_dpi(uint8 &r, uint8 &i) {
  dp = op_readpc();
  op_io();
  rd = op_readdp(dp + regs.x);
  regs.a = op_and(regs.a, rd);
}
//##IMPL
//   case 0x35: return op_read_addri<&SMPcore::op_and>(regs.x);
void SMPcore::op_read_addri(uint8 &r) {
  dp.l = op_readpc();
  dp.h = op_readpc();
  op_io();
  rd = op_read(dp + regs.x);
  regs.a = op_and(regs.a, rd);
}
//##IMPL
//   case 0x36: return op_read_addri<&SMPcore::op_and>(regs.y);
void SMPcore::op_read_addri(uint8 &r) {
  dp.l = op_readpc();
  dp.h = op_readpc();
  op_io();
  rd = op_read(dp + regs.y);
  regs.a = op_and(regs.a, rd);
}
//##IMPL
//   case 0x37: return op_read_idpy<&SMPcore::op_and>();
void SMPcore::op_read_idpy() {
  dp = op_readpc();
  op_io();
  sp.l = op_readdp(dp++);
  sp.h = op_readdp(dp++);
  rd = op_read(sp + regs.y);
  regs.a = op_and(regs.a, rd);
}
//##IMPL
//   case 0x38: return op_write_dp_const<&SMPcore::op_and>();
void SMPcore::op_write_dp_const() {
  rd = op_readpc();
  dp = op_readpc();
  wr = op_readdp(dp);
  wr = op_and(wr, rd);
  op_writedp(dp, wr);
}
//##IMPL
//   case 0x39: return op_write_ix_iy<&SMPcore::op_and>();
void SMPcore::op_write_ix_iy() {
  op_io();
  rd = op_readdp(regs.y);
  wr = op_readdp(regs.x);
  wr = op_and(wr, rd);
  op_writedp(regs.x, wr);
}
//##IMPL
//   case 0x3a: return op_adjust_dpw(+1);
void SMPcore::op_adjust_dpw(signed n) {
  dp = op_readpc();
  rd.w = op_readdp(dp) + 1;
  op_writedp(dp++, rd.l);
  rd.h += op_readdp(dp);
  op_writedp(dp++, rd.h);
  regs.p.n = rd & 0x8000;
  regs.p.z = rd == 0;
}
//##IMPL
//   case 0x3b: return op_adjust_dpx<&SMPcore::op_rol>();
void SMPcore::op_adjust_dpx() {
  dp = op_readpc();
  op_io();
  rd = op_readdp(dp + regs.x);
  rd = op_rol(rd);
  op_writedp(dp + regs.x, rd);
}
//##IMPL
//   case 0x3c: return op_adjust<&SMPcore::op_rol>(regs.a);
void SMPcore::op_adjust(uint8 &r) {
  op_io();
  regs.a = op_rol(regs.a);
}
//##IMPL
//   case 0x3d: return op_adjust<&SMPcore::op_inc>(regs.x);
void SMPcore::op_adjust(uint8 &r) {
  op_io();
  regs.x = op_inc(regs.x);
}
//##IMPL
//   case 0x3e: return op_read_dp<&SMPcore::op_cmp>(regs.x);
void SMPcore::op_read_dp(uint8 &r) {
  dp = op_readpc();
  rd = op_readdp(dp);
  regs.x = op_cmp(regs.x, rd);
}
//##IMPL
//   case 0x3f: return op_jsr_addr();
void SMPcore::op_jsr_addr() {
  rd.l = op_readpc();
  rd.h = op_readpc();
  op_io();
  op_io();
  op_io();
  op_writesp(regs.pc.h);
  op_writesp(regs.pc.l);
  regs.pc = rd;
}
//##IMPL
//   case 0x40: return op_set_flag(regs.p.p, 1);
void SMPcore::op_set_flag(bool &flag, bool data) {
  op_io();
  regs.p.p = 1;
}
//##IMPL
//   case 0x41: return op_jst();
void SMPcore::op_jst() {
  dp = 0xffde - ((opcode >> 4) << 1);
  rd.l = op_read(dp++);
  rd.h = op_read(dp++);
  op_io();
  op_io();
  op_io();
  op_writesp(regs.pc.h);
  op_writesp(regs.pc.l);
  regs.pc = rd;
}
//##IMPL
//   case 0x42: return op_set_bit();
void SMPcore::op_set_bit() {
  dp = op_readpc();
  rd = op_readdp(dp) & ~(1 << (opcode >> 5));
  op_writedp(dp, rd | (!(opcode & 0x10) << (opcode >> 5)));
}
//##IMPL
//   case 0x43: return op_branch_bit();
void SMPcore::op_branch_bit() {
  dp = op_readpc();
  sp = op_readdp(dp);
  rd = op_readpc();
  op_io();
  if((bool)(sp & (1 << (opcode >> 5))) == (bool)(opcode & 0x10)) return;
  op_io();
  op_io();
  regs.pc += (int8)rd;
}
//##IMPL
//   case 0x44: return op_read_dp<&SMPcore::op_eor>(regs.a);
void SMPcore::op_read_dp(uint8 &r) {
  dp = op_readpc();
  rd = op_readdp(dp);
  regs.a = op_eor(regs.a, rd);
}
//##IMPL
//   case 0x45: return op_read_addr<&SMPcore::op_eor>(regs.a);
void SMPcore::op_read_addr(uint8 &r) {
  dp.l = op_readpc();
  dp.h = op_readpc();
  rd = op_read(dp);
  regs.a = op_eor(regs.a, rd);
}
//##IMPL
//   case 0x46: return op_read_ix<&SMPcore::op_eor>();
void SMPcore::op_read_ix() {
  op_io();
  rd = op_readdp(regs.x);
  regs.a = op_eor(regs.a, rd);
}
//##IMPL
//   case 0x47: return op_read_idpx<&SMPcore::op_eor>();
void SMPcore::op_read_idpx() {
  dp = op_readpc() + regs.x;
  op_io();
  sp.l = op_readdp(dp++);
  sp.h = op_readdp(dp++);
  rd = op_read(sp);
  regs.a = op_eor(regs.a, rd);
}
//##IMPL
//   case 0x48: return op_read_const<&SMPcore::op_eor>(regs.a);
void SMPcore::op_read_const(uint8 &r) {
  rd = op_readpc();
  regs.a = op_eor(regs.a, rd);
}
//##IMPL
//   case 0x49: return op_write_dp_dp<&SMPcore::op_eor>();
void SMPcore::op_write_dp_dp() {
  sp = op_readpc();
  rd = op_readdp(sp);
  dp = op_readpc();
  wr = op_readdp(dp);
  wr = op_eor(wr, rd);
  op_writedp(dp, wr);
}
//##IMPL
//   case 0x4a: return op_set_addr_bit();
void SMPcore::op_set_addr_bit() {
  dp.l = op_readpc();
  dp.h = op_readpc();
  bit = dp >> 13;
  dp &= 0x1fff;
  rd = op_read(dp);
  regs.p.c &= (rd & (1 << bit)) ^ 0;
}
//##IMPL
//   case 0x4b: return op_adjust_dp<&SMPcore::op_lsr>();
void SMPcore::op_adjust_dp() {
  dp = op_readpc();
  rd = op_readdp(dp);
  rd = op_lsr(rd);
  op_writedp(dp, rd);
}
//##IMPL
//   case 0x4c: return op_adjust_addr<&SMPcore::op_lsr>();
void SMPcore::op_adjust_addr() {
  dp.l = op_readpc();
  dp.h = op_readpc();
  rd = op_read(dp);
  rd = op_lsr(rd);
  op_write(dp, rd);
}
//##IMPL
//   case 0x4d: return op_push(regs.x);
void SMPcore::op_push(uint8 r) {
  op_io();
  op_io();
  op_writesp(regs.x);
}
//##IMPL
//   case 0x4e: return op_test_addr(0);
void SMPcore::op_test_addr(bool set) {
  dp.l = op_readpc();
  dp.h = op_readpc();
  rd = op_read(dp);
  regs.p.n = (regs.a - rd) & 0x80;
  regs.p.z = (regs.a - rd) == 0;
  op_read(dp);
  op_write(dp, rd & ~regs.a);
}
//##IMPL
//   case 0x4f: return op_jsp_dp();
void SMPcore::op_jsp_dp() {
  rd = op_readpc();
  op_io();
  op_io();
  op_writesp(regs.pc.h);
  op_writesp(regs.pc.l);
  regs.pc = 0xff00 | rd;
}
//##IMPL
//   case 0x50: return op_branch(regs.p.v == 0);
void SMPcore::op_branch(bool condition) {
  rd = op_readpc();
  if(regs.p.v != 0) return;
  op_io();
  op_io();
  regs.pc += (int8)rd;
}
//##IMPL
//   case 0x51: return op_jst();
void SMPcore::op_jst() {
  dp = 0xffde - ((opcode >> 4) << 1);
  rd.l = op_read(dp++);
  rd.h = op_read(dp++);
  op_io();
  op_io();
  op_io();
  op_writesp(regs.pc.h);
  op_writesp(regs.pc.l);
  regs.pc = rd;
}
//##IMPL
//   case 0x52: return op_set_bit();
void SMPcore::op_set_bit() {
  dp = op_readpc();
  rd = op_readdp(dp) & ~(1 << (opcode >> 5));
  op_writedp(dp, rd | (!(opcode & 0x10) << (opcode >> 5)));
}
//##IMPL
//   case 0x53: return op_branch_bit();
void SMPcore::op_branch_bit() {
  dp = op_readpc();
  sp = op_readdp(dp);
  rd = op_readpc();
  op_io();
  if((bool)(sp & (1 << (opcode >> 5))) == (bool)(opcode & 0x10)) return;
  op_io();
  op_io();
  regs.pc += (int8)rd;
}
//##IMPL
//   case 0x54: return op_read_dpi<&SMPcore::op_eor>(regs.a, regs.x);
void SMPcore::op_read_dpi(uint8 &r, uint8 &i) {
  dp = op_readpc();
  op_io();
  rd = op_readdp(dp + regs.x);
  regs.a = op_eor(regs.a, rd);
}
//##IMPL
//   case 0x55: return op_read_addri<&SMPcore::op_eor>(regs.x);
void SMPcore::op_read_addri(uint8 &r) {
  dp.l = op_readpc();
  dp.h = op_readpc();
  op_io();
  rd = op_read(dp + regs.x);
  regs.a = op_eor(regs.a, rd);
}
//##IMPL
//   case 0x56: return op_read_addri<&SMPcore::op_eor>(regs.y);
void SMPcore::op_read_addri(uint8 &r) {
  dp.l = op_readpc();
  dp.h = op_readpc();
  op_io();
  rd = op_read(dp + regs.y);
  regs.a = op_eor(regs.a, rd);
}
//##IMPL
//   case 0x57: return op_read_idpy<&SMPcore::op_eor>();
void SMPcore::op_read_idpy() {
  dp = op_readpc();
  op_io();
  sp.l = op_readdp(dp++);
  sp.h = op_readdp(dp++);
  rd = op_read(sp + regs.y);
  regs.a = op_eor(regs.a, rd);
}
//##IMPL
//   case 0x58: return op_write_dp_const<&SMPcore::op_eor>();
void SMPcore::op_write_dp_const() {
  rd = op_readpc();
  dp = op_readpc();
  wr = op_readdp(dp);
  wr = op_eor(wr, rd);
  op_writedp(dp, wr);
}
//##IMPL
//   case 0x59: return op_write_ix_iy<&SMPcore::op_eor>();
void SMPcore::op_write_ix_iy() {
  op_io();
  rd = op_readdp(regs.y);
  wr = op_readdp(regs.x);
  wr = op_eor(wr, rd);
  op_writedp(regs.x, wr);
}
//##IMPL
//   case 0x5a: return op_read_dpw<&SMPcore::op_cpw>();
void SMPcore::op_read_dpw() {
  dp = op_readpc();
  rd.l = op_readdp(dp++);
  rd.h = op_readdp(dp++);
  regs.ya = op_cpw(regs.ya, rd);
}
//##IMPL
//   case 0x5b: return op_adjust_dpx<&SMPcore::op_lsr>();
void SMPcore::op_adjust_dpx() {
  dp = op_readpc();
  op_io();
  rd = op_readdp(dp + regs.x);
  rd = op_lsr(rd);
  op_writedp(dp + regs.x, rd);
}
//##IMPL
//   case 0x5c: return op_adjust<&SMPcore::op_lsr>(regs.a);
void SMPcore::op_adjust(uint8 &r) {
  op_io();
  regs.a = op_lsr(regs.a);
}
//##IMPL
//   case 0x5d: return op_transfer(regs.a, regs.x);
void SMPcore::op_transfer(uint8 &from, uint8 &to) {
  op_io();
  regs.x = regs.a;
  regs.p.n = (regs.x & 0x80);
  regs.p.z = (regs.x == 0);
}
//##IMPL
//   case 0x5e: return op_read_addr<&SMPcore::op_cmp>(regs.y);
void SMPcore::op_read_addr(uint8 &r) {
  dp.l = op_readpc();
  dp.h = op_readpc();
  rd = op_read(dp);
  regs.y = op_cmp(regs.y, rd);
}
//##IMPL
//   case 0x5f: return op_jmp_addr();
void SMPcore::op_jmp_addr() {
  rd.l = op_readpc();
  rd.h = op_readpc();
  regs.pc = rd;
}
//##IMPL
//   case 0x60: return op_set_flag(regs.p.c, 0);
void SMPcore::op_set_flag(bool &flag, bool data) {
  op_io();
  regs.p.c = 0;
}
//##IMPL
//   case 0x61: return op_jst();
void SMPcore::op_jst() {
  dp = 0xffde - ((opcode >> 4) << 1);
  rd.l = op_read(dp++);
  rd.h = op_read(dp++);
  op_io();
  op_io();
  op_io();
  op_writesp(regs.pc.h);
  op_writesp(regs.pc.l);
  regs.pc = rd;
}
//##IMPL
//   case 0x62: return op_set_bit();
void SMPcore::op_set_bit() {
  dp = op_readpc();
  rd = op_readdp(dp) & ~(1 << (opcode >> 5));
  op_writedp(dp, rd | (!(opcode & 0x10) << (opcode >> 5)));
}
//##IMPL
//   case 0x63: return op_branch_bit();
void SMPcore::op_branch_bit() {
  dp = op_readpc();
  sp = op_readdp(dp);
  rd = op_readpc();
  op_io();
  if((bool)(sp & (1 << (opcode >> 5))) == (bool)(opcode & 0x10)) return;
  op_io();
  op_io();
  regs.pc += (int8)rd;
}
//##IMPL
//   case 0x64: return op_read_dp<&SMPcore::op_cmp>(regs.a);
void SMPcore::op_read_dp(uint8 &r) {
  dp = op_readpc();
  rd = op_readdp(dp);
  regs.a = op_cmp(regs.a, rd);
}
//##IMPL
//   case 0x65: return op_read_addr<&SMPcore::op_cmp>(regs.a);
void SMPcore::op_read_addr(uint8 &r) {
  dp.l = op_readpc();
  dp.h = op_readpc();
  rd = op_read(dp);
  regs.a = op_cmp(regs.a, rd);
}
//##IMPL
//   case 0x66: return op_read_ix<&SMPcore::op_cmp>();
void SMPcore::op_read_ix() {
  op_io();
  rd = op_readdp(regs.x);
  regs.a = op_cmp(regs.a, rd);
}
//##IMPL
//   case 0x67: return op_read_idpx<&SMPcore::op_cmp>();
void SMPcore::op_read_idpx() {
  dp = op_readpc() + regs.x;
  op_io();
  sp.l = op_readdp(dp++);
  sp.h = op_readdp(dp++);
  rd = op_read(sp);
  regs.a = op_cmp(regs.a, rd);
}
//##IMPL
//   case 0x68: return op_read_const<&SMPcore::op_cmp>(regs.a);
void SMPcore::op_read_const(uint8 &r) {
  rd = op_readpc();
  regs.a = op_cmp(regs.a, rd);
}
//##IMPL
//   case 0x69: return op_write_dp_dp<&SMPcore::op_cmp>();
void SMPcore::op_write_dp_dp() {
  sp = op_readpc();
  rd = op_readdp(sp);
  dp = op_readpc();
  wr = op_readdp(dp);
  wr = op_cmp(wr, rd);
  op_io();
}
//##IMPL
//   case 0x6a: return op_set_addr_bit();
void SMPcore::op_set_addr_bit() {
  dp.l = op_readpc();
  dp.h = op_readpc();
  bit = dp >> 13;
  dp &= 0x1fff;
  rd = op_read(dp);
  regs.p.c &= (rd & (1 << bit)) ^ 1;
}
//##IMPL
//   case 0x6b: return op_adjust_dp<&SMPcore::op_ror>();
void SMPcore::op_adjust_dp() {
  dp = op_readpc();
  rd = op_readdp(dp);
  rd = op_ror(rd);
  op_writedp(dp, rd);
}
//##IMPL
//   case 0x6c: return op_adjust_addr<&SMPcore::op_ror>();
void SMPcore::op_adjust_addr() {
  dp.l = op_readpc();
  dp.h = op_readpc();
  rd = op_read(dp);
  rd = op_ror(rd);
  op_write(dp, rd);
}
//##IMPL
//   case 0x6d: return op_push(regs.y);
void SMPcore::op_push(uint8 r) {
  op_io();
  op_io();
  op_writesp(regs.y);
}
//##IMPL
//   case 0x6e: return op_bne_dpdec();
void SMPcore::op_bne_dpdec() {
  dp = op_readpc();
  wr = op_readdp(dp);
  op_writedp(dp, --wr);
  rd = op_readpc();
  if(wr == 0) return;
  op_io();
  op_io();
  regs.pc += (int8)rd;
}
//##IMPL
//   case 0x6f: return op_rts();
void SMPcore::op_rts() {
  rd.l = op_readsp();
  rd.h = op_readsp();
  op_io();
  op_io();
  regs.pc = rd;
}
//##IMPL
//   case 0x70: return op_branch(regs.p.v == 1);
void SMPcore::op_branch(bool condition) {
  rd = op_readpc();
  if(regs.p.v != 1) return;
  op_io();
  op_io();
  regs.pc += (int8)rd;
}
//##IMPL
//   case 0x71: return op_jst();
void SMPcore::op_jst() {
  dp = 0xffde - ((opcode >> 4) << 1);
  rd.l = op_read(dp++);
  rd.h = op_read(dp++);
  op_io();
  op_io();
  op_io();
  op_writesp(regs.pc.h);
  op_writesp(regs.pc.l);
  regs.pc = rd;
}
//##IMPL
//   case 0x72: return op_set_bit();
void SMPcore::op_set_bit() {
  dp = op_readpc();
  rd = op_readdp(dp) & ~(1 << (opcode >> 5));
  op_writedp(dp, rd | (!(opcode & 0x10) << (opcode >> 5)));
}
//##IMPL
//   case 0x73: return op_branch_bit();
void SMPcore::op_branch_bit() {
  dp = op_readpc();
  sp = op_readdp(dp);
  rd = op_readpc();
  op_io();
  if((bool)(sp & (1 << (opcode >> 5))) == (bool)(opcode & 0x10)) return;
  op_io();
  op_io();
  regs.pc += (int8)rd;
}
//##IMPL
//   case 0x74: return op_read_dpi<&SMPcore::op_cmp>(regs.a, regs.x);
void SMPcore::op_read_dpi(uint8 &r, uint8 &i) {
  dp = op_readpc();
  op_io();
  rd = op_readdp(dp + regs.x);
  regs.a = op_cmp(regs.a, rd);
}
//##IMPL
//   case 0x75: return op_read_addri<&SMPcore::op_cmp>(regs.x);
void SMPcore::op_read_addri(uint8 &r) {
  dp.l = op_readpc();
  dp.h = op_readpc();
  op_io();
  rd = op_read(dp + regs.x);
  regs.a = op_cmp(regs.a, rd);
}
//##IMPL
//   case 0x76: return op_read_addri<&SMPcore::op_cmp>(regs.y);
void SMPcore::op_read_addri(uint8 &r) {
  dp.l = op_readpc();
  dp.h = op_readpc();
  op_io();
  rd = op_read(dp + regs.y);
  regs.a = op_cmp(regs.a, rd);
}
//##IMPL
//   case 0x77: return op_read_idpy<&SMPcore::op_cmp>();
void SMPcore::op_read_idpy() {
  dp = op_readpc();
  op_io();
  sp.l = op_readdp(dp++);
  sp.h = op_readdp(dp++);
  rd = op_read(sp + regs.y);
  regs.a = op_cmp(regs.a, rd);
}
//##IMPL
//   case 0x78: return op_write_dp_const<&SMPcore::op_cmp>();
void SMPcore::op_write_dp_const() {
  rd = op_readpc();
  dp = op_readpc();
  wr = op_readdp(dp);
  wr = op_cmp(wr, rd);
  op_io();
}
//##IMPL
//   case 0x79: return op_write_ix_iy<&SMPcore::op_cmp>();
void SMPcore::op_write_ix_iy() {
  op_io();
  rd = op_readdp(regs.y);
  wr = op_readdp(regs.x);
  wr = op_cmp(wr, rd);
  op_io();
}
//##IMPL
//   case 0x7a: return op_read_dpw<&SMPcore::op_adw>();
void SMPcore::op_read_dpw() {
  dp = op_readpc();
  rd.l = op_readdp(dp++);
  op_io();
  rd.h = op_readdp(dp++);
  regs.ya = op_adw(regs.ya, rd);
}
//##IMPL
//   case 0x7b: return op_adjust_dpx<&SMPcore::op_ror>();
void SMPcore::op_adjust_dpx() {
  dp = op_readpc();
  op_io();
  rd = op_readdp(dp + regs.x);
  rd = op_ror(rd);
  op_writedp(dp + regs.x, rd);
}
//##IMPL
//   case 0x7c: return op_adjust<&SMPcore::op_ror>(regs.a);
void SMPcore::op_adjust(uint8 &r) {
  op_io();
  regs.a = op_ror(regs.a);
}
//##IMPL
//   case 0x7d: return op_transfer(regs.x, regs.a);
void SMPcore::op_transfer(uint8 &from, uint8 &to) {
  op_io();
  regs.a = regs.x;
  regs.p.n = (regs.a & 0x80);
  regs.p.z = (regs.a == 0);
}
//##IMPL
//   case 0x7e: return op_read_dp<&SMPcore::op_cmp>(regs.y);
void SMPcore::op_read_dp(uint8 &r) {
  dp = op_readpc();
  rd = op_readdp(dp);
  regs.y = op_cmp(regs.y, rd);
}
//##IMPL
//   case 0x7f: return op_rti();
void SMPcore::op_rti() {
  regs.p = op_readsp();
  rd.l = op_readsp();
  rd.h = op_readsp();
  op_io();
  op_io();
  regs.pc = rd;
}
//##IMPL
//   case 0x80: return op_set_flag(regs.p.c, 1);
void SMPcore::op_set_flag(bool &flag, bool data) {
  op_io();
  regs.p.c = 1;
}
//##IMPL
//   case 0x81: return op_jst();
void SMPcore::op_jst() {
  dp = 0xffde - ((opcode >> 4) << 1);
  rd.l = op_read(dp++);
  rd.h = op_read(dp++);
  op_io();
  op_io();
  op_io();
  op_writesp(regs.pc.h);
  op_writesp(regs.pc.l);
  regs.pc = rd;
}
//##IMPL
//   case 0x82: return op_set_bit();
void SMPcore::op_set_bit() {
  dp = op_readpc();
  rd = op_readdp(dp) & ~(1 << (opcode >> 5));
  op_writedp(dp, rd | (!(opcode & 0x10) << (opcode >> 5)));
}
//##IMPL
//   case 0x83: return op_branch_bit();
void SMPcore::op_branch_bit() {
  dp = op_readpc();
  sp = op_readdp(dp);
  rd = op_readpc();
  op_io();
  if((bool)(sp & (1 << (opcode >> 5))) == (bool)(opcode & 0x10)) return;
  op_io();
  op_io();
  regs.pc += (int8)rd;
}
//##IMPL
//   case 0x84: return op_read_dp<&SMPcore::op_adc>(regs.a);
void SMPcore::op_read_dp(uint8 &r) {
  dp = op_readpc();
  rd = op_readdp(dp);
  regs.a = op_adc(regs.a, rd);
}
//##IMPL
//   case 0x85: return op_read_addr<&SMPcore::op_adc>(regs.a);
void SMPcore::op_read_addr(uint8 &r) {
  dp.l = op_readpc();
  dp.h = op_readpc();
  rd = op_read(dp);
  regs.a = op_adc(regs.a, rd);
}
//##IMPL
//   case 0x86: return op_read_ix<&SMPcore::op_adc>();
void SMPcore::op_read_ix() {
  op_io();
  rd = op_readdp(regs.x);
  regs.a = op_adc(regs.a, rd);
}
//##IMPL
//   case 0x87: return op_read_idpx<&SMPcore::op_adc>();
void SMPcore::op_read_idpx() {
  dp = op_readpc() + regs.x;
  op_io();
  sp.l = op_readdp(dp++);
  sp.h = op_readdp(dp++);
  rd = op_read(sp);
  regs.a = op_adc(regs.a, rd);
}
//##IMPL
//   case 0x88: return op_read_const<&SMPcore::op_adc>(regs.a);
void SMPcore::op_read_const(uint8 &r) {
  rd = op_readpc();
  regs.a = op_adc(regs.a, rd);
}
//##IMPL
//   case 0x89: return op_write_dp_dp<&SMPcore::op_adc>();
void SMPcore::op_write_dp_dp() {
  sp = op_readpc();
  rd = op_readdp(sp);
  dp = op_readpc();
  wr = op_readdp(dp);
  wr = op_adc(wr, rd);
  op_writedp(dp, wr);
}
//##IMPL
//   case 0x8a: return op_set_addr_bit();
void SMPcore::op_set_addr_bit() {
  dp.l = op_readpc();
  dp.h = op_readpc();
  bit = dp >> 13;
  dp &= 0x1fff;
  rd = op_read(dp);
  op_io();
  regs.p.c ^= (bool)(rd & (1 << bit));
}
//##IMPL
//   case 0x8b: return op_adjust_dp<&SMPcore::op_dec>();
void SMPcore::op_adjust_dp() {
  dp = op_readpc();
  rd = op_readdp(dp);
  rd = op_dec(rd);
  op_writedp(dp, rd);
}
//##IMPL
//   case 0x8c: return op_adjust_addr<&SMPcore::op_dec>();
void SMPcore::op_adjust_addr() {
  dp.l = op_readpc();
  dp.h = op_readpc();
  rd = op_read(dp);
  rd = op_dec(rd);
  op_write(dp, rd);
}
//##IMPL
//   case 0x8d: return op_read_const<&SMPcore::op_ld>(regs.y);
void SMPcore::op_read_const(uint8 &r) {
  rd = op_readpc();
  regs.y = op_ld(regs.y, rd);
}
//##IMPL
//   case 0x8e: return op_plp();
void SMPcore::op_plp() {
  op_io();
  op_io();
  regs.p = op_readsp();
}
//##IMPL
//   case 0x8f: return op_write_dp_const<&SMPcore::op_st>();
void SMPcore::op_write_dp_const() {
  rd = op_readpc();
  dp = op_readpc();
  wr = op_readdp(dp);
  wr = op_st(wr, rd);
  op_writedp(dp, wr);
}
//##IMPL
//   case 0x90: return op_branch(regs.p.c == 0);
void SMPcore::op_branch(bool condition) {
  rd = op_readpc();
  if(regs.p.c != 0) return;
  op_io();
  op_io();
  regs.pc += (int8)rd;
}
//##IMPL
//   case 0x91: return op_jst();
void SMPcore::op_jst() {
  dp = 0xffde - ((opcode >> 4) << 1);
  rd.l = op_read(dp++);
  rd.h = op_read(dp++);
  op_io();
  op_io();
  op_io();
  op_writesp(regs.pc.h);
  op_writesp(regs.pc.l);
  regs.pc = rd;
}
//##IMPL
//   case 0x92: return op_set_bit();
void SMPcore::op_set_bit() {
  dp = op_readpc();
  rd = op_readdp(dp) & ~(1 << (opcode >> 5));
  op_writedp(dp, rd | (!(opcode & 0x10) << (opcode >> 5)));
}
//##IMPL
//   case 0x93: return op_branch_bit();
void SMPcore::op_branch_bit() {
  dp = op_readpc();
  sp = op_readdp(dp);
  rd = op_readpc();
  op_io();
  if((bool)(sp & (1 << (opcode >> 5))) == (bool)(opcode & 0x10)) return;
  op_io();
  op_io();
  regs.pc += (int8)rd;
}
//##IMPL
//   case 0x94: return op_read_dpi<&SMPcore::op_adc>(regs.a, regs.x);
void SMPcore::op_read_dpi(uint8 &r, uint8 &i) {
  dp = op_readpc();
  op_io();
  rd = op_readdp(dp + regs.x);
  regs.a = op_adc(regs.a, rd);
}
//##IMPL
//   case 0x95: return op_read_addri<&SMPcore::op_adc>(regs.x);
void SMPcore::op_read_addri(uint8 &r) {
  dp.l = op_readpc();
  dp.h = op_readpc();
  op_io();
  rd = op_read(dp + regs.x);
  regs.a = op_adc(regs.a, rd);
}
//##IMPL
//   case 0x96: return op_read_addri<&SMPcore::op_adc>(regs.y);
void SMPcore::op_read_addri(uint8 &r) {
  dp.l = op_readpc();
  dp.h = op_readpc();
  op_io();
  rd = op_read(dp + regs.y);
  regs.a = op_adc(regs.a, rd);
}
//##IMPL
//   case 0x97: return op_read_idpy<&SMPcore::op_adc>();
void SMPcore::op_read_idpy() {
  dp = op_readpc();
  op_io();
  sp.l = op_readdp(dp++);
  sp.h = op_readdp(dp++);
  rd = op_read(sp + regs.y);
  regs.a = op_adc(regs.a, rd);
}
//##IMPL
//   case 0x98: return op_write_dp_const<&SMPcore::op_adc>();
void SMPcore::op_write_dp_const() {
  rd = op_readpc();
  dp = op_readpc();
  wr = op_readdp(dp);
  wr = op_adc(wr, rd);
  op_writedp(dp, wr);
}
//##IMPL
//   case 0x99: return op_write_ix_iy<&SMPcore::op_adc>();
void SMPcore::op_write_ix_iy() {
  op_io();
  rd = op_readdp(regs.y);
  wr = op_readdp(regs.x);
  wr = op_adc(wr, rd);
  op_writedp(regs.x, wr);
}
//##IMPL
//   case 0x9a: return op_read_dpw<&SMPcore::op_sbw>();
void SMPcore::op_read_dpw() {
  dp = op_readpc();
  rd.l = op_readdp(dp++);
  op_io();
  rd.h = op_readdp(dp++);
  regs.ya = op_sbw(regs.ya, rd);
}
//##IMPL
//   case 0x9b: return op_adjust_dpx<&SMPcore::op_dec>();
void SMPcore::op_adjust_dpx() {
  dp = op_readpc();
  op_io();
  rd = op_readdp(dp + regs.x);
  rd = op_dec(rd);
  op_writedp(dp + regs.x, rd);
}
//##IMPL
//   case 0x9c: return op_adjust<&SMPcore::op_dec>(regs.a);
void SMPcore::op_adjust(uint8 &r) {
  op_io();
  regs.a = op_dec(regs.a);
}
//##IMPL
//   case 0x9d: return op_transfer(regs.s, regs.x);
void SMPcore::op_transfer(uint8 &from, uint8 &to) {
  op_io();
  regs.x = regs.s;
  regs.p.n = (regs.x & 0x80);
  regs.p.z = (regs.x == 0);
}
//##IMPL
//   case 0x9e: return op_div_ya_x();
void SMPcore::op_div_ya_x() {
  op_io();
  op_io();
  op_io();
  op_io();
  op_io();
  op_io();
  op_io();
  op_io();
  op_io();
  op_io();
  op_io();
  //[[
  ya = regs.ya;
  //overflow set if quotient >= 256
  regs.p.v = (regs.y >= regs.x);
  regs.p.h = ((regs.y & 15) >= (regs.x & 15));
  if(regs.y < (regs.x << 1)) {
    //if quotient is <= 511 (will fit into 9-bit result)
    regs.a = ya / regs.x;
    regs.y = ya % regs.x;
  } else {
    //otherwise, the quotient won't fit into regs.p.v + regs.a
    //this emulates the odd behavior of the S-SMP in this case
    regs.a = 255    - (ya - (regs.x << 9)) / (256 - regs.x);
    regs.y = regs.x + (ya - (regs.x << 9)) % (256 - regs.x);
  }
  //result is set based on a (quotient) only
  regs.p.n = (regs.a & 0x80);
  regs.p.z = (regs.a == 0);
  //]]
}
//##IMPL
//   case 0x9f: return op_xcn();
void SMPcore::op_xcn() {
  op_io();
  op_io();
  op_io();
  op_io();
  regs.a = (regs.a >> 4) | (regs.a << 4);
  regs.p.n = regs.a & 0x80;
  regs.p.z = regs.a == 0;
}
//##IMPL
//   case 0xa0: return op_set_flag(regs.p.i, 1);
void SMPcore::op_set_flag(bool &flag, bool data) {
  op_io();
  op_io();
  regs.p.i = 1;
}
//##IMPL
//   case 0xa1: return op_jst();
void SMPcore::op_jst() {
  dp = 0xffde - ((opcode >> 4) << 1);
  rd.l = op_read(dp++);
  rd.h = op_read(dp++);
  op_io();
  op_io();
  op_io();
  op_writesp(regs.pc.h);
  op_writesp(regs.pc.l);
  regs.pc = rd;
}
//##IMPL
//   case 0xa2: return op_set_bit();
void SMPcore::op_set_bit() {
  dp = op_readpc();
  rd = op_readdp(dp) & ~(1 << (opcode >> 5));
  op_writedp(dp, rd | (!(opcode & 0x10) << (opcode >> 5)));
}
//##IMPL
//   case 0xa3: return op_branch_bit();
void SMPcore::op_branch_bit() {
  dp = op_readpc();
  sp = op_readdp(dp);
  rd = op_readpc();
  op_io();
  if((bool)(sp & (1 << (opcode >> 5))) == (bool)(opcode & 0x10)) return;
  op_io();
  op_io();
  regs.pc += (int8)rd;
}
//##IMPL
//   case 0xa4: return op_read_dp<&SMPcore::op_sbc>(regs.a);
void SMPcore::op_read_dp(uint8 &r) {
  dp = op_readpc();
  rd = op_readdp(dp);
  regs.a = op_sbc(regs.a, rd);
}
//##IMPL
//   case 0xa5: return op_read_addr<&SMPcore::op_sbc>(regs.a);
void SMPcore::op_read_addr(uint8 &r) {
  dp.l = op_readpc();
  dp.h = op_readpc();
  rd = op_read(dp);
  regs.a = op_sbc(regs.a, rd);
}
//##IMPL
//   case 0xa6: return op_read_ix<&SMPcore::op_sbc>();
void SMPcore::op_read_ix() {
  op_io();
  rd = op_readdp(regs.x);
  regs.a = op_sbc(regs.a, rd);
}
//##IMPL
//   case 0xa7: return op_read_idpx<&SMPcore::op_sbc>();
void SMPcore::op_read_idpx() {
  dp = op_readpc() + regs.x;
  op_io();
  sp.l = op_readdp(dp++);
  sp.h = op_readdp(dp++);
  rd = op_read(sp);
  regs.a = op_sbc(regs.a, rd);
}
//##IMPL
//   case 0xa8: return op_read_const<&SMPcore::op_sbc>(regs.a);
void SMPcore::op_read_const(uint8 &r) {
  rd = op_readpc();
  regs.a = op_sbc(regs.a, rd);
}
//##IMPL
//   case 0xa9: return op_write_dp_dp<&SMPcore::op_sbc>();
void SMPcore::op_write_dp_dp() {
  sp = op_readpc();
  rd = op_readdp(sp);
  dp = op_readpc();
  wr = op_readdp(dp);
  wr = op_sbc(wr, rd);
  op_writedp(dp, wr);
}
//##IMPL
//   case 0xaa: return op_set_addr_bit();
void SMPcore::op_set_addr_bit() {
  dp.l = op_readpc();
  dp.h = op_readpc();
  bit = dp >> 13;
  dp &= 0x1fff;
  rd = op_read(dp);
  regs.p.c  = (rd & (1 << bit));
}
//##IMPL
//   case 0xab: return op_adjust_dp<&SMPcore::op_inc>();
void SMPcore::op_adjust_dp() {
  dp = op_readpc();
  rd = op_readdp(dp);
  rd = op_inc(rd);
  op_writedp(dp, rd);
}
//##IMPL
//   case 0xac: return op_adjust_addr<&SMPcore::op_inc>();
void SMPcore::op_adjust_addr() {
  dp.l = op_readpc();
  dp.h = op_readpc();
  rd = op_read(dp);
  rd = op_inc(rd);
  op_write(dp, rd);
}
//##IMPL
//   case 0xad: return op_read_const<&SMPcore::op_cmp>(regs.y);
void SMPcore::op_read_const(uint8 &r) {
  rd = op_readpc();
  regs.y = op_cmp(regs.y, rd);
}
//##IMPL
//   case 0xae: return op_pull(regs.a);
void SMPcore::op_pull(uint8 &r) {
  op_io();
  op_io();
  regs.a = op_readsp();
}
//##IMPL
//   case 0xaf: return op_sta_ixinc();
void SMPcore::op_sta_ixinc() {
  op_io();
  op_io();
  op_writedp(regs.x++, regs.a);
}
//##IMPL
//   case 0xb0: return op_branch(regs.p.c == 1);
void SMPcore::op_branch(bool condition) {
  rd = op_readpc();
  if(regs.p.c != 1) return;
  op_io();
  op_io();
  regs.pc += (int8)rd;
}
//##IMPL
//   case 0xb1: return op_jst();
void SMPcore::op_jst() {
  dp = 0xffde - ((opcode >> 4) << 1);
  rd.l = op_read(dp++);
  rd.h = op_read(dp++);
  op_io();
  op_io();
  op_io();
  op_writesp(regs.pc.h);
  op_writesp(regs.pc.l);
  regs.pc = rd;
}
//##IMPL
//   case 0xb2: return op_set_bit();
void SMPcore::op_set_bit() {
  dp = op_readpc();
  rd = op_readdp(dp) & ~(1 << (opcode >> 5));
  op_writedp(dp, rd | (!(opcode & 0x10) << (opcode >> 5)));
}
//##IMPL
//   case 0xb3: return op_branch_bit();
void SMPcore::op_branch_bit() {
  dp = op_readpc();
  sp = op_readdp(dp);
  rd = op_readpc();
  op_io();
  if((bool)(sp & (1 << (opcode >> 5))) == (bool)(opcode & 0x10)) return;
  op_io();
  op_io();
  regs.pc += (int8)rd;
}
//##IMPL
//   case 0xb4: return op_read_dpi<&SMPcore::op_sbc>(regs.a, regs.x);
void SMPcore::op_read_dpi(uint8 &r, uint8 &i) {
  dp = op_readpc();
  op_io();
  rd = op_readdp(dp + regs.x);
  regs.a = op_sbc(regs.a, rd);
}
//##IMPL
//   case 0xb5: return op_read_addri<&SMPcore::op_sbc>(regs.x);
void SMPcore::op_read_addri(uint8 &r) {
  dp.l = op_readpc();
  dp.h = op_readpc();
  op_io();
  rd = op_read(dp + regs.x);
  regs.a = op_sbc(regs.a, rd);
}
//##IMPL
//   case 0xb6: return op_read_addri<&SMPcore::op_sbc>(regs.y);
void SMPcore::op_read_addri(uint8 &r) {
  dp.l = op_readpc();
  dp.h = op_readpc();
  op_io();
  rd = op_read(dp + regs.y);
  regs.a = op_sbc(regs.a, rd);
}
//##IMPL
//   case 0xb7: return op_read_idpy<&SMPcore::op_sbc>();
void SMPcore::op_read_idpy() {
  dp = op_readpc();
  op_io();
  sp.l = op_readdp(dp++);
  sp.h = op_readdp(dp++);
  rd = op_read(sp + regs.y);
  regs.a = op_sbc(regs.a, rd);
}
//##IMPL
//   case 0xb8: return op_write_dp_const<&SMPcore::op_sbc>();
void SMPcore::op_write_dp_const() {
  rd = op_readpc();
  dp = op_readpc();
  wr = op_readdp(dp);
  wr = op_sbc(wr, rd);
  op_writedp(dp, wr);
}
//##IMPL
//   case 0xb9: return op_write_ix_iy<&SMPcore::op_sbc>();
void SMPcore::op_write_ix_iy() {
  op_io();
  rd = op_readdp(regs.y);
  wr = op_readdp(regs.x);
  wr = op_sbc(wr, rd);
  op_writedp(regs.x, wr);
}
//##IMPL
//   case 0xba: return op_read_dpw<&SMPcore::op_ldw>();
void SMPcore::op_read_dpw() {
  dp = op_readpc();
  rd.l = op_readdp(dp++);
  op_io();
  rd.h = op_readdp(dp++);
  regs.ya = op_ldw(regs.ya, rd);
}
//##IMPL
//   case 0xbb: return op_adjust_dpx<&SMPcore::op_inc>();
void SMPcore::op_adjust_dpx() {
  dp = op_readpc();
  op_io();
  rd = op_readdp(dp + regs.x);
  rd = op_inc(rd);
  op_writedp(dp + regs.x, rd);
}
//##IMPL
//   case 0xbc: return op_adjust<&SMPcore::op_inc>(regs.a);
void SMPcore::op_adjust(uint8 &r) {
  op_io();
  regs.a = op_inc(regs.a);
}
//##IMPL
//   case 0xbd: return op_transfer(regs.x, regs.s);
void SMPcore::op_transfer(uint8 &from, uint8 &to) {
  op_io();
  regs.s = regs.x;
}
//##IMPL
//   case 0xbe: return op_das();
void SMPcore::op_das() {
  op_io();
  op_io();
  //[[
  if(!regs.p.c || (regs.a) > 0x99) {
    regs.a -= 0x60;
    regs.p.c = 0;
  }
  if(!regs.p.h || (regs.a & 15) > 0x09) {
    regs.a -= 0x06;
  }
  regs.p.n = (regs.a & 0x80);
  regs.p.z = (regs.a == 0);
  //]]
}
//##IMPL
//   case 0xbf: return op_lda_ixinc();
void SMPcore::op_lda_ixinc() {
  op_io();
  regs.a = op_readdp(regs.x++);
  op_io();
  regs.p.n = regs.a & 0x80;
  regs.p.z = regs.a == 0;
}
//##IMPL
//   case 0xc0: return op_set_flag(regs.p.i, 0);
void SMPcore::op_set_flag(bool &flag, bool data) {
  op_io();
  op_io();
  regs.p.i = 0;
}
//##IMPL
//   case 0xc1: return op_jst();
void SMPcore::op_jst() {
  dp = 0xffde - ((opcode >> 4) << 1);
  rd.l = op_read(dp++);
  rd.h = op_read(dp++);
  op_io();
  op_io();
  op_io();
  op_writesp(regs.pc.h);
  op_writesp(regs.pc.l);
  regs.pc = rd;
}
//##IMPL
//   case 0xc2: return op_set_bit();
void SMPcore::op_set_bit() {
  dp = op_readpc();
  rd = op_readdp(dp) & ~(1 << (opcode >> 5));
  op_writedp(dp, rd | (!(opcode & 0x10) << (opcode >> 5)));
}
//##IMPL
//   case 0xc3: return op_branch_bit();
void SMPcore::op_branch_bit() {
  dp = op_readpc();
  sp = op_readdp(dp);
  rd = op_readpc();
  op_io();
  if((bool)(sp & (1 << (opcode >> 5))) == (bool)(opcode & 0x10)) return;
  op_io();
  op_io();
  regs.pc += (int8)rd;
}
//##IMPL
//   case 0xc4: return op_write_dp(regs.a);
void SMPcore::op_write_dp(uint8 &r) {
  dp = op_readpc();
  op_readdp(dp);
  op_writedp(dp, regs.a);
}
//##IMPL
//   case 0xc5: return op_write_addr(regs.a);
void SMPcore::op_write_addr(uint8 &r) {
  dp.l = op_readpc();
  dp.h = op_readpc();
  op_read(dp);
  op_write(dp, regs.a);
}
//##IMPL
//   case 0xc6: return op_sta_ix();
void SMPcore::op_sta_ix() {
  op_io();
  op_readdp(regs.x);
  op_writedp(regs.x, regs.a);
}
//##IMPL
//   case 0xc7: return op_sta_idpx();
void SMPcore::op_sta_idpx() {
  sp = op_readpc() + regs.x;
  op_io();
  dp.l = op_readdp(sp++);
  dp.h = op_readdp(sp++);
  op_read(dp);
  op_write(dp, regs.a);
}
//##IMPL
//   case 0xc8: return op_read_const<&SMPcore::op_cmp>(regs.x);
void SMPcore::op_read_const(uint8 &r) {
  rd = op_readpc();
  regs.x = op_cmp(regs.x, rd);
}
//##IMPL
//   case 0xc9: return op_write_addr(regs.x);
void SMPcore::op_write_addr(uint8 &r) {
  dp.l = op_readpc();
  dp.h = op_readpc();
  op_read(dp);
  op_write(dp, regs.x);
}
//##IMPL
//   case 0xca: return op_set_addr_bit();
void SMPcore::op_set_addr_bit() {
  dp.l = op_readpc();
  dp.h = op_readpc();
  bit = dp >> 13;
  dp &= 0x1fff;
  rd = op_read(dp);
  op_io();
  rd = (rd & ~(1 << bit)) | (regs.p.c << bit);
  op_write(dp, rd);
}
//##IMPL
//   case 0xcb: return op_write_dp(regs.y);
void SMPcore::op_write_dp(uint8 &r) {
  dp = op_readpc();
  op_readdp(dp);
  op_writedp(dp, regs.y);
}
//##IMPL
//   case 0xcc: return op_write_addr(regs.y);
void SMPcore::op_write_addr(uint8 &r) {
  dp.l = op_readpc();
  dp.h = op_readpc();
  op_read(dp);
  op_write(dp, regs.y);
}
//##IMPL
//   case 0xcd: return op_read_const<&SMPcore::op_ld>(regs.x);
void SMPcore::op_read_const(uint8 &r) {
  rd = op_readpc();
  regs.x = op_ld(regs.x, rd);
}
//##IMPL
//   case 0xce: return op_pull(regs.x);
void SMPcore::op_pull(uint8 &r) {
  op_io();
  op_io();
  regs.x = op_readsp();
}
//##IMPL
//   case 0xcf: return op_mul_ya();
void SMPcore::op_mul_ya() {
  op_io();
  op_io();
  op_io();
  op_io();
  op_io();
  op_io();
  op_io();
  op_io();
  //[[
  ya = regs.y * regs.a;
  regs.a = ya;
  regs.y = ya >> 8;
  //result is set based on y (high-byte) only
  regs.p.n = (regs.y & 0x80);
  regs.p.z = (regs.y == 0);
  //]]
}
//##IMPL
//   case 0xd0: return op_branch(regs.p.z == 0);
void SMPcore::op_branch(bool condition) {
  rd = op_readpc();
  if(regs.p.z != 0) return;
  op_io();
  op_io();
  regs.pc += (int8)rd;
}
//##IMPL
//   case 0xd1: return op_jst();
void SMPcore::op_jst() {
  dp = 0xffde - ((opcode >> 4) << 1);
  rd.l = op_read(dp++);
  rd.h = op_read(dp++);
  op_io();
  op_io();
  op_io();
  op_writesp(regs.pc.h);
  op_writesp(regs.pc.l);
  regs.pc = rd;
}
//##IMPL
//   case 0xd2: return op_set_bit();
void SMPcore::op_set_bit() {
  dp = op_readpc();
  rd = op_readdp(dp) & ~(1 << (opcode >> 5));
  op_writedp(dp, rd | (!(opcode & 0x10) << (opcode >> 5)));
}
//##IMPL
//   case 0xd3: return op_branch_bit();
void SMPcore::op_branch_bit() {
  dp = op_readpc();
  sp = op_readdp(dp);
  rd = op_readpc();
  op_io();
  if((bool)(sp & (1 << (opcode >> 5))) == (bool)(opcode & 0x10)) return;
  op_io();
  op_io();
  regs.pc += (int8)rd;
}
//##IMPL
//   case 0xd4: return op_write_dpi(regs.a, regs.x);
void SMPcore::op_write_dpi(uint8 &r, uint8 &i) {
  dp = op_readpc() + regs.x;
  op_io();
  op_readdp(dp);
  op_writedp(dp, regs.a);
}
//##IMPL
//   case 0xd5: return op_write_addri(regs.x);
void SMPcore::op_write_addri(uint8 &i) {
  dp.l = op_readpc();
  dp.h = op_readpc();
  op_io();
  dp += regs.x;
  op_read(dp);
  op_write(dp, regs.a);
}
//##IMPL
//   case 0xd6: return op_write_addri(regs.y);
void SMPcore::op_write_addri(uint8 &i) {
  dp.l = op_readpc();
  dp.h = op_readpc();
  op_io();
  dp += regs.y;
  op_read(dp);
  op_write(dp, regs.a);
}
//##IMPL
//   case 0xd7: return op_sta_idpy();
void SMPcore::op_sta_idpy() {
  sp = op_readpc();
  dp.l = op_readdp(sp++);
  dp.h = op_readdp(sp++);
  op_io();
  dp += regs.y;
  op_read(dp);
  op_write(dp, regs.a);
}
//##IMPL
//   case 0xd8: return op_write_dp(regs.x);
void SMPcore::op_write_dp(uint8 &r) {
  dp = op_readpc();
  op_readdp(dp);
  op_writedp(dp, regs.x);
}
//##IMPL
//   case 0xd9: return op_write_dpi(regs.x, regs.y);
void SMPcore::op_write_dpi(uint8 &r, uint8 &i) {
  dp = op_readpc() + regs.y;
  op_io();
  op_readdp(dp);
  op_writedp(dp, regs.x);
}
//##IMPL
//   case 0xda: return op_stw_dp();
void SMPcore::op_stw_dp() {
  dp = op_readpc();
  op_readdp(dp);
  op_writedp(dp++, regs.a);
  op_writedp(dp++, regs.y);
}
//##IMPL
//   case 0xdb: return op_write_dpi(regs.y, regs.x);
void SMPcore::op_write_dpi(uint8 &r, uint8 &i) {
  dp = op_readpc() + regs.x;
  op_io();
  op_readdp(dp);
  op_writedp(dp, regs.y);
}
//##IMPL
//   case 0xdc: return op_adjust<&SMPcore::op_dec>(regs.y);
void SMPcore::op_adjust(uint8 &r) {
  op_io();
  regs.y = op_dec(regs.y);
}
//##IMPL
//   case 0xdd: return op_transfer(regs.y, regs.a);
void SMPcore::op_transfer(uint8 &from, uint8 &to) {
  op_io();
  regs.a = regs.y;
  regs.p.n = (regs.a & 0x80);
  regs.p.z = (regs.a == 0);
}
//##IMPL
//   case 0xde: return op_bne_dpx();
void SMPcore::op_bne_dpx() {
  dp = op_readpc();
  op_io();
  sp = op_readdp(dp + regs.x);
  rd = op_readpc();
  op_io();
  if(regs.a == sp) return;
  op_io();
  op_io();
  regs.pc += (int8)rd;
}
//##IMPL
//   case 0xdf: return op_daa();
void SMPcore::op_daa() {
  op_io();
  op_io();
  //[[
  if(regs.p.c || (regs.a) > 0x99) {
    regs.a += 0x60;
    regs.p.c = 1;
  }
  if(regs.p.h || (regs.a & 15) > 0x09) {
    regs.a += 0x06;
  }
  regs.p.n = (regs.a & 0x80);
  regs.p.z = (regs.a == 0);
  //]]
}
//##IMPL
//   case 0xe0: return op_clv();
void SMPcore::op_clv() {
  op_io();
  regs.p.v = 0;
  regs.p.h = 0;
}
//##IMPL
//   case 0xe1: return op_jst();
void SMPcore::op_jst() {
  dp = 0xffde - ((opcode >> 4) << 1);
  rd.l = op_read(dp++);
  rd.h = op_read(dp++);
  op_io();
  op_io();
  op_io();
  op_writesp(regs.pc.h);
  op_writesp(regs.pc.l);
  regs.pc = rd;
}
//##IMPL
//   case 0xe2: return op_set_bit();
void SMPcore::op_set_bit() {
  dp = op_readpc();
  rd = op_readdp(dp) & ~(1 << (opcode >> 5));
  op_writedp(dp, rd | (!(opcode & 0x10) << (opcode >> 5)));
}
//##IMPL
//   case 0xe3: return op_branch_bit();
void SMPcore::op_branch_bit() {
  dp = op_readpc();
  sp = op_readdp(dp);
  rd = op_readpc();
  op_io();
  if((bool)(sp & (1 << (opcode >> 5))) == (bool)(opcode & 0x10)) return;
  op_io();
  op_io();
  regs.pc += (int8)rd;
}
//##IMPL
//   case 0xe4: return op_read_dp<&SMPcore::op_ld>(regs.a);
void SMPcore::op_read_dp(uint8 &r) {
  dp = op_readpc();
  rd = op_readdp(dp);
  regs.a = op_ld(regs.a, rd);
}
//##IMPL
//   case 0xe5: return op_read_addr<&SMPcore::op_ld>(regs.a);
void SMPcore::op_read_addr(uint8 &r) {
  dp.l = op_readpc();
  dp.h = op_readpc();
  rd = op_read(dp);
  regs.a = op_ld(regs.a, rd);
}
//##IMPL
//   case 0xe6: return op_read_ix<&SMPcore::op_ld>();
void SMPcore::op_read_ix() {
  op_io();
  rd = op_readdp(regs.x);
  regs.a = op_ld(regs.a, rd);
}
//##IMPL
//   case 0xe7: return op_read_idpx<&SMPcore::op_ld>();
void SMPcore::op_read_idpx() {
  dp = op_readpc() + regs.x;
  op_io();
  sp.l = op_readdp(dp++);
  sp.h = op_readdp(dp++);
  rd = op_read(sp);
  regs.a = op_ld(regs.a, rd);
}
//##IMPL
//   case 0xe8: return op_read_const<&SMPcore::op_ld>(regs.a);
void SMPcore::op_read_const(uint8 &r) {
  rd = op_readpc();
  regs.a = op_ld(regs.a, rd);
}
//##IMPL
//   case 0xe9: return op_read_addr<&SMPcore::op_ld>(regs.x);
void SMPcore::op_read_addr(uint8 &r) {
  dp.l = op_readpc();
  dp.h = op_readpc();
  rd = op_read(dp);
  regs.x = op_ld(regs.x, rd);
}
//##IMPL
//   case 0xea: return op_set_addr_bit();
void SMPcore::op_set_addr_bit() {
  dp.l = op_readpc();
  dp.h = op_readpc();
  bit = dp >> 13;
  dp &= 0x1fff;
  rd = op_read(dp);
  rd ^= 1 << bit;
  op_write(dp, rd);
}
//##IMPL
//   case 0xeb: return op_read_dp<&SMPcore::op_ld>(regs.y);
void SMPcore::op_read_dp(uint8 &r) {
  dp = op_readpc();
  rd = op_readdp(dp);
  regs.y = op_ld(regs.y, rd);
}
//##IMPL
//   case 0xec: return op_read_addr<&SMPcore::op_ld>(regs.y);
void SMPcore::op_read_addr(uint8 &r) {
  dp.l = op_readpc();
  dp.h = op_readpc();
  rd = op_read(dp);
  regs.y = op_ld(regs.y, rd);
}
//##IMPL
//   case 0xed: return op_cmc();
void SMPcore::op_cmc() {
  op_io();
  op_io();
  regs.p.c = !regs.p.c;
}
//##IMPL
//   case 0xee: return op_pull(regs.y);
void SMPcore::op_pull(uint8 &r) {
  op_io();
  op_io();
  regs.y = op_readsp();
}
//##IMPL
//   case 0xef: return op_wait();
void SMPcore::op_wait() {
  op_io();
  op_io();
  //!!REPEAT
}
//##IMPL
//   case 0xf0: return op_branch(regs.p.z == 1);
void SMPcore::op_branch(bool condition) {
  rd = op_readpc();
  if(regs.p.z != 1) return;
  op_io();
  op_io();
  regs.pc += (int8)rd;
}
//##IMPL
//   case 0xf1: return op_jst();
void SMPcore::op_jst() {
  dp = 0xffde - ((opcode >> 4) << 1);
  rd.l = op_read(dp++);
  rd.h = op_read(dp++);
  op_io();
  op_io();
  op_io();
  op_writesp(regs.pc.h);
  op_writesp(regs.pc.l);
  regs.pc = rd;
}
//##IMPL
//   case 0xf2: return op_set_bit();
void SMPcore::op_set_bit() {
  dp = op_readpc();
  rd = op_readdp(dp) & ~(1 << (opcode >> 5));
  op_writedp(dp, rd | (!(opcode & 0x10) << (opcode >> 5)));
}
//##IMPL
//   case 0xf3: return op_branch_bit();
void SMPcore::op_branch_bit() {
  dp = op_readpc();
  sp = op_readdp(dp);
  rd = op_readpc();
  op_io();
  if((bool)(sp & (1 << (opcode >> 5))) == (bool)(opcode & 0x10)) return;
  op_io();
  op_io();
  regs.pc += (int8)rd;
}
//##IMPL
//   case 0xf4: return op_read_dpi<&SMPcore::op_ld>(regs.a, regs.x);
void SMPcore::op_read_dpi(uint8 &r, uint8 &i) {
  dp = op_readpc();
  op_io();
  rd = op_readdp(dp + regs.x);
  regs.a = op_ld(regs.a, rd);
}
//##IMPL
//   case 0xf5: return op_read_addri<&SMPcore::op_ld>(regs.x);
void SMPcore::op_read_addri(uint8 &r) {
  dp.l = op_readpc();
  dp.h = op_readpc();
  op_io();
  rd = op_read(dp + regs.x);
  regs.a = op_ld(regs.a, rd);
}
//##IMPL
//   case 0xf6: return op_read_addri<&SMPcore::op_ld>(regs.y);
void SMPcore::op_read_addri(uint8 &r) {
  dp.l = op_readpc();
  dp.h = op_readpc();
  op_io();
  rd = op_read(dp + regs.y);
  regs.a = op_ld(regs.a, rd);
}
//##IMPL
//   case 0xf7: return op_read_idpy<&SMPcore::op_ld>();
void SMPcore::op_read_idpy() {
  dp = op_readpc();
  op_io();
  sp.l = op_readdp(dp++);
  sp.h = op_readdp(dp++);
  rd = op_read(sp + regs.y);
  regs.a = op_ld(regs.a, rd);
}
//##IMPL
//   case 0xf8: return op_read_dp<&SMPcore::op_ld>(regs.x);
void SMPcore::op_read_dp(uint8 &r) {
  dp = op_readpc();
  rd = op_readdp(dp);
  regs.x = op_ld(regs.x, rd);
}
//##IMPL
//   case 0xf9: return op_read_dpi<&SMPcore::op_ld>(regs.x, regs.y);
void SMPcore::op_read_dpi(uint8 &r, uint8 &i) {
  dp = op_readpc();
  op_io();
  rd = op_readdp(dp + regs.y);
  regs.x = op_ld(regs.x, rd);
}
//##IMPL
//   case 0xfa: return op_write_dp_dp<&SMPcore::op_st>();
void SMPcore::op_write_dp_dp() {
  sp = op_readpc();
  rd = op_readdp(sp);
  dp = op_readpc();
  wr = op_st(wr, rd);
  op_writedp(dp, wr);
}
//##IMPL
//   case 0xfb: return op_read_dpi<&SMPcore::op_ld>(regs.y, regs.x);
void SMPcore::op_read_dpi(uint8 &r, uint8 &i) {
  dp = op_readpc();
  op_io();
  rd = op_readdp(dp + regs.x);
  regs.y = op_ld(regs.y, rd);
}
//##IMPL
//   case 0xfc: return op_adjust<&SMPcore::op_inc>(regs.y);
void SMPcore::op_adjust(uint8 &r) {
  op_io();
  regs.y = op_inc(regs.y);
}
//##IMPL
//   case 0xfd: return op_transfer(regs.a, regs.y);
void SMPcore::op_transfer(uint8 &from, uint8 &to) {
  op_io();
  regs.y = regs.a;
  regs.p.n = (regs.y & 0x80);
  regs.p.z = (regs.y == 0);
}
//##IMPL
//   case 0xfe: return op_bne_ydec();
void SMPcore::op_bne_ydec() {
  rd = op_readpc();
  op_io();
  op_io();
  if(--regs.y == 0) return;
  op_io();
  op_io();
  regs.pc += (int8)rd;
}
//##IMPL
//   case 0xff: return op_wait();
void SMPcore::op_wait() {
  op_io();
  op_io();
  //!!REPEAT
}
