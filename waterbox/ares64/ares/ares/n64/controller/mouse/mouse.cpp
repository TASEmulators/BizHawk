Mouse::Mouse(Node::Port parent) {
  node = parent->append<Node::Peripheral>("Mouse");

  x      = node->append<Node::Input::Axis>  ("X-Axis");
  y      = node->append<Node::Input::Axis>  ("Y-Axis");
  rclick = node->append<Node::Input::Button>("Right Click");
  lclick = node->append<Node::Input::Button>("Left Click");
}

Mouse::~Mouse() {
}

auto Mouse::comm(n8 send, n8 recv, n8 input[], n8 output[]) -> n2 {
  b1 valid = 0;
  b1 over = 0;

  //status
  if(input[0] == 0x00 || input[0] == 0xff) {
    output[0] = 0x02;  //0x05 = gamepad; 0x02 = mouse
    output[1] = 0x00;
    output[2] = 0x02;  //0x02 = nothing present in controller slot
    valid = 1;
  }

  //read controller state
  if(input[0] == 0x01) {
    u32 data = read();
    output[0] = data >> 24;
    output[1] = data >> 16;
    output[2] = data >>  8;
    output[3] = data >>  0;
    if(recv <= 4) {
      over = 0;
    } else {
      over = 1;
    }
    valid = 1;
  }

  n2 status = 0;
  status.bit(0) = valid;
  status.bit(1) = over;
  return status;
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
