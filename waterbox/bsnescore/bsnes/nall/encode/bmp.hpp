#pragma once

namespace nall::Encode {

struct BMP {
  static auto create(const string& filename, const void* data, uint pitch, uint width, uint height, bool alpha) -> bool {
    auto fp = file::open(filename, file::mode::write);
    if(!fp) return false;

    uint bitsPerPixel  = alpha ? 32 : 24;
    uint bytesPerPixel = bitsPerPixel / 8;
    uint alignedWidth  = width * bytesPerPixel;
    uint paddingLength = 0;
    uint imageSize     = alignedWidth * height;
    uint fileSize      = 0x36 + imageSize;
    while(alignedWidth % 4) alignedWidth++, paddingLength++;

    fp.writel(0x4d42, 2);        //signature
    fp.writel(fileSize, 4);      //file size
    fp.writel(0, 2);             //reserved
    fp.writel(0, 2);             //reserved
    fp.writel(0x36, 4);          //offset

    fp.writel(40, 4);            //DIB size
    fp.writel(width, 4);         //width
    fp.writel(-height, 4);       //height
    fp.writel(1, 2);             //color planes
    fp.writel(bitsPerPixel, 2);  //bits per pixel
    fp.writel(0, 4);             //compression method (BI_RGB)
    fp.writel(imageSize, 4);     //image data size
    fp.writel(3780, 4);          //horizontal resolution
    fp.writel(3780, 4);          //vertical resolution
    fp.writel(0, 4);             //palette size
    fp.writel(0, 4);             //important color count

    pitch >>= 2;
    for(auto y : range(height)) {
      auto p = (const uint32_t*)data + y * pitch;
      for(auto x : range(width)) fp.writel(*p++, bytesPerPixel);
      if(paddingLength) fp.writel(0, paddingLength);
    }

    return true;
  }
};

}
