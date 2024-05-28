struct Tracer : Debugger {
  DeclareClass(Tracer, "debugger.tracer")

  Tracer(string name = {}, string component = {}) : Debugger(name) {
    _component = component;
  }

  auto component() const -> string { return _component; }
  auto enabled() const -> bool { return file() || terminal(); }
  auto prefix() const -> bool { return _prefix; }
  auto terminal() const -> bool { return _terminal; }
  auto file() const -> bool { return _file; }
  auto autoLineBreak() const -> bool { return _autoLineBreak; }

  auto setToggle(function<void ()> toggle) -> void { _toggle = toggle; }
  auto setComponent(string component) -> void { _component = component; }
  auto setPrefix(bool prefix) -> void { _prefix = prefix; }
  auto setTerminal(bool terminal) -> void { _terminal = terminal; if(_toggle) _toggle(); }
  auto setFile(bool file) -> void { _file = file; if(_toggle) _toggle(); }
  auto setAutoLineBreak(bool autoLineBreak) -> void { _autoLineBreak = autoLineBreak; }

  auto serialize(string& output, string depth) -> void override {
    Debugger::serialize(output, depth);
    output.append(depth, "  component: ", _component, "\n");
    output.append(depth, "  prefix: ", _prefix, "\n");
    output.append(depth, "  terminal: ", _terminal, "\n");
    output.append(depth, "  file: ", _file, "\n");
  }

  auto unserialize(Markup::Node node) -> void override {
    Debugger::unserialize(node);
    _component = node["component"].string();
    _prefix = node["prefix"].boolean();
    _terminal = node["terminal"].boolean();
    _file = node["file"].boolean();
  }

protected:
  function<void ()> _toggle;
  string _component;
  bool _prefix = false;
  bool _terminal = false;
  bool _file = false;
  bool _autoLineBreak = true;
};
