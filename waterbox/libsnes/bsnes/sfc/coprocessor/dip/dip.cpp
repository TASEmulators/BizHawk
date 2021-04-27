//DIP switch
//used for Nintendo Super System emulation

#include <sfc/sfc.hpp>

namespace SuperFamicom {

#include "serialization.cpp"
DIP dip;

auto DIP::power() -> void {
}

auto DIP::read(uint, uint8) -> uint8 {
  return value;
}

auto DIP::write(uint, uint8) -> void {
}

}
