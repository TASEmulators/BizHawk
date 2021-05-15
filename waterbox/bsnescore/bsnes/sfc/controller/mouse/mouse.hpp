struct Mouse : Controller {
  enum : uint {
    X, Y, Left, Right,
  };

  Mouse(uint port);

  auto data() -> uint2;
  auto latch(bool data) -> void;

private:
  bool latched;
  uint counter;

  uint speed;  //0 = slow, 1 = normal, 2 = fast
  int  x;      //x-coordinate
  int  y;      //y-coordinate
  bool dx;     //x-direction
  bool dy;     //y-direction
  bool l;      //left button
  bool r;      //right button
};
