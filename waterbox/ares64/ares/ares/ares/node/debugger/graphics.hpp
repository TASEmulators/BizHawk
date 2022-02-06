struct Graphics : Debugger {
  DeclareClass(Graphics, "debugger.graphics")

  Graphics(string name = {}) : Debugger(name) {
  }

  auto width() const -> u32 { return _width; }
  auto height() const -> u32 { return _height; }
  auto capture() const -> vector<u32> { if(_capture) return _capture(); return {}; }

  auto setSize(u32 width, u32 height) -> void { _width = width, _height = height; }
  auto setCapture(function<vector<u32> ()> capture) -> void { _capture = capture; }

  auto serialize(string& output, string depth) -> void override {
    Debugger::serialize(output, depth);
  }

  auto unserialize(Markup::Node node) -> void override {
    Debugger::unserialize(node);
  }

protected:
  u32 _width = 0;
  u32 _height = 0;
  function<vector<u32> ()> _capture;
};
