struct Interface {
	Interface();
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
  
  bool wanttrace;
  virtual void cpuTrace(const char *msg);

	//zero 23-dec-2012
	virtual void* allocSharedMemory(const char* memtype, size_t amt, int initialByte = -1) = 0;
	virtual void freeSharedMemory(void* ptr) = 0;
};

Interface *interface();
