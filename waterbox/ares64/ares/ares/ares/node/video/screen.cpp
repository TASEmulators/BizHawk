Screen::Screen(string name, u32 width, u32 height) : Video(name) {
  _canvasWidth  = width;
  _canvasHeight = height;

  if(width && height) {
    _inputA = alloc_invisible<u32>(width * height);
    _inputB = alloc_invisible<u32>(width * height);
    _output = alloc_invisible<u32>(width * height);
    _rotate = alloc_invisible<u32>(width * height);

    if constexpr(ares::Video::Threaded) {
      _thread = nall::thread::create({&Screen::main, this});
    }
  }
}

Screen::~Screen() {
  if constexpr(ares::Video::Threaded) {
    if(_canvasWidth && _canvasHeight) {
      _kill = true;
      _thread.join();
    }
  }
}

auto Screen::main(uintptr_t) -> void {
  while(!_kill) {
    unique_lock<mutex> lock(_frameMutex);

    auto timeout = std::chrono::milliseconds(10);
    if(_frameCondition.wait_for(lock, timeout, [&] { return _frame.load(); })) {
      refresh();
      _frame = false;
    }

    if(_kill) break;
  }
}

auto Screen::quit() -> void {
  _kill = true;
  _thread.join();
  _sprites.reset();
}

auto Screen::power() -> void {
  lock_guard<recursive_mutex> lock(_mutex);
  memory::fill<u32>(_inputA.data(), _canvasWidth * _canvasHeight, _fillColor);
  memory::fill<u32>(_inputB.data(), _canvasWidth * _canvasHeight, _fillColor);
  memory::fill<u32>(_output.data(), _canvasWidth * _canvasHeight, _fillColor);
  memory::fill<u32>(_rotate.data(), _canvasWidth * _canvasHeight, _fillColor);
}

auto Screen::pixels(bool frame) -> array_span<u32> {
  if(frame == 0) return {_inputA.data(), _canvasWidth * _canvasHeight};
  if(frame == 1) return {_inputB.data(), _canvasWidth * _canvasHeight};
  return {};
}

auto Screen::resetPalette() -> void {
  lock_guard<recursive_mutex> lock(_mutex);
  _palette.reset();
}

auto Screen::resetSprites() -> void {
  lock_guard<recursive_mutex> lock(_mutex);
  _sprites.reset();
}

auto Screen::setRefresh(function<void ()> refresh) -> void {
  lock_guard<recursive_mutex> lock(_mutex);
  _refresh = refresh;
}

auto Screen::refreshRateHint(double pixelFrequency, int dotsPerLine, int linesPerFrame) -> void {
  refreshRateHint(1.0f / ((double)(dotsPerLine * linesPerFrame) / pixelFrequency));
}

auto Screen::refreshRateHint(double refreshRate) -> void {
  lock_guard<recursive_mutex> lock(_mutex);
  platform->refreshRateHint(refreshRate);
}

auto Screen::setViewport(u32 x, u32 y, u32 width, u32 height) -> void {
  lock_guard<recursive_mutex> lock(_mutex);
  _viewportX = x;
  _viewportY = y;
  _viewportWidth  = width;
  _viewportHeight = height;
}

auto Screen::setOverscan(bool overscan) -> void {
  lock_guard<recursive_mutex> lock(_mutex);
  _overscan = overscan;
}

auto Screen::setSize(u32 width, u32 height) -> void {
  lock_guard<recursive_mutex> lock(_mutex);
  _width  = width;
  _height = height;
}

auto Screen::setScale(f64 scaleX, f64 scaleY) -> void {
  lock_guard<recursive_mutex> lock(_mutex);
  _scaleX = scaleX;
  _scaleY = scaleY;
}

auto Screen::setAspect(f64 aspectX, f64 aspectY) -> void {
  lock_guard<recursive_mutex> lock(_mutex);
  _aspectX = aspectX;
  _aspectY = aspectY;
}

auto Screen::setSaturation(f64 saturation) -> void {
  lock_guard<recursive_mutex> lock(_mutex);
  _saturation = saturation;
  _palette.reset();
}

auto Screen::setGamma(f64 gamma) -> void {
  lock_guard<recursive_mutex> lock(_mutex);
  _gamma = gamma;
  _palette.reset();
}

auto Screen::setLuminance(f64 luminance) -> void {
  lock_guard<recursive_mutex> lock(_mutex);
  _luminance = luminance;
  _palette.reset();
}

auto Screen::setFillColor(u32 fillColor) -> void {
  lock_guard<recursive_mutex> lock(_mutex);
  _fillColor = fillColor;
}

auto Screen::setColorBleed(bool colorBleed) -> void {
  lock_guard<recursive_mutex> lock(_mutex);
  _colorBleed = colorBleed;
}

auto Screen::setInterframeBlending(bool interframeBlending) -> void {
  lock_guard<recursive_mutex> lock(_mutex);
  _interframeBlending = interframeBlending;
}

auto Screen::setRotation(u32 rotation) -> void {
  lock_guard<recursive_mutex> lock(_mutex);
  _rotation = rotation;
}

auto Screen::setProgressive(bool progressiveDouble) -> void {
  lock_guard<recursive_mutex> lock(_mutex);
  _interlace = false;
  _progressive = true;
  _progressiveDouble = progressiveDouble;
}

auto Screen::setInterlace(bool interlaceField) -> void {
  lock_guard<recursive_mutex> lock(_mutex);
  _progressive = false;
  _interlace = true;
  _interlaceField = interlaceField;
}

auto Screen::attach(Node::Video::Sprite sprite) -> void {
  lock_guard<recursive_mutex> lock(_mutex);
  if(_sprites.find(sprite)) return;
  _sprites.append(sprite);
}

auto Screen::detach(Node::Video::Sprite sprite) -> void {
  lock_guard<recursive_mutex> lock(_mutex);
  if(!_sprites.find(sprite)) return;
  _sprites.removeByValue(sprite);
}

auto Screen::colors(u32 colors, function<n64 (n32)> color) -> void {
  lock_guard<recursive_mutex> lock(_mutex);
  _colors = colors;
  _color = color;
  _palette.reset();
}

auto Screen::frame() -> void {
  if(runAhead()) return;
  while(_frame) spinloop();

  lock_guard<recursive_mutex> lock(_mutex);
  _inputA.swap(_inputB);
  if constexpr(!ares::Video::Threaded) {
    refresh();
    _frame = false;
  } else {
    _frame = true;
    _frameCondition.notify_one();
  }
}

auto Screen::refresh() -> void {
  lock_guard<recursive_mutex> lock(_mutex);
  if(runAhead()) return;

  refreshPalette();
  if(_refresh) _refresh();

  auto viewX = _viewportX;
  auto viewY = _viewportY;
  auto viewWidth  = _viewportWidth;
  auto viewHeight = _viewportHeight;

  auto pitch  = _canvasWidth;
  auto width  = _canvasWidth;
  auto height = _canvasHeight;
  auto input  = _inputB.data();
  auto output = _output.data();

  for(u32 y : range(height)) {
    auto source = input  + y * pitch;
    auto target = output + y * width;

    if(_interlace) {
      if((_interlaceField & 1) == (y & 1)) {
        for(u32 x : range(width)) {
          auto color = _palette[*source++];
          *target++ = color;
        }
      }
    } else if(_progressive && _progressiveDouble) {
      source = input + (y & ~1) * pitch;
      for(u32 x : range(width)) {
        auto color = _palette[*source++];
        *target++ = color;
      }
    } else if(_interframeBlending) {
      n32 mask = 1 << 24 | 1 << 16 | 1 << 8 | 1 << 0;
      for(u32 x : range(width)) {
        auto a = *target;
        auto b = _palette[*source++];
        *target++ = (a + b - ((a ^ b) & mask)) >> 1;
      }
    } else {
      for(u32 x : range(width)) {
        auto color = _palette[*source++];
        *target++ = color;
      }
    }
  }

  if (_colorBleed) {
    n32 mask = 1 << 24 | 1 << 16 | 1 << 8 | 1 << 0;
    for (u32 y : range(height)) {
      auto target = output + y * width;
      for (u32 x : range(0, width, _colorBleedWidth)) {
        for (u32 offset = 0; offset < _colorBleedWidth && (x + offset) < width; ++offset) {
          u32 next = x + _colorBleedWidth;
          if (next + offset >= width) next = x;
          auto a = target[x + offset];
          auto b = target[next + offset];
          target[x + offset] = (a + b - ((a ^ b) & mask)) >> 1;
        }
      }
    }
  }

  for(auto& sprite : _sprites) {
    if(!sprite->visible()) continue;

    n32 alpha = 255u << 24;
    for(int y : range(sprite->height())) {
      s32 pixelY = sprite->y() + y;
      if(pixelY < 0 || pixelY >= height) continue;

      auto source = sprite->image().data() + y * sprite->width();
      auto target = &output[pixelY * width];
      for(s32 x : range(sprite->width())) {
        s32 pixelX = sprite->x() + x;
        if(pixelX < 0 || pixelX >= width) continue;

        auto pixel = source[x];
        if(pixel >> 24) target[pixelX] = alpha | pixel;
      }
    }
  }

  if(_rotation == 90) {
    //rotate left
    for(u32 y : range(height)) {
      auto source = output + y * width;
      for(u32 x : range(width)) {
        auto target = _rotate.data() + (width - 1 - x) * height + y;
        *target = *source++;
      }
    }
    output = _rotate.data();
    swap(width, height);
    swap(viewWidth, viewHeight);
  }

  if(_rotation == 180) {
    //rotate upside down
    for(u32 y : range(height)) {
      auto source = output + y * width;
      for(u32 x : range(width)) {
        auto target = _rotate.data() + (height - 1 - y) * width + (width - 1 - x);
        *target = *source++;
      }
    }
    output = _rotate.data();
  }

  if(_rotation == 270) {
    //rotate right
    for(u32 y : range(height)) {
      auto source = output + y * width;
      for(u32 x : range(width)) {
        auto target = _rotate.data() + x * height + (height - 1 - y);
        *target = *source++;
      }
    }
    output = _rotate.data();
    swap(width, height);
    swap(viewWidth, viewHeight);
  }

  platform->video(shared(), output + viewX + viewY * width, width * sizeof(u32), viewWidth, viewHeight);
  memory::fill<u32>(_inputB.data(), width * height, _fillColor);
}

auto Screen::refreshPalette() -> void {
  lock_guard<recursive_mutex> lock(_mutex);
  if(_palette) return;

  //generate the color lookup palettes to convert native colors to ARGB8888
  _palette = new u32[_colors];
  for(u32 index : range(_colors)) {
    n64 color = _color(index);
    n16 b = color.bit( 0,15);
    n16 g = color.bit(16,31);
    n16 r = color.bit(32,47);
    n16 a = 65535;

    if(_saturation != 1.0) {
      n16 grayscale = uclamp<16>((r + g + b) / 3);
      f64 inverse = max(0.0, 1.0 - _saturation);
      r = uclamp<16>(r * _saturation + grayscale * inverse);
      g = uclamp<16>(g * _saturation + grayscale * inverse);
      b = uclamp<16>(b * _saturation + grayscale * inverse);
    }

    if(_gamma != 1.0) {
      f64 reciprocal = 1.0 / 32767.0;
      r = r > 32767 ? r : n16(32767 * pow(r * reciprocal, _gamma));
      g = g > 32767 ? g : n16(32767 * pow(g * reciprocal, _gamma));
      b = b > 32767 ? b : n16(32767 * pow(b * reciprocal, _gamma));
    }

    if(_luminance != 1.0) {
      r = uclamp<16>(r * _luminance);
      g = uclamp<16>(g * _luminance);
      b = uclamp<16>(b * _luminance);
    }

    a >>= 8;
    r >>= 8;
    g >>= 8;
    b >>= 8;

    _palette[index] = a << 24 | r << 16 | g << 8 | b << 0;
  }
}

auto Screen::serialize(string& output, string depth) -> void {
  Video::serialize(output, depth);
  output.append(depth, "  width: ", _width, "\n");
  output.append(depth, "  height: ", _height, "\n");
  output.append(depth, "  scaleX: ", _scaleX, "\n");
  output.append(depth, "  scaleY: ", _scaleY, "\n");
  output.append(depth, "  aspectX: ", _aspectX, "\n");
  output.append(depth, "  aspectY: ", _aspectY, "\n");
  output.append(depth, "  colors: ", _colors, "\n");
  output.append(depth, "  saturation: ", _saturation, "\n");
  output.append(depth, "  gamma: ", _gamma, "\n");
  output.append(depth, "  luminance: ", _luminance, "\n");
  output.append(depth, "  fillColor: ", _fillColor, "\n");
  output.append(depth, "  colorBleed: ", _colorBleed, "\n");
  output.append(depth, "  interlace: ", _interlace, "\n");
  output.append(depth, "  interframeBlending: ", _interframeBlending, "\n");
  output.append(depth, "  rotation: ", _rotation, "\n");
}

auto Screen::unserialize(Markup::Node node) -> void {
  Video::unserialize(node);
  _width = node["width"].natural();
  _height = node["height"].natural();
  _scaleX = node["scaleX"].real();
  _scaleY = node["scaleY"].real();
  _aspectX = node["aspectX"].real();
  _aspectY = node["aspectY"].real();
  _colors = node["colors"].natural();
  _saturation = node["saturation"].real();
  _gamma = node["gamma"].real();
  _luminance = node["luminance"].real();
  _fillColor = node["fillColor"].natural();
  _colorBleed = node["colorBleed"].boolean();
  _interlace = node["interlace"].natural();
  _interframeBlending = node["interframeBlending"].boolean();
  _rotation = node["rotation"].natural();
  resetPalette();
  resetSprites();
}
