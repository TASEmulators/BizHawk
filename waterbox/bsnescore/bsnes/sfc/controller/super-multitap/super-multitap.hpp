struct SuperMultitap : Controller {
  enum : uint {
    Up, Down, Left, Right, B, A, Y, X, L, R, Select, Start,
  };

  SuperMultitap(uint port);

  auto data() -> uint2;
  auto latch(bool data) -> void;

private:
  bool latched;
  uint counter1;
  uint counter2;

  struct Gamepad {
    boolean b, y, select, start;
    boolean up, down, left, right;
    boolean a, x, l, r;
  } gamepads[4];
};
