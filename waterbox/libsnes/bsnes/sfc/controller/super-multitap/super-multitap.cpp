SuperMultitap::SuperMultitap(uint port) : Controller(port) {
  latched = 0;
  counter1 = 0;
  counter2 = 0;
}

auto SuperMultitap::data() -> uint2 {
  if(latched) return 2;  //device detection
  uint counter, a, b;

  if(iobit()) {
    counter = counter1;
    if(counter >= 16) return 3;
    counter1++;
    if(counter >= 12) return 0;
    a = 0;  //controller 2
    b = 1;  //controller 3
  } else {
    counter = counter2;
    if(counter >= 16) return 3;
    counter2++;
    if(counter >= 12) return 0;
    a = 2;  //controller 4
    b = 3;  //controller 5
  }

  auto& A = gamepads[a];
  auto& B = gamepads[b];

  switch(counter) {
  case  0: return A.b << 0 | B.b << 1;
  case  1: return A.y << 0 | B.y << 1;
  case  2: return A.select << 0 | B.select << 1;
  case  3: return A.start << 0 | B.start << 1;
  case  4: return (A.up & !A.down) << 0 | (B.up & !B.down) << 1;
  case  5: return (A.down & !A.up) << 0 | (B.down & !B.up) << 1;
  case  6: return (A.left & !A.right) << 0 | (B.left & !B.right) << 1;
  case  7: return (A.right & !A.left) << 0 | (B.right & !B.left) << 1;
  case  8: return A.a << 0 | B.a << 1;
  case  9: return A.x << 0 | B.x << 1;
  case 10: return A.l << 0 | B.l << 1;
  case 11: return A.r << 0 | B.r << 1;
  }
  unreachable;
}

auto SuperMultitap::latch(bool data) -> void {
  if(latched == data) return;
  latched = data;
  counter1 = 0;
  counter2 = 0;

  if(latched == 0) {
    for(uint id : range(4)) {
      auto& gamepad = gamepads[id];
      gamepad.b      = platform->inputPoll(port, ID::Device::SuperMultitap, id * 12 + B);
      gamepad.y      = platform->inputPoll(port, ID::Device::SuperMultitap, id * 12 + Y);
      gamepad.select = platform->inputPoll(port, ID::Device::SuperMultitap, id * 12 + Select);
      gamepad.start  = platform->inputPoll(port, ID::Device::SuperMultitap, id * 12 + Start);
      gamepad.up     = platform->inputPoll(port, ID::Device::SuperMultitap, id * 12 + Up);
      gamepad.down   = platform->inputPoll(port, ID::Device::SuperMultitap, id * 12 + Down);
      gamepad.left   = platform->inputPoll(port, ID::Device::SuperMultitap, id * 12 + Left);
      gamepad.right  = platform->inputPoll(port, ID::Device::SuperMultitap, id * 12 + Right);
      gamepad.a      = platform->inputPoll(port, ID::Device::SuperMultitap, id * 12 + A);
      gamepad.x      = platform->inputPoll(port, ID::Device::SuperMultitap, id * 12 + X);
      gamepad.l      = platform->inputPoll(port, ID::Device::SuperMultitap, id * 12 + L);
      gamepad.r      = platform->inputPoll(port, ID::Device::SuperMultitap, id * 12 + R);
    }
  }
}
