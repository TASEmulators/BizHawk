struct Gamepad : Controller {
  enum : uint {
    Up, Down, Left, Right, B, A, Y, X, L, R, Select, Start, Extra1, Extra2, Extra3, Extra4
  };

  Gamepad(uint port, bool isPayloadController = false);

  auto data() -> uint2;
  auto latch(bool data) -> void;

private:
  bool isPayload;
  uint device;
  uint counter;

  boolean b, y, select, start;
  boolean up, down, left, right;
  boolean a, x, l, r;
  boolean extra1, extra2, extra3, extra4;
};
