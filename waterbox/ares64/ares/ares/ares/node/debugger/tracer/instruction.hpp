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
    _masks.reset();
  }

  auto setDepth(u32 depth) -> void {
    _depth = depth;
    _history.reset();
    _history.resize(depth);
    for(auto& history : _history) history = ~0ull;
  }

  auto address(u64 address) -> bool {
    address &= ~0ull >> (64 - _addressBits);  //mask upper bits of address
    _address = address;
    address >>= _addressMask;  //clip unneeded alignment bits (to reduce _masks size)

    if(_mask) {
      auto mask = _masks.find(address);
      if(!mask) mask = _masks.insert(address);
      if(mask->visit(address)) return false;  //do not trace twice
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
  auto invalidate(u64 address) -> void {
    if(unlikely(_mask)) {
      address &= ~0ull >> (64 - _addressBits);
      address >>= _addressMask;

      auto mask = _masks.find(address);
      if(mask) mask->unvisit(address);
    }
  }

  auto notify(const string& instruction, const string& context, const string& extra = {}) -> void {
    if(!enabled()) return;

    if(_omitted) {
      PlatformLog(shared(), {"[Omitted: ", _omitted, "]"});
      _omitted = 0;
    }

    string output{
      _component, "  ",
      hex(_address, _addressBits + 3 >> 2), "  ",
      instruction, "  ",
      context, "  ",
      extra
    };
    PlatformLog(shared(), {output.strip()});
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
  struct VisitMask {
    VisitMask(u64 address) : upper(address >> 6), mask(0) {}
    auto operator==(const VisitMask& source) const -> bool { return upper == source.upper; }
    auto hash() const -> u32 { return upper; }

    auto visit(u64 address) -> bool {
      const u64 bit = 1ull << (address & 0x3f);
      if(mask & bit) return true;
      mask |= bit;
      return false;
    }

    auto unvisit(u64 address) -> void {
      const u64 bit = 1ull << (address & 0x3f);
      mask &= ~bit;
    }

  private:
    u64 upper;
    u64 mask;
  };

  u32  _addressBits = 32;
  u32  _addressMask = 0;
  bool _mask = false;
  u32  _depth = 4;

//unserialized:
  n64 _address = 0;
  n64 _omitted = 0;
  vector<u64> _history;
  hashset<VisitMask> _masks;
};
