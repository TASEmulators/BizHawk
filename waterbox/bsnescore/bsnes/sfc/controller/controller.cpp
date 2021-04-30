#include <sfc/sfc.hpp>

namespace SuperFamicom {

ControllerPort controllerPort1;
ControllerPort controllerPort2;
#include "gamepad/gamepad.cpp"
#include "mouse/mouse.cpp"
#include "super-multitap/super-multitap.cpp"
#include "super-scope/super-scope.cpp"
#include "justifier/justifier.cpp"

Controller::Controller(uint port) : port(port) {
}

Controller::~Controller() {
}

auto Controller::iobit() -> bool {
  switch(port) {
  case ID::Port::Controller1: return cpu.pio() & 0x40;
  case ID::Port::Controller2: return cpu.pio() & 0x80;
  }
  unreachable;
}

auto Controller::iobit(bool data) -> void {
  switch(port) {
  case ID::Port::Controller1: bus.write(0x4201, (cpu.pio() & ~0x40) | (data << 6)); break;
  case ID::Port::Controller2: bus.write(0x4201, (cpu.pio() & ~0x80) | (data << 7)); break;
  }
}

//

auto ControllerPort::connect(uint deviceID) -> void {
  if(!system.loaded()) return;
  delete device;

  switch(deviceID) { default:
  case ID::Device::None: device = new Controller(port); break;
  case ID::Device::Gamepad: device = new Gamepad(port); break;
  case ID::Device::Mouse: device = new Mouse(port); break;
  case ID::Device::SuperMultitap: device = new SuperMultitap(port); break;
  case ID::Device::SuperScope: device = new SuperScope(port); break;
  case ID::Device::Justifier: device = new Justifier(port, false); break;
  case ID::Device::Justifiers: device = new Justifier(port, true); break;
  }
}

auto ControllerPort::power(uint port) -> void {
  this->port = port;
}

auto ControllerPort::unload() -> void {
  delete device;
  device = nullptr;
}

auto ControllerPort::serialize(serializer& s) -> void {
}

}
