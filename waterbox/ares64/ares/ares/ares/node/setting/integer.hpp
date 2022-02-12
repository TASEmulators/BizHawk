struct Integer : Setting {
  DeclareClass(Integer, "setting.integer")

  Integer(string name = {}, s64 value = {}, function<void (s64)> modify = {}) : Setting(name) {
    _currentValue = value;
    _latchedValue = value;
    _modify = modify;
  }

  auto modify(s64 value) const -> void { if(_modify) return _modify(value); }
  auto value() const -> s64 { return _currentValue; }
  auto latch() const -> s64 { return _latchedValue; }

  auto setModify(function<void (s64)> modify) { _modify = modify; }

  auto setValue(s64 value) -> void {
    if(_allowedValues && !_allowedValues.find(value)) return;
    _currentValue = value;
    if(_dynamic) setLatch();
  }

  auto setLatch() -> void override {
    if(_latchedValue == _currentValue) return;
    _latchedValue = _currentValue;
    modify(_latchedValue);
  }

  auto setAllowedValues(vector<s64> allowedValues) -> void {
    _allowedValues = allowedValues;
    if(_allowedValues && !_allowedValues.find(_currentValue)) setValue(_allowedValues.first());
  }

  auto readValue() const -> string override { return value(); }
  auto readLatch() const -> string override { return latch(); }
  auto readAllowedValues() const -> vector<string> override {
    vector<string> values;
    for(auto value : _allowedValues) values.append(value);
    return values;
  }
  auto writeValue(string value) -> void override { setValue(value.integer()); }

protected:
  function<void (s64)> _modify;
  s64 _currentValue = {};
  s64 _latchedValue = {};
  vector<s64> _allowedValues;
};
