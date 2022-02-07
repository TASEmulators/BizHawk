//determine mode 7 line groups for perspective correction
auto PPU::Line::cacheMode7HD() -> void {
  ppu.mode7LineGroups.count = 0;
  if(ppu.hdPerspective()) {
    #define isLineMode7(line) (line.io.bg1.tileMode == TileMode::Mode7 && !line.io.displayDisable && ( \
      (line.io.bg1.aboveEnable || line.io.bg1.belowEnable) \
    ))
    bool state = false;
    uint y;
    //find the moe 7 groups
    for(y = 0; y < Line::count; y++) {
      if(state != isLineMode7(ppu.lines[Line::start + y])) {
        state = !state;
        if(state) {
          ppu.mode7LineGroups.startLine[ppu.mode7LineGroups.count] = ppu.lines[Line::start + y].y;
        } else {
          ppu.mode7LineGroups.endLine[ppu.mode7LineGroups.count] = ppu.lines[Line::start + y].y - 1;
          //the lines at the edges of mode 7 groups may be erroneous, so start and end lines for interpolation are moved inside
          int offset = (ppu.mode7LineGroups.endLine[ppu.mode7LineGroups.count] - ppu.mode7LineGroups.startLine[ppu.mode7LineGroups.count]) / 8;
          ppu.mode7LineGroups.startLerpLine[ppu.mode7LineGroups.count] = ppu.mode7LineGroups.startLine[ppu.mode7LineGroups.count] + offset;
          ppu.mode7LineGroups.endLerpLine[ppu.mode7LineGroups.count] = ppu.mode7LineGroups.endLine[ppu.mode7LineGroups.count] - offset;
          ppu.mode7LineGroups.count++;
        }
      }
    }
    #undef isLineMode7
    if(state) {
      //close the last group if necessary
      ppu.mode7LineGroups.endLine[ppu.mode7LineGroups.count] = ppu.lines[Line::start + y].y - 1;
      int offset = (ppu.mode7LineGroups.endLine[ppu.mode7LineGroups.count] - ppu.mode7LineGroups.startLine[ppu.mode7LineGroups.count]) / 8;
      ppu.mode7LineGroups.startLerpLine[ppu.mode7LineGroups.count] = ppu.mode7LineGroups.startLine[ppu.mode7LineGroups.count] + offset;
      ppu.mode7LineGroups.endLerpLine[ppu.mode7LineGroups.count] = ppu.mode7LineGroups.endLine[ppu.mode7LineGroups.count] - offset;
      ppu.mode7LineGroups.count++;
    }

    //detect groups that do not have perspective
    for(int i : range(ppu.mode7LineGroups.count)) {
      int a = -1, b = -1, c = -1, d = -1;  //the mode 7 scale factors of the current line
      int aPrev = -1, bPrev = -1, cPrev = -1, dPrev = -1;  //the mode 7 scale factors of the previous line
      bool aVar = false, bVar = false, cVar = false, dVar = false;  //has a varying value been found for the factors?
      bool aInc = false, bInc = false, cInc = false, dInc = false;  //has the variation been an increase or decrease?
      for(y = ppu.mode7LineGroups.startLerpLine[i]; y <= ppu.mode7LineGroups.endLerpLine[i]; y++) {
        a = ((int)((int16)(ppu.lines[y].io.mode7.a)));
        b = ((int)((int16)(ppu.lines[y].io.mode7.b)));
        c = ((int)((int16)(ppu.lines[y].io.mode7.c)));
        d = ((int)((int16)(ppu.lines[y].io.mode7.d)));
        //has the value of 'a' changed compared to the last line?
        //(and is the factor larger than zero, which happens sometimes and seems to be game-specific, mostly at the edges of the screen)
        if(aPrev > 0 && a > 0 && a != aPrev) {
          if(!aVar) {
            //if there has been no variation yet, store that there is one and store if it is an increase or decrease
            aVar = true;
            aInc = a > aPrev;
          } else if(aInc != a > aPrev) {
            //if there has been an increase and now we have a decrease, or vice versa, set the interpolation lines to -1
            //to deactivate perspective correction for this group and stop analyzing it further
            ppu.mode7LineGroups.startLerpLine[i] = -1;
            ppu.mode7LineGroups.endLerpLine[i] = -1;
            break;
          }
        }
        if(bPrev > 0 && b > 0 && b != bPrev) {
          if(!bVar) {
            bVar = true;
            bInc = b > bPrev;
          } else if(bInc != b > bPrev) {
            ppu.mode7LineGroups.startLerpLine[i] = -1;
            ppu.mode7LineGroups.endLerpLine[i] = -1;
            break;
          }
        }
        if(cPrev > 0 && c > 0 && c != cPrev) {
          if(!cVar) {
            cVar = true;
            cInc = c > cPrev;
          } else if(cInc != c > cPrev) {
            ppu.mode7LineGroups.startLerpLine[i] = -1;
            ppu.mode7LineGroups.endLerpLine[i] = -1;
            break;
          }
        }
        if(dPrev > 0 && d > 0 && d != bPrev) {
          if(!dVar) {
            dVar = true;
            dInc = d > dPrev;
          } else if(dInc != d > dPrev) {
            ppu.mode7LineGroups.startLerpLine[i] = -1;
            ppu.mode7LineGroups.endLerpLine[i] = -1;
            break;
          }
        }
        aPrev = a, bPrev = b, cPrev = c, dPrev = d;
      }
    }
  }
}

auto PPU::Line::renderMode7HD(PPU::IO::Background& self, uint8 source) -> void {
  const bool extbg = source == Source::BG2;
  const uint scale = ppu.hdScale();

  Pixel  pixel;
  Pixel* above = &this->above[-1];
  Pixel* below = &this->below[-1];

  //find the first and last scanline for interpolation
  int y_a = -1;
  int y_b = -1;
  #define isLineMode7(n) (ppu.lines[n].io.bg1.tileMode == TileMode::Mode7 && !ppu.lines[n].io.displayDisable && ( \
    (ppu.lines[n].io.bg1.aboveEnable || ppu.lines[n].io.bg1.belowEnable) \
  ))
  if(ppu.hdPerspective()) {
    //find the mode 7 line group this line is in and use its interpolation lines
    for(int i : range(ppu.mode7LineGroups.count)) {
      if(y >= ppu.mode7LineGroups.startLine[i] && y <= ppu.mode7LineGroups.endLine[i]) {
        y_a = ppu.mode7LineGroups.startLerpLine[i];
        y_b = ppu.mode7LineGroups.endLerpLine[i];
        break;
      }
    }
  }
  if(y_a == -1 || y_b == -1) {
    //if perspective correction is disabled or the group was detected as non-perspective, use the neighboring lines
    y_a = y;
    y_b = y;
    if(y_a >   1 && isLineMode7(y_a)) y_a--;
    if(y_b < 239 && isLineMode7(y_b)) y_b++;
  }
  #undef isLineMode7

  Line line_a = ppu.lines[y_a];
  float a_a = (int16)line_a.io.mode7.a;
  float b_a = (int16)line_a.io.mode7.b;
  float c_a = (int16)line_a.io.mode7.c;
  float d_a = (int16)line_a.io.mode7.d;

  Line line_b = ppu.lines[y_b];
  float a_b = (int16)line_b.io.mode7.a;
  float b_b = (int16)line_b.io.mode7.b;
  float c_b = (int16)line_b.io.mode7.c;
  float d_b = (int16)line_b.io.mode7.d;

  int hcenter = (int13)io.mode7.x;
  int vcenter = (int13)io.mode7.y;
  int hoffset = (int13)io.mode7.hoffset;
  int voffset = (int13)io.mode7.voffset;

  if(io.mode7.vflip) {
    y_a = 255 - y_a;
    y_b = 255 - y_b;
  }

  bool windowAbove[256];
  bool windowBelow[256];
  renderWindow(self.window, self.window.aboveEnable, windowAbove);
  renderWindow(self.window, self.window.belowEnable, windowBelow);

  int pixelYp = INT_MIN;
  for(int ys : range(scale)) {
    float yf = y + ys * 1.0 / scale - 0.5;
    if(io.mode7.vflip) yf = 255 - yf;

    float a = 1.0 / lerp(y_a, 1.0 / a_a, y_b, 1.0 / a_b, yf);
    float b = 1.0 / lerp(y_a, 1.0 / b_a, y_b, 1.0 / b_b, yf);
    float c = 1.0 / lerp(y_a, 1.0 / c_a, y_b, 1.0 / c_b, yf);
    float d = 1.0 / lerp(y_a, 1.0 / d_a, y_b, 1.0 / d_b, yf);

    int ht = (hoffset - hcenter) % 1024;
    float vty = ((voffset - vcenter) % 1024) + yf;
    float originX = (a * ht) + (b * vty) + (hcenter << 8);
    float originY = (c * ht) + (d * vty) + (vcenter << 8);

    int pixelXp = INT_MIN;
    for(int x : range(256)) {
      bool doAbove = self.aboveEnable && !windowAbove[x];
      bool doBelow = self.belowEnable && !windowBelow[x];

      for(int xs : range(scale)) {
        float xf = x + xs * 1.0 / scale - 0.5;
        if(io.mode7.hflip) xf = 255 - xf;

        int pixelX = (originX + a * xf) / 256;
        int pixelY = (originY + c * xf) / 256;

        above++;
        below++;

        //only compute color again when coordinates have changed
        if(pixelX != pixelXp || pixelY != pixelYp) {
          uint tile    = io.mode7.repeat == 3 && ((pixelX | pixelY) & ~1023) ? 0 : (ppu.vram[(pixelY >> 3 & 127) * 128 + (pixelX >> 3 & 127)] & 0xff);
          uint palette = io.mode7.repeat == 2 && ((pixelX | pixelY) & ~1023) ? 0 : (ppu.vram[(((pixelY & 7) << 3) + (pixelX & 7)) + (tile << 6)] >> 8);

          uint8 priority;
          if(!extbg) {
            priority = self.priority_enabled[0] ? self.priority[0] : 0;
          } else {
            priority = self.priority_enabled[palette >> 7] ? self.priority[palette >> 7] : 0;
            palette &= 0x7f;
          }
          if(!palette) continue;

          uint16 color;
          if(io.col.directColor && !extbg) {
            color = directColor(0, palette);
          } else {
            color = cgram[palette];
          }

          pixel = {source, priority, color};
          pixelXp = pixelX;
          pixelYp = pixelY;
        }

        if(doAbove && (!extbg || pixel.priority > above->priority)) *above = pixel;
        if(doBelow && (!extbg || pixel.priority > below->priority)) *below = pixel;
      }
    }
  }

  if(ppu.ss()) {
    uint divisor = scale * scale;
    for(uint p : range(256)) {
      uint ab = 0, bb = 0;
      uint ag = 0, bg = 0;
      uint ar = 0, br = 0;
      for(uint y : range(scale)) {
        auto above = &this->above[p * scale];
        auto below = &this->below[p * scale];
        for(uint x : range(scale)) {
          uint a = above[x].color;
          uint b = below[x].color;
          ab += a >>  0 & 31;
          ag += a >>  5 & 31;
          ar += a >> 10 & 31;
          bb += b >>  0 & 31;
          bg += b >>  5 & 31;
          br += b >> 10 & 31;
        }
      }
      uint16 aboveColor = ab / divisor << 0 | ag / divisor << 5 | ar / divisor << 10;
      uint16 belowColor = bb / divisor << 0 | bg / divisor << 5 | br / divisor << 10;
      this->above[p] = {source, this->above[p * scale].priority, aboveColor};
      this->below[p] = {source, this->below[p * scale].priority, belowColor};
    }
  }
}

//interpolation and extrapolation
auto PPU::Line::lerp(float pa, float va, float pb, float vb, float pr) -> float {
  if(va == vb || pr == pa) return va;
  if(pr == pb) return vb;
  return va + (vb - va) / (pb - pa) * (pr - pa);
}
