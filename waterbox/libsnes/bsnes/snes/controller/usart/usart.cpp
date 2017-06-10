#ifdef CONTROLLER_CPP

//Synchronous serial communications cable emulation

//Hardware:
//Teensy++ 2.0 USB
//AT90USB1286

//Connection Diagram:
//[SNES]  [Teensy]
// +5v     ---
// Clock   D5
// Latch   D2
// Data1   D3
// Data2   ---
// IOBit   ---
// GND     GND

void USART::enter() {
  if(init && main) {
    init({ &USART::usleep, this }, { &USART::read, this }, { &USART::write, this });
    main();
  }
  while(true) step(1000000);  //fallback; main should never return
}

void USART::usleep(unsigned milliseconds) {
  step(milliseconds);
}

//SNES -> USART
uint8 USART::read() {
  while(txbuffer.size() == 0) step(1);
  uint8 data = txbuffer[0];
  txbuffer.remove(0);
  return data;
}

//USART -> SNES
void USART::write(uint8 data) {
  rxbuffer.append(data ^ 0xff);
}

//clock
uint2 USART::data() {
  //SNES -> USART
  if(txlength == 0) {
    if(latched == 0) txlength++;
  } else if(txlength <= 8) {
    txdata = (latched << 7) | (txdata >> 1);
    txlength++;
  } else {
    if(latched == 1) txbuffer.append(txdata);
    txlength = 0;
  }

  //USART -> SNES
  if(rxlength == 0 && rxbuffer.size()) {
    data1 = 1;
    rxdata = rxbuffer[0];
    rxbuffer.remove(0);
    rxlength++;
  } else if(rxlength <= 8) {
    data1 = rxdata & 1;
    rxdata >>= 1;
    rxlength++;
  } else {
    data1 = 0;
    rxlength = 0;
  }

  return (data2 << 1) | (data1 << 0);
}

//latch
void USART::latch(bool data) {
  latched = data;
}

USART::USART(bool port) : Controller(port) {
  latched = 0;
  data1 = 0;
  data2 = 0;

  rxlength = 0;
  rxdata = 0;

  txlength = 0;
  txdata = 0;

  string filename = interface()->path(Cartridge::Slot::Base, "usart.so");
  if(open_absolute(filename)) {
    init = sym("usart_init");
    main = sym("usart_main");
    if(init && main) create(Controller::Enter, 1000000);
  }
}

USART::~USART() {
  if(opened()) close();
}

#endif
