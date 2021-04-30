struct Mosaic {
  //mosaic.cpp
  alwaysinline auto enable() const -> bool;
  alwaysinline auto voffset() const -> uint;
  auto scanline() -> void;
  auto power() -> void;

  //serialization.cpp
  auto serialize(serializer&) -> void;

  uint5 size;
  uint5 vcounter;
};
