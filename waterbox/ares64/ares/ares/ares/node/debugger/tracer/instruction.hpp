struct Instruction : Tracer {
  DeclareClass(Instruction, "debugger.tracer.instruction")

  Instruction(string name = {}, string component = {}) : Tracer(name, component) {
    setMask(_mask);
    setDepth(_depth);
  }

  auto addressBits() const -> u32 { return _addressBits; }
  auto addressMask() const -> u32 { return _addressMask; }
  auto mask() const -> bool { return _mask; }
  auto depth() const -> u32 { return _depth; }

  auto setAddressBits(u32 addressBits, u32 addressMask = 0) -> void {
    _addressBits = addressBits;
    _addressMask = addressMask;
  }

  auto setMask(bool mask) -> void {
    _mask = mask;
  }

  auto setDepth(u32 depth) -> void {
    _depth = depth;
    _history.reset();
    _history.resize(depth);
    for(auto& history : _history) history = ~0;
  }

  auto address(u32 address) -> bool {
    address &= (1ull << _addressBits) - 1;  //mask upper bits of address
    _address = address;
    address >>= _addressMask;  //clip unneeded alignment bits (to reduce _masks size)

    if(_mask && updateMasks()) {
      if(_masks[address >> 3] & 1 << (address & 7)) return false;  //do not trace twice
      _masks[address >> 3] |= 1 << (address & 7);
    }

    if(_depth) {
      for(auto history : _history) {
        if(_address == history) {
          _omitted++;
          return false;  //do not trace again if recently traced
        }
      }
      for(auto index : range(_depth - 1)) {
        _history[index] = _history[index + 1];
      }
      _history.last() = _address;
    }

    return true;
  }

  //mark an already-executed address as not executed yet for trace masking.
  //call when writing to executable RAM to support self-modifying code.
  auto invalidate(u32 address) -> void {
    if(unlikely(_mask && updateMasks())) {
      address &= (1ull << _addressBits) - 1;
      address >>= _addressMask;
      _masks[address >> 3] &= ~(1 << (address & 7));
    }
  }

  auto notify(const string& instruction, const string& context, const string& extra = {}) -> void {
    if(!enabled()) return;

    if(_omitted) {
      PlatformLog({
        "[Omitted: ", _omitted, "]\n"}
      );
      _omitted = 0;
    }

    string output{
      _component, "  ",
      hex(_address, _addressBits + 3 >> 2), "  ",
      instruction, "  ",
      context, "  ",
      extra
    };
    PlatformLog({output.strip(), "\n"});
  }

  auto serialize(string& output, string depth) -> void override {
    Tracer::serialize(output, depth);
    output.append(depth, "  addressBits: ", _addressBits, "\n");
    output.append(depth, "  addressMask: ", _addressMask, "\n");
    output.append(depth, "  mask: ", _mask, "\n");
    output.append(depth, "  depth: ", _depth, "\n");
  }

  auto unserialize(Markup::Node node) -> void override {
    Tracer::unserialize(node);
    _addressBits = node["addressBits"].natural();
    _addressMask = node["addressMask"].natural();
    _mask = node["mask"].boolean();
    _depth = node["depth"].natural();

    setMask(_mask);
    setDepth(_depth);
  }

protected:
  auto updateMasks() -> bool {
    auto size = 1ull << _addressBits >> _addressMask >> 3;
    if(!_mask || !size) return _masks.reset(), false;
    if(_masks.size() == size) return true;
    _masks.reset();
    _masks.resize(size);
    return true;
  }

  u32  _addressBits = 32;
  u32  _addressMask = 0;
  bool _mask = false;
  u32  _depth = 4;

//unserialized:
  n64 _address = 0;
  n64 _omitted = 0;
  vector<u32> _history;
  vector<u08> _masks;
};
