Gamepad::Gamepad(uint port, bool isPayloadController) : Controller(port), isPayload(isPayloadController) {
  device = isPayloadController ? ID::Device::ExtendedGamepad : ID::Device::Gamepad;
  latched = 0;
  counter = 0;
}

auto Gamepad::data() -> uint2 {
  if(counter >= 16) return 1;
  if(latched == 1) return platform->inputPoll(port, device, B);
  if (counter >= 12 && !isPayload) return 0;  //12-15: signature

  //note: D-pad physically prevents up+down and left+right from being pressed at the same time
  // patched this "fix" out because it is handled in bizhawk frontend and fixing it here does not seem right anyway
  switch(counter++) {
  case  0: return b;
  case  1: return y;
  case  2: return select;
  case  3: return start;
  case  4: return up;
  case  5: return down;
  case  6: return left;
  case  7: return right;
  case  8: return a;
  case  9: return x;
  case 10: return l;
  case 11: return r;
  case 12: return extra1;
  case 13: return extra2;
  case 14: return extra3;
  case 15: return extra4;
  }
  unreachable;
}

auto Gamepad::latch(bool data) -> void {
  if(latched == data) return;
  latched = data;
  counter = 0;

  if(latched == 0) {
    if (port == ID::Port::Controller1) platform->notify("LATCH");
    b      = platform->inputPoll(port, device, B);
    y      = platform->inputPoll(port, device, Y);
    select = platform->inputPoll(port, device, Select);
    start  = platform->inputPoll(port, device, Start);
    up     = platform->inputPoll(port, device, Up);
    down   = platform->inputPoll(port, device, Down);
    left   = platform->inputPoll(port, device, Left);
    right  = platform->inputPoll(port, device, Right);
    a      = platform->inputPoll(port, device, A);
    x      = platform->inputPoll(port, device, X);
    l      = platform->inputPoll(port, device, L);
    r      = platform->inputPoll(port, device, R);
    if (!isPayload) return;
    extra1 = platform->inputPoll(port, device, Extra1);
    extra2 = platform->inputPoll(port, device, Extra2);
    extra3 = platform->inputPoll(port, device, Extra3);
    extra4 = platform->inputPoll(port, device, Extra4);
  }
}
