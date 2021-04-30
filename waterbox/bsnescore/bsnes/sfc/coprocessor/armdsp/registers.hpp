struct Bridge {
  struct Buffer {
    bool ready;
    uint8 data;
  };
  Buffer cputoarm;
  Buffer armtocpu;
  uint32 timer;
  uint32 timerlatch;
  bool reset;
  bool ready;
  bool signal;

  auto status() const -> uint8 {
    return (
      armtocpu.ready << 0
    | signal         << 2
    | cputoarm.ready << 3
    | ready          << 7
    );
  }
} bridge;
