#include <gameboy/gameboy.hpp>

#define MEMORY_CPP
namespace GameBoy {

Unmapped unmapped;
Bus bus;

uint8_t& Memory::operator[](unsigned addr) {
  return data[addr];
}

void Memory::allocate(unsigned size_) {
  free();
  size = size_;
  data = new uint8_t[size]();
}

void Memory::copy(const uint8_t *data_, unsigned size_) {
  free();
  size = size_;
  data = new uint8_t[size];
  memcpy(data, data_, size);
}

void Memory::free() {
  if(data) {
    delete[] data;
    data = 0;
  }
}

Memory::Memory() {
  data = 0;
  size = 0;
}

Memory::~Memory() {
  free();
}

//

uint8 Bus::read(uint16 addr) {
  uint8 data = mmio[addr]->mmio_read(addr);
  return data;
}

void Bus::write(uint16 addr, uint8 data) {
  mmio[addr]->mmio_write(addr, data);
}

void Bus::power() {
  for(unsigned n = 0x0000; n <= 0xffff; n++) mmio[n] = &unmapped;
}

}
