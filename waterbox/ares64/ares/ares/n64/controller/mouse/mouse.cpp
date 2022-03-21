Mouse::Mouse(Node::Port parent) {
  node = parent->append<Node::Peripheral>("Mouse");

  x      = node->append<Node::Input::Axis>  ("X-Axis");
  y      = node->append<Node::Input::Axis>  ("Y-Axis");
  rclick = node->append<Node::Input::Button>("Right Click");
  lclick = node->append<Node::Input::Button>("Left Click");
}

auto Mouse::read() -> n32 {
  platform->input(x);
  platform->input(y);
  platform->input(rclick);
  platform->input(lclick);

  n32 data;
  data.byte(0) = y->value();
  data.byte(1) = x->value();
  data.bit(22) = 0;  //GND
  data.bit(23) = 0;  //RST
  data.bit(30) = rclick->value();
  data.bit(31) = lclick->value();

  return data;
}
