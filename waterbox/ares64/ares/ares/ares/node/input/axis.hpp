struct Axis : Input {
  DeclareClass(Axis, "input.axis")
  using Input::Input;

  auto value() const -> s64 { return _value; }
  auto setValue(s64 value) -> void { _value = value; }

protected:
  s64 _value = 0;
  s64 _minimum = -32768;
  s64 _maximum = +32767;
};
