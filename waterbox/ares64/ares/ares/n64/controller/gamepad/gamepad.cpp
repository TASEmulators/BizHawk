Gamepad::Gamepad(Node::Port parent) {
  node = parent->append<Node::Peripheral>("Gamepad");

  port = node->append<Node::Port>("Pak");
  port->setFamily("Nintendo 64");
  port->setType("Pak");
  port->setHotSwappable(true);
  port->setAllocate([&](auto name) { return allocate(name); });
  port->setConnect([&] { return connect(); });
  port->setDisconnect([&] { return disconnect(); });
  port->setSupported({"Controller Pak", "Rumble Pak"});

  x           = node->append<Node::Input::Axis>  ("X-Axis");
  y           = node->append<Node::Input::Axis>  ("Y-Axis");
  up          = node->append<Node::Input::Button>("Up");
  down        = node->append<Node::Input::Button>("Down");
  left        = node->append<Node::Input::Button>("Left");
  right       = node->append<Node::Input::Button>("Right");
  b           = node->append<Node::Input::Button>("B");
  a           = node->append<Node::Input::Button>("A");
  cameraUp    = node->append<Node::Input::Button>("C-Up");
  cameraDown  = node->append<Node::Input::Button>("C-Down");
  cameraLeft  = node->append<Node::Input::Button>("C-Left");
  cameraRight = node->append<Node::Input::Button>("C-Right");
  l           = node->append<Node::Input::Button>("L");
  r           = node->append<Node::Input::Button>("R");
  z           = node->append<Node::Input::Button>("Z");
  start       = node->append<Node::Input::Button>("Start");
}

Gamepad::~Gamepad() {
  disconnect();
}

auto Gamepad::save() -> void {
  if(!slot) return;
  if(slot->name() == "Controller Pak") {
    ram.save(pak->write("save.pak"));
  }
}

auto Gamepad::allocate(string name) -> Node::Peripheral {
  if(name == "Controller Pak") return slot = port->append<Node::Peripheral>("Controller Pak");
  if(name == "Rumble Pak"    ) return slot = port->append<Node::Peripheral>("Rumble Pak");
  return {};
}

auto Gamepad::connect() -> void {
  if(!slot) return;
  if(slot->name() == "Controller Pak") {
    node->setPak(pak = platform->pak(node));
    ram.allocate(32_KiB);
    formatControllerPak();
    if(auto fp = pak->read("save.pak")) {
      if(fp->attribute("loaded").boolean()) {
        ram.load(pak->read("save.pak"));
      }
    }
  }
  if(slot->name() == "Rumble Pak") {
    motor = node->append<Node::Input::Rumble>("Rumble");
  }
}

auto Gamepad::disconnect() -> void {
  if(!slot) return;
  if(slot->name() == "Controller Pak") {
    save();
    ram.reset();
  }
  if(slot->name() == "Rumble Pak") {
    rumble(false);
    node->remove(motor);
    motor.reset();
  }
  port->remove(slot);
  slot.reset();
}

auto Gamepad::rumble(bool enable) -> void {
  if(!motor) return;
  motor->setEnable(enable);
  platform->input(motor);
}

auto Gamepad::read() -> n32 {
  platform->input(x);
  platform->input(y);
  platform->input(up);
  platform->input(down);
  platform->input(left);
  platform->input(right);
  platform->input(b);
  platform->input(a);
  platform->input(cameraUp);
  platform->input(cameraDown);
  platform->input(cameraLeft);
  platform->input(cameraRight);
  platform->input(l);
  platform->input(r);
  platform->input(z);
  platform->input(start);

  //scale {-32768 ... +32767} to {-84 ... +84}
  auto ax = x->value() * 85.0 / 32767.0;
  auto ay = y->value() * 85.0 / 32767.0;

  //create scaled circular dead-zone in range {-15 ... +15}
  auto length = sqrt(ax * ax + ay * ay);
  if(length < 16.0) {
    length = 0.0;
  } else if(length > 85.0) {
    length = 85.0 / length;
  } else {
    length = (length - 16.0) * 85.0 / 69.0 / length;
  }
  ax *= length;
  ay *= length;

  //bound diagonals to an octagonal range {-68 ... +68}
  if(ax != 0.0 && ay != 0.0) {
    auto slope = ay / ax;
    auto edgex = copysign(85.0 / (abs(slope) + 16.0 / 69.0), ax);
    auto edgey = copysign(min(abs(edgex * slope), 85.0 / (1.0 / abs(slope) + 16.0 / 69.0)), ay);
    edgex = edgey / slope;

    auto scale = sqrt(edgex * edgex + edgey * edgey) / 85.0;
    ax *= scale;
    ay *= scale;
  }

  n32 data;
  data.byte(0) = -ay;
  data.byte(1) = +ax;
  data.bit(16) = cameraRight->value();
  data.bit(17) = cameraLeft->value();
  data.bit(18) = cameraDown->value();
  data.bit(19) = cameraUp->value();
  data.bit(20) = r->value();
  data.bit(21) = l->value();
  data.bit(22) = 0;  //GND
  data.bit(23) = 0;  //RST
  data.bit(24) = right->value() & !left->value();
  data.bit(25) = left->value() & !right->value();
  data.bit(26) = down->value() & !up->value();
  data.bit(27) = up->value() & !down->value();
  data.bit(28) = start->value();
  data.bit(29) = z->value();
  data.bit(30) = b->value();
  data.bit(31) = a->value();

  //when L+R+Start are pressed: the X/Y axes are zeroed, RST is set, and Start is cleared
  if(l->value() && r->value() && start->value()) {
    data.byte(0) = 0;  //Y-Axis
    data.byte(1) = 0;  //X-Axis
    data.bit(23) = 1;  //RST
    data.bit(28) = 0;  //Start
  }

  return data;
}

//controller paks contain 32KB of SRAM split into 128 pages of 256 bytes each.
//the first 5 pages are for storing system data, and the remaining 123 for game data.
auto Gamepad::formatControllerPak() -> void {
  ram.fill(0x00);

  //page 0 (system area)
  n6  fieldA = random();
  n19 fieldB = random();
  n27 fieldC = random();
  for(u32 area : array<u8[4]>{1,3,4,6}) {
    ram.write<Byte>(area * 0x20 + 0x01, fieldA);  //unknown
    ram.write<Word>(area * 0x20 + 0x04, fieldB);  //serial# hi
    ram.write<Word>(area * 0x20 + 0x08, fieldC);  //serial# lo
    ram.write<Half>(area * 0x20 + 0x18, 0x0001);  //device ID
    ram.write<Byte>(area * 0x20 + 0x1a, 0x01);    //banks (0x01 = 32KB)
    ram.write<Byte>(area * 0x20 + 0x1b, 0x00);    //version#
    u16 checksum = 0;
    u16 inverted = 0;
    for(u32 half : range(14)) {
      u16 data = ram.read<Half>(area * 0x20 + half * 2);
      checksum +=  data;
      inverted += ~data;
    }
    ram.write<Half>(area * 0x20 + 0x1c, checksum);
    ram.write<Half>(area * 0x20 + 0x1e, inverted);
  }

  //pages 1+2 (inode table)
  for(u32 page : array<u8[2]>{1,2}) {
    ram.write<Byte>(0x100 * page + 0x01, 0x71);  //unknown
    for(u32 slot : range(5,128)) {
      ram.write<Byte>(0x100 * page + slot * 2 + 0x01, 0x03);  //0x01 = stop, 0x03 = empty
    }
  }

  //pages 3+4 (note table)
  //pages 5-127 (game saves)
}

auto Gamepad::serialize(serializer& s) -> void {
  s(ram);
  rumble(false);
}
