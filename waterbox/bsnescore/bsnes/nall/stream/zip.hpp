#ifdef NALL_STREAM_INTERNAL_HPP

namespace nall {

struct zipstream : memorystream {
  inline zipstream(const stream &stream, const string &filter = "*") {
    unsigned size = stream.size();
    uint8_t *data = new uint8_t[size];
    stream.read(data, size);

    zip archive;
    if(archive.open(data, size) == false) return;
    delete[] data;

    for(auto &file : archive.file) {
      if(file.name.wildcard(filter)) {
        archive.extract(file, pdata, psize);
        return;
      }
    }
  }

  inline ~zipstream() {
    if(pdata) delete[] pdata;
  }
};

}

#endif
