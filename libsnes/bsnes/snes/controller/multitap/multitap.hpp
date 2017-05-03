struct Multitap : Controller {
  uint2 data();
  void latch(bool data);
  Multitap(bool port);
  void serialize(serializer& s);
private:
	bool connected;
	bool toggleConnectedInput;
  bool latched;
  unsigned counter1;
  unsigned counter2;
};
