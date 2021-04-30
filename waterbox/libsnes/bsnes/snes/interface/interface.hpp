
#define TRACE_CPU 0
#define TRACE_SMP 1
#define TRACE_GB 2

#define TRACE_MASK_NONE (0)
#define TRACE_CPU_MASK (1<<TRACE_CPU)
#define TRACE_SMP_MASK (1<<TRACE_SMP)
#define TRACE_GB_MASK (1<<TRACE_GB)

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
  
  uint32_t wanttrace;
  virtual void cpuTrace(uint32_t which, const char *msg);

	//zero 23-dec-2012
	virtual void* allocSharedMemory(const char* memtype, size_t amt, int initialByte = -1) = 0;
	virtual void freeSharedMemory(void* ptr) = 0;
};

Interface *interface();
