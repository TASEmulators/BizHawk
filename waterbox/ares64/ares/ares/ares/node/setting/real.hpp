struct Real : Setting {
  DeclareClass(Real, "setting.real")

  Real(string name = {}, f64 value = {}, function<void (f64)> modify = {}) : Setting(name) {
    _currentValue = value;
    _latchedValue = value;
    _modify = modify;
  }

  auto modify(f64 value) const -> void { if(_modify) return _modify(value); }
  auto value() const -> f64 { return _currentValue; }
  auto latch() const -> f64 { return _latchedValue; }

  auto setModify(function<void (f64)> modify) { _modify = modify; }

  auto setValue(f64 value) -> void {
    if(_allowedValues && !_allowedValues.find(value)) return;
    _currentValue = value;
    if(_dynamic) setLatch();
  }

  auto setLatch() -> void override {
    if(_latchedValue == _currentValue) return;
    _latchedValue = _currentValue;
    modify(_latchedValue);
  }

  auto setAllowedValues(vector<f64> allowedValues) -> void {
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
  auto writeValue(string value) -> void override { setValue(value.real()); }

protected:
  function<void (f64)> _modify;
  f64 _currentValue = {};
  f64 _latchedValue = {};
  vector<f64> _allowedValues;
};
