struct Screen : Video {
  DeclareClass(Screen, "video.screen")
  using Video::Video;

  Screen(string name = {}, u32 width = 0, u32 height = 0);
  ~Screen();
  auto main(uintptr_t) -> void;
  auto quit() -> void;
  auto power() -> void;

  auto canvasWidth() const -> u32 { return _canvasWidth; }
  auto canvasHeight() const -> u32 { return _canvasHeight; }
  auto width() const -> u32 { return _width; }
  auto height() const -> u32 { return _height; }
  auto scaleX() const -> f64 { return _scaleX; }
  auto scaleY() const -> f64 { return _scaleY; }
  auto aspectX() const -> f64 { return _aspectX; }
  auto aspectY() const -> f64 { return _aspectY; }
  auto colors() const -> u32 { return _colors; }
  auto pixels(bool frame = 0) -> array_span<u32>;

  auto saturation() const -> double { return _saturation; }
  auto gamma() const -> double { return _gamma; }
  auto luminance() const -> double { return _luminance; }

  auto fillColor() const -> u32 { return _fillColor; }
  auto colorBleed() const -> bool { return _colorBleed; }
  auto interframeBlending() const -> bool { return _interframeBlending; }
  auto rotation() const -> u32 { return _rotation; }

  auto resetPalette() -> void;
  auto resetSprites() -> void;

  auto setRefresh(function<void ()> refresh) -> void;
  auto setViewport(u32 x, u32 y, u32 width, u32 height) -> void;

  auto setSize(u32 width, u32 height) -> void;
  auto setScale(f64 scaleX, f64 scaleY) -> void;
  auto setAspect(f64 aspectX, f64 aspectY) -> void;

  auto setSaturation(f64 saturation) -> void;
  auto setGamma(f64 gamma) -> void;
  auto setLuminance(f64 luminance) -> void;

  auto setFillColor(u32 fillColor) -> void;
  auto setColorBleed(bool colorBleed) -> void;
  auto setInterframeBlending(bool interframeBlending) -> void;
  auto setRotation(u32 rotation) -> void;

  auto setProgressive(bool progressiveDouble = false) -> void;
  auto setInterlace(bool interlaceField) -> void;

  auto attach(Node::Video::Sprite) -> void;
  auto detach(Node::Video::Sprite) -> void;

  auto colors(u32 colors, function<n64 (n32)> color) -> void;
  auto frame() -> void;
  auto refresh() -> void;

  auto serialize(string& output, string depth) -> void override;
  auto unserialize(Markup::Node node) -> void override;

private:
  auto refreshPalette() -> void;

protected:
  u32  _canvasWidth = 0;
  u32  _canvasHeight = 0;
  u32  _width = 0;
  u32  _height = 0;
  f64  _scaleX = 1.0;
  f64  _scaleY = 1.0;
  f64  _aspectX = 1.0;
  f64  _aspectY = 1.0;
  u32  _colors = 0;
  f64  _saturation = 1.0;
  f64  _gamma = 1.0;
  f64  _luminance = 1.0;
  u32  _fillColor = 0;
  bool _colorBleed = false;
  bool _interframeBlending = false;
  u32  _rotation = 0;  //counter-clockwise (90 = left, 270 = right)

  function<n64 (n32)> _color;
  unique_pointer<u32> _inputA;
  unique_pointer<u32> _inputB;
  unique_pointer<u32> _output;
  unique_pointer<u32> _rotate;
  unique_pointer<u32[]> _palette;
  vector<Node::Video::Sprite> _sprites;

//unserialized:
  nall::thread _thread;
  recursive_mutex _mutex;
  atomic<bool> _kill = false;
  atomic<bool> _frame = false;
  function<void ()> _refresh;
  bool _progressive = false;
  bool _progressiveDouble = false;
  bool _interlace = false;
  bool _interlaceField = false;
  u32  _viewportX = 0;
  u32  _viewportY = 0;
  u32  _viewportWidth = 0;
  u32  _viewportHeight = 0;
};
