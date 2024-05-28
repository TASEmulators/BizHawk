#pragma once

namespace nall {

inline auto image::impose(blend mode, u32 targetX, u32 targetY, image source, u32 sourceX, u32 sourceY, u32 sourceWidth, u32 sourceHeight) -> void {
  source.transform(_endian, _depth, _alpha.mask(), _red.mask(), _green.mask(), _blue.mask());

  for(u32 y = 0; y < sourceHeight; y++) {
    const u8* sp = source._data + source.pitch() * (sourceY + y) + source.stride() * sourceX;
    u8* dp = _data + pitch() * (targetY + y) + stride() * targetX;
    for(u32 x = 0; x < sourceWidth; x++) {
      u64 sourceColor = source.read(sp);
      u64 targetColor = read(dp);

      s64 sa = (sourceColor & _alpha.mask()) >> _alpha.shift();
      s64 sr = (sourceColor & _red.mask()  ) >> _red.shift();
      s64 sg = (sourceColor & _green.mask()) >> _green.shift();
      s64 sb = (sourceColor & _blue.mask() ) >> _blue.shift();

      s64 da = (targetColor & _alpha.mask()) >> _alpha.shift();
      s64 dr = (targetColor & _red.mask()  ) >> _red.shift();
      s64 dg = (targetColor & _green.mask()) >> _green.shift();
      s64 db = (targetColor & _blue.mask() ) >> _blue.shift();

      u64 a, r, g, b;

      switch(mode) {
      case blend::add:
        a = max(sa, da);
        r = min(_red.mask()   >> _red.shift(),   ((sr * sa) >> _alpha.depth()) + ((dr * da) >> _alpha.depth()));
        g = min(_green.mask() >> _green.shift(), ((sg * sa) >> _alpha.depth()) + ((dg * da) >> _alpha.depth()));
        b = min(_blue.mask()  >> _blue.shift(),  ((sb * sa) >> _alpha.depth()) + ((db * da) >> _alpha.depth()));
        break;

      case blend::sourceAlpha:
        a = max(sa, da);
        r = dr + (((sr - dr) * sa) >> _alpha.depth());
        g = dg + (((sg - dg) * sa) >> _alpha.depth());
        b = db + (((sb - db) * sa) >> _alpha.depth());
        break;

      case blend::sourceColor:
        a = sa;
        r = sr;
        g = sg;
        b = sb;
        break;

      case blend::targetAlpha:
        a = max(sa, da);
        r = sr + (((dr - sr) * da) >> _alpha.depth());
        g = sg + (((dg - sg) * da) >> _alpha.depth());
        b = sb + (((db - sb) * da) >> _alpha.depth());
        break;

      case blend::targetColor:
        a = da;
        r = dr;
        g = dg;
        b = db;
        break;
      }

      write(dp, (a << _alpha.shift()) | (r << _red.shift()) | (g << _green.shift()) | (b << _blue.shift()));
      sp += source.stride();
      dp += stride();
    }
  }
}

}
