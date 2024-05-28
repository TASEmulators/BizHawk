struct Natural : Setting {
  DeclareClass(Natural, "setting.natural")

  Natural(string name = {}, u64 value = {}, function<void (u64)> modify = {}) : Setting(name) {
    _currentValue = value;
    _latchedValue = value;
    _modify = modify;
  }

  auto modify(u64 value) const -> void { if(_modify) return _modify(value); }
  auto value() const -> u64 { return _currentValue; }
  auto latch() const -> u64 { return _latchedValue; }

  auto setModify(function<void (u64)> modify) { _modify = modify; }

  auto setValue(u64 value) -> void {
    if(_allowedValues && !_allowedValues.find(value)) return;
    _currentValue = value;
    if(_dynamic) setLatch();
  }

  auto setLatch() -> void override {
    if(_latchedValue == _currentValue) return;
    _latchedValue = _currentValue;
    modify(_latchedValue);
  }

  auto setAllowedValues(vector<u64> allowedValues) -> void {
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
  auto writeValue(string value) -> void override { setValue(value.natural()); }

protected:
  function<void (u64)> _modify;
  u64 _currentValue = {};
  u64 _latchedValue = {};
  vector<u64> _allowedValues;
};
