#ifdef CONTROLLER_CPP

uint2 Gamepad::data() {
  if(counter >= 16) return 1;
  uint2 result = interface->inputPoll(port, Input::Device::Joypad, 0, counter);
  if(latched == 0) counter++;
  return result;
}

void Gamepad::latch(bool data) {
  if(latched == data) return;
  latched = data;
  counter = 0;
}

Gamepad::Gamepad(bool port) : Controller(port) {
  latched = 0;
  counter = 0;
}

#endif
