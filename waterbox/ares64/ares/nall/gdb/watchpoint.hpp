#pragma once

#include <nall/tcptext/tcptext-server.hpp>

namespace nall::GDB {

  enum class WatchpointType : u32 {
    WRITE, READ, ACCESS
  };

  struct Watchpoint {
    u64 addressStart{0};
    u64 addressEnd{0};
    u64 addressStartOrg{0}; // un-normalized address, GDB needs this
    WatchpointType type{};

    auto operator==(const Watchpoint& w) const {
      return addressStart == w.addressStart && addressEnd == w.addressEnd
        && addressStartOrg == w.addressStartOrg && type == w.type;
    }

    auto hasOverlap(u64 start, u64 end) const {
      return (end >= addressStart) && (start <= addressEnd);
    }

    auto getTypePrefix() const -> string {
      if(type == WatchpointType::WRITE)return "watch:";
      if(type == WatchpointType::READ)return "rwatch:";
      return "awatch:";
    }
  };
}