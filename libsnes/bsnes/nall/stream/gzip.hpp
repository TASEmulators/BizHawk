#ifdef NALL_STREAM_INTERNAL_HPP

namespace nall {

struct gzipstream : memorystream {
  inline gzipstream(const stream &stream) {
    unsigned size = stream.size();
    uint8_t *data = new uint8_t[size];
    stream.read(data, size);

    gzip archive;
    bool result = archive.decompress(data, size);
    delete[] data;
    if(result == false) return;

    psize = archive.size;
    pdata = new uint8_t[psize];
    memcpy(pdata, archive.data, psize);
  }

  inline ~gzipstream() {
    if(pdata) delete[] pdata;
  }
};

}

#endif
