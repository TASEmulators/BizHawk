struct Decompressor {
  struct IM {  //input manager
    IM(SDD1::Decompressor& self) : self(self) {}
    auto init(uint offset) -> void;
    auto getCodeWord(uint8 codeLength) -> uint8;
    auto serialize(serializer&) -> void;

  private:
    Decompressor& self;
    uint offset;
    uint bitCount;
  };

  struct GCD {  //golomb-code decoder
    GCD(SDD1::Decompressor& self) : self(self) {}
    auto getRunCount(uint8 codeNumber, uint8& mpsCount, bool& lpsIndex) -> void;
    auto serialize(serializer&) -> void;

  private:
    Decompressor& self;
    static const uint8 runCount[256];
  };

  struct BG {  //bits generator
    BG(SDD1::Decompressor& self, uint8 codeNumber) : self(self), codeNumber(codeNumber) {}
    auto init() -> void;
    auto getBit(bool& endOfRun) -> uint8;
    auto serialize(serializer&) -> void;

  private:
    Decompressor& self;
    const uint8 codeNumber;
    uint8 mpsCount;
    bool lpsIndex;
  };

  struct PEM {  //probability estimation module
    PEM(SDD1::Decompressor& self) : self(self) {}
    auto init() -> void;
    auto getBit(uint8 context) -> uint8;
    auto serialize(serializer&) -> void;

  private:
    Decompressor& self;
    struct State {
      uint8 codeNumber;
      uint8 nextIfMps;
      uint8 nextIfLps;
    };
    static const State evolutionTable[33];
    struct ContextInfo {
      uint8 status;
      uint8 mps;
    } contextInfo[32];
  };

  struct CM {  //context model
    CM(SDD1::Decompressor& self) : self(self) {}
    auto init(uint offset) -> void;
    auto getBit() -> uint8;
    auto serialize(serializer&) -> void;

  private:
    Decompressor& self;
    uint8 bitplanesInfo;
    uint8 contextBitsInfo;
    uint8 bitNumber;
    uint8 currentBitplane;
    uint16 previousBitplaneBits[8];
  };

  struct OL {  //output logic
    OL(SDD1::Decompressor& self) : self(self) {}
    auto init(uint offset) -> void;
    auto decompress() -> uint8;
    auto serialize(serializer&) -> void;

  private:
    Decompressor& self;
    uint8 bitplanesInfo;
    uint8 r0, r1, r2;
  };

  Decompressor();
  auto init(uint offset) -> void;
  auto read() -> uint8;
  auto serialize(serializer&) -> void;

  IM  im;
  GCD gcd;
  BG  bg0, bg1, bg2, bg3, bg4, bg5, bg6, bg7;
  PEM pem;
  CM  cm;
  OL  ol;
};
