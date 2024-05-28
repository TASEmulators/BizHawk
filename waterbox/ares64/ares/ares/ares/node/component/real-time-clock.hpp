struct RealTimeClock : Component {
  DeclareClass(RealTimeClock, "component.real-time-clock")
  using Component::Component;

  auto update() -> void { if(_update) return _update(); }
  auto timestamp() const -> u64 { return _timestamp; }

  auto setUpdate(function<void ()> update) -> void { _update = update; }
  auto setTimestamp(u64 timestamp) -> void { _timestamp = timestamp; }

  auto synchronize(u64 timestamp = 0) -> void {
    if(!timestamp) timestamp = chrono::timestamp();
    _timestamp = timestamp;
    update();
  }

  auto serialize(string& output, string depth) -> void override {
    Component::serialize(output, depth);
    output.append(depth, "  timestamp: ", _timestamp, "\n");
  }

  auto unserialize(Markup::Node node) -> void override {
    Component::unserialize(node);
    _timestamp = node["timestamp"].natural();
  }

protected:
  function<void ()> _update;
  u64 _timestamp;
};
