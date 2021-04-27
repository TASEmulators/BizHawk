auto SPC700::instructionAbsoluteBitModify(uint3 mode) -> void {
  uint16 address = fetch();
  address |= fetch() << 8;
  uint3 bit = address >> 13;
  address &= 0x1fff;
  uint8 data = read(address);
  switch(mode) {
  case 0:  //or addr:bit
    idle();
    CF |= bool(data & 1 << bit);
    break;
  case 1:  //or !addr:bit
    idle();
    CF |= !bool(data & 1 << bit);
    break;
  case 2:  //and addr:bit
    CF &= bool(data & 1 << bit);
    break;
  case 3:  //and !addr:bit
    CF &= !bool(data & 1 << bit);
    break;
  case 4:  //eor addr:bit
    idle();
    CF ^= bool(data & 1 << bit);
    break;
  case 5:  //ld addr:bit
    CF = bool(data & 1 << bit);
    break;
  case 6:  //st addr:bit
    idle();
    data &= ~(1 << bit);
    data |= CF << bit;
    write(address, data);
    break;
  case 7:  //not addr:bit
    data ^= 1 << bit;
    write(address, data);
    break;
  }
}

auto SPC700::instructionAbsoluteBitSet(uint3 bit, bool value) -> void {
  uint8 address = fetch();
  uint8 data = load(address);
  data &= ~(1 << bit);
  data |= value << bit;
  store(address, data);
}

auto SPC700::instructionAbsoluteRead(fpb op, uint8& target) -> void {
  uint16 address = fetch();
  address |= fetch() << 8;
  uint8 data = read(address);
  target = alu(target, data);
}

auto SPC700::instructionAbsoluteModify(fps op) -> void {
  uint16 address = fetch();
  address |= fetch() << 8;
  uint8 data = read(address);
  write(address, alu(data));
}

auto SPC700::instructionAbsoluteWrite(uint8& data) -> void {
  uint16 address = fetch();
  address |= fetch() << 8;
  read(address);
  write(address, data);
}

auto SPC700::instructionAbsoluteIndexedRead(fpb op, uint8& index) -> void {
  uint16 address = fetch();
  address |= fetch() << 8;
  idle();
  uint8 data = read(address + index);
  A = alu(A, data);
}

auto SPC700::instructionAbsoluteIndexedWrite(uint8& index) -> void {
  uint16 address = fetch();
  address |= fetch() << 8;
  idle();
  read(address + index);
  write(address + index, A);
}

auto SPC700::instructionBranch(bool take) -> void {
  uint8 data = fetch();
  if(!take) return;
  idle();
  idle();
  PC += (int8)data;
}

auto SPC700::instructionBranchBit(uint3 bit, bool match) -> void {
  uint8 address = fetch();
  uint8 data = load(address);
  idle();
  uint8 displacement = fetch();
  if(bool(data & 1 << bit) != match) return;
  idle();
  idle();
  PC += (int8)displacement;
}

auto SPC700::instructionBranchNotDirect() -> void {
  uint8 address = fetch();
  uint8 data = load(address);
  idle();
  uint8 displacement = fetch();
  if(A == data) return;
  idle();
  idle();
  PC += (int8)displacement;
}

auto SPC700::instructionBranchNotDirectDecrement() -> void {
  uint8 address = fetch();
  uint8 data = load(address);
  store(address, --data);
  uint8 displacement = fetch();
  if(data == 0) return;
  idle();
  idle();
  PC += (int8)displacement;
}

auto SPC700::instructionBranchNotDirectIndexed(uint8& index) -> void {
  uint8 address = fetch();
  idle();
  uint8 data = load(address + index);
  idle();
  uint8 displacement = fetch();
  if(A == data) return;
  idle();
  idle();
  PC += (int8)displacement;
}

auto SPC700::instructionBranchNotYDecrement() -> void {
  read(PC);
  idle();
  uint8 displacement = fetch();
  if(--Y == 0) return;
  idle();
  idle();
  PC += (int8)displacement;
}

auto SPC700::instructionBreak() -> void {
  read(PC);
  push(PC >> 8);
  push(PC >> 0);
  push(P);
  idle();
  uint16 address = read(0xffde + 0);
  address |= read(0xffde + 1) << 8;
  PC = address;
  IF = 0;
  BF = 1;
}

auto SPC700::instructionCallAbsolute() -> void {
  uint16 address = fetch();
  address |= fetch() << 8;
  idle();
  push(PC >> 8);
  push(PC >> 0);
  idle();
  idle();
  PC = address;
}

auto SPC700::instructionCallPage() -> void {
  uint8 address = fetch();
  idle();
  push(PC >> 8);
  push(PC >> 0);
  idle();
  PC = 0xff00 | address;
}

auto SPC700::instructionCallTable(uint4 vector) -> void {
  read(PC);
  idle();
  push(PC >> 8);
  push(PC >> 0);
  idle();
  uint16 address = 0xffde - (vector << 1);
  uint16 pc = read(address + 0);
  pc |= read(address + 1) << 8;
  PC = pc;
}

auto SPC700::instructionComplementCarry() -> void {
  read(PC);
  idle();
  CF = !CF;
}

auto SPC700::instructionDecimalAdjustAdd() -> void {
  read(PC);
  idle();
  if(CF || A > 0x99) {
    A += 0x60;
    CF = 1;
  }
  if(HF || (A & 15) > 0x09) {
    A += 0x06;
  }
  ZF = A == 0;
  NF = A & 0x80;
}

auto SPC700::instructionDecimalAdjustSub() -> void {
  read(PC);
  idle();
  if(!CF || A > 0x99) {
    A -= 0x60;
    CF = 0;
  }
  if(!HF || (A & 15) > 0x09) {
    A -= 0x06;
  }
  ZF = A == 0;
  NF = A & 0x80;
}

auto SPC700::instructionDirectRead(fpb op, uint8& target) -> void {
  uint8 address = fetch();
  uint8 data = load(address);
  target = alu(target, data);
}

auto SPC700::instructionDirectModify(fps op) -> void {
  uint8 address = fetch();
  uint8 data = load(address);
  store(address, alu(data));
}

auto SPC700::instructionDirectWrite(uint8& data) -> void {
  uint8 address = fetch();
  load(address);
  store(address, data);
}

auto SPC700::instructionDirectDirectCompare(fpb op) -> void {
  uint8 source = fetch();
  uint8 rhs = load(source);
  uint8 target = fetch();
  uint8 lhs = load(target);
  lhs = alu(lhs, rhs);
  idle();
}

auto SPC700::instructionDirectDirectModify(fpb op) -> void {
  uint8 source = fetch();
  uint8 rhs = load(source);
  uint8 target = fetch();
  uint8 lhs = load(target);
  lhs = alu(lhs, rhs);
  store(target, lhs);
}

auto SPC700::instructionDirectDirectWrite() -> void {
  uint8 source = fetch();
  uint8 data = load(source);
  uint8 target = fetch();
  store(target, data);
}

auto SPC700::instructionDirectImmediateCompare(fpb op) -> void {
  uint8 immediate = fetch();
  uint8 address = fetch();
  uint8 data = load(address);
  data = alu(data, immediate);
  idle();
}

auto SPC700::instructionDirectImmediateModify(fpb op) -> void {
  uint8 immediate = fetch();
  uint8 address = fetch();
  uint8 data = load(address);
  data = alu(data, immediate);
  store(address, data);
}

auto SPC700::instructionDirectImmediateWrite() -> void {
  uint8 immediate = fetch();
  uint8 address = fetch();
  load(address);
  store(address, immediate);
}

auto SPC700::instructionDirectCompareWord(fpw op) -> void {
  uint8 address = fetch();
  uint16 data = load(address + 0);
  data |= load(address + 1) << 8;
  YA = alu(YA, data);
}

auto SPC700::instructionDirectReadWord(fpw op) -> void {
  uint8 address = fetch();
  uint16 data = load(address + 0);
  idle();
  data |= load(address + 1) << 8;
  YA = alu(YA, data);
}

auto SPC700::instructionDirectModifyWord(int adjust) -> void {
  uint8 address = fetch();
  uint16 data = load(address + 0) + adjust;
  store(address + 0, data >> 0);
  data += load(address + 1) << 8;
  store(address + 1, data >> 8);
  ZF = data == 0;
  NF = data & 0x8000;
}

auto SPC700::instructionDirectWriteWord() -> void {
  uint8 address = fetch();
  load(address + 0);
  store(address + 0, A);
  store(address + 1, Y);
}

auto SPC700::instructionDirectIndexedRead(fpb op, uint8& target, uint8& index) -> void {
  uint8 address = fetch();
  idle();
  uint8 data = load(address + index);
  target = alu(target, data);
}

auto SPC700::instructionDirectIndexedModify(fps op, uint8& index) -> void {
  uint8 address = fetch();
  idle();
  uint8 data = load(address + index);
  store(address + index, alu(data));
}

auto SPC700::instructionDirectIndexedWrite(uint8& data, uint8& index) -> void {
  uint8 address = fetch();
  idle();
  load(address + index);
  store(address + index, data);
}

auto SPC700::instructionDivide() -> void {
  read(PC);
  idle();
  idle();
  idle();
  idle();
  idle();
  idle();
  idle();
  idle();
  idle();
  idle();
  uint16 ya = YA;
  //overflow set if quotient >= 256
  HF = (Y & 15) >= (X & 15);
  VF = Y >= X;
  if(Y < (X << 1)) {
    //if quotient is <= 511 (will fit into 9-bit result)
    A = ya / X;
    Y = ya % X;
  } else {
    //otherwise, the quotient won't fit into VF + A
    //this emulates the odd behavior of the S-SMP in this case
    A = 255 - (ya - (X << 9)) / (256 - X);
    Y = X   + (ya - (X << 9)) % (256 - X);
  }
  //result is set based on a (quotient) only
  ZF = A == 0;
  NF = A & 0x80;
}

auto SPC700::instructionExchangeNibble() -> void {
  read(PC);
  idle();
  idle();
  idle();
  A = A >> 4 | A << 4;
  ZF = A == 0;
  NF = A & 0x80;
}

auto SPC700::instructionFlagSet(bool& flag, bool value) -> void {
  read(PC);
  if(&flag == &IF) idle();
  flag = value;
}

auto SPC700::instructionImmediateRead(fpb op, uint8& target) -> void {
  uint8 data = fetch();
  target = alu(target, data);
}

auto SPC700::instructionImpliedModify(fps op, uint8& target) -> void {
  read(PC);
  target = alu(target);
}

auto SPC700::instructionIndexedIndirectRead(fpb op, uint8& index) -> void {
  uint8 indirect = fetch();
  idle();
  uint16 address = load(indirect + index + 0);
  address |= load(indirect + index + 1) << 8;
  uint8 data = read(address);
  A = alu(A, data);
}

auto SPC700::instructionIndexedIndirectWrite(uint8& data, uint8& index) -> void {
  uint8 indirect = fetch();
  idle();
  uint16 address = load(indirect + index + 0);
  address |= load(indirect + index + 1) << 8;
  read(address);
  write(address, data);
}

auto SPC700::instructionIndirectIndexedRead(fpb op, uint8& index) -> void {
  uint8 indirect = fetch();
  uint16 address = load(indirect + 0);
  address |= load(indirect + 1) << 8;
  idle();
  uint8 data = read(address + index);
  A = alu(A, data);
}

auto SPC700::instructionIndirectIndexedWrite(uint8& data, uint8& index) -> void {
  uint8 indirect = fetch();
  uint16 address = load(indirect + 0);
  address |= load(indirect + 1) << 8;
  idle();
  read(address + index);
  write(address + index, data);
}

auto SPC700::instructionIndirectXRead(fpb op) -> void {
  read(PC);
  uint8 data = load(X);
  A = alu(A, data);
}

auto SPC700::instructionIndirectXWrite(uint8& data) -> void {
  read(PC);
  load(X);
  store(X, data);
}

auto SPC700::instructionIndirectXIncrementRead(uint8& data) -> void {
  read(PC);
  data = load(X++);
  idle();  //quirk: consumes extra idle cycle compared to most read instructions
  ZF = data == 0;
  NF = data & 0x80;
}

auto SPC700::instructionIndirectXIncrementWrite(uint8& data) -> void {
  read(PC);
  idle();  //quirk: not a read cycle as with most write instructions
  store(X++, data);
}

auto SPC700::instructionIndirectXCompareIndirectY(fpb op) -> void {
  read(PC);
  uint8 rhs = load(Y);
  uint8 lhs = load(X);
  lhs = alu(lhs, rhs);
  idle();
}

auto SPC700::instructionIndirectXWriteIndirectY(fpb op) -> void {
  read(PC);
  uint8 rhs = load(Y);
  uint8 lhs = load(X);
  lhs = alu(lhs, rhs);
  store(X, lhs);
}

auto SPC700::instructionJumpAbsolute() -> void {
  uint16 address = fetch();
  address |= fetch() << 8;
  PC = address;
}

auto SPC700::instructionJumpIndirectX() -> void {
  uint16 address = fetch();
  address |= fetch() << 8;
  idle();
  uint16 pc = read(address + X + 0);
  pc |= read(address + X + 1) << 8;
  PC = pc;
}

auto SPC700::instructionMultiply() -> void {
  read(PC);
  idle();
  idle();
  idle();
  idle();
  idle();
  idle();
  idle();
  uint16 ya = Y * A;
  A = ya >> 0;
  Y = ya >> 8;
  //result is set based on y (high-byte) only
  ZF = Y == 0;
  NF = Y & 0x80;
}

auto SPC700::instructionNoOperation() -> void {
  read(PC);
}

auto SPC700::instructionOverflowClear() -> void {
  read(PC);
  HF = 0;
  VF = 0;
}

auto SPC700::instructionPull(uint8& data) -> void {
  read(PC);
  idle();
  data = pull();
}

auto SPC700::instructionPullP() -> void {
  read(PC);
  idle();
  P = pull();
}

auto SPC700::instructionPush(uint8 data) -> void {
  read(PC);
  push(data);
  idle();
}

auto SPC700::instructionReturnInterrupt() -> void {
  read(PC);
  idle();
  P = pull();
  uint16 address = pull();
  address |= pull() << 8;
  PC = address;
}

auto SPC700::instructionReturnSubroutine() -> void {
  read(PC);
  idle();
  uint16 address = pull();
  address |= pull() << 8;
  PC = address;
}

auto SPC700::instructionStop() -> void {
  r.stop = true;
  while(r.stop && !synchronizing()) {
    read(PC);
    idle();
  }
}

auto SPC700::instructionTestSetBitsAbsolute(bool set) -> void {
  uint16 address = fetch();
  address |= fetch() << 8;
  uint8 data = read(address);
  ZF = (A - data) == 0;
  NF = (A - data) & 0x80;
  read(address);
  write(address, set ? data | A : data & ~A);
}

auto SPC700::instructionTransfer(uint8& from, uint8& to) -> void {
  read(PC);
  to = from;
  if(&to == &S) return;
  ZF = to == 0;
  NF = to & 0x80;
}

auto SPC700::instructionWait() -> void {
  r.wait = true;
  while(r.wait && !synchronizing()) {
    read(PC);
    idle();
  }
}
