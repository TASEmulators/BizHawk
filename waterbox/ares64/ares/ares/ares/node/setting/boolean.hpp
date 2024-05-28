struct Boolean : Setting {
  DeclareClass(Boolean, "setting.boolean")

  Boolean(string name = {}, bool value = {}, function<void (bool)> modify = {}) : Setting(name) {
    _currentValue = value;
    _latchedValue = value;
    _modify = modify;
  }

  auto modify(bool value) const -> void { if(_modify) return _modify(value); }
  auto value() const -> bool { return _currentValue; }
  auto latch() const -> bool { return _latchedValue; }

  auto setModify(function<void (bool)> modify) { _modify = modify; }

  auto setValue(bool value) -> void {
    _currentValue = value;
    if(_dynamic) setLatch();
  }

  auto setLatch() -> void override {
    if(_latchedValue == _currentValue) return;
    _latchedValue = _currentValue;
    modify(_latchedValue);
  }

  auto readValue() const -> string override { return value(); }
  auto readLatch() const -> string override { return latch(); }
  auto readAllowedValues() const -> vector<string> override { return {"false", "true"}; }
  auto writeValue(string value) -> void override { setValue(value.boolean()); }

protected:
  function<void (bool)> _modify;
  bool _currentValue = {};
  bool _latchedValue = {};
};
