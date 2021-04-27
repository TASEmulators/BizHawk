struct Satellaview : Expansion {
  Satellaview();
  ~Satellaview();

  auto read(uint addr, uint8 data) -> uint8;
  auto write(uint addr, uint8 data) -> void;

private:
  struct {
    uint8 r2188, r2189, r218a, r218b;
    uint8 r218c, r218d, r218e, r218f;
    uint8 r2190, r2191, r2192, r2193;
    uint8 r2194, r2195, r2196, r2197;
    uint8 r2198, r2199, r219a, r219b;
    uint8 r219c, r219d, r219e, r219f;

    uint8 rtcCounter;
    uint8 rtcHour;
    uint8 rtcMinute;
    uint8 rtcSecond;
  } regs;
};
