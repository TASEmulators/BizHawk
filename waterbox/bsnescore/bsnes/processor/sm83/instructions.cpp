auto SM83::instructionADC_Direct_Data(uint8& target) -> void {
  target = ADD(target, operand(), CF);
}

auto SM83::instructionADC_Direct_Direct(uint8& target, uint8& source) -> void {
  target = ADD(target, source, CF);
}

auto SM83::instructionADC_Direct_Indirect(uint8& target, uint16& source) -> void {
  target = ADD(target, read(source), CF);
}

auto SM83::instructionADD_Direct_Data(uint8& target) -> void {
  target = ADD(target, operand());
}

auto SM83::instructionADD_Direct_Direct(uint8& target, uint8& source) -> void {
  target = ADD(target, source);
}

auto SM83::instructionADD_Direct_Direct(uint16& target, uint16& source) -> void {
  idle();
  uint32 x = target + source;
  uint32 y = (uint12)target + (uint12)source;
  target = x;
  CF = x > 0xffff;
  HF = y > 0x0fff;
  NF = 0;
}

auto SM83::instructionADD_Direct_Indirect(uint8& target, uint16& source) -> void {
  target = ADD(target, read(source));
}

auto SM83::instructionADD_Direct_Relative(uint16& target) -> void {
  auto data = operand();
  idle();
  idle();
  CF = (uint8)target + (uint8)data > 0xff;
  HF = (uint4)target + (uint4)data > 0x0f;
  NF = 0;
  ZF = 0;
  target += (int8)data;
}

auto SM83::instructionAND_Direct_Data(uint8& target) -> void {
  target = AND(target, operand());
}

auto SM83::instructionAND_Direct_Direct(uint8& target, uint8& source) -> void {
  target = AND(target, source);
}

auto SM83::instructionAND_Direct_Indirect(uint8& target, uint16& source) -> void {
  target = AND(target, read(source));
}

auto SM83::instructionBIT_Index_Direct(uint3 index, uint8& data) -> void {
  BIT(index, data);
}

auto SM83::instructionBIT_Index_Indirect(uint3 index, uint16& address) -> void {
  auto data = read(address);
  BIT(index, data);
}

auto SM83::instructionCALL_Condition_Address(bool take) -> void {
  auto address = operands();
  if(!take) return;
  idle();
  push(PC);
  PC = address;
}

auto SM83::instructionCCF() -> void {
  CF = !CF;
  HF = 0;
  NF = 0;
}

auto SM83::instructionCP_Direct_Data(uint8& target) -> void {
  CP(target, operand());
}

auto SM83::instructionCP_Direct_Direct(uint8& target, uint8& source) -> void {
  CP(target, source);
}

auto SM83::instructionCP_Direct_Indirect(uint8& target, uint16& source) -> void {
  CP(target, read(source));
}

auto SM83::instructionCPL() -> void {
  A = ~A;
  HF = 1;
  NF = 1;
}

auto SM83::instructionDAA() -> void {
  uint16 a = A;
  if(!NF) {
    if(HF || (uint4)a > 0x09) a += 0x06;
    if(CF || (uint8)a > 0x9f) a += 0x60;
  } else {
    if(HF) {
      a -= 0x06;
      if(!CF) a &= 0xff;
    }
    if(CF) a -= 0x60;
  }
  A = a;
  CF |= bit1(a,8);
  HF = 0;
  ZF = A == 0;
}

auto SM83::instructionDEC_Direct(uint8& data) -> void {
  data = DEC(data);
}

auto SM83::instructionDEC_Direct(uint16& data) -> void {
  idle();
  data--;
}

auto SM83::instructionDEC_Indirect(uint16& address) -> void {
  auto data = read(address);
  write(address, DEC(data));
}

auto SM83::instructionDI() -> void {
  r.ime = 0;
}

auto SM83::instructionEI() -> void {
  r.ei = 1;
}

auto SM83::instructionHALT() -> void {
  r.halt = 1;
  while(r.halt) halt();
}

auto SM83::instructionINC_Direct(uint8& data) -> void {
  data = INC(data);
}

auto SM83::instructionINC_Direct(uint16& data) -> void {
  idle();
  data++;
}

auto SM83::instructionINC_Indirect(uint16& address) -> void {
  auto data = read(address);
  write(address, INC(data));
}

auto SM83::instructionJP_Condition_Address(bool take) -> void {
  auto address = operands();
  if(!take) return;
  idle();
  PC = address;
}

auto SM83::instructionJP_Direct(uint16& data) -> void {
  PC = data;
}

auto SM83::instructionJR_Condition_Relative(bool take) -> void {
  auto data = operand();
  if(!take) return;
  idle();
  PC += (int8)data;
}

auto SM83::instructionLD_Address_Direct(uint8& data) -> void {
  write(operands(), data);
}

auto SM83::instructionLD_Address_Direct(uint16& data) -> void {
  store(operands(), data);
}

auto SM83::instructionLD_Direct_Address(uint8& data) -> void {
  data = read(operands());
}

auto SM83::instructionLD_Direct_Data(uint8& target) -> void {
  target = operand();
}

auto SM83::instructionLD_Direct_Data(uint16& target) -> void {
  target = operands();
}

auto SM83::instructionLD_Direct_Direct(uint8& target, uint8& source) -> void {
  target = source;
}

auto SM83::instructionLD_Direct_Direct(uint16& target, uint16& source) -> void {
  idle();
  target = source;
}

auto SM83::instructionLD_Direct_DirectRelative(uint16& target, uint16& source) -> void {
  auto data = operand();
  idle();
  CF = (uint8)source + (uint8)data > 0xff;
  HF = (uint4)source + (uint4)data > 0x0f;
  NF = 0;
  ZF = 0;
  target = source + (int8)data;
}

auto SM83::instructionLD_Direct_Indirect(uint8& target, uint16& source) -> void {
  target = read(source);
}

auto SM83::instructionLD_Direct_IndirectDecrement(uint8& target, uint16& source) -> void {
  target = read(source--);
}

auto SM83::instructionLD_Direct_IndirectIncrement(uint8& target, uint16& source) -> void {
  target = read(source++);
}

auto SM83::instructionLD_Indirect_Data(uint16& target) -> void {
  write(target, operand());
}

auto SM83::instructionLD_Indirect_Direct(uint16& target, uint8& source) -> void {
  write(target, source);
}

auto SM83::instructionLD_IndirectDecrement_Direct(uint16& target, uint8& source) -> void {
  write(target--, source);
}

auto SM83::instructionLD_IndirectIncrement_Direct(uint16& target, uint8& source) -> void {
  write(target++, source);
}

auto SM83::instructionLDH_Address_Direct(uint8& data) -> void {
  write(0xff00 | operand(), data);
}

auto SM83::instructionLDH_Direct_Address(uint8& data) -> void {
  data = read(0xff00 | operand());
}

auto SM83::instructionLDH_Direct_Indirect(uint8& target, uint8& source) -> void {
  target = read(0xff00 | source);
}

auto SM83::instructionLDH_Indirect_Direct(uint8& target, uint8& source) -> void {
  write(0xff00 | target, source);
}

auto SM83::instructionNOP() -> void {
}

auto SM83::instructionOR_Direct_Data(uint8& target) -> void {
  target = OR(target, operand());
}

auto SM83::instructionOR_Direct_Direct(uint8& target, uint8& source) -> void {
  target = OR(target, source);
}

auto SM83::instructionOR_Direct_Indirect(uint8& target, uint16& source) -> void {
  target = OR(target, read(source));
}

auto SM83::instructionPOP_Direct(uint16& data) -> void {
  data = pop();
}

auto SM83::instructionPUSH_Direct(uint16& data) -> void {
  idle();
  push(data);
}

auto SM83::instructionRES_Index_Direct(uint3 index, uint8& data) -> void {
  bit1(data,index) = 0;
}

auto SM83::instructionRES_Index_Indirect(uint3 index, uint16& address) -> void {
  auto data = read(address);
  bit1(data,index) = 0;
  write(address, data);
}

auto SM83::instructionRET() -> void {
  auto address = pop();
  idle();
  PC = address;
}

auto SM83::instructionRET_Condition(bool take) -> void {
  idle();
  if(!take) return;
  PC = pop();
  idle();
}

auto SM83::instructionRETI() -> void {
  auto address = pop();
  idle();
  PC = address;
  r.ime = 1;
}

auto SM83::instructionRL_Direct(uint8& data) -> void {
  data = RL(data);
}

auto SM83::instructionRL_Indirect(uint16& address) -> void {
  auto data = read(address);
  write(address, RL(data));
}

auto SM83::instructionRLA() -> void {
  A = RL(A);
  ZF = 0;
}

auto SM83::instructionRLC_Direct(uint8& data) -> void {
  data = RLC(data);
}

auto SM83::instructionRLC_Indirect(uint16& address) -> void {
  auto data = read(address);
  write(address, RLC(data));
}

auto SM83::instructionRLCA() -> void {
  A = RLC(A);
  ZF = 0;
}

auto SM83::instructionRR_Direct(uint8& data) -> void {
  data = RR(data);
}

auto SM83::instructionRR_Indirect(uint16& address) -> void {
  auto data = read(address);
  write(address, RR(data));
}

auto SM83::instructionRRA() -> void {
  A = RR(A);
  ZF = 0;
}

auto SM83::instructionRRC_Direct(uint8& data) -> void {
  data = RRC(data);
}

auto SM83::instructionRRC_Indirect(uint16& address) -> void {
  auto data = read(address);
  write(address, RRC(data));
}

auto SM83::instructionRRCA() -> void {
  A = RRC(A);
  ZF = 0;
}

auto SM83::instructionRST_Implied(uint8 vector) -> void {
  idle();
  push(PC);
  PC = vector;
}

auto SM83::instructionSBC_Direct_Data(uint8& target) -> void {
  target = SUB(target, operand(), CF);
}

auto SM83::instructionSBC_Direct_Direct(uint8& target, uint8& source) -> void {
  target = SUB(target, source, CF);
}

auto SM83::instructionSBC_Direct_Indirect(uint8& target, uint16& source) -> void {
  target = SUB(target, read(source), CF);
}

auto SM83::instructionSCF() -> void {
  CF = 1;
  HF = 0;
  NF = 0;
}

auto SM83::instructionSET_Index_Direct(uint3 index, uint8& data) -> void {
  bit1(data,index) = 1;
}

auto SM83::instructionSET_Index_Indirect(uint3 index, uint16& address) -> void {
  auto data = read(address);
  bit1(data,index) = 1;
  write(address, data);
}

auto SM83::instructionSLA_Direct(uint8& data) -> void {
  data = SLA(data);
}

auto SM83::instructionSLA_Indirect(uint16& address) -> void {
  auto data = read(address);
  write(address, SLA(data));
}

auto SM83::instructionSRA_Direct(uint8& data) -> void {
  data = SRA(data);
}

auto SM83::instructionSRA_Indirect(uint16& address) -> void {
  auto data = read(address);
  write(address, SRA(data));
}

auto SM83::instructionSRL_Direct(uint8& data) -> void {
  data = SRL(data);
}

auto SM83::instructionSRL_Indirect(uint16& address) -> void {
  auto data = read(address);
  write(address, SRL(data));
}

auto SM83::instructionSTOP() -> void {
  if(!stoppable()) return;
  r.stop = 1;
  while(r.stop) stop();
}

auto SM83::instructionSUB_Direct_Data(uint8& target) -> void {
  target = SUB(target, operand());
}

auto SM83::instructionSUB_Direct_Direct(uint8& target, uint8& source) -> void {
  target = SUB(target, source);
}

auto SM83::instructionSUB_Direct_Indirect(uint8& target, uint16& source) -> void {
  target = SUB(target, read(source));
}

auto SM83::instructionSWAP_Direct(uint8& data) -> void {
  data = SWAP(data);
}

auto SM83::instructionSWAP_Indirect(uint16& address) -> void {
  auto data = read(address);
  write(address, SWAP(data));
}

auto SM83::instructionXOR_Direct_Data(uint8& target) -> void {
  target = XOR(target, operand());
}

auto SM83::instructionXOR_Direct_Direct(uint8& target, uint8& source) -> void {
  target = XOR(target, source);
}

auto SM83::instructionXOR_Direct_Indirect(uint8& target, uint16& source) -> void {
  target = XOR(target, read(source));
}
