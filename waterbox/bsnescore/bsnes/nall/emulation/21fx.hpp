#pragma once

#include <nall/nall.hpp>
#include <nall/serial.hpp>
using namespace nall;

using  int8 = Integer< 8>;
using int16 = Integer<16>;
using int24 = Integer<24>;
using int32 = Integer<32>;
using int64 = Integer<64>;

using  uint8 = Natural< 8>;
using uint16 = Natural<16>;
using uint24 = Natural<24>;
using uint32 = Natural<32>;
using uint64 = Natural<64>;

struct FX {
  auto open(Arguments& arguments) -> bool;
  auto close() -> void;
  auto readable() -> bool;
  auto read() -> uint8_t;
  auto writable() -> bool;
  auto write(uint8_t data) -> void;

  auto read(uint offset, uint length) -> vector<uint8_t>;
  auto write(uint offset, const void* buffer, uint length) -> void;
  auto write(uint offset, const vector<uint8_t>& buffer) -> void { write(offset, buffer.data(), buffer.size()); }
  auto execute(uint offset) -> void;

  auto read(uint offset) -> uint8_t;
  auto write(uint offset, uint8_t data) -> void;

  serial device;
};

auto FX::open(Arguments& arguments) -> bool {
  //device name override support
  string name;
  arguments.take("--device", name);
  if(!device.open(name)) {
    print("[21fx] error: unable to open hardware device\n");
    return false;
  }

  //flush the device (to clear floating inputs)
  while(true) {
    while(readable()) read();
    auto iplrom = read(0x2184, 122);
    auto sha256 = Hash::SHA256(iplrom).digest();
    if(sha256 == "41b79712a4a2d16d39894ae1b38cde5c41dad22eadc560df631d39f13df1e4b9") break;
  }

  return true;
}

auto FX::close() -> void {
  device.close();
}

auto FX::readable() -> bool {
  return device.readable();
}

//1000ns delay avoids burning CPU core at 100%; does not slow down max transfer rate at all
auto FX::read() -> uint8_t {
  while(!readable()) usleep(1000);
  uint8_t buffer[1] = {0};
  device.read(buffer, 1);
  return buffer[0];
}

auto FX::writable() -> bool {
  return device.writable();
}

auto FX::write(uint8_t data) -> void {
  while(!writable()) usleep(1000);
  uint8_t buffer[1] = {data};
  device.write(buffer, 1);
}

//

auto FX::read(uint offset, uint length) -> vector<uint8_t> {
  write(0x21);
  write(0x66);
  write(0x78);
  write(offset >> 16);
  write(offset >>  8);
  write(offset >>  0);
  write(0x01);
  write(length >>  8);
  write(length >>  0);
  write(0x00);

  vector<uint8_t> buffer;
  while(length--) buffer.append(read());
  return buffer;
}

auto FX::write(uint offset, const void* data, uint length) -> void {
  write(0x21);
  write(0x66);
  write(0x78);
  write(offset >> 16);
  write(offset >>  8);
  write(offset >>  0);
  write(0x01);
  write(length >>  8);
  write(length >>  0);
  write(0x01);

  auto buffer = (uint8_t*)data;
  for(auto n : range(length)) write(buffer[n]);
  write(0x00);
}

auto FX::execute(uint offset) -> void {
  write(0x21);
  write(0x66);
  write(0x78);
  write(offset >> 16);
  write(offset >>  8);
  write(offset >>  0);
  write(0x00);
}

//

auto FX::read(uint offset) -> uint8_t {
  auto buffer = read(offset, 1);
  return buffer[0];
}

auto FX::write(uint offset, uint8_t data) -> void {
  vector<uint8_t> buffer = {data};
  write(offset, buffer);
}
