struct DSP4 {
  auto power() -> void;

  auto read(uint addr, uint8 data) -> uint8;
  auto write(uint addr, uint8 data) -> void;

  auto serialize(serializer&) -> void;
};

extern DSP4 dsp4;
