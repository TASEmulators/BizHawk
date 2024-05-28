struct Controller {
  Node::Peripheral node;

  virtual ~Controller() = default;
  virtual auto save() -> void {}
  virtual auto comm(n8 send, n8 recv, n8 input[], n8 output[]) -> n2 { return 1; }
  virtual auto reset() -> void {}
  virtual auto read() -> n32 { return 0; }
  virtual auto serialize(serializer&) -> void {}
};

#include "port.hpp"
#include "gamepad/gamepad.hpp"
#include "mouse/mouse.hpp"
