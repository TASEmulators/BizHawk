struct Gamepad : Controller {
  Node::Port port;
  Node::Peripheral slot;
  VFS::Pak pak;
  Memory::Writable ram;  //Toshiba TC55257DFL-85V
  Node::Input::Rumble motor;

  Node::Input::Axis x;
  Node::Input::Axis y;
  Node::Input::Button up;
  Node::Input::Button down;
  Node::Input::Button left;
  Node::Input::Button right;
  Node::Input::Button b;
  Node::Input::Button a;
  Node::Input::Button cameraUp;
  Node::Input::Button cameraDown;
  Node::Input::Button cameraLeft;
  Node::Input::Button cameraRight;
  Node::Input::Button l;
  Node::Input::Button r;
  Node::Input::Button z;
  Node::Input::Button start;

  Gamepad(Node::Port);
  ~Gamepad();
  auto save() -> void override;
  auto allocate(string name) -> Node::Peripheral;
  auto connect() -> void;
  auto disconnect() -> void;
  auto rumble(bool enable) -> void;
  auto read() -> n32 override;
  auto formatControllerPak() -> void;
  auto serialize(serializer&) -> void override;
};
