//performance-focused, scanline-based, parallelized implementation of PPU

//limitations:
//* mid-scanline effects not support
//* vertical mosaic coordinates are not exact
//* (hardware-mod) 128KB VRAM mode not supported

#define PPU PPUfast

struct PPU : PPUcounter {
  alwaysinline auto interlace() const -> bool;
  alwaysinline auto overscan() const -> bool;
  alwaysinline auto vdisp() const -> uint;
  alwaysinline auto hires() const -> bool;
  alwaysinline auto hd() const -> bool;
  alwaysinline auto ss() const -> bool;
  alwaysinline auto hdScale() const -> uint;
  alwaysinline auto hdPerspective() const -> bool;
  alwaysinline auto hdSupersample() const -> bool;
  alwaysinline auto hdMosaic() const -> bool;
  alwaysinline auto deinterlace() const -> bool;
  alwaysinline auto renderCycle() const -> uint;
  alwaysinline auto noVRAMBlocking() const -> bool;

  //ppu.cpp
  PPU();
  ~PPU();

  auto synchronizeCPU() -> void;
  static auto Enter() -> void;
  alwaysinline auto step(uint clocks) -> void;
  auto main() -> void;
  auto scanline() -> void;
  auto refresh() -> void;
  auto load() -> bool;
  auto power(bool reset) -> void;

  //serialization.cpp
  auto serialize(serializer&) -> void;

public:
  struct Source { enum : uint8 { BG1, BG2, BG3, BG4, OBJ1, OBJ2, COL }; };
  struct TileMode { enum : uint8 { BPP2, BPP4, BPP8, Mode7, Inactive }; };
  struct ScreenMode { enum : uint8 { Above, Below }; };

  struct Latch {
    //serialization.cpp
    auto serialize(serializer&) -> void;

    bool interlace = 0;
    bool overscan = 0;
    bool hires = 0;
    bool hd = 0;
    bool ss = 0;

    uint16 vram = 0;
    uint8 oam = 0;
    uint8 cgram = 0;

    uint16 oamAddress = 0;
    uint8 cgramAddress = 0;

    uint8 mode7 = 0;
    bool counters = 0;
    bool hcounter = 0;  //hdot
    bool vcounter = 0;

    struct PPUstate {
      //serialization.cpp
      auto serialize(serializer&) -> void;

      uint8 mdr = 0;
      uint8 bgofs = 0;
    } ppu1, ppu2;
  };

  struct IO {
    //serialization.cpp
    auto serialize(serializer&) -> void;

    bool displayDisable = 1;
    uint8 displayBrightness = 0;
    uint16 oamBaseAddress = 0;
    uint16 oamAddress = 0;
    bool oamPriority = 0;
    bool bgPriority = 0;
    uint8 bgMode = 0;
    bool vramIncrementMode = 0;
    uint8 vramMapping = 0;
    uint8 vramIncrementSize = 0;
    uint16 vramAddress = 0;
    uint8 cgramAddress = 0;
    bool cgramAddressLatch = 0;
    uint16 hcounter = 0;  //hdot
    uint16 vcounter = 0;
    bool interlace = 0;
    bool overscan = 0;
    bool pseudoHires = 0;
    bool extbg = 0;

    struct Mosaic {
      //serialization.cpp
      auto serialize(serializer&) -> void;

      uint8 size = 1;
      uint8 counter = 0;
    } mosaic;

    struct Mode7 {
      //serialization.cpp
      auto serialize(serializer&) -> void;

      bool hflip = 0;
      bool vflip = 0;
      uint repeat = 0;
      uint16 a = 0;
      uint16 b = 0;
      uint16 c = 0;
      uint16 d = 0;
      uint16 x = 0;
      uint16 y = 0;
      uint16 hoffset = 0;
      uint16 voffset = 0;
    } mode7;

    struct Window {
      //serialization.cpp
      auto serialize(serializer&) -> void;

      uint8 oneLeft = 0;
      uint8 oneRight = 0;
      uint8 twoLeft = 0;
      uint8 twoRight = 0;
    } window;

    struct WindowLayer {
      //serialization.cpp
      auto serialize(serializer&) -> void;

      bool oneEnable = 0;
      bool oneInvert = 0;
      bool twoEnable = 0;
      bool twoInvert = 0;
      uint mask = 0;
      bool aboveEnable = 0;
      bool belowEnable = 0;
    };

    struct WindowColor {
      //serialization.cpp
      auto serialize(serializer&) -> void;

      bool oneEnable = 0;
      bool oneInvert = 0;
      bool twoEnable = 0;
      bool twoInvert = 0;
      uint mask = 0;
      uint aboveMask = 0;
      uint belowMask = 0;
    };

    struct Background {
      //serialization.cpp
      auto serialize(serializer&) -> void;

      WindowLayer window;

      bool aboveEnable = 0;
      bool belowEnable = 0;
      bool mosaicEnable = 0;
      uint16 tiledataAddress = 0;
      uint16 screenAddress = 0;
      uint8 screenSize = 0;
      bool tileSize = 0;
      uint16 hoffset = 0;
      uint16 voffset = 0;
      uint8 tileMode = 0;
      uint8 priority[2] = {};
    } bg1, bg2, bg3, bg4;

    struct Object {
      //serialization.cpp
      auto serialize(serializer&) -> void;

      WindowLayer window;

      bool aboveEnable = 0;
      bool belowEnable = 0;
      bool interlace = 0;
      uint8 baseSize = 0;
      uint8 nameselect = 0;
      uint16 tiledataAddress = 0;
      uint8 first = 0;
      bool rangeOver = 0;
      bool timeOver = 0;
      uint8 priority[4] = {};
    } obj;

    struct Color {
      //serialization.cpp
      auto serialize(serializer&) -> void;

      WindowColor window;

      bool enable[7] = {};
      bool directColor = 0;
      bool blendMode = 0;  //0 = fixed; 1 = pixel
      bool halve = 0;
      bool mathMode = 0;   //0 = add; 1 = sub
      uint16 fixedColor = 0;
    } col;
  };

  struct Object {
    //serialization.cpp
    auto serialize(serializer&) -> void;

    uint16 x = 0;
    uint8 y = 0;
    uint8 character = 0;
    bool nameselect = 0;
    bool vflip = 0;
    bool hflip = 0;
    uint8 priority = 0;
    uint8 palette = 0;
    bool size = 0;
  };

  struct ObjectItem {
    bool valid = 0;
    uint8 index = 0;
    uint8 width = 0;
    uint8 height = 0;
  };

  struct ObjectTile {
    bool valid = 0;
    uint16 x = 0;
    uint8 y = 0;
    uint8 priority = 0;
    uint8 palette = 0;
    bool hflip = 0;
    uint32 data = 0;
  };

  struct Pixel {
    uint8 source = 0;
    uint8 priority = 0;
    uint16 color = 0;
  };

  //io.cpp
  auto latchCounters(uint hcounter, uint vcounter) -> void;
  auto latchCounters() -> void;
  alwaysinline auto vramAddress() const -> uint;
  alwaysinline auto readVRAM() -> uint16;
  template<bool Byte> alwaysinline auto writeVRAM(uint8 data) -> void;
  alwaysinline auto readOAM(uint10 address) -> uint8;
  alwaysinline auto writeOAM(uint10 address, uint8 data) -> void;
  template<bool Byte> alwaysinline auto readCGRAM(uint8 address) -> uint8;
  alwaysinline auto writeCGRAM(uint8 address, uint15 data) -> void;
  auto readIO(uint address, uint8 data) -> uint8;
  auto writeIO(uint address, uint8 data) -> void;
  auto updateVideoMode() -> void;

  //object.cpp
  auto oamAddressReset() -> void;
  auto oamSetFirstObject() -> void;
  auto readObject(uint10 address) -> uint8;
  auto writeObject(uint10 address, uint8 data) -> void;

//serialized:
  Latch latch;
  IO io;

  uint16 vram[32 * 1024] = {};
  uint16 cgram[256] = {};
  Object objects[128] = {};

  //[unserialized]
  uint16* output = {};
  uint16* lightTable[16] = {};

  uint ItemLimit = 0;
  uint TileLimit = 0;

  struct Line {
    //line.cpp
    inline auto field() const -> bool { return fieldID; }
    static auto flush() -> void;
    auto cache() -> void;
    auto render(bool field) -> void;
    auto pixel(uint x, Pixel above, Pixel below) const -> uint16;
    auto blend(uint x, uint y, bool halve) const -> uint16;
    alwaysinline auto directColor(uint paletteIndex, uint paletteColor) const -> uint16;
    alwaysinline auto plotAbove(uint x, uint8 source, uint8 priority, uint16 color) -> void;
    alwaysinline auto plotBelow(uint x, uint8 source, uint8 priority, uint16 color) -> void;
    alwaysinline auto plotHD(Pixel*, uint x, uint8 source, uint8 priority, uint16 color, bool hires, bool subpixel) -> void;

    //background.cpp
    auto renderBackground(PPU::IO::Background&, uint8 source) -> void;
    auto getTile(PPU::IO::Background&, uint hoffset, uint voffset) -> uint;

    //mode7.cpp
    auto renderMode7(PPU::IO::Background&, uint8 source) -> void;

    //mode7hd.cpp
    static auto cacheMode7HD() -> void;
    auto renderMode7HD(PPU::IO::Background&, uint8 source) -> void;
    alwaysinline auto lerp(float pa, float va, float pb, float vb, float pr) -> float;

    //mode7hd-avx2.cpp
    auto renderMode7HD_AVX2(
      PPU::IO::Background&, uint8 source,
      Pixel* above, Pixel* below,
      bool* windowAbove, bool* windowBelow,
      float originX, float a,
      float originY, float c
    ) -> void;

    //object.cpp
    auto renderObject(PPU::IO::Object&) -> void;

    //window.cpp
    auto renderWindow(PPU::IO::WindowLayer&, bool enable, bool output[256]) -> void;
    auto renderWindow(PPU::IO::WindowColor&, uint mask,   bool output[256]) -> void;

  //unserialized:
    uint y;  //constant
    bool fieldID;

    IO io;
    uint16 cgram[256];

    ObjectItem items[128];  //32 on real hardware
    ObjectTile tiles[128];  //34 on real hardware; 1024 max (128 * 64-width tiles)

    Pixel above[256 * 9 * 9];
    Pixel below[256 * 9 * 9];

    bool windowAbove[256];
    bool windowBelow[256];

    //flush()
    static uint start;
    static uint count;
  };

//unserialized:
  Line lines[240];

  //used to help detect when the video output size changes between frames to clear overscan area.
  struct Frame {
    uint pitch = 0;
    uint width = 0;
    uint height = 0;
  } frame;

  struct Mode7LineGroups {
    int count = -1;
    int startLine[32];
    int endLine[32];
    int startLerpLine[32];
    int endLerpLine[32];
  } mode7LineGroups;
};

extern PPU ppufast;

#undef PPU
