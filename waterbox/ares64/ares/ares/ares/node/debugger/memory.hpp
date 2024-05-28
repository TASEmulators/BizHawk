struct Memory : Debugger {
  DeclareClass(Memory, "debugger.memory")

  Memory(string name = {}) : Debugger(name) {
  }

  auto size() const -> u32 { return _size; }
  auto read(u32 address) const -> n8 { if(_read) return _read(address); return 0; }
  auto write(u32 address, u8 data) const -> void { if(_write) return _write(address, data); }

  auto setSize(u32 size) -> void { _size = size; }
  auto setRead(function<u8 (u32)> read) -> void { _read = read; }
  auto setWrite(function<void (u32, u8)> write) -> void { _write = write; }

  auto serialize(string& output, string depth) -> void override {
    Debugger::serialize(output, depth);
  }

  auto unserialize(Markup::Node node) -> void override {
    Debugger::unserialize(node);
  }

protected:
  u32 _size = 0;
  function<u8 (u32)> _read;
  function<void (u32, u8)> _write;
};
