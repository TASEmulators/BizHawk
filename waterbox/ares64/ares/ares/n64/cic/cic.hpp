
struct CIC {
  enum State : u32 { BootRegion, BootSeed, BootChecksum, Run, Challenge, Dead };
  enum Region : u32 { NTSC, PAL };
  enum ChallengeAlgo : bool { DummyChallenge, RealChallenge };
  enum Type : u32 { Cartridge, DD64 };

  struct {
    nall::queue<n1> bits;

    auto empty() -> bool { return bits.empty(); }
    auto size() -> u32 { return bits.size(); }
    auto write(n1 data) -> void { bits.write(data); }
    auto read() -> n1 { return bits.read(); }
    auto writeNibble(n4 data) -> void {
      write(data.bit(3));
      write(data.bit(2));
      write(data.bit(1));
      write(data.bit(0));
    }
    auto readNibble() -> n4 {
      n4 data;
      data.bit(3) = read();
      data.bit(2) = read();
      data.bit(1) = read();
      data.bit(0) = read();
      return data;
    }
  } fifo;
  n8 seed;
  n48 checksum; //ipl2 checksum
  n1 type;
  n1 region;
  n1 challengeAlgo;
  u32 state;
  string model;

  //cic.cpp
  auto power(bool reset) -> void;
  auto poll() -> void;
  auto scramble(n4 *buf, int size) -> void;

  //io.cpp
  auto readBit() -> n1;
  auto readNibble() -> n4;
  auto writeBit(n1 cmd) -> void;
  auto writeNibble(n4 cmd) -> void;

  //commands.cpp
  auto cmdCompare() -> void;
  auto cmdDie() -> void;
  auto cmdChallenge() -> void;
  auto cmdReset() -> void;
  auto challenge(n4 data[30]) -> void;

  //serialization.cpp
  auto serialize(serializer&) -> void;
};

extern CIC cic;
