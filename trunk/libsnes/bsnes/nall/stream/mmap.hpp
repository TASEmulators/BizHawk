#ifdef NALL_STREAM_INTERNAL_HPP

namespace nall {

struct mmapstream : stream {
  inline bool seekable() const { return true; }
  inline bool readable() const { return true; }
  inline bool writable() const { return pwritable; }
  inline bool randomaccess() const { return false; }

  inline unsigned size() const { return pmmap.size(); }
  inline unsigned offset() const { return poffset; }
  inline void seek(unsigned offset) const { poffset = offset; }

  inline uint8_t read() const { return pdata[poffset++]; }
  inline void write(uint8_t data) const { pdata[poffset++] = data; }

  inline uint8_t read(unsigned offset) const { return pdata[offset]; }
  inline void write(unsigned offset, uint8_t data) const { pdata[offset] = data; }

  inline mmapstream(const string &filename) {
    pmmap.open(filename, filemap::mode::readwrite);
    pwritable = pmmap.open();
    if(!pwritable) pmmap.open(filename, filemap::mode::read);
    pdata = pmmap.data(), poffset = 0;
  }

private:
  mutable filemap pmmap;
  mutable uint8_t *pdata;
  mutable unsigned pwritable, poffset;
};

}

#endif
