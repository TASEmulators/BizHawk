struct Expansion : Thread {
  Expansion();
  virtual ~Expansion();
};

struct ExpansionPort {
  auto connect(uint deviceID) -> void;

  auto power() -> void;
  auto unload() -> void;
  auto serialize(serializer&) -> void;

  Expansion* device = nullptr;
};

extern ExpansionPort expansionPort;

#include <sfc/expansion/satellaview/satellaview.hpp>
//#include <sfc/expansion/21fx/21fx.hpp>
