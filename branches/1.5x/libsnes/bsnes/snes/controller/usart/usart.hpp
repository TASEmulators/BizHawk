struct USART : Controller, public library {
  void enter();
  void usleep(unsigned milliseconds);
  uint8 read();
  void write(uint8 data);

  uint2 data();
  void latch(bool data);

  USART(bool port);
  ~USART();

private:
  bool latched;
  bool data1;
  bool data2;

  uint8 rxlength;
  uint8 rxdata;
  vector<uint8> rxbuffer;

  uint8 txlength;
  uint8 txdata;
  vector<uint8> txbuffer;

  function<void (function<void (unsigned)>, function<uint8 ()>, function<void (uint8)>)> init;
  function<void ()> main;
};
