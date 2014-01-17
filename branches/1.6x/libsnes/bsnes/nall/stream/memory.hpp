#ifdef NALL_STREAM_INTERNAL_HPP

namespace nall {

struct memorystream : stream {
  inline bool seekable() const { return true; }
  inline bool readable() const { return true; }
  inline bool writable() const { return pwritable; }
  inline bool randomaccess() const { return true; }

  inline unsigned size() const { return psize; }
  inline unsigned offset() const { return poffset; }
  inline void seek(unsigned offset) const { poffset = offset; }

  inline uint8_t read() const { return pdata[poffset++]; }
  inline void write(uint8_t data) const { pdata[poffset++] = data; }

  inline uint8_t read(unsigned offset) const { return pdata[offset]; }
  inline void write(unsigned offset, uint8_t data) const { pdata[offset] = data; }

  inline memorystream() : pdata(nullptr), psize(0), poffset(0), pwritable(true) {}

  inline memorystream(uint8_t *data, unsigned size) {
    pdata = data, psize = size, poffset = 0;
    pwritable = true;
  }

  inline memorystream(const uint8_t *data, unsigned size) {
    pdata = (uint8_t*)data, psize = size, poffset = 0;
    pwritable = false;
  }

protected:
  mutable uint8_t *pdata;
  mutable unsigned psize, poffset, pwritable;
};

}

#endif
