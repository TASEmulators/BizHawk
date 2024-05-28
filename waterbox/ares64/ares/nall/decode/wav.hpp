#pragma once

#include <nall/file.hpp>
#include <nall/string.hpp>

namespace nall::Decode {

struct WAV {
  auto open(const string& filename) -> bool;
  auto close() -> void;
  auto read() -> u64;
  auto end() const -> bool;
  auto size() const -> u64;

  file_buffer fp;
  u32 channels = 0;
  u32 frequency = 0;
  u32 bitrate = 0;
  u32 samples = 0;
  u32 headerSize = 0;
};

inline auto WAV::open(const string& filename) -> bool {
  close();

  if(fp = file::open(filename, file::mode::read)) {
    if(fp.read() != 'R') return false;
    if(fp.read() != 'I') return false;
    if(fp.read() != 'F') return false;
    if(fp.read() != 'F') return false;

    u32 chunkSize = fp.readl(4);

    if(fp.read() != 'W') return false;
    if(fp.read() != 'A') return false;
    if(fp.read() != 'V') return false;
    if(fp.read() != 'E') return false;

    if(fp.read() != 'f') return false;
    if(fp.read() != 'm') return false;
    if(fp.read() != 't') return false;
    if(fp.read() != ' ') return false;

    u32 subchunkSize = fp.readl(4);
    if(subchunkSize != 16) return false;

    u16 format = fp.readl(2);
    if(format != 1) return false;  //only PCM is supported

    channels = fp.readl(2);
    frequency = fp.readl(4);
    u32 byteRate = fp.readl(4);
    u16 blockAlign = fp.readl(2);
    bitrate = fp.readl(2);

    //todo: handle LIST chunk better than this
    while(!fp.end() && fp.read() != 'd');
    while(!fp.end() && fp.read() != 'a');
    while(!fp.end() && fp.read() != 't');
    while(!fp.end() && fp.read() != 'a');
    if(fp.end()) return false;

    u32 dataSize = fp.readl(4);
    u32 remaining = fp.size() - fp.offset();
    samples = remaining / (bitrate / 8) / channels;
    headerSize = fp.offset();
    return true;
  }

  return false;
}

inline auto WAV::close() -> void {
  fp.close();
  channels = 0;
  frequency = 0;
  bitrate = 0;
  samples = 0;
}

inline auto WAV::read() -> u64 {
  return fp.readl((bitrate / 8) * channels);
}

inline auto WAV::end() const -> bool {
  return fp.end();
}

inline auto WAV::size() const -> u64 {
  return samples * (bitrate / 8) * channels;
}

}
