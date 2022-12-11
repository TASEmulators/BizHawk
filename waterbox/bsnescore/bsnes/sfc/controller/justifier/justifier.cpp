Justifier::Justifier(uint port, bool chained):
Controller(port),
chained(chained),
device(!chained ? ID::Device::Justifier : ID::Device::Justifiers)
{
  latched = 0;
  counter = 0;
  active = 0;
  prev = 0;

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

auto Justifier::data() -> uint2 {
  if(counter >= 32) return 1;

  if(counter == 0) {
    player1.trigger = platform->inputPoll(port, device, 0 + Trigger);
    player1.start   = platform->inputPoll(port, device, 0 + Start);
  }

  if(counter == 0 && chained) {
    player2.trigger = platform->inputPoll(port, device, 4 + Trigger);
    player2.start   = platform->inputPoll(port, device, 4 + Start);
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

  unreachable;
}

auto Justifier::latch(bool data) -> void {
  if(latched == data) return;
  latched = data;
  counter = 0;
  if(latched == 0) active = !active;  //toggle between both controllers, even when unchained
}

auto Justifier::latch() -> void {
  if(!active) {
    int nx = platform->inputPoll(port, device, 0 + X);
    int ny = platform->inputPoll(port, device, 0 + Y);
    player1.x = max(-16, min(256 + 16, nx + player1.x));
    player1.y = max(-16, min((int)ppu.vdisp() + 16, ny + player1.y));
    bool offscreen = (player1.x < 0 || player1.y < 0 || player1.x >= 256 || player1.y >= (int)ppu.vdisp());
    if(!offscreen) ppu.latchCounters(player1.x, player1.y);
  }
  else {
    int nx = platform->inputPoll(port, device, 4 + X);
    int ny = platform->inputPoll(port, device, 4 + Y);
    player2.x = max(-16, min(256 + 16, nx + player2.x));
    player2.y = max(-16, min((int)ppu.vdisp() + 16, ny + player2.y));
    bool offscreen = (player2.x < 0 || player2.y < 0 || player2.x >= 256 || player2.y >= (int)ppu.vdisp());
    if(!offscreen) ppu.latchCounters(player2.x, player2.y);
  }
}

auto Justifier::draw(uint16_t* data, uint pitch, uint width, uint height) -> void {
  pitch >>= 1;
  float scaleX = (float)width  / 256.0;
  float scaleY = (float)height / (float)ppu.vdisp();
  int length = (float)width / 256.0 * 4.0;

  auto plot = [&](int x, int y, uint16_t color) -> void {
    if(x >= 0 && y >= 0 && x < (int)width && y < (int)height) {
      data[y * pitch + x] = color;
    }
  };

  { int x = player1.x * scaleX;
    int y = player1.y * scaleY;

    uint16_t color = 0x03e0;
    uint16_t black = 0x0000;

    for(int px = x - length - 1; px <= x + length + 1; px++) plot(px, y - 1, black);
    for(int px = x - length - 1; px <= x + length + 1; px++) plot(px, y + 1, black);
    for(int py = y - length - 1; py <= y + length + 1; py++) plot(x - 1, py, black);
    for(int py = y - length - 1; py <= y + length + 1; py++) plot(x + 1, py, black);
    plot(x - length - 1, y, black);
    plot(x + length + 1, y, black);
    plot(x, y - length - 1, black);
    plot(x, y + length + 1, black);
    for(int px = x - length; px <= x + length; px++) plot(px, y, color);
    for(int py = y - length; py <= y + length; py++) plot(x, py, color);
  }

  if(chained)
  { int x = player2.x * scaleX;
    int y = player2.y * scaleY;

    uint16_t color = 0x7c00;
    uint16_t black = 0x0000;

    for(int px = x - length - 1; px <= x + length + 1; px++) plot(px, y - 1, black);
    for(int px = x - length - 1; px <= x + length + 1; px++) plot(px, y + 1, black);
    for(int py = y - length - 1; py <= y + length + 1; py++) plot(x - 1, py, black);
    for(int py = y - length - 1; py <= y + length + 1; py++) plot(x + 1, py, black);
    plot(x - length - 1, y, black);
    plot(x + length + 1, y, black);
    plot(x, y - length - 1, black);
    plot(x, y + length + 1, black);
    for(int px = x - length; px <= x + length; px++) plot(px, y, color);
    for(int py = y - length; py <= y + length; py++) plot(x, py, color);
  }
}
