struct String : Setting {
  DeclareClass(String, "setting.string")

  String(string name = {}, string value = {}, function<void (string)> modify = {}) : Setting(name) {
    _currentValue = value;
    _latchedValue = value;
    _modify = modify;
  }

  auto modify(string value) const -> void { if(_modify) return _modify(value); }
  auto value() const -> string { return _currentValue; }
  auto latch() const -> string { return _latchedValue; }

  auto setModify(function<void (string)> modify) { _modify = modify; }

  auto setValue(string value) -> void {
    if(_allowedValues && !_allowedValues.find(value)) return;
    _currentValue = value;
    if(_dynamic) setLatch();
  }

  auto setLatch() -> void override {
    if(_latchedValue == _currentValue) return;
    _latchedValue = _currentValue;
    modify(_latchedValue);
  }

  auto setAllowedValues(vector<string> allowedValues) -> void {
    _allowedValues = allowedValues;
    if(_allowedValues && !_allowedValues.find(_currentValue)) setValue(_allowedValues.first());
  }

  auto readValue() const -> string override { return value(); }
  auto readLatch() const -> string override { return latch(); }
  auto readAllowedValues() const -> vector<string> override { return _allowedValues; }
  auto writeValue(string value) -> void override { setValue(value); }

protected:
  function<void (string)> _modify;
  string _currentValue = {};
  string _latchedValue = {};
  vector<string> _allowedValues;
};
