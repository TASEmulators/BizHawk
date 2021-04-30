#ifdef NALL_STREAM_INTERNAL_HPP

namespace nall {

struct filestream : stream {
  inline bool seekable() const { return true; }
  inline bool readable() const { return true; }
  inline bool writable() const { return pwritable; }
  inline bool randomaccess() const { return false; }

  inline unsigned size() const { return pfile.size(); }
  inline unsigned offset() const { return pfile.offset(); }
  inline void seek(unsigned offset) const { pfile.seek(offset); }

  inline uint8_t read() const { return pfile.read(); }
  inline void write(uint8_t data) const { pfile.write(data); }

  inline filestream(const string &filename) {
    pfile.open(filename, file::mode::readwrite);
    pwritable = pfile.open();
    if(!pwritable) pfile.open(filename, file::mode::read);
  }

private:
  mutable file pfile;
  bool pwritable;
};

}

#endif
