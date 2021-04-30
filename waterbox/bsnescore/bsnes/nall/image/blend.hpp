#pragma once

namespace nall {

auto image::impose(blend mode, unsigned targetX, unsigned targetY, image source, unsigned sourceX, unsigned sourceY, unsigned sourceWidth, unsigned sourceHeight) -> void {
  source.transform(_endian, _depth, _alpha.mask(), _red.mask(), _green.mask(), _blue.mask());

  for(unsigned y = 0; y < sourceHeight; y++) {
    const uint8_t* sp = source._data + source.pitch() * (sourceY + y) + source.stride() * sourceX;
    uint8_t* dp = _data + pitch() * (targetY + y) + stride() * targetX;
    for(unsigned x = 0; x < sourceWidth; x++) {
      uint64_t sourceColor = source.read(sp);
      uint64_t targetColor = read(dp);

      int64_t sa = (sourceColor & _alpha.mask()) >> _alpha.shift();
      int64_t sr = (sourceColor & _red.mask()  ) >> _red.shift();
      int64_t sg = (sourceColor & _green.mask()) >> _green.shift();
      int64_t sb = (sourceColor & _blue.mask() ) >> _blue.shift();

      int64_t da = (targetColor & _alpha.mask()) >> _alpha.shift();
      int64_t dr = (targetColor & _red.mask()  ) >> _red.shift();
      int64_t dg = (targetColor & _green.mask()) >> _green.shift();
      int64_t db = (targetColor & _blue.mask() ) >> _blue.shift();

      uint64_t a, r, g, b;

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
