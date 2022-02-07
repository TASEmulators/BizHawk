SuperMultitap::SuperMultitap(uint port, bool isPayloadController) :
Controller(port),
isPayloadController(isPayloadController),
device(isPayloadController ? ID::Device::Payload : ID::Device::SuperMultitap) {
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
    if(counter >= 12 && !isPayloadController) return 0;
    a = 0;  //controller 2
    b = 1;  //controller 3
  } else {
    counter = counter2;
    if(counter >= 16) return 3;
    counter2++;
    if(counter >= 12 && !isPayloadController) return 0;
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
  case  4: return A.up << 0 | B.up << 1;
  case  5: return A.down << 0 | B.down << 1;
  case  6: return A.left << 0 | B.left << 1;
  case  7: return A.right << 0 | B.right << 1;
  case  8: return A.a << 0 | B.a << 1;
  case  9: return A.x << 0 | B.x << 1;
  case 10: return A.l << 0 | B.l << 1;
  case 11: return A.r << 0 | B.r << 1;
  case 12: return A.extra1 << 0 | B.extra1 << 1;
  case 13: return A.extra2 << 0 | B.extra2 << 1;
  case 14: return A.extra3 << 0 | B.extra3 << 1;
  case 15: return A.extra4 << 0 | B.extra4 << 1;
  }
  unreachable;
}

auto SuperMultitap::latch(bool data) -> void {
  if(latched == data) return;
  latched = data;
  counter1 = 0;
  counter2 = 0;

  if(latched == 0) {
    uint offset = isPayloadController ? 16 : 12;
    for(uint id : range(4)) {
      auto& gamepad = gamepads[id];
      gamepad.b      = platform->inputPoll(port, device, id * offset + B);
      gamepad.y      = platform->inputPoll(port, device, id * offset + Y);
      gamepad.select = platform->inputPoll(port, device, id * offset + Select);
      gamepad.start  = platform->inputPoll(port, device, id * offset + Start);
      gamepad.up     = platform->inputPoll(port, device, id * offset + Up);
      gamepad.down   = platform->inputPoll(port, device, id * offset + Down);
      gamepad.left   = platform->inputPoll(port, device, id * offset + Left);
      gamepad.right  = platform->inputPoll(port, device, id * offset + Right);
      gamepad.a      = platform->inputPoll(port, device, id * offset + A);
      gamepad.x      = platform->inputPoll(port, device, id * offset + X);
      gamepad.l      = platform->inputPoll(port, device, id * offset + L);
      gamepad.r      = platform->inputPoll(port, device, id * offset + R);
      if (!isPayloadController) continue;
      gamepad.extra1 = platform->inputPoll(port, device, id * offset + Extra1);
      gamepad.extra2 = platform->inputPoll(port, device, id * offset + Extra2);
      gamepad.extra3 = platform->inputPoll(port, device, id * offset + Extra3);
      gamepad.extra4 = platform->inputPoll(port, device, id * offset + Extra4);
    }
  }
}
