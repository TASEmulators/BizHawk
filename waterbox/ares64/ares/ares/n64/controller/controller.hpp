struct Controller {
  Node::Peripheral node;

  virtual ~Controller() = default;
  virtual auto save() -> void {}
  virtual auto read() -> n32 { return 0; }
  virtual auto serialize(serializer&) -> void {}
};

#include "port.hpp"
#include "gamepad/gamepad.hpp"
