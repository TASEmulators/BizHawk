struct Button : Input {
  DeclareClass(Button, "input.button")
  using Input::Input;

  auto value() const -> bool { return _value; }
  auto setValue(bool value) -> void { _value = value; }

protected:
  bool _value = 0;
};
