auto SM5K::disassembleInstruction() -> string {
  string s;

  n8 opcode  = ROM[PC + 0 & sizeof(ROM) - 1];
  n8 operand = ROM[PC + 1 & sizeof(ROM) - 1];

  string p2 = {"0x", hex(n2(opcode), 1L)};
  string p4 = {"0x", hex(n4(opcode), 1L)};
  string p5 = {"0x", hex(n5(opcode), 2L)};
  string p6 = {"0x", hex(n6(opcode), 2L)};
  string p8 = {"0x", hex(n8(operand), 2L)};
  string pc = {"0x", hex(n4(opcode) << 8 | operand, 3L)};

  switch(opcode) {
  case 0x00 ... 0x0f: s = {"adx  ", p4}; break;
  case 0x10 ... 0x1f: s = {"lax  ", p4}; break;
  case 0x20 ... 0x2f: s = {"lblx ", p4}; break;
  case 0x30 ... 0x3f: s = {"lbmx ", p4}; break;
  case 0x40 ... 0x43: s = {"rm   ", p2}; break;
  case 0x44 ... 0x47: s = {"sm   ", p2}; break;
  case 0x48 ... 0x4b: s = {"tm   ", p2}; break;
  case 0x4c ... 0x4f: s = {"tpb  ", p2}; break;
  case 0x50 ... 0x53: s = {"lda  ", p2}; break;
  case 0x54 ... 0x57: s = {"exc  ", p2}; break;
  case 0x58 ... 0x5b: s = {"exci ", p2}; break;
  case 0x5c ... 0x5f: s = {"excd ", p2}; break;
  case 0x60:          s = {"rc   "    }; break;
  case 0x61:          s = {"sc   "    }; break;
  case 0x62:          s = {"id   "    }; break;
  case 0x63:          s = {"ie   "    }; break;
  case 0x64:          s = {"exax "    }; break;
  case 0x65:          s = {"atx  "    }; break;
  case 0x66:          s = {"exbm "    }; break;
  case 0x67:          s = {"exbl "    }; break;
  case 0x68:          s = {"ex   "    }; break;
  case 0x69:          s = {"dta  ", p8}; break;
  case 0x6a:          s = {"pat  ", p8}; break;
  case 0x6b:          s = {"tabl "    }; break;
  case 0x6c:          s = {"ta   "    }; break;
  case 0x6d:          s = {"tb   "    }; break;
  case 0x6e:          s = {"tc   "    }; break;
  case 0x6f:          s = {"tam  "    }; break;
  case 0x70:          s = {"inl  "    }; break;
  case 0x71:          s = {"outl "    }; break;
  case 0x72:          s = {"anp  "    }; break;
  case 0x73:          s = {"orp  "    }; break;
  case 0x74:          s = {"in   "    }; break;
  case 0x75:          s = {"out  "    }; break;
  case 0x76:          s = {"stop "    }; break;
  case 0x77:          s = {"halt "    }; break;
  case 0x78:          s = {"incb "    }; break;
  case 0x79:          s = {"coma "    }; break;
  case 0x7a:          s = {"add  "    }; break;
  case 0x7b:          s = {"adc  "    }; break;
  case 0x7c:          s = {"decb "    }; break;
  case 0x7d:          s = {"rtn  "    }; break;
  case 0x7e:          s = {"rtns "    }; break;
  case 0x7f:          s = {"rtni "    }; break;
  case 0x80 ... 0xbf: s = {"tr   ", p6}; break;
  case 0xc0 ... 0xdf: s = {"trs  ", p5}; break;
  case 0xe0 ... 0xef: s = {"tl   ", pc}; break;
  case 0xf0 ... 0xff: s = {"call ", pc}; break;
  }

  while(s.size() < 10) s.append(" ");
  return s;
}

auto SM5K::disassembleContext() -> string {
  string s;
  s.append("A:",    hex(A,    1L), " ");
  s.append("X:",    hex(X,    1L), " ");
  s.append("B:",    hex(B,    2L), " ");
  s.append("C:",    hex(C,    1L), " ");
  s.append("P0:",   hex(P0,   1L), " ");
  s.append("P1:",   hex(P1,   1L), " ");
  s.append("P2:",   hex(P2,   1L), " ");
  s.append("P3:",   hex(P3,   1L), " ");
  s.append("P4:",   hex(P4,   1L), " ");
  s.append("P5:",   hex(P5,   1L), " ");
  s.append("SP:",   hex(SP,   1L), " ");
  s.append("SB:",   hex(SB,   2L), " ");
  s.append("IFA:",  hex(IFA,  1L), " ");
  s.append("IFB:",  hex(IFB,  1L), " ");
  s.append("IFT:",  hex(IFT,  1L), " ");
  s.append("IME:",  hex(IME,  1L), " ");
  s.append("SKIP:", hex(SKIP, 1L));
  return s;
}
