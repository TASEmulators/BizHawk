Gamepad::Gamepad(Node::Port parent) {
  node = parent->append<Node::Peripheral>("Gamepad");

  port = node->append<Node::Port>("Pak");
  port->setFamily("Nintendo 64");
  port->setType("Pak");
  port->setHotSwappable(true);
  port->setAllocate([&](auto name) { return allocate(name); });
  port->setConnect([&] { return connect(); });
  port->setDisconnect([&] { return disconnect(); });
  port->setSupported({"Controller Pak", "Rumble Pak", "Transfer Pak"});

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
#if false
  if(!slot) return;
  if(slot->name() == "Controller Pak") {
    ram.save(pak->write("save.pak"));
  }
  if(slot->name() == "Transfer Pak") {
    if(transferPak.canSave()) {
      transferPak.ram.save(pak->write("gbram.pak"));
    }
  }
#endif
}

auto Gamepad::allocate(string name) -> Node::Peripheral {
  if(name == "Controller Pak") return slot = port->append<Node::Peripheral>("Controller Pak");
  if(name == "Rumble Pak"    ) return slot = port->append<Node::Peripheral>("Rumble Pak");
  if(name == "Transfer Pak"  ) return slot = port->append<Node::Peripheral>("Transfer Pak");
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
  if(slot->name() == "Transfer Pak") {
    node->setPak(pak = platform->pak(node));
    transferPak.allocate(pak->read("gbrom.pak"), pak->read("gbram.pak"));
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
  if(slot->name() == "Transfer Pak") {
    if(transferPak.canSave()) {
      save();
    }
    transferPak.reset();
  }
  port->remove(slot);
  slot.reset();
}

auto Gamepad::rumble(bool enable) -> void {
  if(!motor) return;
  motor->setEnable(enable);
  platform->input(motor);
}

auto Gamepad::comm(n8 send, n8 recv, n8 input[], n8 output[]) -> n2 {
  b1 valid = 0;
  b1 over = 0;

  //status
  if(input[0] == 0x00 || input[0] == 0xff) {
    output[0] = 0x05;  //0x05 = gamepad; 0x02 = mouse
    output[1] = 0x00;
    output[2] = 0x02;  //0x02 = nothing present in controller slot
    if(ram || motor || transferPak) {
      output[2] = 0x01;  //0x01 = pak present
    }
    valid = 1;
  }

  //read controller state
  if(input[0] == 0x01) {
    u32 data = read();
    output[0] = data >> 24;
    output[1] = data >> 16;
    output[2] = data >>  8;
    output[3] = data >>  0;
    if(recv <= 4) {
      over = 0;
    } else {
      over = 1;
    }
    valid = 1;
  }

  //read pak
  if(input[0] == 0x02 && send >= 3 && recv >= 1) {
    //controller pak
    if(ram) {
      u16 address = (input[1] << 8 | input[2] << 0) & ~31;
      if(pif.addressCRC(address) == (n5)input[2]) {
        for(u32 index : range(recv - 1)) {
          if(address <= 0x7FFF) output[index] = ram.read<Byte>(address);
          else output[index] = 0;
          address++;
        }
        output[recv - 1] = pif.dataCRC({&output[0], recv - 1u});
        valid = 1;
      }
    }

    //rumble pak
    if(motor) {
      u16 address = (input[1] << 8 | input[2] << 0) & ~31;
      if(pif.addressCRC(address) == (n5)input[2]) {
        for(u32 index : range(recv - 1)) {
          if(address <= 0x7FFF) output[index] = 0;
          else if(address <= 0x8FFF) output[index] = 0x80;
          else output[index] = motor->enable() ? 0xFF : 0x00;
          address++;
        }
        output[recv - 1] = pif.dataCRC({&output[0], recv - 1u});
        valid = 1;
      }
    }

    //transfer pak
    if(transferPak) {
      u16 address = (input[1] << 8 | input[2] << 0) & ~31;
      if(pif.addressCRC(address) == (n5)input[2]) {
        for(u32 index : range(recv - 1)) output[index] = transferPak.read(address++);
        output[recv - 1] = pif.dataCRC({&output[0], recv - 1u});
        valid = 1;
      }
    }
  }

  //write pak
  if(input[0] == 0x03 && send >= 3 && recv >= 1) {
    //controller pak
    if(ram) {
      u16 address = (input[1] << 8 | input[2] << 0) & ~31;
      if(pif.addressCRC(address) == (n5)input[2]) {
        for(u32 index : range(send - 3)) {
          if(address <= 0x7FFF) ram.write<Byte>(address, input[3 + index]);
          address++;
        }
        output[0] = pif.dataCRC({&input[3], send - 3u});
        valid = 1;
      }
    }

    //rumble pak
    if(motor) {
      u16 address = (input[1] << 8 | input[2] << 0) & ~31;
      if(pif.addressCRC(address) == (n5)input[2]) {
        output[0] = pif.dataCRC({&input[3], send - 3u});
        valid = 1;
        if(address >= 0xC000) rumble(input[3] & 1);
      }
    }

    //transfer pak
    if(transferPak) {
      u16 address = (input[1] << 8 | input[2] << 0) & ~31;
      if(pif.addressCRC(address) == (n5)input[2]) {
        for(u32 index : range(send - 3)) {
          transferPak.write(address++, input[3 + index]);
        }
        output[0] = pif.dataCRC({&input[3], send - 3u});
        valid = 1;
      }
    }
  }

  n2 status = 0;
  status.bit(0) = valid;
  status.bit(1) = over;
  return status;
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

#if false
  auto cardinalMax   = 85.0;
  auto diagonalMax   = 69.0;
  auto innerDeadzone =  7.0; // default should remain 7 (~8.2% of 85) as the deadzone is axial in nature and fights cardinalMax
  auto outerDeadzoneRadiusMax = 2.0 / sqrt(2.0) * (diagonalMax / cardinalMax * (cardinalMax - innerDeadzone) + innerDeadzone); //from linear scaling equation, substitute outerDeadzoneRadiusMax*sqrt(2)/2 for lengthAbsoluteX and set diagonalMax as the result then solve for outerDeadzoneRadiusMax

  //scale {-32768 ... +32767} to {-outerDeadzoneRadiusMax ... +outerDeadzoneRadiusMax}
  auto ax = x->value() * outerDeadzoneRadiusMax / 32767.0;
  auto ay = y->value() * outerDeadzoneRadiusMax / 32767.0;
  
  //create inner axial dead-zone in range {-innerDeadzone ... +innerDeadzone} and scale from it up to outer circular dead-zone of radius outerDeadzoneRadiusMax
  auto length = sqrt(ax * ax + ay * ay);
  if(length <= outerDeadzoneRadiusMax) {
    auto lengthAbsoluteX = abs(ax);
    auto lengthAbsoluteY = abs(ay);
    if(lengthAbsoluteX <= innerDeadzone) {
      lengthAbsoluteX = 0.0;
    } else {
      lengthAbsoluteX = (lengthAbsoluteX - innerDeadzone) * cardinalMax / (cardinalMax - innerDeadzone) / lengthAbsoluteX;
    }
    ax *= lengthAbsoluteX;
    if(lengthAbsoluteY <= innerDeadzone) {
      lengthAbsoluteY = 0.0;
    } else {
      lengthAbsoluteY = (lengthAbsoluteY - innerDeadzone) * cardinalMax / (cardinalMax - innerDeadzone) / lengthAbsoluteY;
    }
    ay *= lengthAbsoluteY;
  } else {
    length = outerDeadzoneRadiusMax / length;
    ax *= length;
    ay *= length;
  }
  
  //bound diagonals to an octagonal range {-diagonalMax ... +diagonalMax}
  if(ax != 0.0 && ay != 0.0) {
    auto slope = ay / ax;
    auto edgex = copysign(cardinalMax / (abs(slope) + (cardinalMax - diagonalMax) / diagonalMax), ax);
    auto edgey = copysign(min(abs(edgex * slope), cardinalMax / (1.0 / abs(slope) + (cardinalMax - diagonalMax) / diagonalMax)), ay);
    edgex = edgey / slope;

    length = sqrt(ax * ax + ay * ay);
    auto distanceToEdge = sqrt(edgex * edgex + edgey * edgey);
    if(length > distanceToEdge) {
      ax = edgex;
      ay = edgey;
    }
  }

  //keep cardinal input within positive and negative bounds of cardinalMax
  if(abs(ax) > cardinalMax) ax = copysign(cardinalMax, ax);
  if(abs(ay) > cardinalMax) ay = copysign(cardinalMax, ay);
  
  //add epsilon to counteract floating point precision error
  ax = copysign(abs(ax) + 1e-09, ax);
  ay = copysign(abs(ay) + 1e-09, ay);
#endif
  
  n32 data;
  data.byte(0) = y->value();
  data.byte(1) = x->value();
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
