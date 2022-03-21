struct Mouse : Controller {
  Node::Input::Axis x;
  Node::Input::Axis y;
  Node::Input::Button rclick;
  Node::Input::Button lclick;

  Mouse(Node::Port);
  auto read() -> n32 override;
};
