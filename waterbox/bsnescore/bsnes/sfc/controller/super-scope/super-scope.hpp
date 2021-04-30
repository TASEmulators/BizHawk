struct SuperScope : Controller {
  enum : uint {
    X, Y, Trigger, Cursor, Turbo, Pause,
  };

  SuperScope(uint port);

  auto data() -> uint2;
  auto latch(bool data) -> void;
  auto latch() -> void override;
  auto draw(uint16_t* data, uint pitch, uint width, uint height) -> void override;

private:
  bool latched;
  uint counter;

  int x;
  int y;

  bool trigger;
  bool cursor;
  bool turbo;
  bool pause;
  bool offscreen;

  bool oldturbo;
  bool triggerlock;
  bool pauselock;

  uint prev;
};
