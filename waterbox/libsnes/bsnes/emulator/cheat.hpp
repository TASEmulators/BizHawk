#pragma once

namespace Emulator {

struct Cheat {
  struct Code {
    auto operator==(const Code& code) const -> bool {
      if(address != code.address) return false;
      if(data != code.data) return false;
      if((bool)compare != (bool)code.compare) return false;
      if(compare && code.compare && compare() != code.compare()) return false;
      return true;
    }

    uint address;
    uint data;
    maybe<uint> compare;
    bool enable;
    uint restore;
  };

  explicit operator bool() const {
    return codes.size() > 0;
  }

  auto reset() -> void {
    codes.reset();
  }

  auto append(uint address, uint data, maybe<uint> compare = {}) -> void {
    codes.append({address, data, compare});
  }

  auto assign(const vector<string>& list) -> void {
    reset();
    for(auto& entry : list) {
      for(auto code : entry.split("+")) {
        auto part = code.transform("=?", "//").split("/");
        if(part.size() == 2) append(part[0].hex(), part[1].hex());
        if(part.size() == 3) append(part[0].hex(), part[2].hex(), part[1].hex());
      }
    }
  }

  auto find(uint address, uint compare) -> maybe<uint> {
    for(auto& code : codes) {
      if(code.address == address && (!code.compare || code.compare() == compare)) {
        return code.data;
      }
    }
    return nothing;
  }

  vector<Code> codes;
};

}
