struct DSP2 {
  auto power() -> void;

  auto read(uint addr, uint8 data) -> uint8;
  auto write(uint addr, uint8 data) -> void;

  auto serialize(serializer&) -> void;

  struct {
    bool waiting_for_command;
    unsigned command;
    unsigned in_count,  in_index;
    unsigned out_count, out_index;

    uint8_t  parameters[512];
    uint8_t  output[512];

    uint8  op05transparent;
    bool   op05haslen;
    int    op05len;
    bool   op06haslen;
    int    op06len;
    uint16 op09word1;
    uint16 op09word2;
    bool   op0dhaslen;
    int    op0doutlen;
    int    op0dinlen;
  } status;

  void op01();
  void op03();
  void op05();
  void op06();
  void op09();
  void op0d();
};

extern DSP2 dsp2;
