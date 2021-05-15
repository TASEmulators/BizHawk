// SNES controller port pinout:
//  -------------------------------
// | (1) (2) (3) (4) | (5) (6) (7) )
//  -------------------------------
// pin    name     port1                            port2
//  1:    +5v
//  2:    clock    $4016 read                       $4017 read
//  3:    latch    $4016.d0 write                   $4016.d0 write
//  4:    data1    $4016.d0 read                    $4017.d0 read
//  5:    data2    $4016.d1 read                    $4017.d1 read
//  6:    iobit    $4201.d6 write; $4213.d6 read    $4201.d7 write; $4213.d7 read
//  7:    gnd

struct Controller {
  Controller(uint port);
  virtual ~Controller();

  auto iobit() -> bool;
  auto iobit(bool data) -> void;
  virtual auto data() -> uint2 { return 0; }
  virtual auto latch(bool data) -> void {}
  virtual auto latch() -> void {}  //light guns
  virtual auto draw(uint16_t* output, uint pitch, uint width, uint height) -> void {}  //light guns

  const uint port;
};

struct ControllerPort {
  auto connect(uint deviceID) -> void;

  auto power(uint port) -> void;
  auto unload() -> void;
  auto serialize(serializer&) -> void;

  uint port;
  Controller* device = nullptr;
};

extern ControllerPort controllerPort1;
extern ControllerPort controllerPort2;

#include "gamepad/gamepad.hpp"
#include "mouse/mouse.hpp"
#include "super-multitap/super-multitap.hpp"
#include "super-scope/super-scope.hpp"
#include "justifier/justifier.hpp"
