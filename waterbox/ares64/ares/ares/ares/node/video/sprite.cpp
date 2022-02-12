auto Sprite::setVisible(bool visible) -> void {
  _visible = visible;
}

auto Sprite::setPosition(u32 x, u32 y) -> void {
  _x = x;
  _y = y;
}

auto Sprite::setImage(nall::image image, bool invert) -> void {
  _width = image.width();
  _height = image.height();
  _pixels = new u32[_width * _height];
  image.transform(0, 32, 255u << 24, 255u << 16, 255u << 8, 255u << 0);
  memory::copy(_pixels.data(), _width * _height * sizeof(u32), image.data(), image.size());
  if(!invert) return;

  for(u32 y : range(_height)) {
    auto data = _pixels.data() + y * _width;
    for(u32 x : range(_width)) {
      auto pixel = data[x];
      pixel ^= 0xffffff;
      data[x] = pixel;
    }
  }
}
