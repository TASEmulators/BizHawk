struct Justifier : Controller {
  enum : uint {
    X, Y, Trigger, Start,
  };

  Justifier(uint port, bool chained);

  auto data() -> uint2;
  auto latch(bool data) -> void;
  auto latch() -> void override;
  auto draw(uint16_t* data, uint pitch, uint width, uint height) -> void override;

//private:
  const bool chained;  //true if the second justifier is attached to the first
  const uint device;
  bool latched;
  uint counter;
  uint prev;

  bool active;
  struct Player {
    int x;
    int y;
    bool trigger;
    bool start;
  } player1, player2;
};
