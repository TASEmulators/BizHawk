#ifdef CONTROLLER_CPP

void Justifier::enter() {
  unsigned prev = 0;
  while(true) {
    unsigned next = cpu.vcounter() * 1364 + cpu.hcounter();

    signed x = (active == 0 ? player1.x : player2.x), y = (active == 0 ? player1.y : player2.y);
    bool offscreen = (x < 0 || y < 0 || x >= 256 || y >= (ppu.overscan() ? 240 : 225));

    if(offscreen == false) {
      unsigned target = y * 1364 + (x + 24) * 4;
      if(next >= target && prev < target) {
        //CRT raster detected, toggle iobit to latch counters
        iobit(0);
        iobit(1);
      }
    }

    if(next < prev) {
      int nx1 = interface()->inputPoll(port, Input::Device::Justifier, 0, (unsigned)Input::JustifierID::X);
      int ny1 = interface()->inputPoll(port, Input::Device::Justifier, 0, (unsigned)Input::JustifierID::Y);
      nx1 += player1.x;
      ny1 += player1.y;
      player1.x = max(-16, min(256 + 16, nx1));
      player1.y = max(-16, min(240 + 16, ny1));
    }

    if(next < prev && chained) {
      int nx2 = interface()->inputPoll(port, Input::Device::Justifiers, 1, (unsigned)Input::JustifierID::X);
      int ny2 = interface()->inputPoll(port, Input::Device::Justifiers, 1, (unsigned)Input::JustifierID::Y);
      nx2 += player2.x;
      ny2 += player2.y;
      player2.x = max(-16, min(256 + 16, nx2));
      player2.y = max(-16, min(240 + 16, ny2));
    }

    prev = next;
    step(2);
  }
}

uint2 Justifier::data() {
  if(counter >= 32) return 1;

  if(counter == 0) {
    player1.trigger = interface()->inputPoll(port, Input::Device::Justifier, 0, (unsigned)Input::JustifierID::Trigger);
    player1.start   = interface()->inputPoll(port, Input::Device::Justifier, 0, (unsigned)Input::JustifierID::Start);
  }

  if(counter == 0 && chained) {
    player2.trigger = interface()->inputPoll(port, Input::Device::Justifiers, 1, (unsigned)Input::JustifierID::Trigger);
    player2.start   = interface()->inputPoll(port, Input::Device::Justifiers, 1, (unsigned)Input::JustifierID::Start);
  }

  switch(counter++) {
  case  0: return 0;
  case  1: return 0;
  case  2: return 0;
  case  3: return 0;
  case  4: return 0;
  case  5: return 0;
  case  6: return 0;
  case  7: return 0;
  case  8: return 0;
  case  9: return 0;
  case 10: return 0;
  case 11: return 0;

  case 12: return 1;  //signature
  case 13: return 1;  // ||
  case 14: return 1;  // ||
  case 15: return 0;  // ||

  case 16: return 0;
  case 17: return 1;
  case 18: return 0;
  case 19: return 1;
  case 20: return 0;
  case 21: return 1;
  case 22: return 0;
  case 23: return 1;

  case 24: return player1.trigger;
  case 25: return player2.trigger;
  case 26: return player1.start;
  case 27: return player2.start;
  case 28: return active;

  case 29: return 0;
  case 30: return 0;
  case 31: return 0;
  }
}

void Justifier::latch(bool data) {
  if(latched == data) return;
  latched = data;
  counter = 0;
  if(latched == 0) active = !active;  //toggle between both controllers, even when unchained
}

void Justifier::serialize(serializer& s) {
  Processor::serialize(s);
  //Save block.
  unsigned char block[Controller::SaveSize] = {0};
  block[0] = latched ? 1 : 0;
  block[1] = counter;
  block[2] = active ? 1 : 0;
  block[3] = player1.trigger ? 1 : 0;
  block[4] = player2.trigger ? 1 : 0;
  block[5] = player1.start ? 1 : 0;
  block[6] = player2.start ? 1 : 0;
  block[7] = (unsigned short)player1.x >> 8;
  block[8] = (unsigned short)player1.x;
  block[9] = (unsigned short)player2.x >> 8;
  block[10] = (unsigned short)player2.x;
  block[11] = (unsigned short)player1.y >> 8;
  block[12] = (unsigned short)player1.y;
  block[13] = (unsigned short)player2.y >> 8;
  block[14] = (unsigned short)player2.y;
  s.array(block, Controller::SaveSize);
  if(s.mode() == nall::serializer::Load) {
    latched = (block[0] != 0);
    counter = block[1];
    active = (block[2] != 0);
    player1.trigger = (block[3] != 0);
    player2.trigger = (block[4] != 0);
    player1.start = (block[5] != 0);
    player2.start = (block[6] != 0);
    player1.x = (short)(((unsigned short)block[7] << 8) | (unsigned short)block[8]);
    player2.x = (short)(((unsigned short)block[9] << 8) | (unsigned short)block[10]);
    player1.y = (short)(((unsigned short)block[11] << 8) | (unsigned short)block[12]);
    player2.y = (short)(((unsigned short)block[13] << 8) | (unsigned short)block[14]);
  }
}


Justifier::Justifier(bool port, bool chained) : Controller(port), chained(chained) {
  create(Controller::Enter, 21477272);
  latched = 0;
  counter = 0;
  active = 0;

  player1.x = 256 / 2;
  player1.y = 240 / 2;
  player1.trigger = false;
  player2.start = false;

  player2.x = 256 / 2;
  player2.y = 240 / 2;
  player2.trigger = false;
  player2.start = false;

  if(chained == false) {
    player2.x = -1;
    player2.y = -1;
  } else {
    player1.x -= 16;
    player2.x += 16;
  }
}

#endif
