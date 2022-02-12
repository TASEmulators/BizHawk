struct Port : Object {
  DeclareClass(Port, "port")
  using Object::Object;

  auto type() const -> string { return _type; }
  auto family() const -> string { return _family; }
  auto hotSwappable() const -> bool { return _hotSwappable; }
  auto supported() const -> vector<string> { return _supported; }

  auto setAllocate(function<Node::Peripheral (string)> allocate) -> void { _allocate = allocate; }
  auto setConnect(function<void ()> connect) -> void { _connect = connect; }
  auto setDisconnect(function<void ()> disconnect) -> void { _disconnect = disconnect; }
  auto setType(string type) -> void { _type = type; }
  auto setFamily(string family) -> void { _family = family; }
  auto setHotSwappable(bool hotSwappable) -> void { _hotSwappable = hotSwappable; }
  auto setSupported(vector<string> supported) -> void { _supported = supported; }

  auto connected() -> Node::Peripheral {
    return find<Node::Peripheral>(0);
  }

  auto allocate(string name = {}) -> Node::Peripheral {
    disconnect();
    if(_allocate) return _allocate(name);
    return {};
  }

  auto connect() -> void {
    if(_connect) _connect();
  }

  auto disconnect() -> void {
    if(auto peripheral = connected()) {
      if(_disconnect) _disconnect();
      remove(peripheral);
    }
  }

  auto serialize(string& output, string depth) -> void override {
    Object::serialize(output, depth);
    output.append(depth, "  type: ", _type, "\n");
    output.append(depth, "  family: ", _family, "\n");
    output.append(depth, "  hotSwappable: ", _hotSwappable, "\n");
  }

  auto unserialize(Markup::Node node) -> void override {
    Object::unserialize(node);
    _type = node["type"].string();
    _family = node["family"].string();
    _hotSwappable = node["hotSwappable"].boolean();
  }

  auto copy(Node::Object object) -> void override {
    if(auto source = object->cast<Node::Port>()) {
      Object::copy(source);
      if(auto peripheral = source->find<Node::Peripheral>(0)) {
        if(auto node = allocate(peripheral->name())) {
          node->copy(peripheral);
          connect();
          node->copy(peripheral);
        }
      }
    }
  }

protected:
  function<Node::Peripheral (string)> _allocate;
  function<void ()> _connect;
  function<void ()> _disconnect;
  string _type;
  string _family;
  bool _hotSwappable = false;
  vector<string> _supported;
};
