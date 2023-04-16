#pragma once

namespace nall {

inline image::image(const image& source) {
  operator=(source);
}

inline image::image(image&& source) {
  operator=(forward<image>(source));
}

inline image::image(bool endian, u32 depth, u64 alphaMask, u64 redMask, u64 greenMask, u64 blueMask) {
  _endian = endian;
  _depth  = depth;

  _alpha = {alphaMask, bitDepth(alphaMask), bitShift(alphaMask)};
  _red   = {redMask,   bitDepth(redMask),   bitShift(redMask  )};
  _green = {greenMask, bitDepth(greenMask), bitShift(greenMask)};
  _blue  = {blueMask,  bitDepth(blueMask),  bitShift(blueMask )};
}

inline image::image(const string& filename) {
  load(filename);
}

inline image::image(const void* data_, u32 size) {
  auto data = (const u8*)data_;
  if(size < 4);
  else if(data[0] == 'B' && data[1] == 'M') loadBMP(data, size);
  else if(data[1] == 'P' && data[2] == 'N' && data[3] == 'G') loadPNG(data, size);
}

inline image::image(const vector<u8>& buffer) : image(buffer.data(), buffer.size()) {
}

template<u32 Size> inline image::image(const u8 (&Name)[Size]) : image(Name, Size) {
}

inline image::image() {
}

inline image::~image() {
  free();
}

inline auto image::operator=(const image& source) -> image& {
  if(this == &source) return *this;
  free();

  _width = source._width;
  _height = source._height;

  _endian = source._endian;
  _depth = source._depth;

  _alpha = source._alpha;
  _red = source._red;
  _green = source._green;
  _blue = source._blue;

  _data = allocate(_width, _height, stride());
  memory::copy(_data, source._data, source.size());
  return *this;
}

inline auto image::operator=(image&& source) -> image& {
  if(this == &source) return *this;
  free();

  _width = source._width;
  _height = source._height;

  _endian = source._endian;
  _depth = source._depth;

  _alpha = source._alpha;
  _red = source._red;
  _green = source._green;
  _blue = source._blue;

  _data = source._data;
  source._data = nullptr;
  return *this;
}

inline image::operator bool() const {
  return _data && _width && _height;
}

inline auto image::operator==(const image& source) const -> bool {
  if(_width != source._width) return false;
  if(_height != source._height) return false;

  if(_endian != source._endian) return false;
  if(_depth != source._depth) return false;

  if(_alpha != source._alpha) return false;
  if(_red != source._red) return false;
  if(_green != source._green) return false;
  if(_blue != source._blue) return false;

  return memory::compare(_data, source._data, size()) == 0;
}

inline auto image::operator!=(const image& source) const -> bool {
  return !operator==(source);
}

inline auto image::read(const u8* data) const -> u64 {
  u64 result = 0;
  if(_endian == 0) {
    for(s32 n = stride() - 1; n >= 0; n--) result = (result << 8) | data[n];
  } else {
    for(s32 n = 0; n < stride(); n++) result = (result << 8) | data[n];
  }
  return result;
}

inline auto image::write(u8* data, u64 value) const -> void {
  if(_endian == 0) {
    for(s32 n = 0; n < stride(); n++) {
      data[n] = value;
      value >>= 8;
    }
  } else {
    for(s32 n = stride() - 1; n >= 0; n--) {
      data[n] = value;
      value >>= 8;
    }
  }
}

inline auto image::free() -> void {
  if(_data) delete[] _data;
  _data = nullptr;
}

inline auto image::load(const string& filename) -> bool {
  if(loadBMP(filename) == true) return true;
  if(loadPNG(filename) == true) return true;
  return false;
}

//assumes image and data are in the same format; pitch is adapted to image
inline auto image::copy(const void* data, u32 pitch, u32 width, u32 height) -> void {
  allocate(width, height);
  for(u32 y : range(height)) {
    auto input = (const u8*)data + y * pitch;
    auto output = (u8*)_data + y * this->pitch();
    memory::copy(output, input, width * stride());
  }
}

inline auto image::allocate(u32 width, u32 height) -> void {
  if(_data && _width == width && _height == height) return;
  free();
  _width = width;
  _height = height;
  _data = allocate(_width, _height, stride());
}

//private
inline auto image::allocate(u32 width, u32 height, u32 stride) -> u8* {
  //allocate 1x1 larger than requested; so that linear interpolation does not require bounds-checking
  u32 size = width * height * stride;
  u32 padding = width * stride + stride;
  auto data = new u8[size + padding];
  memory::fill(data + size, padding);
  return data;
}

}
