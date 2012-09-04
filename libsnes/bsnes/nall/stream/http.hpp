#ifdef NALL_STREAM_INTERNAL_HPP

namespace nall {

struct httpstream : stream {
  inline bool seekable() const { return true; }
  inline bool readable() const { return true; }
  inline bool writable() const { return true; }
  inline bool randomaccess() const { return true; }

  inline unsigned size() const { return psize; }
  inline unsigned offset() const { return poffset; }
  inline void seek(unsigned offset) const { poffset = offset; }

  inline uint8_t read() const { return pdata[poffset++]; }
  inline void write(uint8_t data) const { pdata[poffset++] = data; }

  inline uint8_t read(unsigned offset) const { return pdata[offset]; }
  inline void write(unsigned offset, uint8_t data) const { pdata[offset] = data; }

  inline httpstream(const string &url, unsigned port) : pdata(nullptr), psize(0), poffset(0) {
    string uri = url;
    uri.ltrim<1>("http://");
    lstring part = uri.split<1>("/");
    part[1] = { "/", part[1] };

    http connection;
    if(connection.connect(part[0], port) == false) return;
    connection.download(part[1], pdata, psize);
  }

  inline ~httpstream() {
    if(pdata) delete[] pdata;
  }

private:
  mutable uint8_t *pdata;
  mutable unsigned psize, poffset;
};

}

#endif
