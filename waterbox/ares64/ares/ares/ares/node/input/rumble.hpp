struct Rumble : Input {
  DeclareClass(Rumble, "input.rumble")
  using Input::Input;

  auto enable() const -> bool { return _enable; }
  auto setEnable(bool enable) -> void { _enable = enable; }

protected:
  bool _enable = 0;
};
