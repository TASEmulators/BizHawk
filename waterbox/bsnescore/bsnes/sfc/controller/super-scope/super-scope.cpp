//The Super Scope is a light-gun: it detects the CRT beam cannon position,
//and latches the counters by toggling iobit. This only works on controller
//port 2, as iobit there is connected to the PPU H/V counter latch.
//(PIO $4201.d7)

//It is obviously not possible to perfectly simulate an IR light detecting
//a CRT beam cannon, hence this class will read the PPU raster counters.

//A Super Scope can still technically be used in port 1, however it would
//require manual polling of PIO ($4201.d6) to determine when iobit was written.
//Note that no commercial game ever utilizes a Super Scope in port 1.

SuperScope::SuperScope(uint port) : Controller(port) {
  latched = 0;
  counter = 0;

  //center cursor onscreen
  x = 256 / 2;
  y = 240 / 2;

  trigger   = false;
  cursor    = false;
  turbo     = false;
  pause     = false;
  offscreen = false;

  oldturbo    = false;
  triggerlock = false;
  pauselock   = false;

  prev = 0;
}

auto SuperScope::data() -> uint2 {
  if(counter >= 8) return 1;

  if(counter == 0) {
    //turbo is a switch; toggle is edge sensitive
    bool newturbo = platform->inputPoll(port, ID::Device::SuperScope, Turbo);
    if(newturbo && !oldturbo) {
      turbo = !turbo;  //toggle state
    }
    oldturbo = newturbo;

    //trigger is a button
    //if turbo is active, trigger is level sensitive; otherwise, it is edge sensitive
    trigger = false;
    bool newtrigger = platform->inputPoll(port, ID::Device::SuperScope, Trigger);
    if(newtrigger && (turbo || !triggerlock)) {
      trigger = true;
      triggerlock = true;
    } else if(!newtrigger) {
      triggerlock = false;
    }

    //cursor is a button; it is always level sensitive
    cursor = platform->inputPoll(port, ID::Device::SuperScope, Cursor);

    //pause is a button; it is always edge sensitive
    pause = false;
    bool newpause = platform->inputPoll(port, ID::Device::SuperScope, Pause);
    if(newpause && !pauselock) {
      pause = true;
      pauselock = true;
    } else if(!newpause) {
      pauselock = false;
    }

    offscreen = (x < 0 || y < 0 || x >= 256 || y >= ppu.vdisp());
  }

  switch(counter++) {
  case 0: return offscreen ? 0 : trigger;
  case 1: return cursor;
  case 2: return turbo;
  case 3: return pause;
  case 4: return 0;
  case 5: return 0;
  case 6: return offscreen;
  case 7: return 0;  //noise (1 = yes)
  }

  unreachable;
}

auto SuperScope::latch(bool data) -> void {
  if(latched == data) return;
  latched = data;
  counter = 0;
}

auto SuperScope::latch() -> void {
  int nx = platform->inputPoll(port, ID::Device::SuperScope, X);
  int ny = platform->inputPoll(port, ID::Device::SuperScope, Y);
  x = max(-16, min(256 + 16, nx + x));
  y = max(-16, min((int)ppu.vdisp() + 16, ny + y));
  offscreen = (x < 0 || y < 0 || x >= 256 || y >= (int)ppu.vdisp());
  if(!offscreen) ppu.latchCounters(x, y);
}

auto SuperScope::draw(uint16_t* data, uint pitch, uint width, uint height) -> void {
  pitch >>= 1;
  float scaleX = (float)width  / 256.0;
  float scaleY = (float)height / (float)ppu.vdisp();
  int length = (float)width / 256.0 * 4.0;

  int x = this->x * scaleX;
  int y = this->y * scaleY;

  auto plot = [&](int x, int y, uint16_t color) -> void {
    if(x >= 0 && y >= 0 && x < (int)width && y < (int)height) {
      data[y * pitch + x] = color;
    }
  };

  uint16_t color = turbo ? 0x7c00 : 0x03e0;
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
