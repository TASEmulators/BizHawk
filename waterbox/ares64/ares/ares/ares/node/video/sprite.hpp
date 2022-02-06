struct Sprite : Video {
  DeclareClass(Sprite, "video.sprite")
  using Video::Video;

  auto visible() const -> bool { return _visible; }
  auto x() const -> u32 { return _x; }
  auto y() const -> u32 { return _y; }
  auto width() const -> u32 { return _width; }
  auto height() const -> u32 { return _height; }
  auto image() const -> array_view<u32> { return {_pixels.data(), _width * _height}; }

  auto setVisible(bool visible) -> void;
  auto setPosition(u32 x, u32 y) -> void;
  auto setImage(nall::image, bool invert = false) -> void;

protected:
  bool _visible = false;
  u32  _x = 0;
  u32  _y = 0;
  u32  _width = 0;
  u32  _height = 0;
  unique_pointer<u32[]> _pixels;
};
