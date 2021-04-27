#ifndef READABLE_H
#define READABLE_H

struct ReadableMemory : Memory {
  inline auto reset() -> void override {
    delete[] self.data;
    self.data = nullptr;
    self.size = 0;
  }

  inline auto allocate(uint size, uint8 fill = 0xff) -> void override {
    if(self.size != size) {
      delete[] self.data;
      self.data = new uint8[self.size = size];
    }
    for(uint address : range(size)) {
      self.data[address] = fill;
    }
  }

	inline auto copy(const uint8 *data, unsigned size) {
		if(!self.data) {
			self.size = (size & ~255) + ((bool)(size & 255) << 8);
			self.data = new uint8[self.size]();
		}
		memcpy(self.data, data, min(self.size, size));
	}

  inline auto data() -> uint8* override {
    return self.data;
  }

  inline auto size() const -> uint override {
    return self.size;
  }

  inline auto read(uint address, uint8 data = 0) -> uint8 override {
    return self.data[address];
  }

  inline auto write(uint address, uint8 data) -> void override {
    if(Memory::GlobalWriteEnable) {
      self.data[address] = data;
    }
  }

  inline auto operator[](uint address) const -> uint8 {
    return self.data[address];
  }

private:
  struct {
    uint8* data = nullptr;
    uint size = 0;
  } self;
};

#endif
