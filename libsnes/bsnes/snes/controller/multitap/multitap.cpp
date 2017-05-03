#ifdef CONTROLLER_CPP

uint2 Multitap::data() {
	if (latched)
		return connected ? 2 : 0;

	unsigned index, port1, port2;

  if(iobit()) {
    index = counter1;
    if(index >= 16) return 3;
    counter1++;
    port1 = 0;  //controller 1
    port2 = 1;  //controller 2
  } else {
    index = counter2;
    if(index >= 16) return 3;
    counter2++;
    port1 = 2;  //controller 3
    port2 = 3;  //controller 4
  }

  bool data1 = interface()->inputPoll(port, Input::Device::Multitap, port1, index);
  bool data2 = interface()->inputPoll(port, Input::Device::Multitap, port2, index);
  return (data2 << 1) | (data1 << 0);
}

void Multitap::latch(bool data) {
  if(latched == data) return;
  bool newtoggleConnectedInput = interface()->inputPoll(port, Input::Device::Multitap, 0, 16);
  if (newtoggleConnectedInput > toggleConnectedInput)
	  connected ^= true;
  toggleConnectedInput = newtoggleConnectedInput;


  latched = data;
  counter1 = 0;
  counter2 = 0;
}

void Multitap::serialize(serializer& s) {
  Processor::serialize(s);
  //Save block.
  unsigned char block[Controller::SaveSize] = {0};
  block[0] = latched ? 1 : 0;
  block[1] = counter1;
  block[2] = counter2;
  block[3] = connected;
  block[4] = toggleConnectedInput;
  s.array(block, Controller::SaveSize);
  if(s.mode() == nall::serializer::Load) {
    latched = (block[0] != 0);
    counter1 = block[1];
    counter2 = block[2];
	connected = block[3];
	toggleConnectedInput = block[4];
  }
}


Multitap::Multitap(bool port) : Controller(port) {
  latched = 0;
  counter1 = 0;
  counter2 = 0;
  connected = true;
  toggleConnectedInput = false;
}

#endif
