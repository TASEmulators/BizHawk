#ifndef NALL_STREAM_STREAM_HPP
#define NALL_STREAM_STREAM_HPP

namespace nall {

struct stream {
  virtual bool seekable() const = 0;
  virtual bool readable() const = 0;
  virtual bool writable() const = 0;
  virtual bool randomaccess() const = 0;

  virtual unsigned size() const = 0;
  virtual unsigned offset() const = 0;
  virtual void seek(unsigned offset) const = 0;

  virtual uint8_t read() const = 0;
  virtual void write(uint8_t data) const = 0;

  inline virtual uint8_t read(unsigned) const { return 0; }
  inline virtual void write(unsigned, uint8_t) const {}

  inline bool end() const {
    return offset() >= size();
  }

  inline void copy(uint8_t *&data, unsigned &length) const {
    seek(0);
    length = size();
    data = new uint8_t[length];
    for(unsigned n = 0; n < length; n++) data[n] = read();
  }

  inline uintmax_t readl(unsigned length = 1) const {
    uintmax_t data = 0, shift = 0;
    while(length--) { data |= read() << shift; shift += 8; }
    return data;
  }

  inline uintmax_t readm(unsigned length = 1) const {
    uintmax_t data = 0;
    while(length--) data = (data << 8) | read();
    return data;
  }

  inline void read(uint8_t *data, unsigned length) const {
    while(length--) *data++ = read();
  }

  inline void writel(uintmax_t data, unsigned length = 1) const {
    while(length--) {
      write(data);
      data >>= 8;
    }
  }

  inline void writem(uintmax_t data, unsigned length = 1) const {
    uintmax_t shift = 8 * length;
    while(length--) {
      shift -= 8;
      write(data >> shift);
    }
  }

  inline void write(const uint8_t *data, unsigned length) const {
    while(length--) write(*data++);
  }

  struct byte {
    inline operator uint8_t() const { return s.read(offset); }
    inline byte& operator=(uint8_t data) { s.write(offset, data); }
    inline byte(const stream &s, unsigned offset) : s(s), offset(offset) {}

  private:
    const stream &s;
    const unsigned offset;
  };

  inline byte operator[](unsigned offset) const {
    return byte(*this, offset);
  }

  inline stream() {}
  inline virtual ~stream() {}
  stream(const stream&) = delete;
  stream& operator=(const stream&) = delete;
};

}

#endif
