#pragma once

#include <nall/string.hpp>
#include <nall/decode/inflate.hpp>

namespace nall::Decode {

struct PNG {
  PNG();
  ~PNG();

  auto load(const string& filename) -> bool;
  auto load(const u8* sourceData, u32 sourceSize) -> bool;
  auto readbits(const u8*& data) -> u32;

  struct Info {
    u32 width;
    u32 height;
    u32 bitDepth;
    //colorType:
    //0 = L (luma)
    //2 = R,G,B
    //3 = P (palette)
    //4 = L,A
    //6 = R,G,B,A
    u32 colorType;
    u32 compressionMethod;
    u32 filterType;
    u32 interlaceMethod;

    u32 bytesPerPixel;
    u32 pitch;

    u8 palette[256][3];
  } info;

  u8* data = nullptr;
  u32 size = 0;

  u32 bitpos = 0;

protected:
  enum class FourCC : u32 {
    IHDR = 0x49484452,
    PLTE = 0x504c5445,
    IDAT = 0x49444154,
    IEND = 0x49454e44,
  };

  auto interlace(u32 pass, u32 index) -> u32;
  auto inflateSize() -> u32;
  auto deinterlace(const u8*& inputData, u32 pass) -> bool;
  auto filter(u8* outputData, const u8* inputData, u32 width, u32 height) -> bool;
  auto read(const u8* data, u32 length) -> u32;
};

inline PNG::PNG() {
}

inline PNG::~PNG() {
  if(data) delete[] data;
}

inline auto PNG::load(const string& filename) -> bool {
  if(auto memory = file::read(filename)) {
    return load(memory.data(), memory.size());
  }
  return false;
}

inline auto PNG::load(const u8* sourceData, u32 sourceSize) -> bool {
  if(sourceSize < 8) return false;
  if(read(sourceData + 0, 4) != 0x89504e47) return false;
  if(read(sourceData + 4, 4) != 0x0d0a1a0a) return false;

  u8* compressedData = nullptr;
  u32 compressedSize = 0;

  u32 offset = 8;
  while(offset < sourceSize) {
    u32 length   = read(sourceData + offset + 0, 4);
    u32 fourCC   = read(sourceData + offset + 4, 4);
    u32 checksum = read(sourceData + offset + 8 + length, 4);

    if(fourCC == (u32)FourCC::IHDR) {
      info.width             = read(sourceData + offset +  8, 4);
      info.height            = read(sourceData + offset + 12, 4);
      info.bitDepth          = read(sourceData + offset + 16, 1);
      info.colorType         = read(sourceData + offset + 17, 1);
      info.compressionMethod = read(sourceData + offset + 18, 1);
      info.filterType        = read(sourceData + offset + 19, 1);
      info.interlaceMethod   = read(sourceData + offset + 20, 1);

      if(info.bitDepth == 0 || info.bitDepth > 16) return false;
      if(info.bitDepth & (info.bitDepth - 1)) return false;  //not a power of two
      if(info.compressionMethod != 0) return false;
      if(info.filterType != 0) return false;
      if(info.interlaceMethod != 0 && info.interlaceMethod != 1) return false;

      switch(info.colorType) {
      case 0: info.bytesPerPixel = info.bitDepth * 1; break;  //L
      case 2: info.bytesPerPixel = info.bitDepth * 3; break;  //R,G,B
      case 3: info.bytesPerPixel = info.bitDepth * 1; break;  //P
      case 4: info.bytesPerPixel = info.bitDepth * 2; break;  //L,A
      case 6: info.bytesPerPixel = info.bitDepth * 4; break;  //R,G,B,A
      default: return false;
      }

      if(info.colorType == 2 || info.colorType == 4 || info.colorType == 6) {
        if(info.bitDepth != 8 && info.bitDepth != 16) return false;
      }
      if(info.colorType == 3 && info.bitDepth == 16) return false;

      info.bytesPerPixel = (info.bytesPerPixel + 7) / 8;
      info.pitch = (s32)info.width * info.bytesPerPixel;
    }

    if(fourCC == (u32)FourCC::PLTE) {
      if(length % 3) return false;
      for(u32 n = 0, p = offset + 8; n < length / 3; n++) {
        info.palette[n][0] = sourceData[p++];
        info.palette[n][1] = sourceData[p++];
        info.palette[n][2] = sourceData[p++];
      }
    }

    if(fourCC == (u32)FourCC::IDAT) {
      compressedData = (u8*)realloc(compressedData, compressedSize + length);
      memcpy(compressedData + compressedSize, sourceData + offset + 8, length);
      compressedSize += length;
    }

    if(fourCC == (u32)FourCC::IEND) {
      break;
    }

    offset += 4 + 4 + length + 4;
  }

  u32  interlacedSize = inflateSize();
  auto interlacedData = new u8[interlacedSize];

  bool result = inflate(interlacedData, interlacedSize, compressedData + 2, compressedSize - 6);
  free(compressedData);

  if(result == false) {
    delete[] interlacedData;
    return false;
  }

  size = info.width * info.height * info.bytesPerPixel;
  data = new u8[size];

  if(info.interlaceMethod == 0) {
    if(filter(data, interlacedData, info.width, info.height) == false) {
      delete[] interlacedData;
      delete[] data;
      data = nullptr;
      return false;
    }
  } else {
    const u8* passData = interlacedData;
    for(u32 pass = 0; pass < 7; pass++) {
      if(deinterlace(passData, pass) == false) {
        delete[] interlacedData;
        delete[] data;
        data = nullptr;
        return false;
      }
    }
  }

  delete[] interlacedData;
  return true;
}

inline auto PNG::interlace(u32 pass, u32 index) -> u32 {
  static const u32 data[7][4] = {
    //x-distance, y-distance, x-origin, y-origin
    {8, 8, 0, 0},
    {8, 8, 4, 0},
    {4, 8, 0, 4},
    {4, 4, 2, 0},
    {2, 4, 0, 2},
    {2, 2, 1, 0},
    {1, 2, 0, 1},
  };
  return data[pass][index];
}

inline auto PNG::inflateSize() -> u32 {
  if(info.interlaceMethod == 0) {
    return info.width * info.height * info.bytesPerPixel + info.height;
  }

  u32 size = 0;
  for(u32 pass = 0; pass < 7; pass++) {
    u32 xd = interlace(pass, 0), yd = interlace(pass, 1);
    u32 xo = interlace(pass, 2), yo = interlace(pass, 3);
    u32 width  = (info.width  + (xd - xo - 1)) / xd;
    u32 height = (info.height + (yd - yo - 1)) / yd;
    if(width == 0 || height == 0) continue;
    size += width * height * info.bytesPerPixel + height;
  }
  return size;
}

inline auto PNG::deinterlace(const u8*& inputData, u32 pass) -> bool {
  u32 xd = interlace(pass, 0), yd = interlace(pass, 1);
  u32 xo = interlace(pass, 2), yo = interlace(pass, 3);
  u32 width  = (info.width  + (xd - xo - 1)) / xd;
  u32 height = (info.height + (yd - yo - 1)) / yd;
  if(width == 0 || height == 0) return true;

  u32 outputSize = width * height * info.bytesPerPixel;
  auto outputData = new u8[outputSize];
  bool result = filter(outputData, inputData, width, height);

  const u8* rd = outputData;
  for(u32 y = yo; y < info.height; y += yd) {
    u8* wr = data + y * info.pitch;
    for(u32 x = xo; x < info.width; x += xd) {
      for(u32 b = 0; b < info.bytesPerPixel; b++) {
        wr[x * info.bytesPerPixel + b] = *rd++;
      }
    }
  }

  inputData += outputSize + height;
  delete[] outputData;
  return result;
}

inline auto PNG::filter(u8* outputData, const u8* inputData, u32 width, u32 height) -> bool {
  u8* wr = outputData;
  const u8* rd = inputData;
  s32 bpp = info.bytesPerPixel, pitch = width * bpp;
  for(s32 y = 0; y < height; y++) {
    u8 filter = *rd++;

    switch(filter) {
    case 0x00:  //None
      for(s32 x = 0; x < pitch; x++) {
        wr[x] = rd[x];
      }
      break;

    case 0x01:  //Subtract
      for(s32 x = 0; x < pitch; x++) {
        wr[x] = rd[x] + (x - bpp < 0 ? 0 : wr[x - bpp]);
      }
      break;

    case 0x02:  //Above
      for(s32 x = 0; x < pitch; x++) {
        wr[x] = rd[x] + (y - 1 < 0 ? 0 : wr[x - pitch]);
      }
      break;

    case 0x03:  //Average
      for(s32 x = 0; x < pitch; x++) {
        s16 a = x - bpp < 0 ? 0 : wr[x - bpp];
        s16 b = y - 1 < 0 ? 0 : wr[x - pitch];

        wr[x] = rd[x] + (u8)((a + b) / 2);
      }
      break;

    case 0x04:  //Paeth
      for(s32 x = 0; x < pitch; x++) {
        s16 a = x - bpp < 0 ? 0 : wr[x - bpp];
        s16 b = y - 1 < 0 ? 0 : wr[x - pitch];
        s16 c = x - bpp < 0 || y - 1 < 0 ? 0 : wr[x - pitch - bpp];

        s16 p = a + b - c;
        s16 pa = p > a ? p - a : a - p;
        s16 pb = p > b ? p - b : b - p;
        s16 pc = p > c ? p - c : c - p;

        auto paeth = (u8)((pa <= pb && pa <= pc) ? a : (pb <= pc) ? b : c);

        wr[x] = rd[x] + paeth;
      }
      break;

    default:  //Invalid
      return false;
    }

    rd += pitch;
    wr += pitch;
  }

  return true;
}

inline auto PNG::read(const u8* data, u32 length) -> u32 {
  u32 result = 0;
  while(length--) result = (result << 8) | (*data++);
  return result;
}

inline auto PNG::readbits(const u8*& data) -> u32 {
  u32 result = 0;
  switch(info.bitDepth) {
  case 1:
    result = (*data >> bitpos) & 1;
    bitpos++;
    if(bitpos == 8) { data++; bitpos = 0; }
    break;
  case 2:
    result = (*data >> bitpos) & 3;
    bitpos += 2;
    if(bitpos == 8) { data++; bitpos = 0; }
    break;
  case 4:
    result = (*data >> bitpos) & 15;
    bitpos += 4;
    if(bitpos == 8) { data++; bitpos = 0; }
    break;
  case 8:
    result = *data++;
    break;
  case 16:
    result = (data[0] << 8) | (data[1] << 0);
    data += 2;
    break;
  }
  return result;
}

}
