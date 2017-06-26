#ifdef CONTROLLER_CPP

uint2 Mouse::data() {
  if(counter >= 32) return 1;

  if(counter == 0) {
    position_x = interface()->inputPoll(port, Input::Device::Mouse, 0, (unsigned)Input::MouseID::X);  //-n = left, 0 = center, +n = right
    position_y = interface()->inputPoll(port, Input::Device::Mouse, 0, (unsigned)Input::MouseID::Y);  //-n = up,   0 = center, +n = down
  }


  bool direction_x = position_x < 0;  //0 = right, 1 = left
  bool direction_y = position_y < 0;  //0 = down,  1 = up

	int position_x_fixed = position_x;
	int position_y_fixed = position_y;

  if(position_x < 0) position_x_fixed = -position_x;  //abs(position_x)
  if(position_y < 0) position_y_fixed = -position_y;  //abs(position_y)

	position_x_fixed = min(127, position_x_fixed);  //range = 0 - 127
	position_y_fixed = min(127, position_y_fixed);


  switch(counter++) { default:
  case  0: return 0;
  case  1: return 0;
  case  2: return 0;
  case  3: return 0;
  case  4: return 0;
  case  5: return 0;
  case  6: return 0;
  case  7: return 0;

  case  8: return interface()->inputPoll(port, Input::Device::Mouse, 0, (unsigned)Input::MouseID::Right);
  case  9: return interface()->inputPoll(port, Input::Device::Mouse, 0, (unsigned)Input::MouseID::Left);
  case 10: return 0;  //speed (0 = slow, 1 = normal, 2 = fast, 3 = unused)
  case 11: return 0;  // ||

  case 12: return 0;  //signature
  case 13: return 0;  // ||
  case 14: return 0;  // ||
  case 15: return 1;  // ||

	case 16: return (direction_y)?1:0;
  case 17: return (position_y_fixed >> 6) & 1;
  case 18: return (position_y_fixed >> 5) & 1;
  case 19: return (position_y_fixed >> 4) & 1;
  case 20: return (position_y_fixed >> 3) & 1;
  case 21: return (position_y_fixed >> 2) & 1;
  case 22: return (position_y_fixed >> 1) & 1;
  case 23: return (position_y_fixed >> 0) & 1;

  case 24: return (direction_x) ? 1 : 0;
  case 25: return (position_x_fixed >> 6) & 1;
  case 26: return (position_x_fixed >> 5) & 1;
  case 27: return (position_x_fixed >> 4) & 1;
  case 28: return (position_x_fixed >> 3) & 1;
  case 29: return (position_x_fixed >> 2) & 1;
  case 30: return (position_x_fixed >> 1) & 1;
  case 31: return (position_x_fixed >> 0) & 1;
  }
}

void Mouse::latch(bool data) {
  if(latched == data) return;
  latched = data;
  counter = 0;
}

Mouse::Mouse(bool port) : Controller(port) {
  latched = 0;
  counter = 0;
}

#endif
