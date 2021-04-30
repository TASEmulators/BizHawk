HG51B::HG51B() {
  #define bind(id, name, ...) { \
    if(instructionTable[id]) throw; \
    instructionTable[id] = [=] { return instruction##name(__VA_ARGS__); }; \
  }

  #define pattern(s) \
    std::integral_constant<uint16_t, bit::test(s)>::value

  static const uint5 shifts[] = {0, 1, 8, 16};

  //NOP
  for(uint10 null : range(1024)) {
    auto opcode = pattern("0000 00.. .... ....");
    bind(opcode | null << 0, NOP);
  }

  //???
  for(uint10 null : range(1024)) {
    auto opcode = pattern("0000 01.. .... ....");
    bind(opcode | null << 0, NOP);
  }

  //JMP imm
  for(uint8 data : range(256))
  for(uint1 null : range(  2))
  for(uint1 far  : range(  2)) {
    auto opcode = pattern("0000 10f. dddd dddd");
    bind(opcode | data << 0 | null << 8 | far << 9, JMP, data, far, 1);
  }

  //JMP EQ,imm
  for(uint8 data : range(256))
  for(uint1 null : range(  2))
  for(uint1 far  : range(  2)) {
    auto opcode = pattern("0000 11f. dddd dddd");
    bind(opcode | data << 0 | null << 8 | far << 9, JMP, data, far, r.z);
  }

  //JMP GE,imm
  for(uint8 data : range(256))
  for(uint1 null : range(  2))
  for(uint1 far  : range(  2)) {
    auto opcode = pattern("0001 00f. dddd dddd");
    bind(opcode | data << 0 | null << 8 | far << 9, JMP, data, far, r.c);
  }

  //JMP MI,imm
  for(uint8 data : range(256))
  for(uint1 null : range(  2))
  for(uint1 far  : range(  2)) {
    auto opcode = pattern("0001 01f. dddd dddd");
    bind(opcode | data << 0 | null << 8 | far << 9, JMP, data, far, r.n);
  }

  //JMP VS,imm
  for(uint8 data : range(256))
  for(uint1 null : range(  2))
  for(uint1 far  : range(  2)) {
    auto opcode = pattern("0001 10f. dddd dddd");
    bind(opcode | data << 0 | null << 8 | far << 9, JMP, data, far, r.v);
  }

  //WAIT
  for(uint10 null : range(1024)) {
    auto opcode = pattern("0001 11.. .... ....");
    bind(opcode | null << 0, WAIT);
  }

  //???
  for(uint10 null : range(1024)) {
    auto opcode = pattern("0010 00.. .... ....");
    bind(opcode | null << 0, NOP);
  }

  //SKIP V
  for(uint1 take : range(  2))
  for(uint7 null : range(128)) {
    auto opcode = pattern("0010 0100 .... ...t");
    bind(opcode | take << 0 | null << 1, SKIP, take, r.v);
  }

  //SKIP C
  for(uint1 take : range(  2))
  for(uint7 null : range(128)) {
    auto opcode = pattern("0010 0101 .... ...t");
    bind(opcode | take << 0 | null << 1, SKIP, take, r.c);
  }

  //SKIP Z
  for(uint1 take : range(  2))
  for(uint7 null : range(128)) {
    auto opcode = pattern("0010 0110 .... ...t");
    bind(opcode | take << 0 | null << 1, SKIP, take, r.z);
  }

  //SKIP N
  for(uint1 take : range(  2))
  for(uint7 null : range(128)) {
    auto opcode = pattern("0010 0111 .... ...t");
    bind(opcode | take << 0 | null << 1, SKIP, take, r.n);
  }

  //JSR
  for(uint8 data : range(256))
  for(uint1 null : range(  2))
  for(uint1 far  : range(  2)) {
    auto opcode = pattern("0010 10f. dddd dddd");
    bind(opcode | data << 0 | null << 8 | far << 9, JSR, data, far, 1);
  }

  //JSR EQ,imm
  for(uint8 data : range(256))
  for(uint1 null : range(  2))
  for(uint1 far  : range(  2)) {
    auto opcode = pattern("0010 11f. dddd dddd");
    bind(opcode | data << 0 | null << 8 | far << 9, JSR, data, far, r.z);
  }

  //JSR GE,imm
  for(uint8 data : range(256))
  for(uint1 null : range(  2))
  for(uint1 far  : range(  2)) {
    auto opcode = pattern("0011 00f. dddd dddd");
    bind(opcode | data << 0 | null << 8 | far << 9, JSR, data, far, r.c);
  }

  //JSR MI,imm
  for(uint8 data : range(256))
  for(uint1 null : range(  2))
  for(uint1 far  : range(  2)) {
    auto opcode = pattern("0011 01f. dddd dddd");
    bind(opcode | data << 0 | null << 8 | far << 9, JSR, data, far, r.n);
  }

  //JSR VS,imm
  for(uint8 data : range(256))
  for(uint1 null : range(  2))
  for(uint1 far  : range(  2)) {
    auto opcode = pattern("0011 10f. dddd dddd");
    bind(opcode | data << 0 | null << 8 | far << 9, JSR, data, far, r.v);
  }

  //RTS
  for(uint10 null : range(1024)) {
    auto opcode = pattern("0011 11.. .... ....");
    bind(opcode | null << 0, RTS);
  }

  //INC MAR
  for(uint10 null : range(1024)) {
    auto opcode = pattern("0100 00.. .... ....");
    bind(opcode | null << 0, INC, r.mar);
  }

  //???
  for(uint10 null : range(1024)) {
    auto opcode = pattern("0100 01.. .... ....");
    bind(opcode | null << 0, NOP);
  }

  //CMPR A<<s,reg
  for(uint7 reg   : range(128))
  for(uint1 null  : range(  2))
  for(uint2 shift : range(  4)) {
    auto opcode = pattern("0100 10ss .rrr rrrr");
    bind(opcode | reg << 0 | null << 7 | shift << 8, CMPR, reg, shifts[shift]);
  }

  //CMPR A<<s,imm
  for(uint8 imm   : range(256))
  for(uint2 shift : range(  4)) {
    auto opcode = pattern("0100 11ss iiii iiii");
    bind(opcode | imm << 0 | shift << 8, CMPR, imm, shifts[shift]);
  }

  //CMP A<<s,reg
  for(uint7 reg   : range(128))
  for(uint1 null  : range(  2))
  for(uint2 shift : range(  4)) {
    auto opcode = pattern("0101 00ss .rrr rrrr");
    bind(opcode | reg << 0 | null << 7 | shift << 8, CMP, reg, shifts[shift]);
  }

  //CMP A<<s,imm
  for(uint8 imm   : range(256))
  for(uint2 shift : range(  4)) {
    auto opcode = pattern("0101 01ss iiii iiii");
    bind(opcode | imm << 0 | shift << 8, CMP, imm, shifts[shift]);
  }

  //???
  for(uint8 null : range(256)) {
    auto opcode = pattern("0101 1000 .... ....");
    bind(opcode | null << 0, NOP);
  }

  //SXB A
  for(uint8 null : range(256)) {
    auto opcode = pattern("0101 1001 .... ....");
    bind(opcode | null << 0, SXB);
  }

  //SXW A
  for(uint8 null : range(256)) {
    auto opcode = pattern("0101 1010 .... ....");
    bind(opcode | null << 0, SXW);
  }

  //???
  for(uint8 null : range(256)) {
    auto opcode = pattern("0101 1011 .... ....");
    bind(opcode | null << 0, NOP);
  }

  //???
  for(uint10 null : range(1024)) {
    auto opcode = pattern("0101 11.. .... ....");
    bind(opcode | null << 0, NOP);
  }

  //LD A,reg
  for(uint7 reg  : range(128))
  for(uint1 null : range(  2)) {
    auto opcode = pattern("0110 0000 .rrr rrrr");
    bind(opcode | reg << 0 | null << 7, LD, r.a, reg);
  }

  //LD MDR,reg
  for(uint7 reg  : range(128))
  for(uint1 null : range(  2)) {
    auto opcode = pattern("0110 0001 .rrr rrrr");
    bind(opcode | reg << 0 | null << 7, LD, r.mdr, reg);
  }

  //LD MAR,reg
  for(uint7 reg  : range(128))
  for(uint1 null : range(  2)) {
    auto opcode = pattern("0110 0010 .rrr rrrr");
    bind(opcode | reg << 0 | null << 7, LD, r.mar, reg);
  }

  //LD P,reg
  for(uint4 reg  : range(16))
  for(uint4 null : range(16)) {
    auto opcode = pattern("0110 0011 .... rrrr");
    bind(opcode | reg << 0 | null << 4, LD, r.p, reg);
  }

  //LD A,imm
  for(uint8 imm : range(256)) {
    auto opcode = pattern("0110 0100 iiii iiii");
    bind(opcode | imm << 0, LD, r.a, imm);
  }

  //LD MDR,imm
  for(uint8 imm : range(256)) {
    auto opcode = pattern("0110 0101 iiii iiii");
    bind(opcode | imm << 0, LD, r.mdr, imm);
  }

  //LD MAR,imm
  for(uint8 imm : range(256)) {
    auto opcode = pattern("0110 0110 iiii iiii");
    bind(opcode | imm << 0, LD, r.mar, imm);
  }

  //LD P,imm
  for(uint8 imm : range(256)) {
    auto opcode = pattern("0110 0111 iiii iiii");
    bind(opcode | imm << 0, LD, r.p, imm);
  }

  //RDRAM 0,A
  for(uint8 null : range(256)) {
    auto opcode = pattern("0110 1000 .... ....");
    bind(opcode | null << 0, RDRAM, 0, r.a);
  }

  //RDRAM 1,A
  for(uint8 null : range(256)) {
    auto opcode = pattern("0110 1001 .... ....");
    bind(opcode | null << 0, RDRAM, 1, r.a);
  }

  //RDRAM 2,A
  for(uint8 null : range(256)) {
    auto opcode = pattern("0110 1010 .... ....");
    bind(opcode | null << 0, RDRAM, 2, r.a);
  }

  //???
  for(uint8 null : range(256)) {
    auto opcode = pattern("0110 1011 .... ....");
    bind(opcode | null << 0, NOP);
  }

  //RDRAM 0,imm
  for(uint8 imm : range(256)) {
    auto opcode = pattern("0110 1100 iiii iiii");
    bind(opcode | imm << 0, RDRAM, 0, imm);
  }

  //RDRAM 1,imm
  for(uint8 imm : range(256)) {
    auto opcode = pattern("0110 1101 iiii iiii");
    bind(opcode | imm << 0, RDRAM, 1, imm);
  }

  //RDRAM 2,imm
  for(uint8 imm : range(256)) {
    auto opcode = pattern("0110 1110 iiii iiii");
    bind(opcode | imm << 0, RDRAM, 2, imm);
  }

  //???
  for(uint8 null : range(256)) {
    auto opcode = pattern("0110 1111 .... ....");
    bind(opcode | null << 0, NOP);
  }

  //RDROM A
  for(uint10 null : range(1024)) {
    auto opcode = pattern("0111 00.. .... ....");
    bind(opcode | null << 0, RDROM, r.a);
  }

  //RDROM imm
  for(uint10 imm : range(1024)) {
    auto opcode = pattern("0111 01ii iiii iiii");
    bind(opcode | imm << 0, RDROM, imm);
  }

  //???
  for(uint10 null : range(1024)) {
    auto opcode = pattern("0111 10.. .... ....");
    bind(opcode | null << 0, NOP);
  }

  //LD PL,imm
  for(uint8 imm : range(256)) {
    auto opcode = pattern("0111 1100 iiii iiii");
    bind(opcode | imm << 0, LDL, r.p, imm);
  }

  //LD PH,imm
  for(uint7 imm  : range(128))
  for(uint1 null : range(  2)) {
    auto opcode = pattern("0111 1101 .iii iiii");
    bind(opcode | imm << 0 | null << 7, LDH, r.p, imm);
  }

  //???
  for(uint9 null : range(512)) {
    auto opcode = pattern("0111 111. .... ....");
    bind(opcode | null << 0, NOP);
  }

  //ADD A<<s,reg
  for(uint7 reg   : range(128))
  for(uint1 null  : range(  2))
  for(uint2 shift : range(  4)) {
    auto opcode = pattern("1000 00ss .rrr rrrr");
    bind(opcode | reg << 0 | null << 7 | shift << 8, ADD, reg, shifts[shift]);
  }

  //ADD A<<s,imm
  for(uint8 imm   : range(256))
  for(uint2 shift : range(  4)) {
    auto opcode = pattern("1000 01ss iiii iiii");
    bind(opcode | imm << 0 | shift << 8, ADD, imm, shifts[shift]);
  }

  //SUBR A<<s,reg
  for(uint7 reg   : range(128))
  for(uint1 null  : range(  2))
  for(uint2 shift : range(  4)) {
    auto opcode = pattern("1000 10ss .rrr rrrr");
    bind(opcode | reg << 0 | null << 7 | shift << 8, SUBR, reg, shifts[shift]);
  }

  //SUBR A<<s,imm
  for(uint8 imm   : range(256))
  for(uint2 shift : range(  4)) {
    auto opcode = pattern("1000 11ss iiii iiii");
    bind(opcode | imm << 0 | shift << 8, SUBR, imm, shifts[shift]);
  }

  //SUB A<<s,reg
  for(uint7 reg   : range(128))
  for(uint1 null  : range(  2))
  for(uint2 shift : range(  4)) {
    auto opcode = pattern("1001 00ss .rrr rrrr");
    bind(opcode | reg << 0 | null << 7 | shift << 8, SUB, reg, shifts[shift]);
  }

  //SUB A<<s,imm
  for(uint8 imm   : range(256))
  for(uint2 shift : range(  4)) {
    auto opcode = pattern("1001 01ss iiii iiii");
    bind(opcode | imm << 0 | shift << 8, SUB, imm, shifts[shift]);
  }

  //MUL reg
  for(uint7 reg  : range(128))
  for(uint3 null : range(  8)) {
    auto opcode = pattern("1001 10.. .rrr rrrr");
    bind(opcode | reg << 0 | null << 7, MUL, reg);
  }

  //MUL imm
  for(uint8 imm  : range(256))
  for(uint2 null : range(  4)) {
    auto opcode = pattern("1001 11.. iiii iiii");
    bind(opcode | imm << 0 | null << 8, MUL, imm);
  }

  //XNOR A<<s,reg
  for(uint7 reg   : range(128))
  for(uint1 null  : range(  2))
  for(uint2 shift : range(  4)) {
    auto opcode = pattern("1010 00ss .rrr rrrr");
    bind(opcode | reg << 0 | null << 7 | shift << 8, XNOR, reg, shifts[shift]);
  }

  //XNOR A<<s,imm
  for(uint8 imm   : range(256))
  for(uint2 shift : range(  4)) {
    auto opcode = pattern("1010 01ss iiii iiii");
    bind(opcode | imm << 0 | shift << 8, XNOR, imm, shifts[shift]);
  }

  //XOR A<<s,reg
  for(uint7 reg   : range(128))
  for(uint1 null  : range(  2))
  for(uint2 shift : range(  4)) {
    auto opcode = pattern("1010 10ss .rrr rrrr");
    bind(opcode | reg << 0 | null << 7 | shift << 8, XOR, reg, shifts[shift]);
  }

  //XOR A<<s,imm
  for(uint8 imm   : range(256))
  for(uint2 shift : range(  4)) {
    auto opcode = pattern("1010 11ss iiii iiii");
    bind(opcode | imm << 0 | shift << 8, XOR, imm, shifts[shift]);
  }

  //AND A<<s,reg
  for(uint7 reg   : range(128))
  for(uint1 null  : range(  2))
  for(uint2 shift : range(  4)) {
    auto opcode = pattern("1011 00ss .rrr rrrr");
    bind(opcode | reg << 0 | null << 7 | shift << 8, AND, reg, shifts[shift]);
  }

  //AND A<<s,imm
  for(uint8 imm   : range(256))
  for(uint2 shift : range(  4)) {
    auto opcode = pattern("1011 01ss iiii iiii");
    bind(opcode | imm << 0 | shift << 8, AND, imm, shifts[shift]);
  }

  //OR A<<s,reg
  for(uint7 reg   : range(128))
  for(uint1 null  : range(  2))
  for(uint2 shift : range(  4)) {
    auto opcode = pattern("1011 10ss .rrr rrrr");
    bind(opcode | reg << 0 | null << 7 | shift << 8, OR, reg, shifts[shift]);
  }

  //OR A<<s,imm
  for(uint8 imm   : range(256))
  for(uint2 shift : range(  4)) {
    auto opcode = pattern("1011 11ss iiii iiii");
    bind(opcode | imm << 0 | shift << 8, OR, imm, shifts[shift]);
  }

  //SHR A,reg
  for(uint7 reg  : range(128))
  for(uint3 null : range(  8)) {
    auto opcode = pattern("1100 00.. .rrr rrrr");
    bind(opcode | reg << 0 | null << 7, SHR, reg);
  }

  //SHR A,imm
  for(uint5 imm  : range(32))
  for(uint5 null : range(32)) {
    auto opcode = pattern("1100 01.. ...i iiii");
    bind(opcode | imm << 0 | null << 5, SHR, imm);
  }

  //ASR A,reg
  for(uint7 reg  : range(128))
  for(uint3 null : range(  8)) {
    auto opcode = pattern("1100 10.. .rrr rrrr");
    bind(opcode | reg << 0 | null << 7, ASR, reg);
  }

  //ASR A,imm
  for(uint5 imm  : range(32))
  for(uint5 null : range(32)) {
    auto opcode = pattern("1100 11.. ...i iiii");
    bind(opcode | imm << 0 | null << 5, ASR, imm);
  }

  //ROR A,reg
  for(uint7 reg  : range(128))
  for(uint3 null : range(  8)) {
    auto opcode = pattern("1101 00.. .rrr rrrr");
    bind(opcode | reg << 0 | null << 7, ROR, reg);
  }

  //ROR A,imm
  for(uint5 imm  : range(32))
  for(uint5 null : range(32)) {
    auto opcode = pattern("1101 01.. ...i iiii");
    bind(opcode | imm << 0 | null << 5, ROR, imm);
  }

  //SHL A,reg
  for(uint7 reg  : range(128))
  for(uint3 null : range(  8)) {
    auto opcode = pattern("1101 10.. .rrr rrrr");
    bind(opcode | reg << 0 | null << 7, SHL, reg);
  }

  //SHL A,imm
  for(uint5 imm  : range(32))
  for(uint5 null : range(32)) {
    auto opcode = pattern("1101 11.. ...i iiii");
    bind(opcode | imm << 0 | null << 5, SHL, imm);
  }

  //ST reg,A
  for(uint7 reg  : range(128))
  for(uint1 null : range(  2)) {
    auto opcode = pattern("1110 0000 .rrr rrrr");
    bind(opcode | reg << 0 | null << 7, ST, reg, r.a);
  }

  //ST reg,MDR
  for(uint7 reg  : range(128))
  for(uint1 null : range(  2)) {
    auto opcode = pattern("1110 0001 .rrr rrrr");
    bind(opcode | reg << 0 | null << 7, ST, reg, r.mdr);
  }

  //???
  for(uint9 null : range(512)) {
    auto opcode = pattern("1110 001. .... ....");
    bind(opcode | null << 0, NOP);
  }

  //???
  for(uint10 null : range(1024)) {
    auto opcode = pattern("1110 01.. .... ....");
    bind(opcode | null << 0, NOP);
  }

  //WRRAM 0,A
  for(uint8 null : range(256)) {
    auto opcode = pattern("1110 1000 .... ....");
    bind(opcode | null << 0, WRRAM, 0, r.a);
  }

  //WRRAM 1,A
  for(uint8 null : range(256)) {
    auto opcode = pattern("1110 1001 .... ....");
    bind(opcode | null << 0, WRRAM, 1, r.a);
  }

  //WRRAM 2,A
  for(uint8 null : range(256)) {
    auto opcode = pattern("1110 1010 .... ....");
    bind(opcode | null << 0, WRRAM, 2, r.a);
  }

  //???
  for(uint8 null : range(256)) {
    auto opcode = pattern("1110 1011 .... ....");
    bind(opcode | null << 0, NOP);
  }

  //WRRAM 0,imm
  for(uint8 imm : range(256)) {
    auto opcode = pattern("1110 1100 iiii iiii");
    bind(opcode | imm << 0, WRRAM, 0, imm);
  }

  //WRRAM 1,imm
  for(uint8 imm : range(256)) {
    auto opcode = pattern("1110 1101 iiii iiii");
    bind(opcode | imm << 0, WRRAM, 1, imm);
  }

  //WRRAM 2,imm
  for(uint8 imm : range(256)) {
    auto opcode = pattern("1110 1110 iiii iiii");
    bind(opcode | imm << 0, WRRAM, 2, imm);
  }

  //???
  for(uint8 null : range(256)) {
    auto opcode = pattern("1110 1111 .... ....");
    bind(opcode | null << 0, NOP);
  }

  //SWAP A,Rn
  for(uint4 reg  : range(16))
  for(uint6 null : range(64)) {
    auto opcode = pattern("1111 00.. .... rrrr");
    bind(opcode | reg << 0 | null << 4, SWAP, r.a, reg);
  }

  //???
  for(uint10 null : range(1024)) {
    auto opcode = pattern("1111 01.. .... ....");
    bind(opcode | null << 0, NOP);
  }

  //CLEAR
  for(uint10 null : range(1024)) {
    auto opcode = pattern("1111 10.. .... ....");
    bind(opcode | null << 0, CLEAR);
  }

  //HALT
  for(uint10 null : range(1024)) {
    auto opcode = pattern("1111 11.. .... ....");
    bind(opcode | null << 0, HALT);
  }

  #undef bind
  #undef pattern

  for(uint opcode : range(65536)) {
    if(!instructionTable[opcode]) throw;
  }
}
