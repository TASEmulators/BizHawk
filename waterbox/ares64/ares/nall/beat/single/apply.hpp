#pragma once

namespace nall::Beat::Single {

inline auto apply(array_view<u8> source, array_view<u8> beat, maybe<string&> manifest = {}, maybe<string&> result = {}) -> maybe<vector<u8>> {
  #define error(text) { if(result) *result = {"error: ", text}; return {}; }
  #define warning(text) { if(result) *result = {"warning: ", text}; return target; }
  #define success() { if(result) *result = ""; return target; }
  if(beat.size() < 19) error("beat size mismatch");

  vector<u8> target;

  u32 beatOffset = 0;
  auto read = [&]() -> u8 {
    return beat[beatOffset++];
  };

  auto decode = [&]() -> u64 {
    u64 data = 0, shift = 1;
    while(true) {
      u8 x = read();
      data += (x & 0x7f) * shift;
      if(x & 0x80) break;
      shift <<= 7;
      data += shift;
    }
    return data;
  };

  auto write = [&](u8 data) {
    target.append(data);
  };

  if(read() != 'B') error("beat header invalid");
  if(read() != 'P') error("beat header invalid");
  if(read() != 'S') error("beat header invalid");
  if(read() != '1') error("beat version mismatch");
  if(decode() != source.size()) error("source size mismatch");
  u32 targetSize = decode();
  target.reserve(targetSize);
  u32 metadataSize = decode();
  for(u32 n : range(metadataSize)) {
    auto data = read();
    if(manifest) manifest->append((char)data);
  }

  enum : u32 { SourceRead, TargetRead, SourceCopy, TargetCopy };

  u32 sourceRelativeOffset = 0, targetRelativeOffset = 0;
  while(beatOffset < beat.size() - 12) {
    u32 length = decode();
    u32 mode = length & 3;
    length = (length >> 2) + 1;

    if(mode == SourceRead) {
      while(length--) write(source[target.size()]);
    } else if(mode == TargetRead) {
      while(length--) write(read());
    } else {
      s32 offset = decode();
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

  u32 sourceHash = 0, targetHash = 0, beatHash = 0;
  for(u32 shift : range(0, 32, 8)) sourceHash |= read() << shift;
  for(u32 shift : range(0, 32, 8)) targetHash |= read() << shift;
  for(u32 shift : range(0, 32, 8)) beatHash   |= read() << shift;

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
