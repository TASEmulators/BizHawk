#pragma once

namespace nall::Beat::Single {

inline auto apply(array_view<uint8_t> source, array_view<uint8_t> beat, maybe<string&> manifest = {}, maybe<string&> result = {}) -> maybe<vector<uint8_t>> {
  #define error(text) { if(result) *result = {"error: ", text}; return {}; }
  #define warning(text) { if(result) *result = {"warning: ", text}; return target; }
  #define success() { if(result) *result = ""; return target; }
  if(beat.size() < 19) error("beat size mismatch");

  vector<uint8_t> target;

  uint beatOffset = 0;
  auto read = [&]() -> uint8_t {
    return beat[beatOffset++];
  };

  auto decode = [&]() -> uint64_t {
    uint64_t data = 0, shift = 1;
    while(true) {
      uint8_t x = read();
      data += (x & 0x7f) * shift;
      if(x & 0x80) break;
      shift <<= 7;
      data += shift;
    }
    return data;
  };

  auto write = [&](uint8_t data) {
    target.append(data);
  };

  if(read() != 'B') error("beat header invalid");
  if(read() != 'P') error("beat header invalid");
  if(read() != 'S') error("beat header invalid");
  if(read() != '1') error("beat version mismatch");
  if(decode() != source.size()) error("source size mismatch");
  uint targetSize = decode();
  target.reserve(targetSize);
  uint metadataSize = decode();
  for(uint n : range(metadataSize)) {
    auto data = read();
    if(manifest) manifest->append((char)data);
  }

  enum : uint { SourceRead, TargetRead, SourceCopy, TargetCopy };

  uint sourceRelativeOffset = 0, targetRelativeOffset = 0;
  while(beatOffset < beat.size() - 12) {
    uint length = decode();
    uint mode = length & 3;
    length = (length >> 2) + 1;

    if(mode == SourceRead) {
      while(length--) write(source[target.size()]);
    } else if(mode == TargetRead) {
      while(length--) write(read());
    } else {
      int offset = decode();
      offset = offset & 1 ? -(offset >> 1) : (offset >> 1);
      if(mode == SourceCopy) {
        sourceRelativeOffset += offset;
        while(length--) write(source[sourceRelativeOffset++]);
      } else {
        targetRelativeOffset += offset;
        while(length--) write(target[targetRelativeOffset++]);
      }
    }
  }

  uint32_t sourceHash = 0, targetHash = 0, beatHash = 0;
  for(uint shift : range(0, 32, 8)) sourceHash |= read() << shift;
  for(uint shift : range(0, 32, 8)) targetHash |= read() << shift;
  for(uint shift : range(0, 32, 8)) beatHash   |= read() << shift;

  if(target.size() != targetSize) warning("target size mismatch");
  if(sourceHash != Hash::CRC32(source).value()) warning("source hash mismatch");
  if(targetHash != Hash::CRC32(target).value()) warning("target hash mismatch");
  if(beatHash != Hash::CRC32({beat.data(), beat.size() - 4}).value()) warning("beat hash mismatch");

  success();
  #undef error
  #undef warning
  #undef success
}

}
