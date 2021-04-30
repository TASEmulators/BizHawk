#pragma once

namespace nall {

auto image::bitDepth(uint64_t color) -> unsigned {
  unsigned depth = 0;
  if(color) while((color & 1) == 0) color >>= 1;
  while((color & 1) == 1) { color >>= 1; depth++; }
  return depth;
}

auto image::bitShift(uint64_t color) -> unsigned {
  unsigned shift = 0;
  if(color) while((color & 1) == 0) { color >>= 1; shift++; }
  return shift;
}

auto image::normalize(uint64_t color, unsigned sourceDepth, unsigned targetDepth) -> uint64_t {
  if(sourceDepth == 0 || targetDepth == 0) return 0;
  while(sourceDepth < targetDepth) {
    color = (color << sourceDepth) | color;
    sourceDepth += sourceDepth;
  }
  if(targetDepth < sourceDepth) color >>= (sourceDepth - targetDepth);
  return color;
}

}
