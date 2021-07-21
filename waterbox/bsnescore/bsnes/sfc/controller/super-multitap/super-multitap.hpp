struct SuperMultitap : Controller {
  enum : uint {
    Up, Down, Left, Right, B, A, Y, X, L, R, Select, Start, Extra1, Extra2, Extra3, Extra4
  };

  SuperMultitap(uint port, bool isPayloadController);

  auto data() -> uint2;
  auto latch(bool data) -> void;

private:
  bool isPayloadController;
  uint device;
  bool latched;
  uint counter1;
  uint counter2;

  struct Gamepad {
    boolean b, y, select, start;
    boolean up, down, left, right;
    boolean a, x, l, r;
    boolean extra1, extra2, extra3, extra4;
  } gamepads[4];
};
