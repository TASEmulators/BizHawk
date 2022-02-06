//Video Interface

struct VI : Thread, Memory::IO<VI> {
  Node::Object node;
  Node::Video::Screen screen;

  struct Debugger {
    //debugger.cpp
    auto load(Node::Object) -> void;
    auto io(bool mode, u32 address, u32 data) -> void;

    struct Tracer {
      Node::Debugger::Tracer::Notification io;
    } tracer;
  } debugger;

  //vi.cpp
  auto load(Node::Object) -> void;
  auto unload() -> void;

  auto main() -> void;
  auto step(u32 clocks) -> void;
  auto refresh() -> void;
  auto power(bool reset) -> void;

  //io.cpp
  auto readWord(u32 address) -> u32;
  auto writeWord(u32 address, u32 data) -> void;

  //serialization.cpp
  auto serialize(serializer&) -> void;

  struct IO {
    n2  colorDepth;
    n1  gammaDither;
    n1  gamma;
    n1  divot;
    n1  serrate;  //interlace
    n2  antialias;
    n32 reserved;
    n24 dramAddress;
    n12 width;
    n10 coincidence = 256;
    n8  hsyncWidth;
    n8  colorBurstWidth;
    n4  vsyncWidth;
    n10 colorBurstHsync;
    n10 halfLinesPerField;
    n12 quarterLineDuration;
    n5  palLeapPattern;
    n12 hsyncLeap[2];
    n10 hend;
    n10 hstart;
    n10 vend;
    n10 vstart;
    n10 colorBurstEnd;
    n10 colorBurstStart;
    n12 xscale;
    n12 xsubpixel;
    n12 yscale;
    n12 ysubpixel;

  //internal:
    n9  vcounter;
    n1  field;
  } io;

//unserialized:
  bool refreshed;

  #if defined(VULKAN)
  bool gpuOutputValid = false;
  #endif
};

extern VI vi;
