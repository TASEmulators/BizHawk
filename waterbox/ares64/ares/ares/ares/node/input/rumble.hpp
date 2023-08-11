struct Rumble : Input {
  DeclareClass(Rumble, "input.rumble")
  using Input::Input;

  auto weakValue() const -> u16 { return _weak; }
  auto strongValue() const -> u16 { return _strong; }

  auto setValues(u16 weak, u16 strong) -> void { _weak = weak; _strong = strong; }

  // For systems with binary motors
  auto enable() const -> bool { return _weak > 0 || _strong > 0; }
  auto setEnable(bool enable) -> void { _weak = enable ? 65535 : 0; _strong = enable ? 65535 : 0; }
protected:
  u16 _weak = 0;
  u16 _strong = 0;
};
