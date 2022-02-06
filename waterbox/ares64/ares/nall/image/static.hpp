#pragma once

namespace nall {

inline auto image::bitDepth(u64 color) -> u32 {
  u32 depth = 0;
  if(color) while((color & 1) == 0) color >>= 1;
  while((color & 1) == 1) { color >>= 1; depth++; }
  return depth;
}

inline auto image::bitShift(u64 color) -> u32 {
  u32 shift = 0;
  if(color) while((color & 1) == 0) { color >>= 1; shift++; }
  return shift;
}

inline auto image::normalize(u64 color, u32 sourceDepth, u32 targetDepth) -> u64 {
  if(sourceDepth == 0 || targetDepth == 0) return 0;
  while(sourceDepth < targetDepth) {
    color = (color << sourceDepth) | color;
    sourceDepth += sourceDepth;
  }
  if(targetDepth < sourceDepth) color >>= (sourceDepth - targetDepth);
  return color;
}

}
