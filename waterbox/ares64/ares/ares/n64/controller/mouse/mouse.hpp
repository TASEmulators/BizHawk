struct Mouse : Controller {
  Node::Input::Axis x;
  Node::Input::Axis y;
  Node::Input::Button rclick;
  Node::Input::Button lclick;

  Mouse(Node::Port);
  ~Mouse();

  auto comm(n8 send, n8 recv, n8 input[], n8 output[]) -> n2 override;
  auto read() -> n32 override;
};
