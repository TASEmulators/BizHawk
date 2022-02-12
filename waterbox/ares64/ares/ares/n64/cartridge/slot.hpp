struct CartridgeSlot {
  Node::Port port;
  Cartridge cartridge;

  //slot.cpp
  CartridgeSlot(string name);
  auto load(Node::Object) -> void;
  auto unload() -> void;

  const string name;
};

extern CartridgeSlot cartridgeSlot;
