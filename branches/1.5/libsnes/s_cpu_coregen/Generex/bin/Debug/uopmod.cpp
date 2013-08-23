const int uoptable[][] = {

//   case 0x00: return op_nop();
//   void SMPcore::op_nop() {
{
  1, // op_io();
  2, // //!!NEXT
},
//   case 0x01: return op_jst();
//   void SMPcore::op_jst() {
{
  3, // dp = 0xffde - ((opcode >> 4) << 1);
  4, // rd.l = op_read(dp++);
  5, // rd.h = op_read(dp++);
  1, // op_io();
  1, // op_io();
  1, // op_io();
  6, // op_writesp(regs.pc.h);
  7, // op_writesp(regs.pc.l);
  8, // regs.pc = rd;
  2, // //!!NEXT
},
//   case 0x02: return op_set_bit();
//   void SMPcore::op_set_bit() {
{
  9, // dp = op_readpc();
  10, // rd = op_readdp(dp) & ~(1 << (opcode >> 5));
  11, // op_writedp(dp, rd | (!(opcode & 0x10) << (opcode >> 5)));
  2, // //!!NEXT
},
//   case 0x03: return op_branch_bit();
//   void SMPcore::op_branch_bit() {
{
  9, // dp = op_readpc();
  12, // sp = op_readdp(dp);
  13, // rd = op_readpc();
  1, // op_io();
  14, // if((bool)(sp & (1 << (opcode >> 5))) == (bool)(opcode & 0x10)) return;
  1, // op_io();
  1, // op_io();
  15, // regs.pc += (int8)rd;
  2, // //!!NEXT
},
//   case 0x04: return op_read_dp<&SMPcore::op_or>(regs.a);
//   void SMPcore::op_read_dp(uint8 &r) {
{
  9, // dp = op_readpc();
  16, // rd = op_readdp(dp);
  17, // regs.a = op_or(regs.a, rd);
  2, // //!!NEXT
},
//   case 0x05: return op_read_addr<&SMPcore::op_or>(regs.a);
//   void SMPcore::op_read_addr(uint8 &r) {
{
  18, // dp.l = op_readpc();
  19, // dp.h = op_readpc();
  20, // rd = op_read(dp);
  17, // regs.a = op_or(regs.a, rd);
  2, // //!!NEXT
},
//   case 0x06: return op_read_ix<&SMPcore::op_or>();
//   void SMPcore::op_read_ix() {
{
  1, // op_io();
  21, // rd = op_readdp(regs.x);
  17, // regs.a = op_or(regs.a, rd);
  2, // //!!NEXT
},
//   case 0x07: return op_read_idpx<&SMPcore::op_or>();
//   void SMPcore::op_read_idpx() {
{
  22, // dp = op_readpc() + regs.x;
  1, // op_io();
  23, // sp.l = op_readdp(dp++);
  24, // sp.h = op_readdp(dp++);
  25, // rd = op_read(sp);
  17, // regs.a = op_or(regs.a, rd);
  2, // //!!NEXT
},
//   case 0x08: return op_read_const<&SMPcore::op_or>(regs.a);
//   void SMPcore::op_read_const(uint8 &r) {
{
  13, // rd = op_readpc();
  17, // regs.a = op_or(regs.a, rd);
  2, // //!!NEXT
},
//   case 0x09: return op_write_dp_dp<&SMPcore::op_or>();
//   void SMPcore::op_write_dp_dp() {
{
  26, // sp = op_readpc();
  27, // rd = op_readdp(sp);
  9, // dp = op_readpc();
  28, // wr = op_readdp(dp);
  29, // wr = op_or(wr, rd);
  30, // op_writedp(dp, wr);
  2, // //!!NEXT
},
//   case 0x0a: return op_set_addr_bit();
//   void SMPcore::op_set_addr_bit() {
{
  18, // dp.l = op_readpc();
  19, // dp.h = op_readpc();
  31, // bit = dp >> 13;
  32, // dp &= 0x1fff;
  20, // rd = op_read(dp);
  1, // op_io();
  33, // regs.p.c |= (rd & (1 << bit)) ^ 0;
  2, // //!!NEXT
},
//   case 0x0b: return op_adjust_dp<&SMPcore::op_asl>();
//   void SMPcore::op_adjust_dp() {
{
  9, // dp = op_readpc();
  16, // rd = op_readdp(dp);
  34, // rd = op_asl(rd);
  35, // op_writedp(dp, rd);
  2, // //!!NEXT
},
//   case 0x0c: return op_adjust_addr<&SMPcore::op_asl>();
//   void SMPcore::op_adjust_addr() {
{
  18, // dp.l = op_readpc();
  19, // dp.h = op_readpc();
  20, // rd = op_read(dp);
  34, // rd = op_asl(rd);
  36, // op_write(dp, rd);
  2, // //!!NEXT
},
//   case 0x0d: return op_push(regs.p);
//   void SMPcore::op_push(uint8 r) {
{
  1, // op_io();
  1, // op_io();
  37, // op_writesp(regs.p);
  2, // //!!NEXT
},
//   case 0x0e: return op_test_addr(1);
//   void SMPcore::op_test_addr(bool set) {
{
  18, // dp.l = op_readpc();
  19, // dp.h = op_readpc();
  20, // rd = op_read(dp);
  38, // regs.p.n = (regs.a - rd) & 0x80;
  39, // regs.p.z = (regs.a - rd) == 0;
  40, // op_read(dp);
  41, // op_write(dp, rd | regs.a);
  2, // //!!NEXT
},
//   case 0x0f: return op_brk();
//   void SMPcore::op_brk() {
{
  42, // rd.l = op_read(0xffde);
  43, // rd.h = op_read(0xffdf);
  1, // op_io();
  1, // op_io();
  6, // op_writesp(regs.pc.h);
  7, // op_writesp(regs.pc.l);
  37, // op_writesp(regs.p);
  8, // regs.pc = rd;
  44, // regs.p.b = 1;
  45, // regs.p.i = 0;
  2, // //!!NEXT
},
//   case 0x10: return op_branch(regs.p.n == 0);
//   void SMPcore::op_branch(bool condition) {
{
  13, // rd = op_readpc();
  46, // if(regs.p.n != 0) return;
  1, // op_io();
  1, // op_io();
  15, // regs.pc += (int8)rd;
  2, // //!!NEXT
},
//   case 0x11: return op_jst();
//   void SMPcore::op_jst() {
{
  3, // dp = 0xffde - ((opcode >> 4) << 1);
  4, // rd.l = op_read(dp++);
  5, // rd.h = op_read(dp++);
  1, // op_io();
  1, // op_io();
  1, // op_io();
  6, // op_writesp(regs.pc.h);
  7, // op_writesp(regs.pc.l);
  8, // regs.pc = rd;
  2, // //!!NEXT
},
//   case 0x12: return op_set_bit();
//   void SMPcore::op_set_bit() {
{
  9, // dp = op_readpc();
  10, // rd = op_readdp(dp) & ~(1 << (opcode >> 5));
  11, // op_writedp(dp, rd | (!(opcode & 0x10) << (opcode >> 5)));
  2, // //!!NEXT
},
//   case 0x13: return op_branch_bit();
//   void SMPcore::op_branch_bit() {
{
  9, // dp = op_readpc();
  12, // sp = op_readdp(dp);
  13, // rd = op_readpc();
  1, // op_io();
  14, // if((bool)(sp & (1 << (opcode >> 5))) == (bool)(opcode & 0x10)) return;
  1, // op_io();
  1, // op_io();
  15, // regs.pc += (int8)rd;
  2, // //!!NEXT
},
//   case 0x14: return op_read_dpi<&SMPcore::op_or>(regs.a, regs.x);
//   void SMPcore::op_read_dpi(uint8 &r, uint8 &i) {
{
  9, // dp = op_readpc();
  1, // op_io();
  47, // rd = op_readdp(dp + regs.x);
  17, // regs.a = op_or(regs.a, rd);
  2, // //!!NEXT
},
//   case 0x15: return op_read_addri<&SMPcore::op_or>(regs.x);
//   void SMPcore::op_read_addri(uint8 &r) {
{
  18, // dp.l = op_readpc();
  19, // dp.h = op_readpc();
  1, // op_io();
  48, // rd = op_read(dp + regs.x);
  17, // regs.a = op_or(regs.a, rd);
  2, // //!!NEXT
},
//   case 0x16: return op_read_addri<&SMPcore::op_or>(regs.y);
//   void SMPcore::op_read_addri(uint8 &r) {
{
  18, // dp.l = op_readpc();
  19, // dp.h = op_readpc();
  1, // op_io();
  49, // rd = op_read(dp + regs.y);
  17, // regs.a = op_or(regs.a, rd);
  2, // //!!NEXT
},
//   case 0x17: return op_read_idpy<&SMPcore::op_or>();
//   void SMPcore::op_read_idpy() {
{
  9, // dp = op_readpc();
  1, // op_io();
  23, // sp.l = op_readdp(dp++);
  24, // sp.h = op_readdp(dp++);
  50, // rd = op_read(sp + regs.y);
  17, // regs.a = op_or(regs.a, rd);
  2, // //!!NEXT
},
//   case 0x18: return op_write_dp_const<&SMPcore::op_or>();
//   void SMPcore::op_write_dp_const() {
{
  13, // rd = op_readpc();
  9, // dp = op_readpc();
  28, // wr = op_readdp(dp);
  29, // wr = op_or(wr, rd);
  30, // op_writedp(dp, wr);
  2, // //!!NEXT
},
//   case 0x19: return op_write_ix_iy<&SMPcore::op_or>();
//   void SMPcore::op_write_ix_iy() {
{
  1, // op_io();
  51, // rd = op_readdp(regs.y);
  52, // wr = op_readdp(regs.x);
  29, // wr = op_or(wr, rd);
  53, // op_writedp(regs.x, wr);
  2, // //!!NEXT
},
//   case 0x1a: return op_adjust_dpw(-1);
//   void SMPcore::op_adjust_dpw(signed n) {
{
  9, // dp = op_readpc();
  54, // rd.w = op_readdp(dp) - 1;
  55, // op_writedp(dp++, rd.l);
  56, // rd.h += op_readdp(dp);
  57, // op_writedp(dp++, rd.h);
  58, // regs.p.n = rd & 0x8000;
  59, // regs.p.z = rd == 0;
  2, // //!!NEXT
},
//   case 0x1b: return op_adjust_dpx<&SMPcore::op_asl>();
//   void SMPcore::op_adjust_dpx() {
{
  9, // dp = op_readpc();
  1, // op_io();
  47, // rd = op_readdp(dp + regs.x);
  34, // rd = op_asl(rd);
  60, // op_writedp(dp + regs.x, rd);
  2, // //!!NEXT
},
//   case 0x1c: return op_adjust<&SMPcore::op_asl>(regs.a);
//   void SMPcore::op_adjust(uint8 &r) {
{
  1, // op_io();
  61, // regs.a = op_asl(regs.a);
  2, // //!!NEXT
},
//   case 0x1d: return op_adjust<&SMPcore::op_dec>(regs.x);
//   void SMPcore::op_adjust(uint8 &r) {
{
  1, // op_io();
  62, // regs.x = op_dec(regs.x);
  2, // //!!NEXT
},
//   case 0x1e: return op_read_addr<&SMPcore::op_cmp>(regs.x);
//   void SMPcore::op_read_addr(uint8 &r) {
{
  18, // dp.l = op_readpc();
  19, // dp.h = op_readpc();
  20, // rd = op_read(dp);
  63, // regs.x = op_cmp(regs.x, rd);
  2, // //!!NEXT
},
//   case 0x1f: return op_jmp_iaddrx();
//   void SMPcore::op_jmp_iaddrx() {
{
  18, // dp.l = op_readpc();
  19, // dp.h = op_readpc();
  1, // op_io();
  64, // dp += regs.x;
  4, // rd.l = op_read(dp++);
  5, // rd.h = op_read(dp++);
  8, // regs.pc = rd;
  2, // //!!NEXT
},
//   case 0x20: return op_set_flag(regs.p.p, 0);
//   void SMPcore::op_set_flag(bool &flag, bool data) {
{
  1, // op_io();
  65, // regs.p.p = 0;
  2, // //!!NEXT
},
//   case 0x21: return op_jst();
//   void SMPcore::op_jst() {
{
  3, // dp = 0xffde - ((opcode >> 4) << 1);
  4, // rd.l = op_read(dp++);
  5, // rd.h = op_read(dp++);
  1, // op_io();
  1, // op_io();
  1, // op_io();
  6, // op_writesp(regs.pc.h);
  7, // op_writesp(regs.pc.l);
  8, // regs.pc = rd;
  2, // //!!NEXT
},
//   case 0x22: return op_set_bit();
//   void SMPcore::op_set_bit() {
{
  9, // dp = op_readpc();
  10, // rd = op_readdp(dp) & ~(1 << (opcode >> 5));
  11, // op_writedp(dp, rd | (!(opcode & 0x10) << (opcode >> 5)));
  2, // //!!NEXT
},
//   case 0x23: return op_branch_bit();
//   void SMPcore::op_branch_bit() {
{
  9, // dp = op_readpc();
  12, // sp = op_readdp(dp);
  13, // rd = op_readpc();
  1, // op_io();
  14, // if((bool)(sp & (1 << (opcode >> 5))) == (bool)(opcode & 0x10)) return;
  1, // op_io();
  1, // op_io();
  15, // regs.pc += (int8)rd;
  2, // //!!NEXT
},
//   case 0x24: return op_read_dp<&SMPcore::op_and>(regs.a);
//   void SMPcore::op_read_dp(uint8 &r) {
{
  9, // dp = op_readpc();
  16, // rd = op_readdp(dp);
  66, // regs.a = op_and(regs.a, rd);
  2, // //!!NEXT
},
//   case 0x25: return op_read_addr<&SMPcore::op_and>(regs.a);
//   void SMPcore::op_read_addr(uint8 &r) {
{
  18, // dp.l = op_readpc();
  19, // dp.h = op_readpc();
  20, // rd = op_read(dp);
  66, // regs.a = op_and(regs.a, rd);
  2, // //!!NEXT
},
//   case 0x26: return op_read_ix<&SMPcore::op_and>();
//   void SMPcore::op_read_ix() {
{
  1, // op_io();
  21, // rd = op_readdp(regs.x);
  66, // regs.a = op_and(regs.a, rd);
  2, // //!!NEXT
},
//   case 0x27: return op_read_idpx<&SMPcore::op_and>();
//   void SMPcore::op_read_idpx() {
{
  22, // dp = op_readpc() + regs.x;
  1, // op_io();
  23, // sp.l = op_readdp(dp++);
  24, // sp.h = op_readdp(dp++);
  25, // rd = op_read(sp);
  66, // regs.a = op_and(regs.a, rd);
  2, // //!!NEXT
},
//   case 0x28: return op_read_const<&SMPcore::op_and>(regs.a);
//   void SMPcore::op_read_const(uint8 &r) {
{
  13, // rd = op_readpc();
  66, // regs.a = op_and(regs.a, rd);
  2, // //!!NEXT
},
//   case 0x29: return op_write_dp_dp<&SMPcore::op_and>();
//   void SMPcore::op_write_dp_dp() {
{
  26, // sp = op_readpc();
  27, // rd = op_readdp(sp);
  9, // dp = op_readpc();
  28, // wr = op_readdp(dp);
  67, // wr = op_and(wr, rd);
  30, // op_writedp(dp, wr);
  2, // //!!NEXT
},
//   case 0x2a: return op_set_addr_bit();
//   void SMPcore::op_set_addr_bit() {
{
  18, // dp.l = op_readpc();
  19, // dp.h = op_readpc();
  31, // bit = dp >> 13;
  32, // dp &= 0x1fff;
  20, // rd = op_read(dp);
  1, // op_io();
  68, // regs.p.c |= (rd & (1 << bit)) ^ 1;
  2, // //!!NEXT
},
//   case 0x2b: return op_adjust_dp<&SMPcore::op_rol>();
//   void SMPcore::op_adjust_dp() {
{
  9, // dp = op_readpc();
  16, // rd = op_readdp(dp);
  69, // rd = op_rol(rd);
  35, // op_writedp(dp, rd);
  2, // //!!NEXT
},
//   case 0x2c: return op_adjust_addr<&SMPcore::op_rol>();
//   void SMPcore::op_adjust_addr() {
{
  18, // dp.l = op_readpc();
  19, // dp.h = op_readpc();
  20, // rd = op_read(dp);
  69, // rd = op_rol(rd);
  36, // op_write(dp, rd);
  2, // //!!NEXT
},
//   case 0x2d: return op_push(regs.a);
//   void SMPcore::op_push(uint8 r) {
{
  1, // op_io();
  1, // op_io();
  70, // op_writesp(regs.a);
  2, // //!!NEXT
},
//   case 0x2e: return op_bne_dp();
//   void SMPcore::op_bne_dp() {
{
  9, // dp = op_readpc();
  12, // sp = op_readdp(dp);
  13, // rd = op_readpc();
  1, // op_io();
  71, // if(regs.a == sp) return;
  1, // op_io();
  1, // op_io();
  15, // regs.pc += (int8)rd;
  2, // //!!NEXT
},
//   case 0x2f: return op_branch(true);
//   void SMPcore::op_branch(bool condition) {
{
  13, // rd = op_readpc();
  1, // op_io();
  1, // op_io();
  15, // regs.pc += (int8)rd;
  2, // //!!NEXT
},
//   case 0x30: return op_branch(regs.p.n == 1);
//   void SMPcore::op_branch(bool condition) {
{
  13, // rd = op_readpc();
  72, // if(regs.p.n != 1) return;
  1, // op_io();
  1, // op_io();
  15, // regs.pc += (int8)rd;
  2, // //!!NEXT
},
//   case 0x31: return op_jst();
//   void SMPcore::op_jst() {
{
  3, // dp = 0xffde - ((opcode >> 4) << 1);
  4, // rd.l = op_read(dp++);
  5, // rd.h = op_read(dp++);
  1, // op_io();
  1, // op_io();
  1, // op_io();
  6, // op_writesp(regs.pc.h);
  7, // op_writesp(regs.pc.l);
  8, // regs.pc = rd;
  2, // //!!NEXT
},
//   case 0x32: return op_set_bit();
//   void SMPcore::op_set_bit() {
{
  9, // dp = op_readpc();
  10, // rd = op_readdp(dp) & ~(1 << (opcode >> 5));
  11, // op_writedp(dp, rd | (!(opcode & 0x10) << (opcode >> 5)));
  2, // //!!NEXT
},
//   case 0x33: return op_branch_bit();
//   void SMPcore::op_branch_bit() {
{
  9, // dp = op_readpc();
  12, // sp = op_readdp(dp);
  13, // rd = op_readpc();
  1, // op_io();
  14, // if((bool)(sp & (1 << (opcode >> 5))) == (bool)(opcode & 0x10)) return;
  1, // op_io();
  1, // op_io();
  15, // regs.pc += (int8)rd;
  2, // //!!NEXT
},
//   case 0x34: return op_read_dpi<&SMPcore::op_and>(regs.a, regs.x);
//   void SMPcore::op_read_dpi(uint8 &r, uint8 &i) {
{
  9, // dp = op_readpc();
  1, // op_io();
  47, // rd = op_readdp(dp + regs.x);
  66, // regs.a = op_and(regs.a, rd);
  2, // //!!NEXT
},
//   case 0x35: return op_read_addri<&SMPcore::op_and>(regs.x);
//   void SMPcore::op_read_addri(uint8 &r) {
{
  18, // dp.l = op_readpc();
  19, // dp.h = op_readpc();
  1, // op_io();
  48, // rd = op_read(dp + regs.x);
  66, // regs.a = op_and(regs.a, rd);
  2, // //!!NEXT
},
//   case 0x36: return op_read_addri<&SMPcore::op_and>(regs.y);
//   void SMPcore::op_read_addri(uint8 &r) {
{
  18, // dp.l = op_readpc();
  19, // dp.h = op_readpc();
  1, // op_io();
  49, // rd = op_read(dp + regs.y);
  66, // regs.a = op_and(regs.a, rd);
  2, // //!!NEXT
},
//   case 0x37: return op_read_idpy<&SMPcore::op_and>();
//   void SMPcore::op_read_idpy() {
{
  9, // dp = op_readpc();
  1, // op_io();
  23, // sp.l = op_readdp(dp++);
  24, // sp.h = op_readdp(dp++);
  50, // rd = op_read(sp + regs.y);
  66, // regs.a = op_and(regs.a, rd);
  2, // //!!NEXT
},
//   case 0x38: return op_write_dp_const<&SMPcore::op_and>();
//   void SMPcore::op_write_dp_const() {
{
  13, // rd = op_readpc();
  9, // dp = op_readpc();
  28, // wr = op_readdp(dp);
  67, // wr = op_and(wr, rd);
  30, // op_writedp(dp, wr);
  2, // //!!NEXT
},
//   case 0x39: return op_write_ix_iy<&SMPcore::op_and>();
//   void SMPcore::op_write_ix_iy() {
{
  1, // op_io();
  51, // rd = op_readdp(regs.y);
  52, // wr = op_readdp(regs.x);
  67, // wr = op_and(wr, rd);
  53, // op_writedp(regs.x, wr);
  2, // //!!NEXT
},
//   case 0x3a: return op_adjust_dpw(+1);
//   void SMPcore::op_adjust_dpw(signed n) {
{
  9, // dp = op_readpc();
  73, // rd.w = op_readdp(dp) + 1;
  55, // op_writedp(dp++, rd.l);
  56, // rd.h += op_readdp(dp);
  57, // op_writedp(dp++, rd.h);
  58, // regs.p.n = rd & 0x8000;
  59, // regs.p.z = rd == 0;
  2, // //!!NEXT
},
//   case 0x3b: return op_adjust_dpx<&SMPcore::op_rol>();
//   void SMPcore::op_adjust_dpx() {
{
  9, // dp = op_readpc();
  1, // op_io();
  47, // rd = op_readdp(dp + regs.x);
  69, // rd = op_rol(rd);
  60, // op_writedp(dp + regs.x, rd);
  2, // //!!NEXT
},
//   case 0x3c: return op_adjust<&SMPcore::op_rol>(regs.a);
//   void SMPcore::op_adjust(uint8 &r) {
{
  1, // op_io();
  74, // regs.a = op_rol(regs.a);
  2, // //!!NEXT
},
//   case 0x3d: return op_adjust<&SMPcore::op_inc>(regs.x);
//   void SMPcore::op_adjust(uint8 &r) {
{
  1, // op_io();
  75, // regs.x = op_inc(regs.x);
  2, // //!!NEXT
},
//   case 0x3e: return op_read_dp<&SMPcore::op_cmp>(regs.x);
//   void SMPcore::op_read_dp(uint8 &r) {
{
  9, // dp = op_readpc();
  16, // rd = op_readdp(dp);
  63, // regs.x = op_cmp(regs.x, rd);
  2, // //!!NEXT
},
//   case 0x3f: return op_jsr_addr();
//   void SMPcore::op_jsr_addr() {
{
  76, // rd.l = op_readpc();
  77, // rd.h = op_readpc();
  1, // op_io();
  1, // op_io();
  1, // op_io();
  6, // op_writesp(regs.pc.h);
  7, // op_writesp(regs.pc.l);
  8, // regs.pc = rd;
  2, // //!!NEXT
},
//   case 0x40: return op_set_flag(regs.p.p, 1);
//   void SMPcore::op_set_flag(bool &flag, bool data) {
{
  1, // op_io();
  78, // regs.p.p = 1;
  2, // //!!NEXT
},
//   case 0x41: return op_jst();
//   void SMPcore::op_jst() {
{
  3, // dp = 0xffde - ((opcode >> 4) << 1);
  4, // rd.l = op_read(dp++);
  5, // rd.h = op_read(dp++);
  1, // op_io();
  1, // op_io();
  1, // op_io();
  6, // op_writesp(regs.pc.h);
  7, // op_writesp(regs.pc.l);
  8, // regs.pc = rd;
  2, // //!!NEXT
},
//   case 0x42: return op_set_bit();
//   void SMPcore::op_set_bit() {
{
  9, // dp = op_readpc();
  10, // rd = op_readdp(dp) & ~(1 << (opcode >> 5));
  11, // op_writedp(dp, rd | (!(opcode & 0x10) << (opcode >> 5)));
  2, // //!!NEXT
},
//   case 0x43: return op_branch_bit();
//   void SMPcore::op_branch_bit() {
{
  9, // dp = op_readpc();
  12, // sp = op_readdp(dp);
  13, // rd = op_readpc();
  1, // op_io();
  14, // if((bool)(sp & (1 << (opcode >> 5))) == (bool)(opcode & 0x10)) return;
  1, // op_io();
  1, // op_io();
  15, // regs.pc += (int8)rd;
  2, // //!!NEXT
},
//   case 0x44: return op_read_dp<&SMPcore::op_eor>(regs.a);
//   void SMPcore::op_read_dp(uint8 &r) {
{
  9, // dp = op_readpc();
  16, // rd = op_readdp(dp);
  79, // regs.a = op_eor(regs.a, rd);
  2, // //!!NEXT
},
//   case 0x45: return op_read_addr<&SMPcore::op_eor>(regs.a);
//   void SMPcore::op_read_addr(uint8 &r) {
{
  18, // dp.l = op_readpc();
  19, // dp.h = op_readpc();
  20, // rd = op_read(dp);
  79, // regs.a = op_eor(regs.a, rd);
  2, // //!!NEXT
},
//   case 0x46: return op_read_ix<&SMPcore::op_eor>();
//   void SMPcore::op_read_ix() {
{
  1, // op_io();
  21, // rd = op_readdp(regs.x);
  79, // regs.a = op_eor(regs.a, rd);
  2, // //!!NEXT
},
//   case 0x47: return op_read_idpx<&SMPcore::op_eor>();
//   void SMPcore::op_read_idpx() {
{
  22, // dp = op_readpc() + regs.x;
  1, // op_io();
  23, // sp.l = op_readdp(dp++);
  24, // sp.h = op_readdp(dp++);
  25, // rd = op_read(sp);
  79, // regs.a = op_eor(regs.a, rd);
  2, // //!!NEXT
},
//   case 0x48: return op_read_const<&SMPcore::op_eor>(regs.a);
//   void SMPcore::op_read_const(uint8 &r) {
{
  13, // rd = op_readpc();
  79, // regs.a = op_eor(regs.a, rd);
  2, // //!!NEXT
},
//   case 0x49: return op_write_dp_dp<&SMPcore::op_eor>();
//   void SMPcore::op_write_dp_dp() {
{
  26, // sp = op_readpc();
  27, // rd = op_readdp(sp);
  9, // dp = op_readpc();
  28, // wr = op_readdp(dp);
  80, // wr = op_eor(wr, rd);
  30, // op_writedp(dp, wr);
  2, // //!!NEXT
},
//   case 0x4a: return op_set_addr_bit();
//   void SMPcore::op_set_addr_bit() {
{
  18, // dp.l = op_readpc();
  19, // dp.h = op_readpc();
  31, // bit = dp >> 13;
  32, // dp &= 0x1fff;
  20, // rd = op_read(dp);
  81, // regs.p.c &= (rd & (1 << bit)) ^ 0;
  2, // //!!NEXT
},
//   case 0x4b: return op_adjust_dp<&SMPcore::op_lsr>();
//   void SMPcore::op_adjust_dp() {
{
  9, // dp = op_readpc();
  16, // rd = op_readdp(dp);
  82, // rd = op_lsr(rd);
  35, // op_writedp(dp, rd);
  2, // //!!NEXT
},
//   case 0x4c: return op_adjust_addr<&SMPcore::op_lsr>();
//   void SMPcore::op_adjust_addr() {
{
  18, // dp.l = op_readpc();
  19, // dp.h = op_readpc();
  20, // rd = op_read(dp);
  82, // rd = op_lsr(rd);
  36, // op_write(dp, rd);
  2, // //!!NEXT
},
//   case 0x4d: return op_push(regs.x);
//   void SMPcore::op_push(uint8 r) {
{
  1, // op_io();
  1, // op_io();
  83, // op_writesp(regs.x);
  2, // //!!NEXT
},
//   case 0x4e: return op_test_addr(0);
//   void SMPcore::op_test_addr(bool set) {
{
  18, // dp.l = op_readpc();
  19, // dp.h = op_readpc();
  20, // rd = op_read(dp);
  38, // regs.p.n = (regs.a - rd) & 0x80;
  39, // regs.p.z = (regs.a - rd) == 0;
  40, // op_read(dp);
  84, // op_write(dp, rd & ~regs.a);
  2, // //!!NEXT
},
//   case 0x4f: return op_jsp_dp();
//   void SMPcore::op_jsp_dp() {
{
  13, // rd = op_readpc();
  1, // op_io();
  1, // op_io();
  6, // op_writesp(regs.pc.h);
  7, // op_writesp(regs.pc.l);
  85, // regs.pc = 0xff00 | rd;
  2, // //!!NEXT
},
//   case 0x50: return op_branch(regs.p.v == 0);
//   void SMPcore::op_branch(bool condition) {
{
  13, // rd = op_readpc();
  86, // if(regs.p.v != 0) return;
  1, // op_io();
  1, // op_io();
  15, // regs.pc += (int8)rd;
  2, // //!!NEXT
},
//   case 0x51: return op_jst();
//   void SMPcore::op_jst() {
{
  3, // dp = 0xffde - ((opcode >> 4) << 1);
  4, // rd.l = op_read(dp++);
  5, // rd.h = op_read(dp++);
  1, // op_io();
  1, // op_io();
  1, // op_io();
  6, // op_writesp(regs.pc.h);
  7, // op_writesp(regs.pc.l);
  8, // regs.pc = rd;
  2, // //!!NEXT
},
//   case 0x52: return op_set_bit();
//   void SMPcore::op_set_bit() {
{
  9, // dp = op_readpc();
  10, // rd = op_readdp(dp) & ~(1 << (opcode >> 5));
  11, // op_writedp(dp, rd | (!(opcode & 0x10) << (opcode >> 5)));
  2, // //!!NEXT
},
//   case 0x53: return op_branch_bit();
//   void SMPcore::op_branch_bit() {
{
  9, // dp = op_readpc();
  12, // sp = op_readdp(dp);
  13, // rd = op_readpc();
  1, // op_io();
  14, // if((bool)(sp & (1 << (opcode >> 5))) == (bool)(opcode & 0x10)) return;
  1, // op_io();
  1, // op_io();
  15, // regs.pc += (int8)rd;
  2, // //!!NEXT
},
//   case 0x54: return op_read_dpi<&SMPcore::op_eor>(regs.a, regs.x);
//   void SMPcore::op_read_dpi(uint8 &r, uint8 &i) {
{
  9, // dp = op_readpc();
  1, // op_io();
  47, // rd = op_readdp(dp + regs.x);
  79, // regs.a = op_eor(regs.a, rd);
  2, // //!!NEXT
},
//   case 0x55: return op_read_addri<&SMPcore::op_eor>(regs.x);
//   void SMPcore::op_read_addri(uint8 &r) {
{
  18, // dp.l = op_readpc();
  19, // dp.h = op_readpc();
  1, // op_io();
  48, // rd = op_read(dp + regs.x);
  79, // regs.a = op_eor(regs.a, rd);
  2, // //!!NEXT
},
//   case 0x56: return op_read_addri<&SMPcore::op_eor>(regs.y);
//   void SMPcore::op_read_addri(uint8 &r) {
{
  18, // dp.l = op_readpc();
  19, // dp.h = op_readpc();
  1, // op_io();
  49, // rd = op_read(dp + regs.y);
  79, // regs.a = op_eor(regs.a, rd);
  2, // //!!NEXT
},
//   case 0x57: return op_read_idpy<&SMPcore::op_eor>();
//   void SMPcore::op_read_idpy() {
{
  9, // dp = op_readpc();
  1, // op_io();
  23, // sp.l = op_readdp(dp++);
  24, // sp.h = op_readdp(dp++);
  50, // rd = op_read(sp + regs.y);
  79, // regs.a = op_eor(regs.a, rd);
  2, // //!!NEXT
},
//   case 0x58: return op_write_dp_const<&SMPcore::op_eor>();
//   void SMPcore::op_write_dp_const() {
{
  13, // rd = op_readpc();
  9, // dp = op_readpc();
  28, // wr = op_readdp(dp);
  80, // wr = op_eor(wr, rd);
  30, // op_writedp(dp, wr);
  2, // //!!NEXT
},
//   case 0x59: return op_write_ix_iy<&SMPcore::op_eor>();
//   void SMPcore::op_write_ix_iy() {
{
  1, // op_io();
  51, // rd = op_readdp(regs.y);
  52, // wr = op_readdp(regs.x);
  80, // wr = op_eor(wr, rd);
  53, // op_writedp(regs.x, wr);
  2, // //!!NEXT
},
//   case 0x5a: return op_read_dpw<&SMPcore::op_cpw>();
//   void SMPcore::op_read_dpw() {
{
  9, // dp = op_readpc();
  87, // rd.l = op_readdp(dp++);
  88, // rd.h = op_readdp(dp++);
  89, // regs.ya = op_cpw(regs.ya, rd);
  2, // //!!NEXT
},
//   case 0x5b: return op_adjust_dpx<&SMPcore::op_lsr>();
//   void SMPcore::op_adjust_dpx() {
{
  9, // dp = op_readpc();
  1, // op_io();
  47, // rd = op_readdp(dp + regs.x);
  82, // rd = op_lsr(rd);
  60, // op_writedp(dp + regs.x, rd);
  2, // //!!NEXT
},
//   case 0x5c: return op_adjust<&SMPcore::op_lsr>(regs.a);
//   void SMPcore::op_adjust(uint8 &r) {
{
  1, // op_io();
  90, // regs.a = op_lsr(regs.a);
  2, // //!!NEXT
},
//   case 0x5d: return op_transfer(regs.a, regs.x);
//   void SMPcore::op_transfer(uint8 &from, uint8 &to) {
{
  1, // op_io();
  91, // regs.x = regs.a;
  92, // regs.p.n = (regs.x & 0x80);
  93, // regs.p.z = (regs.x == 0);
  2, // //!!NEXT
},
//   case 0x5e: return op_read_addr<&SMPcore::op_cmp>(regs.y);
//   void SMPcore::op_read_addr(uint8 &r) {
{
  18, // dp.l = op_readpc();
  19, // dp.h = op_readpc();
  20, // rd = op_read(dp);
  94, // regs.y = op_cmp(regs.y, rd);
  2, // //!!NEXT
},
//   case 0x5f: return op_jmp_addr();
//   void SMPcore::op_jmp_addr() {
{
  76, // rd.l = op_readpc();
  77, // rd.h = op_readpc();
  8, // regs.pc = rd;
  2, // //!!NEXT
},
//   case 0x60: return op_set_flag(regs.p.c, 0);
//   void SMPcore::op_set_flag(bool &flag, bool data) {
{
  1, // op_io();
  95, // regs.p.c = 0;
  2, // //!!NEXT
},
//   case 0x61: return op_jst();
//   void SMPcore::op_jst() {
{
  3, // dp = 0xffde - ((opcode >> 4) << 1);
  4, // rd.l = op_read(dp++);
  5, // rd.h = op_read(dp++);
  1, // op_io();
  1, // op_io();
  1, // op_io();
  6, // op_writesp(regs.pc.h);
  7, // op_writesp(regs.pc.l);
  8, // regs.pc = rd;
  2, // //!!NEXT
},
//   case 0x62: return op_set_bit();
//   void SMPcore::op_set_bit() {
{
  9, // dp = op_readpc();
  10, // rd = op_readdp(dp) & ~(1 << (opcode >> 5));
  11, // op_writedp(dp, rd | (!(opcode & 0x10) << (opcode >> 5)));
  2, // //!!NEXT
},
//   case 0x63: return op_branch_bit();
//   void SMPcore::op_branch_bit() {
{
  9, // dp = op_readpc();
  12, // sp = op_readdp(dp);
  13, // rd = op_readpc();
  1, // op_io();
  14, // if((bool)(sp & (1 << (opcode >> 5))) == (bool)(opcode & 0x10)) return;
  1, // op_io();
  1, // op_io();
  15, // regs.pc += (int8)rd;
  2, // //!!NEXT
},
//   case 0x64: return op_read_dp<&SMPcore::op_cmp>(regs.a);
//   void SMPcore::op_read_dp(uint8 &r) {
{
  9, // dp = op_readpc();
  16, // rd = op_readdp(dp);
  96, // regs.a = op_cmp(regs.a, rd);
  2, // //!!NEXT
},
//   case 0x65: return op_read_addr<&SMPcore::op_cmp>(regs.a);
//   void SMPcore::op_read_addr(uint8 &r) {
{
  18, // dp.l = op_readpc();
  19, // dp.h = op_readpc();
  20, // rd = op_read(dp);
  96, // regs.a = op_cmp(regs.a, rd);
  2, // //!!NEXT
},
//   case 0x66: return op_read_ix<&SMPcore::op_cmp>();
//   void SMPcore::op_read_ix() {
{
  1, // op_io();
  21, // rd = op_readdp(regs.x);
  96, // regs.a = op_cmp(regs.a, rd);
  2, // //!!NEXT
},
//   case 0x67: return op_read_idpx<&SMPcore::op_cmp>();
//   void SMPcore::op_read_idpx() {
{
  22, // dp = op_readpc() + regs.x;
  1, // op_io();
  23, // sp.l = op_readdp(dp++);
  24, // sp.h = op_readdp(dp++);
  25, // rd = op_read(sp);
  96, // regs.a = op_cmp(regs.a, rd);
  2, // //!!NEXT
},
//   case 0x68: return op_read_const<&SMPcore::op_cmp>(regs.a);
//   void SMPcore::op_read_const(uint8 &r) {
{
  13, // rd = op_readpc();
  96, // regs.a = op_cmp(regs.a, rd);
  2, // //!!NEXT
},
//   case 0x69: return op_write_dp_dp<&SMPcore::op_cmp>();
//   void SMPcore::op_write_dp_dp() {
{
  26, // sp = op_readpc();
  27, // rd = op_readdp(sp);
  9, // dp = op_readpc();
  28, // wr = op_readdp(dp);
  97, // wr = op_cmp(wr, rd);
  1, // op_io();
  2, // //!!NEXT
},
//   case 0x6a: return op_set_addr_bit();
//   void SMPcore::op_set_addr_bit() {
{
  18, // dp.l = op_readpc();
  19, // dp.h = op_readpc();
  31, // bit = dp >> 13;
  32, // dp &= 0x1fff;
  20, // rd = op_read(dp);
  98, // regs.p.c &= (rd & (1 << bit)) ^ 1;
  2, // //!!NEXT
},
//   case 0x6b: return op_adjust_dp<&SMPcore::op_ror>();
//   void SMPcore::op_adjust_dp() {
{
  9, // dp = op_readpc();
  16, // rd = op_readdp(dp);
  99, // rd = op_ror(rd);
  35, // op_writedp(dp, rd);
  2, // //!!NEXT
},
//   case 0x6c: return op_adjust_addr<&SMPcore::op_ror>();
//   void SMPcore::op_adjust_addr() {
{
  18, // dp.l = op_readpc();
  19, // dp.h = op_readpc();
  20, // rd = op_read(dp);
  99, // rd = op_ror(rd);
  36, // op_write(dp, rd);
  2, // //!!NEXT
},
//   case 0x6d: return op_push(regs.y);
//   void SMPcore::op_push(uint8 r) {
{
  1, // op_io();
  1, // op_io();
  100, // op_writesp(regs.y);
  2, // //!!NEXT
},
//   case 0x6e: return op_bne_dpdec();
//   void SMPcore::op_bne_dpdec() {
{
  9, // dp = op_readpc();
  28, // wr = op_readdp(dp);
  101, // op_writedp(dp, --wr);
  13, // rd = op_readpc();
  102, // if(wr == 0) return;
  1, // op_io();
  1, // op_io();
  15, // regs.pc += (int8)rd;
  2, // //!!NEXT
},
//   case 0x6f: return op_rts();
//   void SMPcore::op_rts() {
{
  103, // rd.l = op_readsp();
  104, // rd.h = op_readsp();
  1, // op_io();
  1, // op_io();
  8, // regs.pc = rd;
  2, // //!!NEXT
},
//   case 0x70: return op_branch(regs.p.v == 1);
//   void SMPcore::op_branch(bool condition) {
{
  13, // rd = op_readpc();
  105, // if(regs.p.v != 1) return;
  1, // op_io();
  1, // op_io();
  15, // regs.pc += (int8)rd;
  2, // //!!NEXT
},
//   case 0x71: return op_jst();
//   void SMPcore::op_jst() {
{
  3, // dp = 0xffde - ((opcode >> 4) << 1);
  4, // rd.l = op_read(dp++);
  5, // rd.h = op_read(dp++);
  1, // op_io();
  1, // op_io();
  1, // op_io();
  6, // op_writesp(regs.pc.h);
  7, // op_writesp(regs.pc.l);
  8, // regs.pc = rd;
  2, // //!!NEXT
},
//   case 0x72: return op_set_bit();
//   void SMPcore::op_set_bit() {
{
  9, // dp = op_readpc();
  10, // rd = op_readdp(dp) & ~(1 << (opcode >> 5));
  11, // op_writedp(dp, rd | (!(opcode & 0x10) << (opcode >> 5)));
  2, // //!!NEXT
},
//   case 0x73: return op_branch_bit();
//   void SMPcore::op_branch_bit() {
{
  9, // dp = op_readpc();
  12, // sp = op_readdp(dp);
  13, // rd = op_readpc();
  1, // op_io();
  14, // if((bool)(sp & (1 << (opcode >> 5))) == (bool)(opcode & 0x10)) return;
  1, // op_io();
  1, // op_io();
  15, // regs.pc += (int8)rd;
  2, // //!!NEXT
},
//   case 0x74: return op_read_dpi<&SMPcore::op_cmp>(regs.a, regs.x);
//   void SMPcore::op_read_dpi(uint8 &r, uint8 &i) {
{
  9, // dp = op_readpc();
  1, // op_io();
  47, // rd = op_readdp(dp + regs.x);
  96, // regs.a = op_cmp(regs.a, rd);
  2, // //!!NEXT
},
//   case 0x75: return op_read_addri<&SMPcore::op_cmp>(regs.x);
//   void SMPcore::op_read_addri(uint8 &r) {
{
  18, // dp.l = op_readpc();
  19, // dp.h = op_readpc();
  1, // op_io();
  48, // rd = op_read(dp + regs.x);
  96, // regs.a = op_cmp(regs.a, rd);
  2, // //!!NEXT
},
//   case 0x76: return op_read_addri<&SMPcore::op_cmp>(regs.y);
//   void SMPcore::op_read_addri(uint8 &r) {
{
  18, // dp.l = op_readpc();
  19, // dp.h = op_readpc();
  1, // op_io();
  49, // rd = op_read(dp + regs.y);
  96, // regs.a = op_cmp(regs.a, rd);
  2, // //!!NEXT
},
//   case 0x77: return op_read_idpy<&SMPcore::op_cmp>();
//   void SMPcore::op_read_idpy() {
{
  9, // dp = op_readpc();
  1, // op_io();
  23, // sp.l = op_readdp(dp++);
  24, // sp.h = op_readdp(dp++);
  50, // rd = op_read(sp + regs.y);
  96, // regs.a = op_cmp(regs.a, rd);
  2, // //!!NEXT
},
//   case 0x78: return op_write_dp_const<&SMPcore::op_cmp>();
//   void SMPcore::op_write_dp_const() {
{
  13, // rd = op_readpc();
  9, // dp = op_readpc();
  28, // wr = op_readdp(dp);
  97, // wr = op_cmp(wr, rd);
  1, // op_io();
  2, // //!!NEXT
},
//   case 0x79: return op_write_ix_iy<&SMPcore::op_cmp>();
//   void SMPcore::op_write_ix_iy() {
{
  1, // op_io();
  51, // rd = op_readdp(regs.y);
  52, // wr = op_readdp(regs.x);
  97, // wr = op_cmp(wr, rd);
  1, // op_io();
  2, // //!!NEXT
},
//   case 0x7a: return op_read_dpw<&SMPcore::op_adw>();
//   void SMPcore::op_read_dpw() {
{
  9, // dp = op_readpc();
  87, // rd.l = op_readdp(dp++);
  1, // op_io();
  88, // rd.h = op_readdp(dp++);
  106, // regs.ya = op_adw(regs.ya, rd);
  2, // //!!NEXT
},
//   case 0x7b: return op_adjust_dpx<&SMPcore::op_ror>();
//   void SMPcore::op_adjust_dpx() {
{
  9, // dp = op_readpc();
  1, // op_io();
  47, // rd = op_readdp(dp + regs.x);
  99, // rd = op_ror(rd);
  60, // op_writedp(dp + regs.x, rd);
  2, // //!!NEXT
},
//   case 0x7c: return op_adjust<&SMPcore::op_ror>(regs.a);
//   void SMPcore::op_adjust(uint8 &r) {
{
  1, // op_io();
  107, // regs.a = op_ror(regs.a);
  2, // //!!NEXT
},
//   case 0x7d: return op_transfer(regs.x, regs.a);
//   void SMPcore::op_transfer(uint8 &from, uint8 &to) {
{
  1, // op_io();
  108, // regs.a = regs.x;
  109, // regs.p.n = (regs.a & 0x80);
  110, // regs.p.z = (regs.a == 0);
  2, // //!!NEXT
},
//   case 0x7e: return op_read_dp<&SMPcore::op_cmp>(regs.y);
//   void SMPcore::op_read_dp(uint8 &r) {
{
  9, // dp = op_readpc();
  16, // rd = op_readdp(dp);
  94, // regs.y = op_cmp(regs.y, rd);
  2, // //!!NEXT
},
//   case 0x7f: return op_rti();
//   void SMPcore::op_rti() {
{
  111, // regs.p = op_readsp();
  103, // rd.l = op_readsp();
  104, // rd.h = op_readsp();
  1, // op_io();
  1, // op_io();
  8, // regs.pc = rd;
  2, // //!!NEXT
},
//   case 0x80: return op_set_flag(regs.p.c, 1);
//   void SMPcore::op_set_flag(bool &flag, bool data) {
{
  1, // op_io();
  112, // regs.p.c = 1;
  2, // //!!NEXT
},
//   case 0x81: return op_jst();
//   void SMPcore::op_jst() {
{
  3, // dp = 0xffde - ((opcode >> 4) << 1);
  4, // rd.l = op_read(dp++);
  5, // rd.h = op_read(dp++);
  1, // op_io();
  1, // op_io();
  1, // op_io();
  6, // op_writesp(regs.pc.h);
  7, // op_writesp(regs.pc.l);
  8, // regs.pc = rd;
  2, // //!!NEXT
},
//   case 0x82: return op_set_bit();
//   void SMPcore::op_set_bit() {
{
  9, // dp = op_readpc();
  10, // rd = op_readdp(dp) & ~(1 << (opcode >> 5));
  11, // op_writedp(dp, rd | (!(opcode & 0x10) << (opcode >> 5)));
  2, // //!!NEXT
},
//   case 0x83: return op_branch_bit();
//   void SMPcore::op_branch_bit() {
{
  9, // dp = op_readpc();
  12, // sp = op_readdp(dp);
  13, // rd = op_readpc();
  1, // op_io();
  14, // if((bool)(sp & (1 << (opcode >> 5))) == (bool)(opcode & 0x10)) return;
  1, // op_io();
  1, // op_io();
  15, // regs.pc += (int8)rd;
  2, // //!!NEXT
},
//   case 0x84: return op_read_dp<&SMPcore::op_adc>(regs.a);
//   void SMPcore::op_read_dp(uint8 &r) {
{
  9, // dp = op_readpc();
  16, // rd = op_readdp(dp);
  113, // regs.a = op_adc(regs.a, rd);
  2, // //!!NEXT
},
//   case 0x85: return op_read_addr<&SMPcore::op_adc>(regs.a);
//   void SMPcore::op_read_addr(uint8 &r) {
{
  18, // dp.l = op_readpc();
  19, // dp.h = op_readpc();
  20, // rd = op_read(dp);
  113, // regs.a = op_adc(regs.a, rd);
  2, // //!!NEXT
},
//   case 0x86: return op_read_ix<&SMPcore::op_adc>();
//   void SMPcore::op_read_ix() {
{
  1, // op_io();
  21, // rd = op_readdp(regs.x);
  113, // regs.a = op_adc(regs.a, rd);
  2, // //!!NEXT
},
//   case 0x87: return op_read_idpx<&SMPcore::op_adc>();
//   void SMPcore::op_read_idpx() {
{
  22, // dp = op_readpc() + regs.x;
  1, // op_io();
  23, // sp.l = op_readdp(dp++);
  24, // sp.h = op_readdp(dp++);
  25, // rd = op_read(sp);
  113, // regs.a = op_adc(regs.a, rd);
  2, // //!!NEXT
},
//   case 0x88: return op_read_const<&SMPcore::op_adc>(regs.a);
//   void SMPcore::op_read_const(uint8 &r) {
{
  13, // rd = op_readpc();
  113, // regs.a = op_adc(regs.a, rd);
  2, // //!!NEXT
},
//   case 0x89: return op_write_dp_dp<&SMPcore::op_adc>();
//   void SMPcore::op_write_dp_dp() {
{
  26, // sp = op_readpc();
  27, // rd = op_readdp(sp);
  9, // dp = op_readpc();
  28, // wr = op_readdp(dp);
  114, // wr = op_adc(wr, rd);
  30, // op_writedp(dp, wr);
  2, // //!!NEXT
},
//   case 0x8a: return op_set_addr_bit();
//   void SMPcore::op_set_addr_bit() {
{
  18, // dp.l = op_readpc();
  19, // dp.h = op_readpc();
  31, // bit = dp >> 13;
  32, // dp &= 0x1fff;
  20, // rd = op_read(dp);
  1, // op_io();
  115, // regs.p.c ^= (bool)(rd & (1 << bit));
  2, // //!!NEXT
},
//   case 0x8b: return op_adjust_dp<&SMPcore::op_dec>();
//   void SMPcore::op_adjust_dp() {
{
  9, // dp = op_readpc();
  16, // rd = op_readdp(dp);
  116, // rd = op_dec(rd);
  35, // op_writedp(dp, rd);
  2, // //!!NEXT
},
//   case 0x8c: return op_adjust_addr<&SMPcore::op_dec>();
//   void SMPcore::op_adjust_addr() {
{
  18, // dp.l = op_readpc();
  19, // dp.h = op_readpc();
  20, // rd = op_read(dp);
  116, // rd = op_dec(rd);
  36, // op_write(dp, rd);
  2, // //!!NEXT
},
//   case 0x8d: return op_read_const<&SMPcore::op_ld>(regs.y);
//   void SMPcore::op_read_const(uint8 &r) {
{
  13, // rd = op_readpc();
  117, // regs.y = op_ld(regs.y, rd);
  2, // //!!NEXT
},
//   case 0x8e: return op_plp();
//   void SMPcore::op_plp() {
{
  1, // op_io();
  1, // op_io();
  111, // regs.p = op_readsp();
  2, // //!!NEXT
},
//   case 0x8f: return op_write_dp_const<&SMPcore::op_st>();
//   void SMPcore::op_write_dp_const() {
{
  13, // rd = op_readpc();
  9, // dp = op_readpc();
  28, // wr = op_readdp(dp);
  118, // wr = op_st(wr, rd);
  30, // op_writedp(dp, wr);
  2, // //!!NEXT
},
//   case 0x90: return op_branch(regs.p.c == 0);
//   void SMPcore::op_branch(bool condition) {
{
  13, // rd = op_readpc();
  119, // if(regs.p.c != 0) return;
  1, // op_io();
  1, // op_io();
  15, // regs.pc += (int8)rd;
  2, // //!!NEXT
},
//   case 0x91: return op_jst();
//   void SMPcore::op_jst() {
{
  3, // dp = 0xffde - ((opcode >> 4) << 1);
  4, // rd.l = op_read(dp++);
  5, // rd.h = op_read(dp++);
  1, // op_io();
  1, // op_io();
  1, // op_io();
  6, // op_writesp(regs.pc.h);
  7, // op_writesp(regs.pc.l);
  8, // regs.pc = rd;
  2, // //!!NEXT
},
//   case 0x92: return op_set_bit();
//   void SMPcore::op_set_bit() {
{
  9, // dp = op_readpc();
  10, // rd = op_readdp(dp) & ~(1 << (opcode >> 5));
  11, // op_writedp(dp, rd | (!(opcode & 0x10) << (opcode >> 5)));
  2, // //!!NEXT
},
//   case 0x93: return op_branch_bit();
//   void SMPcore::op_branch_bit() {
{
  9, // dp = op_readpc();
  12, // sp = op_readdp(dp);
  13, // rd = op_readpc();
  1, // op_io();
  14, // if((bool)(sp & (1 << (opcode >> 5))) == (bool)(opcode & 0x10)) return;
  1, // op_io();
  1, // op_io();
  15, // regs.pc += (int8)rd;
  2, // //!!NEXT
},
//   case 0x94: return op_read_dpi<&SMPcore::op_adc>(regs.a, regs.x);
//   void SMPcore::op_read_dpi(uint8 &r, uint8 &i) {
{
  9, // dp = op_readpc();
  1, // op_io();
  47, // rd = op_readdp(dp + regs.x);
  113, // regs.a = op_adc(regs.a, rd);
  2, // //!!NEXT
},
//   case 0x95: return op_read_addri<&SMPcore::op_adc>(regs.x);
//   void SMPcore::op_read_addri(uint8 &r) {
{
  18, // dp.l = op_readpc();
  19, // dp.h = op_readpc();
  1, // op_io();
  48, // rd = op_read(dp + regs.x);
  113, // regs.a = op_adc(regs.a, rd);
  2, // //!!NEXT
},
//   case 0x96: return op_read_addri<&SMPcore::op_adc>(regs.y);
//   void SMPcore::op_read_addri(uint8 &r) {
{
  18, // dp.l = op_readpc();
  19, // dp.h = op_readpc();
  1, // op_io();
  49, // rd = op_read(dp + regs.y);
  113, // regs.a = op_adc(regs.a, rd);
  2, // //!!NEXT
},
//   case 0x97: return op_read_idpy<&SMPcore::op_adc>();
//   void SMPcore::op_read_idpy() {
{
  9, // dp = op_readpc();
  1, // op_io();
  23, // sp.l = op_readdp(dp++);
  24, // sp.h = op_readdp(dp++);
  50, // rd = op_read(sp + regs.y);
  113, // regs.a = op_adc(regs.a, rd);
  2, // //!!NEXT
},
//   case 0x98: return op_write_dp_const<&SMPcore::op_adc>();
//   void SMPcore::op_write_dp_const() {
{
  13, // rd = op_readpc();
  9, // dp = op_readpc();
  28, // wr = op_readdp(dp);
  114, // wr = op_adc(wr, rd);
  30, // op_writedp(dp, wr);
  2, // //!!NEXT
},
//   case 0x99: return op_write_ix_iy<&SMPcore::op_adc>();
//   void SMPcore::op_write_ix_iy() {
{
  1, // op_io();
  51, // rd = op_readdp(regs.y);
  52, // wr = op_readdp(regs.x);
  114, // wr = op_adc(wr, rd);
  53, // op_writedp(regs.x, wr);
  2, // //!!NEXT
},
//   case 0x9a: return op_read_dpw<&SMPcore::op_sbw>();
//   void SMPcore::op_read_dpw() {
{
  9, // dp = op_readpc();
  87, // rd.l = op_readdp(dp++);
  1, // op_io();
  88, // rd.h = op_readdp(dp++);
  120, // regs.ya = op_sbw(regs.ya, rd);
  2, // //!!NEXT
},
//   case 0x9b: return op_adjust_dpx<&SMPcore::op_dec>();
//   void SMPcore::op_adjust_dpx() {
{
  9, // dp = op_readpc();
  1, // op_io();
  47, // rd = op_readdp(dp + regs.x);
  116, // rd = op_dec(rd);
  60, // op_writedp(dp + regs.x, rd);
  2, // //!!NEXT
},
//   case 0x9c: return op_adjust<&SMPcore::op_dec>(regs.a);
//   void SMPcore::op_adjust(uint8 &r) {
{
  1, // op_io();
  121, // regs.a = op_dec(regs.a);
  2, // //!!NEXT
},
//   case 0x9d: return op_transfer(regs.s, regs.x);
//   void SMPcore::op_transfer(uint8 &from, uint8 &to) {
{
  1, // op_io();
  122, // regs.x = regs.s;
  92, // regs.p.n = (regs.x & 0x80);
  93, // regs.p.z = (regs.x == 0);
  2, // //!!NEXT
},
//   case 0x9e: return op_div_ya_x();
//   void SMPcore::op_div_ya_x() {
{
  1, // op_io();
  1, // op_io();
  1, // op_io();
  1, // op_io();
  1, // op_io();
  1, // op_io();
  1, // op_io();
  1, // op_io();
  1, // op_io();
  1, // op_io();
  1, // op_io();
  //ya = regs.ya;
  ////overflow set if quotient >= 256
  //regs.p.v = (regs.y >= regs.x);
  //regs.p.h = ((regs.y & 15) >= (regs.x & 15));
  //if(regs.y < (regs.x << 1)) {
  ////if quotient is <= 511 (will fit into 9-bit result)
  //regs.a = ya / regs.x;
  //regs.y = ya % regs.x;
  //} else {
  ////otherwise, the quotient won't fit into regs.p.v + regs.a
  ////this emulates the odd behavior of the S-SMP in this case
  //regs.a = 255    - (ya - (regs.x << 9)) / (256 - regs.x);
  //regs.y = regs.x + (ya - (regs.x << 9)) % (256 - regs.x);
  //}
  ////result is set based on a (quotient) only
  //regs.p.n = (regs.a & 0x80);
  //regs.p.z = (regs.a == 0);
  123, // //!!MULTI0
  2, // //!!NEXT
},
//   case 0x9f: return op_xcn();
//   void SMPcore::op_xcn() {
{
  1, // op_io();
  1, // op_io();
  1, // op_io();
  1, // op_io();
  124, // regs.a = (regs.a >> 4) | (regs.a << 4);
  125, // regs.p.n = regs.a & 0x80;
  126, // regs.p.z = regs.a == 0;
  2, // //!!NEXT
},
//   case 0xa0: return op_set_flag(regs.p.i, 1);
//   void SMPcore::op_set_flag(bool &flag, bool data) {
{
  1, // op_io();
  1, // op_io();
  127, // regs.p.i = 1;
  2, // //!!NEXT
},
//   case 0xa1: return op_jst();
//   void SMPcore::op_jst() {
{
  3, // dp = 0xffde - ((opcode >> 4) << 1);
  4, // rd.l = op_read(dp++);
  5, // rd.h = op_read(dp++);
  1, // op_io();
  1, // op_io();
  1, // op_io();
  6, // op_writesp(regs.pc.h);
  7, // op_writesp(regs.pc.l);
  8, // regs.pc = rd;
  2, // //!!NEXT
},
//   case 0xa2: return op_set_bit();
//   void SMPcore::op_set_bit() {
{
  9, // dp = op_readpc();
  10, // rd = op_readdp(dp) & ~(1 << (opcode >> 5));
  11, // op_writedp(dp, rd | (!(opcode & 0x10) << (opcode >> 5)));
  2, // //!!NEXT
},
//   case 0xa3: return op_branch_bit();
//   void SMPcore::op_branch_bit() {
{
  9, // dp = op_readpc();
  12, // sp = op_readdp(dp);
  13, // rd = op_readpc();
  1, // op_io();
  14, // if((bool)(sp & (1 << (opcode >> 5))) == (bool)(opcode & 0x10)) return;
  1, // op_io();
  1, // op_io();
  15, // regs.pc += (int8)rd;
  2, // //!!NEXT
},
//   case 0xa4: return op_read_dp<&SMPcore::op_sbc>(regs.a);
//   void SMPcore::op_read_dp(uint8 &r) {
{
  9, // dp = op_readpc();
  16, // rd = op_readdp(dp);
  128, // regs.a = op_sbc(regs.a, rd);
  2, // //!!NEXT
},
//   case 0xa5: return op_read_addr<&SMPcore::op_sbc>(regs.a);
//   void SMPcore::op_read_addr(uint8 &r) {
{
  18, // dp.l = op_readpc();
  19, // dp.h = op_readpc();
  20, // rd = op_read(dp);
  128, // regs.a = op_sbc(regs.a, rd);
  2, // //!!NEXT
},
//   case 0xa6: return op_read_ix<&SMPcore::op_sbc>();
//   void SMPcore::op_read_ix() {
{
  1, // op_io();
  21, // rd = op_readdp(regs.x);
  128, // regs.a = op_sbc(regs.a, rd);
  2, // //!!NEXT
},
//   case 0xa7: return op_read_idpx<&SMPcore::op_sbc>();
//   void SMPcore::op_read_idpx() {
{
  22, // dp = op_readpc() + regs.x;
  1, // op_io();
  23, // sp.l = op_readdp(dp++);
  24, // sp.h = op_readdp(dp++);
  25, // rd = op_read(sp);
  128, // regs.a = op_sbc(regs.a, rd);
  2, // //!!NEXT
},
//   case 0xa8: return op_read_const<&SMPcore::op_sbc>(regs.a);
//   void SMPcore::op_read_const(uint8 &r) {
{
  13, // rd = op_readpc();
  128, // regs.a = op_sbc(regs.a, rd);
  2, // //!!NEXT
},
//   case 0xa9: return op_write_dp_dp<&SMPcore::op_sbc>();
//   void SMPcore::op_write_dp_dp() {
{
  26, // sp = op_readpc();
  27, // rd = op_readdp(sp);
  9, // dp = op_readpc();
  28, // wr = op_readdp(dp);
  129, // wr = op_sbc(wr, rd);
  30, // op_writedp(dp, wr);
  2, // //!!NEXT
},
//   case 0xaa: return op_set_addr_bit();
//   void SMPcore::op_set_addr_bit() {
{
  18, // dp.l = op_readpc();
  19, // dp.h = op_readpc();
  31, // bit = dp >> 13;
  32, // dp &= 0x1fff;
  20, // rd = op_read(dp);
  130, // regs.p.c  = (rd & (1 << bit));
  2, // //!!NEXT
},
//   case 0xab: return op_adjust_dp<&SMPcore::op_inc>();
//   void SMPcore::op_adjust_dp() {
{
  9, // dp = op_readpc();
  16, // rd = op_readdp(dp);
  131, // rd = op_inc(rd);
  35, // op_writedp(dp, rd);
  2, // //!!NEXT
},
//   case 0xac: return op_adjust_addr<&SMPcore::op_inc>();
//   void SMPcore::op_adjust_addr() {
{
  18, // dp.l = op_readpc();
  19, // dp.h = op_readpc();
  20, // rd = op_read(dp);
  131, // rd = op_inc(rd);
  36, // op_write(dp, rd);
  2, // //!!NEXT
},
//   case 0xad: return op_read_const<&SMPcore::op_cmp>(regs.y);
//   void SMPcore::op_read_const(uint8 &r) {
{
  13, // rd = op_readpc();
  94, // regs.y = op_cmp(regs.y, rd);
  2, // //!!NEXT
},
//   case 0xae: return op_pull(regs.a);
//   void SMPcore::op_pull(uint8 &r) {
{
  1, // op_io();
  1, // op_io();
  132, // regs.a = op_readsp();
  2, // //!!NEXT
},
//   case 0xaf: return op_sta_ixinc();
//   void SMPcore::op_sta_ixinc() {
{
  1, // op_io();
  1, // op_io();
  133, // op_writedp(regs.x++, regs.a);
  2, // //!!NEXT
},
//   case 0xb0: return op_branch(regs.p.c == 1);
//   void SMPcore::op_branch(bool condition) {
{
  13, // rd = op_readpc();
  134, // if(regs.p.c != 1) return;
  1, // op_io();
  1, // op_io();
  15, // regs.pc += (int8)rd;
  2, // //!!NEXT
},
//   case 0xb1: return op_jst();
//   void SMPcore::op_jst() {
{
  3, // dp = 0xffde - ((opcode >> 4) << 1);
  4, // rd.l = op_read(dp++);
  5, // rd.h = op_read(dp++);
  1, // op_io();
  1, // op_io();
  1, // op_io();
  6, // op_writesp(regs.pc.h);
  7, // op_writesp(regs.pc.l);
  8, // regs.pc = rd;
  2, // //!!NEXT
},
//   case 0xb2: return op_set_bit();
//   void SMPcore::op_set_bit() {
{
  9, // dp = op_readpc();
  10, // rd = op_readdp(dp) & ~(1 << (opcode >> 5));
  11, // op_writedp(dp, rd | (!(opcode & 0x10) << (opcode >> 5)));
  2, // //!!NEXT
},
//   case 0xb3: return op_branch_bit();
//   void SMPcore::op_branch_bit() {
{
  9, // dp = op_readpc();
  12, // sp = op_readdp(dp);
  13, // rd = op_readpc();
  1, // op_io();
  14, // if((bool)(sp & (1 << (opcode >> 5))) == (bool)(opcode & 0x10)) return;
  1, // op_io();
  1, // op_io();
  15, // regs.pc += (int8)rd;
  2, // //!!NEXT
},
//   case 0xb4: return op_read_dpi<&SMPcore::op_sbc>(regs.a, regs.x);
//   void SMPcore::op_read_dpi(uint8 &r, uint8 &i) {
{
  9, // dp = op_readpc();
  1, // op_io();
  47, // rd = op_readdp(dp + regs.x);
  128, // regs.a = op_sbc(regs.a, rd);
  2, // //!!NEXT
},
//   case 0xb5: return op_read_addri<&SMPcore::op_sbc>(regs.x);
//   void SMPcore::op_read_addri(uint8 &r) {
{
  18, // dp.l = op_readpc();
  19, // dp.h = op_readpc();
  1, // op_io();
  48, // rd = op_read(dp + regs.x);
  128, // regs.a = op_sbc(regs.a, rd);
  2, // //!!NEXT
},
//   case 0xb6: return op_read_addri<&SMPcore::op_sbc>(regs.y);
//   void SMPcore::op_read_addri(uint8 &r) {
{
  18, // dp.l = op_readpc();
  19, // dp.h = op_readpc();
  1, // op_io();
  49, // rd = op_read(dp + regs.y);
  128, // regs.a = op_sbc(regs.a, rd);
  2, // //!!NEXT
},
//   case 0xb7: return op_read_idpy<&SMPcore::op_sbc>();
//   void SMPcore::op_read_idpy() {
{
  9, // dp = op_readpc();
  1, // op_io();
  23, // sp.l = op_readdp(dp++);
  24, // sp.h = op_readdp(dp++);
  50, // rd = op_read(sp + regs.y);
  128, // regs.a = op_sbc(regs.a, rd);
  2, // //!!NEXT
},
//   case 0xb8: return op_write_dp_const<&SMPcore::op_sbc>();
//   void SMPcore::op_write_dp_const() {
{
  13, // rd = op_readpc();
  9, // dp = op_readpc();
  28, // wr = op_readdp(dp);
  129, // wr = op_sbc(wr, rd);
  30, // op_writedp(dp, wr);
  2, // //!!NEXT
},
//   case 0xb9: return op_write_ix_iy<&SMPcore::op_sbc>();
//   void SMPcore::op_write_ix_iy() {
{
  1, // op_io();
  51, // rd = op_readdp(regs.y);
  52, // wr = op_readdp(regs.x);
  129, // wr = op_sbc(wr, rd);
  53, // op_writedp(regs.x, wr);
  2, // //!!NEXT
},
//   case 0xba: return op_read_dpw<&SMPcore::op_ldw>();
//   void SMPcore::op_read_dpw() {
{
  9, // dp = op_readpc();
  87, // rd.l = op_readdp(dp++);
  1, // op_io();
  88, // rd.h = op_readdp(dp++);
  135, // regs.ya = op_ldw(regs.ya, rd);
  2, // //!!NEXT
},
//   case 0xbb: return op_adjust_dpx<&SMPcore::op_inc>();
//   void SMPcore::op_adjust_dpx() {
{
  9, // dp = op_readpc();
  1, // op_io();
  47, // rd = op_readdp(dp + regs.x);
  131, // rd = op_inc(rd);
  60, // op_writedp(dp + regs.x, rd);
  2, // //!!NEXT
},
//   case 0xbc: return op_adjust<&SMPcore::op_inc>(regs.a);
//   void SMPcore::op_adjust(uint8 &r) {
{
  1, // op_io();
  136, // r = op_inc(r);
  2, // //!!NEXT
},
//   case 0xbd: return op_transfer(regs.x, regs.s);
//   void SMPcore::op_transfer(uint8 &from, uint8 &to) {
{
  1, // op_io();
  137, // regs.s = regs.x;
  2, // //!!NEXT
},
//   case 0xbe: return op_das();
//   void SMPcore::op_das() {
{
  1, // op_io();
  1, // op_io();
  //if(!regs.p.c || (regs.a) > 0x99) {
  //regs.a -= 0x60;
  //regs.p.c = 0;
  //}
  //if(!regs.p.h || (regs.a & 15) > 0x09) {
  //regs.a -= 0x06;
  //}
  //regs.p.n = (regs.a & 0x80);
  //regs.p.z = (regs.a == 0);
  138, // //!!MULTI1
  2, // //!!NEXT
},
//   case 0xbf: return op_lda_ixinc();
//   void SMPcore::op_lda_ixinc() {
{
  1, // op_io();
  139, // regs.a = op_readdp(regs.x++);
  1, // op_io();
  125, // regs.p.n = regs.a & 0x80;
  126, // regs.p.z = regs.a == 0;
  2, // //!!NEXT
},
//   case 0xc0: return op_set_flag(regs.p.i, 0);
//   void SMPcore::op_set_flag(bool &flag, bool data) {
{
  1, // op_io();
  1, // op_io();
  45, // regs.p.i = 0;
  2, // //!!NEXT
},
//   case 0xc1: return op_jst();
//   void SMPcore::op_jst() {
{
  3, // dp = 0xffde - ((opcode >> 4) << 1);
  4, // rd.l = op_read(dp++);
  5, // rd.h = op_read(dp++);
  1, // op_io();
  1, // op_io();
  1, // op_io();
  6, // op_writesp(regs.pc.h);
  7, // op_writesp(regs.pc.l);
  8, // regs.pc = rd;
  2, // //!!NEXT
},
//   case 0xc2: return op_set_bit();
//   void SMPcore::op_set_bit() {
{
  9, // dp = op_readpc();
  10, // rd = op_readdp(dp) & ~(1 << (opcode >> 5));
  11, // op_writedp(dp, rd | (!(opcode & 0x10) << (opcode >> 5)));
  2, // //!!NEXT
},
//   case 0xc3: return op_branch_bit();
//   void SMPcore::op_branch_bit() {
{
  9, // dp = op_readpc();
  12, // sp = op_readdp(dp);
  13, // rd = op_readpc();
  1, // op_io();
  14, // if((bool)(sp & (1 << (opcode >> 5))) == (bool)(opcode & 0x10)) return;
  1, // op_io();
  1, // op_io();
  15, // regs.pc += (int8)rd;
  2, // //!!NEXT
},
//   case 0xc4: return op_write_dp(regs.a);
//   void SMPcore::op_write_dp(uint8 &r) {
{
  9, // dp = op_readpc();
  140, // op_readdp(dp);
  141, // op_writedp(dp, regs.a);
  2, // //!!NEXT
},
//   case 0xc5: return op_write_addr(regs.a);
//   void SMPcore::op_write_addr(uint8 &r) {
{
  18, // dp.l = op_readpc();
  19, // dp.h = op_readpc();
  40, // op_read(dp);
  142, // op_write(dp, regs.a);
  2, // //!!NEXT
},
//   case 0xc6: return op_sta_ix();
//   void SMPcore::op_sta_ix() {
{
  1, // op_io();
  143, // op_readdp(regs.x);
  144, // op_writedp(regs.x, regs.a);
  2, // //!!NEXT
},
//   case 0xc7: return op_sta_idpx();
//   void SMPcore::op_sta_idpx() {
{
  145, // sp = op_readpc() + regs.x;
  1, // op_io();
  146, // dp.l = op_readdp(sp++);
  147, // dp.h = op_readdp(sp++);
  40, // op_read(dp);
  142, // op_write(dp, regs.a);
  2, // //!!NEXT
},
//   case 0xc8: return op_read_const<&SMPcore::op_cmp>(regs.x);
//   void SMPcore::op_read_const(uint8 &r) {
{
  13, // rd = op_readpc();
  63, // regs.x = op_cmp(regs.x, rd);
  2, // //!!NEXT
},
//   case 0xc9: return op_write_addr(regs.x);
//   void SMPcore::op_write_addr(uint8 &r) {
{
  18, // dp.l = op_readpc();
  19, // dp.h = op_readpc();
  40, // op_read(dp);
  148, // op_write(dp, regs.x);
  2, // //!!NEXT
},
//   case 0xca: return op_set_addr_bit();
//   void SMPcore::op_set_addr_bit() {
{
  18, // dp.l = op_readpc();
  19, // dp.h = op_readpc();
  31, // bit = dp >> 13;
  32, // dp &= 0x1fff;
  20, // rd = op_read(dp);
  1, // op_io();
  149, // rd = (rd & ~(1 << bit)) | (regs.p.c << bit);
  36, // op_write(dp, rd);
  2, // //!!NEXT
},
//   case 0xcb: return op_write_dp(regs.y);
//   void SMPcore::op_write_dp(uint8 &r) {
{
  9, // dp = op_readpc();
  140, // op_readdp(dp);
  150, // op_writedp(dp, regs.y);
  2, // //!!NEXT
},
//   case 0xcc: return op_write_addr(regs.y);
//   void SMPcore::op_write_addr(uint8 &r) {
{
  18, // dp.l = op_readpc();
  19, // dp.h = op_readpc();
  40, // op_read(dp);
  151, // op_write(dp, regs.y);
  2, // //!!NEXT
},
//   case 0xcd: return op_read_const<&SMPcore::op_ld>(regs.x);
//   void SMPcore::op_read_const(uint8 &r) {
{
  13, // rd = op_readpc();
  152, // regs.x = op_ld(regs.x, rd);
  2, // //!!NEXT
},
//   case 0xce: return op_pull(regs.x);
//   void SMPcore::op_pull(uint8 &r) {
{
  1, // op_io();
  1, // op_io();
  153, // regs.x = op_readsp();
  2, // //!!NEXT
},
//   case 0xcf: return op_mul_ya();
//   void SMPcore::op_mul_ya() {
{
  1, // op_io();
  1, // op_io();
  1, // op_io();
  1, // op_io();
  1, // op_io();
  1, // op_io();
  1, // op_io();
  1, // op_io();
  //ya = regs.y * regs.a;
  //regs.a = ya;
  //regs.y = ya >> 8;
  ////result is set based on y (high-byte) only
  //regs.p.n = (regs.y & 0x80);
  //regs.p.z = (regs.y == 0);
  154, // //!!MULTI2
  2, // //!!NEXT
},
//   case 0xd0: return op_branch(regs.p.z == 0);
//   void SMPcore::op_branch(bool condition) {
{
  13, // rd = op_readpc();
  155, // if(regs.p.z != 0) return;
  1, // op_io();
  1, // op_io();
  15, // regs.pc += (int8)rd;
  2, // //!!NEXT
},
//   case 0xd1: return op_jst();
//   void SMPcore::op_jst() {
{
  3, // dp = 0xffde - ((opcode >> 4) << 1);
  4, // rd.l = op_read(dp++);
  5, // rd.h = op_read(dp++);
  1, // op_io();
  1, // op_io();
  1, // op_io();
  6, // op_writesp(regs.pc.h);
  7, // op_writesp(regs.pc.l);
  8, // regs.pc = rd;
  2, // //!!NEXT
},
//   case 0xd2: return op_set_bit();
//   void SMPcore::op_set_bit() {
{
  9, // dp = op_readpc();
  10, // rd = op_readdp(dp) & ~(1 << (opcode >> 5));
  11, // op_writedp(dp, rd | (!(opcode & 0x10) << (opcode >> 5)));
  2, // //!!NEXT
},
//   case 0xd3: return op_branch_bit();
//   void SMPcore::op_branch_bit() {
{
  9, // dp = op_readpc();
  12, // sp = op_readdp(dp);
  13, // rd = op_readpc();
  1, // op_io();
  14, // if((bool)(sp & (1 << (opcode >> 5))) == (bool)(opcode & 0x10)) return;
  1, // op_io();
  1, // op_io();
  15, // regs.pc += (int8)rd;
  2, // //!!NEXT
},
//   case 0xd4: return op_write_dpi(regs.a, regs.x);
//   void SMPcore::op_write_dpi(uint8 &r, uint8 &i) {
{
  22, // dp = op_readpc() + regs.x;
  1, // op_io();
  140, // op_readdp(dp);
  141, // op_writedp(dp, regs.a);
  2, // //!!NEXT
},
//   case 0xd5: return op_write_addri(regs.x);
//   void SMPcore::op_write_addri(uint8 &i) {
{
  18, // dp.l = op_readpc();
  19, // dp.h = op_readpc();
  1, // op_io();
  64, // dp += regs.x;
  40, // op_read(dp);
  142, // op_write(dp, regs.a);
  2, // //!!NEXT
},
//   case 0xd6: return op_write_addri(regs.y);
//   void SMPcore::op_write_addri(uint8 &i) {
{
  18, // dp.l = op_readpc();
  19, // dp.h = op_readpc();
  1, // op_io();
  156, // dp += regs.y;
  40, // op_read(dp);
  142, // op_write(dp, regs.a);
  2, // //!!NEXT
},
//   case 0xd7: return op_sta_idpy();
//   void SMPcore::op_sta_idpy() {
{
  26, // sp = op_readpc();
  146, // dp.l = op_readdp(sp++);
  147, // dp.h = op_readdp(sp++);
  1, // op_io();
  156, // dp += regs.y;
  40, // op_read(dp);
  142, // op_write(dp, regs.a);
  2, // //!!NEXT
},
//   case 0xd8: return op_write_dp(regs.x);
//   void SMPcore::op_write_dp(uint8 &r) {
{
  9, // dp = op_readpc();
  140, // op_readdp(dp);
  157, // op_writedp(dp, regs.x);
  2, // //!!NEXT
},
//   case 0xd9: return op_write_dpi(regs.x, regs.y);
//   void SMPcore::op_write_dpi(uint8 &r, uint8 &i) {
{
  158, // dp = op_readpc() + regs.y;
  1, // op_io();
  140, // op_readdp(dp);
  157, // op_writedp(dp, regs.x);
  2, // //!!NEXT
},
//   case 0xda: return op_stw_dp();
//   void SMPcore::op_stw_dp() {
{
  9, // dp = op_readpc();
  140, // op_readdp(dp);
  159, // op_writedp(dp++, regs.a);
  160, // op_writedp(dp++, regs.y);
  2, // //!!NEXT
},
//   case 0xdb: return op_write_dpi(regs.y, regs.x);
//   void SMPcore::op_write_dpi(uint8 &r, uint8 &i) {
{
  22, // dp = op_readpc() + regs.x;
  1, // op_io();
  140, // op_readdp(dp);
  150, // op_writedp(dp, regs.y);
  2, // //!!NEXT
},
//   case 0xdc: return op_adjust<&SMPcore::op_dec>(regs.y);
//   void SMPcore::op_adjust(uint8 &r) {
{
  1, // op_io();
  161, // regs.y = op_dec(regs.y);
  2, // //!!NEXT
},
//   case 0xdd: return op_transfer(regs.y, regs.a);
//   void SMPcore::op_transfer(uint8 &from, uint8 &to) {
{
  1, // op_io();
  162, // regs.a = regs.y;
  109, // regs.p.n = (regs.a & 0x80);
  110, // regs.p.z = (regs.a == 0);
  2, // //!!NEXT
},
//   case 0xde: return op_bne_dpx();
//   void SMPcore::op_bne_dpx() {
{
  9, // dp = op_readpc();
  1, // op_io();
  163, // sp = op_readdp(dp + regs.x);
  13, // rd = op_readpc();
  1, // op_io();
  71, // if(regs.a == sp) return;
  1, // op_io();
  1, // op_io();
  15, // regs.pc += (int8)rd;
  2, // //!!NEXT
},
//   case 0xdf: return op_daa();
//   void SMPcore::op_daa() {
{
  1, // op_io();
  1, // op_io();
  //if(regs.p.c || (regs.a) > 0x99) {
  //regs.a += 0x60;
  //regs.p.c = 1;
  //}
  //if(regs.p.h || (regs.a & 15) > 0x09) {
  //regs.a += 0x06;
  //}
  //regs.p.n = (regs.a & 0x80);
  //regs.p.z = (regs.a == 0);
  164, // //!!MULTI3
  2, // //!!NEXT
},
//   case 0xe0: return op_clv();
//   void SMPcore::op_clv() {
{
  1, // op_io();
  165, // regs.p.v = 0;
  166, // regs.p.h = 0;
  2, // //!!NEXT
},
//   case 0xe1: return op_jst();
//   void SMPcore::op_jst() {
{
  3, // dp = 0xffde - ((opcode >> 4) << 1);
  4, // rd.l = op_read(dp++);
  5, // rd.h = op_read(dp++);
  1, // op_io();
  1, // op_io();
  1, // op_io();
  6, // op_writesp(regs.pc.h);
  7, // op_writesp(regs.pc.l);
  8, // regs.pc = rd;
  2, // //!!NEXT
},
//   case 0xe2: return op_set_bit();
//   void SMPcore::op_set_bit() {
{
  9, // dp = op_readpc();
  10, // rd = op_readdp(dp) & ~(1 << (opcode >> 5));
  11, // op_writedp(dp, rd | (!(opcode & 0x10) << (opcode >> 5)));
  2, // //!!NEXT
},
//   case 0xe3: return op_branch_bit();
//   void SMPcore::op_branch_bit() {
{
  9, // dp = op_readpc();
  12, // sp = op_readdp(dp);
  13, // rd = op_readpc();
  1, // op_io();
  14, // if((bool)(sp & (1 << (opcode >> 5))) == (bool)(opcode & 0x10)) return;
  1, // op_io();
  1, // op_io();
  15, // regs.pc += (int8)rd;
  2, // //!!NEXT
},
//   case 0xe4: return op_read_dp<&SMPcore::op_ld>(regs.a);
//   void SMPcore::op_read_dp(uint8 &r) {
{
  9, // dp = op_readpc();
  16, // rd = op_readdp(dp);
  167, // regs.a = op_ld(regs.a, rd);
  2, // //!!NEXT
},
//   case 0xe5: return op_read_addr<&SMPcore::op_ld>(regs.a);
//   void SMPcore::op_read_addr(uint8 &r) {
{
  18, // dp.l = op_readpc();
  19, // dp.h = op_readpc();
  20, // rd = op_read(dp);
  167, // regs.a = op_ld(regs.a, rd);
  2, // //!!NEXT
},
//   case 0xe6: return op_read_ix<&SMPcore::op_ld>();
//   void SMPcore::op_read_ix() {
{
  1, // op_io();
  21, // rd = op_readdp(regs.x);
  167, // regs.a = op_ld(regs.a, rd);
  2, // //!!NEXT
},
//   case 0xe7: return op_read_idpx<&SMPcore::op_ld>();
//   void SMPcore::op_read_idpx() {
{
  22, // dp = op_readpc() + regs.x;
  1, // op_io();
  23, // sp.l = op_readdp(dp++);
  24, // sp.h = op_readdp(dp++);
  25, // rd = op_read(sp);
  167, // regs.a = op_ld(regs.a, rd);
  2, // //!!NEXT
},
//   case 0xe8: return op_read_const<&SMPcore::op_ld>(regs.a);
//   void SMPcore::op_read_const(uint8 &r) {
{
  13, // rd = op_readpc();
  167, // regs.a = op_ld(regs.a, rd);
  2, // //!!NEXT
},
//   case 0xe9: return op_read_addr<&SMPcore::op_ld>(regs.x);
//   void SMPcore::op_read_addr(uint8 &r) {
{
  18, // dp.l = op_readpc();
  19, // dp.h = op_readpc();
  20, // rd = op_read(dp);
  152, // regs.x = op_ld(regs.x, rd);
  2, // //!!NEXT
},
//   case 0xea: return op_set_addr_bit();
//   void SMPcore::op_set_addr_bit() {
{
  18, // dp.l = op_readpc();
  19, // dp.h = op_readpc();
  31, // bit = dp >> 13;
  32, // dp &= 0x1fff;
  20, // rd = op_read(dp);
  168, // rd ^= 1 << bit;
  36, // op_write(dp, rd);
  2, // //!!NEXT
},
//   case 0xeb: return op_read_dp<&SMPcore::op_ld>(regs.y);
//   void SMPcore::op_read_dp(uint8 &r) {
{
  9, // dp = op_readpc();
  16, // rd = op_readdp(dp);
  117, // regs.y = op_ld(regs.y, rd);
  2, // //!!NEXT
},
//   case 0xec: return op_read_addr<&SMPcore::op_ld>(regs.y);
//   void SMPcore::op_read_addr(uint8 &r) {
{
  18, // dp.l = op_readpc();
  19, // dp.h = op_readpc();
  20, // rd = op_read(dp);
  117, // regs.y = op_ld(regs.y, rd);
  2, // //!!NEXT
},
//   case 0xed: return op_cmc();
//   void SMPcore::op_cmc() {
{
  1, // op_io();
  1, // op_io();
  169, // regs.p.c = !regs.p.c;
  2, // //!!NEXT
},
//   case 0xee: return op_pull(regs.y);
//   void SMPcore::op_pull(uint8 &r) {
{
  1, // op_io();
  1, // op_io();
  170, // regs.y = op_readsp();
  2, // //!!NEXT
},
//   case 0xef: return op_wait();
//   void SMPcore::op_wait() {
{
  1, // op_io();
  1, // op_io();
  171, // //!!REPEAT
},
//   case 0xf0: return op_branch(regs.p.z == 1);
//   void SMPcore::op_branch(bool condition) {
{
  13, // rd = op_readpc();
  172, // if(regs.p.z != 1) return;
  1, // op_io();
  1, // op_io();
  15, // regs.pc += (int8)rd;
  2, // //!!NEXT
},
//   case 0xf1: return op_jst();
//   void SMPcore::op_jst() {
{
  3, // dp = 0xffde - ((opcode >> 4) << 1);
  4, // rd.l = op_read(dp++);
  5, // rd.h = op_read(dp++);
  1, // op_io();
  1, // op_io();
  1, // op_io();
  6, // op_writesp(regs.pc.h);
  7, // op_writesp(regs.pc.l);
  8, // regs.pc = rd;
  2, // //!!NEXT
},
//   case 0xf2: return op_set_bit();
//   void SMPcore::op_set_bit() {
{
  9, // dp = op_readpc();
  10, // rd = op_readdp(dp) & ~(1 << (opcode >> 5));
  11, // op_writedp(dp, rd | (!(opcode & 0x10) << (opcode >> 5)));
  2, // //!!NEXT
},
//   case 0xf3: return op_branch_bit();
//   void SMPcore::op_branch_bit() {
{
  9, // dp = op_readpc();
  12, // sp = op_readdp(dp);
  13, // rd = op_readpc();
  1, // op_io();
  14, // if((bool)(sp & (1 << (opcode >> 5))) == (bool)(opcode & 0x10)) return;
  1, // op_io();
  1, // op_io();
  15, // regs.pc += (int8)rd;
  2, // //!!NEXT
},
//   case 0xf4: return op_read_dpi<&SMPcore::op_ld>(regs.a, regs.x);
//   void SMPcore::op_read_dpi(uint8 &r, uint8 &i) {
{
  9, // dp = op_readpc();
  1, // op_io();
  47, // rd = op_readdp(dp + regs.x);
  167, // regs.a = op_ld(regs.a, rd);
  2, // //!!NEXT
},
//   case 0xf5: return op_read_addri<&SMPcore::op_ld>(regs.x);
//   void SMPcore::op_read_addri(uint8 &r) {
{
  18, // dp.l = op_readpc();
  19, // dp.h = op_readpc();
  1, // op_io();
  48, // rd = op_read(dp + regs.x);
  167, // regs.a = op_ld(regs.a, rd);
  2, // //!!NEXT
},
//   case 0xf6: return op_read_addri<&SMPcore::op_ld>(regs.y);
//   void SMPcore::op_read_addri(uint8 &r) {
{
  18, // dp.l = op_readpc();
  19, // dp.h = op_readpc();
  1, // op_io();
  49, // rd = op_read(dp + regs.y);
  167, // regs.a = op_ld(regs.a, rd);
  2, // //!!NEXT
},
//   case 0xf7: return op_read_idpy<&SMPcore::op_ld>();
//   void SMPcore::op_read_idpy() {
{
  9, // dp = op_readpc();
  1, // op_io();
  23, // sp.l = op_readdp(dp++);
  24, // sp.h = op_readdp(dp++);
  50, // rd = op_read(sp + regs.y);
  167, // regs.a = op_ld(regs.a, rd);
  2, // //!!NEXT
},
//   case 0xf8: return op_read_dp<&SMPcore::op_ld>(regs.x);
//   void SMPcore::op_read_dp(uint8 &r) {
{
  9, // dp = op_readpc();
  16, // rd = op_readdp(dp);
  152, // regs.x = op_ld(regs.x, rd);
  2, // //!!NEXT
},
//   case 0xf9: return op_read_dpi<&SMPcore::op_ld>(regs.x, regs.y);
//   void SMPcore::op_read_dpi(uint8 &r, uint8 &i) {
{
  9, // dp = op_readpc();
  1, // op_io();
  173, // rd = op_readdp(dp + regs.y);
  152, // regs.x = op_ld(regs.x, rd);
  2, // //!!NEXT
},
//   case 0xfa: return op_write_dp_dp<&SMPcore::op_st>();
//   void SMPcore::op_write_dp_dp() {
{
  26, // sp = op_readpc();
  27, // rd = op_readdp(sp);
  9, // dp = op_readpc();
  118, // wr = op_st(wr, rd);
  30, // op_writedp(dp, wr);
  2, // //!!NEXT
},
//   case 0xfb: return op_read_dpi<&SMPcore::op_ld>(regs.y, regs.x);
//   void SMPcore::op_read_dpi(uint8 &r, uint8 &i) {
{
  9, // dp = op_readpc();
  1, // op_io();
  47, // rd = op_readdp(dp + regs.x);
  117, // regs.y = op_ld(regs.y, rd);
  2, // //!!NEXT
},
//   case 0xfc: return op_adjust<&SMPcore::op_inc>(regs.y);
//   void SMPcore::op_adjust(uint8 &r) {
{
  1, // op_io();
  174, // regs.y = op_inc(regs.y);
  2, // //!!NEXT
},
//   case 0xfd: return op_transfer(regs.a, regs.y);
//   void SMPcore::op_transfer(uint8 &from, uint8 &to) {
{
  1, // op_io();
  175, // regs.y = regs.a;
  176, // regs.p.n = (regs.y & 0x80);
  177, // regs.p.z = (regs.y == 0);
  2, // //!!NEXT
},
//   case 0xfe: return op_bne_ydec();
//   void SMPcore::op_bne_ydec() {
{
  13, // rd = op_readpc();
  1, // op_io();
  1, // op_io();
  178, // if(--regs.y == 0) return;
  1, // op_io();
  1, // op_io();
  15, // regs.pc += (int8)rd;
  2, // //!!NEXT
},
//   case 0xff: return op_wait();
//   void SMPcore::op_wait() {
{
  1, // op_io();
  1, // op_io();
  171, // //!!REPEAT
},

}; // const int uoptable[][] = {






void SMPcore::op_step()
{
  switch (uoptable[opcode][uindex])
  {
    case 1:
      op_io();
      break;
    case 2:
      opcode = op_readpc(); //!!NEXT
	  uindex = -1;
      break;
    case 3:
      dp = 0xffde - ((opcode >> 4) << 1);
      break;
    case 4:
      rd.l = op_read(dp++);
      break;
    case 5:
      rd.h = op_read(dp++);
      break;
    case 6:
      op_writesp(regs.pc.h);
      break;
    case 7:
      op_writesp(regs.pc.l);
      break;
    case 8:
      regs.pc = rd;
      break;
    case 9:
      dp = op_readpc();
      break;
    case 10:
      rd = op_readdp(dp) & ~(1 << (opcode >> 5));
      break;
    case 11:
      op_writedp(dp, rd | (!(opcode & 0x10) << (opcode >> 5)));
      break;
    case 12:
      sp = op_readdp(dp);
      break;
    case 13:
      rd = op_readpc();
      break;
    case 14:
      if((bool)(sp & (1 << (opcode >> 5))) == (bool)(opcode & 0x10)) op_next();
      break;
    case 15:
      regs.pc += (int8)rd;
      break;
    case 16:
      rd = op_readdp(dp);
      break;
    case 17:
      regs.a = op_or(regs.a, rd);
      break;
    case 18:
      dp.l = op_readpc();
      break;
    case 19:
      dp.h = op_readpc();
      break;
    case 20:
      rd = op_read(dp);
      break;
    case 21:
      rd = op_readdp(regs.x);
      break;
    case 22:
      dp = op_readpc() + regs.x;
      break;
    case 23:
      sp.l = op_readdp(dp++);
      break;
    case 24:
      sp.h = op_readdp(dp++);
      break;
    case 25:
      rd = op_read(sp);
      break;
    case 26:
      sp = op_readpc();
      break;
    case 27:
      rd = op_readdp(sp);
      break;
    case 28:
      wr = op_readdp(dp);
      break;
    case 29:
      wr = op_or(wr, rd);
      break;
    case 30:
      op_writedp(dp, wr);
      break;
    case 31:
      bit = dp >> 13;
      break;
    case 32:
      dp &= 0x1fff;
      break;
    case 33:
      regs.p.c |= (rd & (1 << bit)) ^ 0;
      break;
    case 34:
      rd = op_asl(rd);
      break;
    case 35:
      op_writedp(dp, rd);
      break;
    case 36:
      op_write(dp, rd);
      break;
    case 37:
      op_writesp(regs.p);
      break;
    case 38:
      regs.p.n = (regs.a - rd) & 0x80;
      break;
    case 39:
      regs.p.z = (regs.a - rd) == 0;
      break;
    case 40:
      op_read(dp);
      break;
    case 41:
      op_write(dp, rd | regs.a);
      break;
    case 42:
      rd.l = op_read(0xffde);
      break;
    case 43:
      rd.h = op_read(0xffdf);
      break;
    case 44:
      regs.p.b = 1;
      break;
    case 45:
      regs.p.i = 0;
      break;
    case 46:
      if(regs.p.n != 0) op_next();
      break;
    case 47:
      rd = op_readdp(dp + regs.x);
      break;
    case 48:
      rd = op_read(dp + regs.x);
      break;
    case 49:
      rd = op_read(dp + regs.y);
      break;
    case 50:
      rd = op_read(sp + regs.y);
      break;
    case 51:
      rd = op_readdp(regs.y);
      break;
    case 52:
      wr = op_readdp(regs.x);
      break;
    case 53:
      op_writedp(regs.x, wr);
      break;
    case 54:
      rd.w = op_readdp(dp) - 1;
      break;
    case 55:
      op_writedp(dp++, rd.l);
      break;
    case 56:
      rd.h += op_readdp(dp);
      break;
    case 57:
      op_writedp(dp++, rd.h);
      break;
    case 58:
      regs.p.n = rd & 0x8000;
      break;
    case 59:
      regs.p.z = rd == 0;
      break;
    case 60:
      op_writedp(dp + regs.x, rd);
      break;
    case 61:
      regs.a = op_asl(regs.a);
      break;
    case 62:
      regs.x = op_dec(regs.x);
      break;
    case 63:
      regs.x = op_cmp(regs.x, rd);
      break;
    case 64:
      dp += regs.x;
      break;
    case 65:
      regs.p.p = 0;
      break;
    case 66:
      regs.a = op_and(regs.a, rd);
      break;
    case 67:
      wr = op_and(wr, rd);
      break;
    case 68:
      regs.p.c |= (rd & (1 << bit)) ^ 1;
      break;
    case 69:
      rd = op_rol(rd);
      break;
    case 70:
      op_writesp(regs.a);
      break;
    case 71:
      if(regs.a == sp) op_next();
      break;
    case 72:
      if(regs.p.n != 1) op_next();
      break;
    case 73:
      rd.w = op_readdp(dp) + 1;
      break;
    case 74:
      regs.a = op_rol(regs.a);
      break;
    case 75:
      regs.x = op_inc(regs.x);
      break;
    case 76:
      rd.l = op_readpc();
      break;
    case 77:
      rd.h = op_readpc();
      break;
    case 78:
      regs.p.p = 1;
      break;
    case 79:
      regs.a = op_eor(regs.a, rd);
      break;
    case 80:
      wr = op_eor(wr, rd);
      break;
    case 81:
      regs.p.c &= (rd & (1 << bit)) ^ 0;
      break;
    case 82:
      rd = op_lsr(rd);
      break;
    case 83:
      op_writesp(regs.x);
      break;
    case 84:
      op_write(dp, rd & ~regs.a);
      break;
    case 85:
      regs.pc = 0xff00 | rd;
      break;
    case 86:
      if(regs.p.v != 0) op_next();
      break;
    case 87:
      rd.l = op_readdp(dp++);
      break;
    case 88:
      rd.h = op_readdp(dp++);
      break;
    case 89:
      regs.ya = op_cpw(regs.ya, rd);
      break;
    case 90:
      regs.a = op_lsr(regs.a);
      break;
    case 91:
      regs.x = regs.a;
      break;
    case 92:
      regs.p.n = (regs.x & 0x80);
      break;
    case 93:
      regs.p.z = (regs.x == 0);
      break;
    case 94:
      regs.y = op_cmp(regs.y, rd);
      break;
    case 95:
      regs.p.c = 0;
      break;
    case 96:
      regs.a = op_cmp(regs.a, rd);
      break;
    case 97:
      wr = op_cmp(wr, rd);
      break;
    case 98:
      regs.p.c &= (rd & (1 << bit)) ^ 1;
      break;
    case 99:
      rd = op_ror(rd);
      break;
    case 100:
      op_writesp(regs.y);
      break;
    case 101:
      op_writedp(dp, --wr);
      break;
    case 102:
      if(wr == 0) op_next();
      break;
    case 103:
      rd.l = op_readsp();
      break;
    case 104:
      rd.h = op_readsp();
      break;
    case 105:
      if(regs.p.v != 1) op_next();
      break;
    case 106:
      regs.ya = op_adw(regs.ya, rd);
      break;
    case 107:
      regs.a = op_ror(regs.a);
      break;
    case 108:
      regs.a = regs.x;
      break;
    case 109:
      regs.p.n = (regs.a & 0x80);
      break;
    case 110:
      regs.p.z = (regs.a == 0);
      break;
    case 111:
      regs.p = op_readsp();
      break;
    case 112:
      regs.p.c = 1;
      break;
    case 113:
      regs.a = op_adc(regs.a, rd);
      break;
    case 114:
      wr = op_adc(wr, rd);
      break;
    case 115:
      regs.p.c ^= (bool)(rd & (1 << bit));
      break;
    case 116:
      rd = op_dec(rd);
      break;
    case 117:
      regs.y = op_ld(regs.y, rd);
      break;
    case 118:
      wr = op_st(wr, rd);
      break;
    case 119:
      if(regs.p.c != 0) op_next();
      break;
    case 120:
      regs.ya = op_sbw(regs.ya, rd);
      break;
    case 121:
      regs.a = op_dec(regs.a);
      break;
    case 122:
      regs.x = regs.s;
      break;
    case 123:
      //!!MULTI0
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
      break;
    case 124:
      regs.a = (regs.a >> 4) | (regs.a << 4);
      break;
    case 125:
      regs.p.n = regs.a & 0x80;
      break;
    case 126:
      regs.p.z = regs.a == 0;
      break;
    case 127:
      regs.p.i = 1;
      break;
    case 128:
      regs.a = op_sbc(regs.a, rd);
      break;
    case 129:
      wr = op_sbc(wr, rd);
      break;
    case 130:
      regs.p.c  = (rd & (1 << bit));
      break;
    case 131:
      rd = op_inc(rd);
      break;
    case 132:
      regs.a = op_readsp();
      break;
    case 133:
      op_writedp(regs.x++, regs.a);
      break;
    case 134:
      if(regs.p.c != 1) op_next();
      break;
    case 135:
      regs.ya = op_ldw(regs.ya, rd);
      break;
    case 136:
      r = op_inc(r);
      break;
    case 137:
      regs.s = regs.x;
      break;
    case 138:
      //!!MULTI1
	  if(!regs.p.c || (regs.a) > 0x99) {
        regs.a -= 0x60;
        regs.p.c = 0;
      }
      if(!regs.p.h || (regs.a & 15) > 0x09) {
        regs.a -= 0x06;
      }
      regs.p.n = (regs.a & 0x80);
      regs.p.z = (regs.a == 0);
      break;
    case 139:
      regs.a = op_readdp(regs.x++);
      break;
    case 140:
      op_readdp(dp);
      break;
    case 141:
      op_writedp(dp, regs.a);
      break;
    case 142:
      op_write(dp, regs.a);
      break;
    case 143:
      op_readdp(regs.x);
      break;
    case 144:
      op_writedp(regs.x, regs.a);
      break;
    case 145:
      sp = op_readpc() + regs.x;
      break;
    case 146:
      dp.l = op_readdp(sp++);
      break;
    case 147:
      dp.h = op_readdp(sp++);
      break;
    case 148:
      op_write(dp, regs.x);
      break;
    case 149:
      rd = (rd & ~(1 << bit)) | (regs.p.c << bit);
      break;
    case 150:
      op_writedp(dp, regs.y);
      break;
    case 151:
      op_write(dp, regs.y);
      break;
    case 152:
      regs.x = op_ld(regs.x, rd);
      break;
    case 153:
      regs.x = op_readsp();
      break;
    case 154:
      //!!MULTI2
      ya = regs.y * regs.a;
      regs.a = ya;
      regs.y = ya >> 8;
      //result is set based on y (high-byte) only
      regs.p.n = (regs.y & 0x80);
      regs.p.z = (regs.y == 0);
      break;
    case 155:
      if(regs.p.z != 0) op_next();
      break;
    case 156:
      dp += regs.y;
      break;
    case 157:
      op_writedp(dp, regs.x);
      break;
    case 158:
      dp = op_readpc() + regs.y;
      break;
    case 159:
      op_writedp(dp++, regs.a);
      break;
    case 160:
      op_writedp(dp++, regs.y);
      break;
    case 161:
      regs.y = op_dec(regs.y);
      break;
    case 162:
      regs.a = regs.y;
      break;
    case 163:
      sp = op_readdp(dp + regs.x);
      break;
    case 164:
      //!!MULTI3
	  if(regs.p.c || (regs.a) > 0x99) {
        regs.a += 0x60;
        regs.p.c = 1;
      }
      if(regs.p.h || (regs.a & 15) > 0x09) {
        regs.a += 0x06;
      }
      regs.p.n = (regs.a & 0x80);
      regs.p.z = (regs.a == 0);
      break;
    case 165:
      regs.p.v = 0;
      break;
    case 166:
      regs.p.h = 0;
      break;
    case 167:
      regs.a = op_ld(regs.a, rd);
      break;
    case 168:
      rd ^= 1 << bit;
      break;
    case 169:
      regs.p.c = !regs.p.c;
      break;
    case 170:
      regs.y = op_readsp();
      break;
    case 171:
      uindex = -1; //!!REPEAT
      break;
    case 172:
      if(regs.p.z != 1) op_next();
      break;
    case 173:
      rd = op_readdp(dp + regs.y);
      break;
    case 174:
      regs.y = op_inc(regs.y);
      break;
    case 175:
      regs.y = regs.a;
      break;
    case 176:
      regs.p.n = (regs.y & 0x80);
      break;
    case 177:
      regs.p.z = (regs.y == 0);
      break;
    case 178:
      if(--regs.y == 0) op_next();
      break;
  }
  uindex++;
}
