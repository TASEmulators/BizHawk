struct Interface {
  virtual void videoRefresh(const uint32_t *data, bool hires, bool interlace, bool overscan);
  virtual void audioSample(int16_t lsample, int16_t rsample);
  virtual int16_t inputPoll(bool port, Input::Device device, unsigned index, unsigned id);
  
  virtual void inputNotify(int index);
  
  virtual string path(Cartridge::Slot slot, const string &hint) = 0;
  virtual void message(const string &text);
  virtual time_t currentTime();
  virtual time_t randomSeed();

  //zero 27-sep-2012
  virtual void scanlineStart(int line) = 0;

  //zero 17-oct-2012
  virtual int getBackdropColor();
  
  bool wanttrace = false;
  virtual void cpuTrace(const char *msg);
};

extern Interface *interface;
