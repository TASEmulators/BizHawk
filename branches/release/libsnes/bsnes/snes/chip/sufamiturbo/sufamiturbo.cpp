#include <snes/snes.hpp>

#define SUFAMITURBO_CPP
namespace SNES {

#include "serialization.cpp"
SufamiTurbo sufamiturbo;

SufamiTurbo::SufamiTurbo()
{
	slotA.ram.setName("SUFAMI_TURBO_A_RAM");
	slotB.ram.setName("SUFAMI_TURBO_B_RAM");
}

void SufamiTurbo::load() {
  slotA.ram.map(allocate<uint8>(128 * 1024, 0xff), 128 * 1024);
  slotB.ram.map(allocate<uint8>(128 * 1024, 0xff), 128 * 1024);

  if(slotA.rom.data()) {
    cartridge.nvram.append({ "program.ram", slotA.ram.data(), slotA.ram.size(), Cartridge::Slot::SufamiTurboA });
  } else {
    slotA.rom.map(allocate<uint8>(128 * 1024, 0xff), 128 * 1024);
  }

  if(slotB.rom.data()) {
    cartridge.nvram.append({ "program.ram", slotB.ram.data(), slotB.ram.size(), Cartridge::Slot::SufamiTurboB });
  } else {
    slotB.rom.map(allocate<uint8>(128 * 1024, 0xff), 128 * 1024);
  }
}

void SufamiTurbo::unload() {
  slotA.rom.reset();
  slotA.ram.reset();
  slotB.rom.reset();
  slotB.ram.reset();
}

}
