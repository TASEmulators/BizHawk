struct Setting : Object {
  DeclareClass(Setting, "setting")
  using Object::Object;

  auto dynamic() const -> bool { return _dynamic; }

  auto setDynamic(bool dynamic) -> void {
    _dynamic = dynamic;
  }

  virtual auto setLatch() -> void {}

  virtual auto readValue() const -> string { return {}; }
  virtual auto readLatch() const -> string { return {}; }
  virtual auto readAllowedValues() const -> vector<string> { return {}; }
  virtual auto writeValue(string value) -> void {}

  auto load(Node::Object source) -> bool override {
    if(!Object::load(source)) return false;
    if(auto setting = source->cast<shared_pointer<Setting>>()) writeValue(setting->readValue());
    return true;
  }

  auto copy(Node::Object object) -> void override {
    if(auto source = object->cast<Node::Setting::Setting>()) {
      Object::copy(source);
      writeValue(source->readValue());
      setLatch();
    }
  }

  auto serialize(string& output, string depth) -> void override {
    Object::serialize(output, depth);
    output.append(depth, "  dynamic: ", _dynamic, "\n");
    output.append(depth, "  value: ", readValue(), "\n");
  }

  auto unserialize(Markup::Node node) -> void override {
    Object::unserialize(node);
    _dynamic = node["dynamic"].boolean();
    writeValue(node["value"].string());
  }

protected:
  bool _dynamic = false;
};
