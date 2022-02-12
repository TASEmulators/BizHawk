#define op(id, name, ...) \
  case id: \
    if(SKIP) { SKIP = 0; return; } \
    return instruction##name(__VA_ARGS__); \

auto SM5K::interrupt(n2 id) -> void {
  SR[SP++] = PC;
  PU = 2;
  PL = id << 1;
  HALT = 0;
  STOP = 0;
}

auto SM5K::instruction() -> void {
  if(IFA & RE.bit(0) & IME) return interrupt(0);
  if(IFB & RE.bit(1) & IME) return interrupt(1);
  if(IFT & RE.bit(2) & IME) return interrupt(2);
  if(HALT) return timerStep();
  if(STOP) return timerStep();

  n8 opcode = fetch();
  switch(opcode) {
  op(0x00 ... 0x0f, ADX,  n4(opcode));
  op(0x10 ... 0x1f, LAX,  n4(opcode));
  op(0x20 ... 0x2f, LBLX, n4(opcode));
  op(0x30 ... 0x3f, LBMX, n4(opcode));
  op(0x40 ... 0x43, RM,   n2(opcode));
  op(0x44 ... 0x47, SM,   n2(opcode));
  op(0x48 ... 0x4b, TM,   n2(opcode));
  op(0x4c ... 0x4f, TPB,  n2(opcode));
  op(0x50 ... 0x53, LDA,  n2(opcode));
  op(0x54 ... 0x57, EXC,  n2(opcode));
  op(0x58 ... 0x5b, EXCI, n2(opcode));
  op(0x5c ... 0x5f, EXCD, n2(opcode));
  op(0x60,          RC    );
  op(0x61,          SC    );
  op(0x62,          ID    );
  op(0x63,          IE    );
  op(0x64,          EXAX  );
  op(0x65,          ATX   );
  op(0x66,          EXBM  );
  op(0x67,          EXBL  );
  op(0x68,          EX    );
  op(0x69,          DTA,  fetch());
  op(0x6a,          PAT,  fetch());
  op(0x6b,          TABL  );
  op(0x6c,          TA    );
  op(0x6d,          TB    );
  op(0x6e,          TC    );
  op(0x6f,          TAM   );
  op(0x70,          INL   );
  op(0x71,          OUTL  );
  op(0x72,          ANP   );
  op(0x73,          ORP   );
  op(0x74,          IN    );
  op(0x75,          OUT   );
  op(0x76,          STOP  );
  op(0x77,          HALT  );
  op(0x78,          INCB  );
  op(0x79,          COMA  );
  op(0x7a,          ADD   );
  op(0x7b,          ADC   );
  op(0x7c,          DECB  );
  op(0x7d,          RTN   );
  op(0x7e,          RTNS  );
  op(0x7f,          RTNI  );
  op(0x80 ... 0xbf, TR,   n6(opcode));
  op(0xc0 ... 0xdf, TRS,  n5(opcode));
  op(0xe0 ... 0xef, TL,   n4(opcode) << 8 | fetch());
  op(0xf0 ... 0xff, CALL, n4(opcode) << 8 | fetch());
  }
}

#undef op
