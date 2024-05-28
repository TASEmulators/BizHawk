#include <n64/n64.hpp>

#include "angrylion.h"

namespace ares::Nintendo64 {

VI vi;
#include "io.cpp"
#include "debugger.cpp"
#include "serialization.cpp"

bool BobDeinterlace = false;
bool FastVI = false;

auto VI::load(Node::Object parent) -> void {
  node = parent->append<Node::Object>("VI");

  u32 width = 640;
  u32 height = 576;

  #if defined(VULKAN)
  if (vulkan.enable) {
    width *= vulkan.outputUpscale;
    height *= vulkan.outputUpscale;
  }
  #endif
  screen = node->append<Node::Video::Screen>("Screen", width, height);
  screen->setRefresh({&VI::refresh, this});
  screen->refreshRateHint(Region::PAL() ? 50 : 60); // TODO: More accurate refresh rate hint
  screen->colors((1 << 24) + (1 << 15), [&](n32 color) -> n64 {
    if(color < (1 << 24)) {
      u64 a = 65535;
      u64 r = image::normalize(color >> 16 & 255, 8, 16);
      u64 g = image::normalize(color >>  8 & 255, 8, 16);
      u64 b = image::normalize(color >>  0 & 255, 8, 16);
      return a << 48 | r << 32 | g << 16 | b << 0;
    } else {
      u64 a = 65535;
      u64 r = image::normalize(color >> 10 & 31, 5, 16);
      u64 g = image::normalize(color >>  5 & 31, 5, 16);
      u64 b = image::normalize(color >>  0 & 31, 5, 16);
      return a << 48 | r << 32 | g << 16 | b << 0;
    }
  });
  
  #if defined(VULKAN)
  if(vulkan.enable) {
    screen->setSize(vulkan.outputUpscale * 640, vulkan.outputUpscale * 480);
    if(!vulkan.supersampleScanout) {
      screen->setScale(1.0 / vulkan.outputUpscale, 1.0 / vulkan.outputUpscale);
    }
  } else {
    screen->setSize(640, 480);
  }
  #else
  screen->setSize(640, 480);
  #endif

  debugger.load(node);
}

auto VI::unload() -> void {
  debugger = {};
  node->remove(screen);
  screen.reset();
  node.reset();
}

auto VI::main() -> void {
  while(Thread::clock < 0) {
    if(++io.vcounter >= (Region::NTSC() ? 262 : 312) + io.field) {
      io.vcounter = 0;
      io.field = io.field + 1 & io.serrate;
      #if defined(VULKAN)
      if (vulkan.enable) {
        gpuOutputValid = vulkan.scanoutAsync(io.field);
        vulkan.frame();
      }
      #endif
      angrylion::UpdateScreen(FastVI);
      angrylion::FinalizeFrame(BobDeinterlace);
      refreshed = true;
#if false
      screen->frame();
#endif
    }

    //field is not compared
    if(io.vcounter << 1 == io.coincidence) {
      mi.raise(MI::IRQ::VI);
    }

    if(Region::NTSC()) step(system.frequency() / 60 / 262);
    if(Region::PAL ()) step(system.frequency() / 50 / 312);
  }
}

auto VI::refresh() -> void {
  #if defined(VULKAN)
  if(vulkan.enable && gpuOutputValid) {
    const u8* rgba = nullptr;
    u32 width = 0, height = 0;
    vulkan.mapScanoutRead(rgba, width, height);
    if(rgba) {
      screen->setViewport(0, 0, width, height);
      for(u32 y : range(height)) {
        u32 y_fix = y; 
        // When weave interlacing is active, we need to fix the order of interleaved lines for the image output
        // but only when the VI is set to interlance and we don't use supersampling (causes severe bugs)
        // Otherwise proceed as normal
        if(io.serrate == 1 && vulkan.weaveDeinterlacing && !vulkan.supersampleScanout) y_fix = (y % 2 == 0)? y+1 : y-1; // Swap each even/odd line
        auto source = rgba + width * y_fix * sizeof(u32);
        auto target = screen->pixels(1).data() + y * vulkan.outputUpscale * 640;
        for(u32 x : range(width)) {
          target[x] = source[x * 4 + 0] << 16 | source[x * 4 + 1] << 8 | source[x * 4 + 2] << 0;
        }
      }
    } else {
      screen->setViewport(0, 0, 1, 1);
      screen->pixels(1).data()[0] = 0;
    }
    vulkan.unmapScanoutRead();
    vulkan.endScanout();
    return;
  }
  #endif

  if(io.serrate == 0) screen->setProgressive(0);
  if(io.serrate == 1) screen->setInterlace(!io.field);

  u32 hscan_start = Region::NTSC() ? 108 : 128;
  u32 vscan_start = Region::NTSC() ?  34 :  44;
  u32 hscan_len   = Region::NTSC() ? 640 : 640;
  u32 vscan_len   = Region::NTSC() ? 480 : 576;
  u32 hscan_stop  = hscan_start + hscan_len;
  u32 vscan_stop  = vscan_start + vscan_len;
  screen->setViewport(0, 0, hscan_len, vscan_len);

  i32 dy0 = vi.io.vstart;
  i32 dy1 = vi.io.vend;   if (dy1 < dy0) dy1 = vscan_stop;
  i32 dx0 = vi.io.hstart;
  i32 dx1 = vi.io.hend;

  dy0 = max(vscan_start, dy0);
  dy1 = min(vscan_stop,  dy1);
  dx0 = max(hscan_start, dx0);
  dx1 = min(hscan_stop,  dx1);

  // Undocumented VI guard-band "hardware bug" (match parallel-RDP)
  if(dx0 >= hscan_start) dx0 += 8;
  if(dx1 <  hscan_stop)  dx1 -= 7;

  u32 pitch = vi.io.width;
  if(vi.io.colorDepth == 2) {
    //15bpp
    u32 y0 = vi.io.ysubpixel + vi.io.yscale * (dy0 - vi.io.vstart);
    for(i32 dy = dy0; dy < dy1; dy++) {
      if(!io.serrate || (dy & 1) == !io.field) {
        u32 address = vi.io.dramAddress + (y0 >> 11) * pitch * 2;
        auto line = screen->pixels(1).data() + (dy - vscan_start) * hscan_len;
        u32 x0 = vi.io.xsubpixel + vi.io.xscale * (dx0 - vi.io.hstart);
        for(i32 dx = dx0; dx < dx1; dx++) {
          u16 data = rdram.ram.read<Half>(address + (x0 >> 10) * 2, "VI");
          line[dx - hscan_start] = 1 << 24 | data >> 1;
          x0 += vi.io.xscale;
        }
      }
      y0 += vi.io.yscale;
    }
  }

  if(vi.io.colorDepth == 3) {
    //24bpp
    u32 y0 = vi.io.ysubpixel + vi.io.yscale * (dy0 - vi.io.vstart);
    for(i32 dy = dy0; dy < dy1; dy++) {
      if(!io.serrate || (dy & 1) == !io.field) {
        u32 address = vi.io.dramAddress + (y0 >> 11) * pitch * 4;
        auto line = screen->pixels(1).data() + (dy - vscan_start) * hscan_len;
        u32 x0 = vi.io.xsubpixel + vi.io.xscale * (dx0 - vi.io.hstart);
        for(i32 dx = dx0; dx < dx1; dx++) {
          u32 data = rdram.ram.read<Word>(address + (x0 >> 10) * 4, "VI");
          line[dx - hscan_start] = data >> 8;
          x0 += vi.io.xscale;
        }
      }
      y0 += vi.io.yscale;
    }
  }
}

auto VI::power(bool reset) -> void {
  Thread::reset();
  screen->power();
  io = {};
  refreshed = false;

  #if defined(VULKAN)
  gpuOutputValid = false;
  #endif
}

}
