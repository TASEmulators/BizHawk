auto PIF::Debugger::load(Node::Object parent) -> void {
  memory.ram = parent->append<Node::Debugger::Memory>("PIF RAM");
  memory.ram->setSize(64);
  memory.ram->setRead([&](u32 address) -> u8 {
    return pif.ram.read<Byte>(address);
  });
  memory.ram->setWrite([&](u32 address, u8 data) -> void {
    return pif.ram.write<Byte>(address, data);
  });
}
