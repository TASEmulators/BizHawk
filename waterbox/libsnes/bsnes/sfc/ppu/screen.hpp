struct Screen {
  auto scanline() -> void;
  auto run() -> void;
  auto power() -> void;

  auto below(bool hires) -> uint16;
  auto above() -> uint16;

  auto blend(uint x, uint y) const -> uint15;
  alwaysinline auto paletteColor(uint8 palette) const -> uint15;
  alwaysinline auto directColor(uint8 palette, uint3 paletteGroup) const -> uint15;
  alwaysinline auto fixedColor() const -> uint15;

  auto serialize(serializer&) -> void;

  uint16* lineA;
  uint16* lineB;

  uint15 cgram[256];

  struct IO {
    uint1 blendMode;
    uint1 directColor;

    uint1 colorMode;
    uint1 colorHalve;
    struct Layer {
      uint1 colorEnable;
    } bg1, bg2, bg3, bg4, obj, back;

    uint5 colorBlue;
    uint5 colorGreen;
    uint5 colorRed;
  } io;

  struct Math {
    struct Screen {
      uint15 color;
       uint1 colorEnable;
    } above, below;
    uint1 transparent;
    uint1 blendMode;
    uint1 colorHalve;
  } math;

  friend class PPU;
};
