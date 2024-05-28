#pragma once

#include <nall/nall.hpp>
#include <nall/serial.hpp>
using namespace nall;

struct FX {
  auto open(Arguments& arguments) -> bool;
  auto close() -> void;
  auto readable() -> bool;
  auto read() -> u8;
  auto writable() -> bool;
  auto write(u8 data) -> void;

  auto read(u32 offset, u32 length) -> vector<u8>;
  auto write(u32 offset, const void* buffer, u32 length) -> void;
  auto write(u32 offset, const vector<u8>& buffer) -> void { write(offset, buffer.data(), buffer.size()); }
  auto execute(u32 offset) -> void;

  auto read(u32 offset) -> u8;
  auto write(u32 offset, u8 data) -> void;

  serial device;
};

inline auto FX::open(Arguments& arguments) -> bool {
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

inline auto FX::close() -> void {
  device.close();
}

inline auto FX::readable() -> bool {
  return device.readable();
}

//1000ns delay avoids burning CPU core at 100%; does not slow down max transfer rate at all
inline auto FX::read() -> u8 {
  while(!readable()) usleep(1000);
  u8 buffer[1] = {0};
  device.read(buffer, 1);
  return buffer[0];
}

inline auto FX::writable() -> bool {
  return device.writable();
}

inline auto FX::write(u8 data) -> void {
  while(!writable()) usleep(1000);
  u8 buffer[1] = {data};
  device.write(buffer, 1);
}

//

inline auto FX::read(u32 offset, u32 length) -> vector<u8> {
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

  vector<u8> buffer;
  while(length--) buffer.append(read());
  return buffer;
}

inline auto FX::write(u32 offset, const void* data, u32 length) -> void {
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

  auto buffer = (u8*)data;
  for(auto n : range(length)) write(buffer[n]);
  write(0x00);
}

inline auto FX::execute(u32 offset) -> void {
  write(0x21);
  write(0x66);
  write(0x78);
  write(offset >> 16);
  write(offset >>  8);
  write(offset >>  0);
  write(0x00);
}

//

inline auto FX::read(u32 offset) -> u8 {
  auto buffer = read(offset, 1);
  return buffer[0];
}

inline auto FX::write(u32 offset, u8 data) -> void {
  vector<u8> buffer = {data};
  write(offset, buffer);
}
