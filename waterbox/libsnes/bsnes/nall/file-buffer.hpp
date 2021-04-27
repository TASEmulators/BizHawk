#pragma once

#include <nall/platform.hpp>
#include <nall/array-span.hpp>
#include <nall/array-view.hpp>
#include <nall/inode.hpp>
#include <nall/range.hpp>
#include <nall/stdint.hpp>
#include <nall/string.hpp>
#include <nall/utility.hpp>
#include <nall/varint.hpp>
#include <nall/hash/sha256.hpp>

namespace nall {

//on Windows (at least for 7 and earlier), FILE* is not buffered
//thus, reading/writing one byte at a time will be dramatically slower
//on all other OSes, FILE* is buffered
//in order to ensure good performance, file_buffer implements its own buffer
//this speeds up Windows substantially, without harming performance elsewhere much

struct file_buffer {
  struct mode { enum : uint { read, write, modify, append }; };
  struct index { enum : uint { absolute, relative }; };

  file_buffer(const file_buffer&) = delete;
  auto operator=(const file_buffer&) -> file_buffer& = delete;

  file_buffer() = default;
  file_buffer(const string& filename, uint mode) { open(filename, mode); }

  file_buffer(file_buffer&& source) { operator=(move(source)); }

  ~file_buffer() { close(); }

  auto operator=(file_buffer&& source) -> file_buffer& {
    buffer = source.buffer;
    bufferOffset = source.bufferOffset;
    bufferDirty = source.bufferDirty;
    fileHandle = source.fileHandle;
    fileOffset = source.fileOffset;
    fileSize = source.fileSize;
    fileMode = source.fileMode;

    source.bufferOffset = -1;
    source.bufferDirty = false;
    source.fileHandle = nullptr;
    source.fileOffset = 0;
    source.fileSize = 0;
    source.fileMode = mode::read;

    return *this;
  }

  explicit operator bool() const {
    return (bool)fileHandle;
  }

  auto read() -> uint8_t {
    if(!fileHandle) return 0;              //file not open
    if(fileMode == mode::write) return 0;  //reads not permitted
    if(fileOffset >= fileSize) return 0;   //cannot read past end of file
    bufferSynchronize();
    return buffer[fileOffset++ & buffer.size() - 1];
  }

  template<typename T = uint64_t> auto readl(uint length = 1) -> T {
    T data = 0;
    for(uint n : range(length)) {
      data |= (T)read() << n * 8;
    }
    return data;
  }

  template<typename T = uint64_t> auto readm(uint length = 1) -> T {
    T data = 0;
    while(length--) {
      data <<= 8;
      data |= read();
    }
    return data;
  }

  auto reads(uint length) -> string {
    string result;
    result.resize(length);
    for(auto& byte : result) byte = read();
    return result;
  }

  auto read(array_span<uint8_t> memory) -> void {
    for(auto& byte : memory) byte = read();
  }

  auto write(uint8_t data) -> void {
    if(!fileHandle) return;             //file not open
    if(fileMode == mode::read) return;  //writes not permitted
    bufferSynchronize();
    buffer[fileOffset++ & buffer.size() - 1] = data;
    bufferDirty = true;
    if(fileOffset > fileSize) fileSize = fileOffset;
  }

  template<typename T = uint64_t> auto writel(T data, uint length = 1) -> void {
    while(length--) {
      write(uint8_t(data));
      data >>= 8;
    }
  }

  template<typename T = uint64_t> auto writem(T data, uint length = 1) -> void {
    for(uint n : reverse(range(length))) {
      write(uint8_t(data >> n * 8));
    }
  }

  auto writes(const string& s) -> void {
    for(auto& byte : s) write(byte);
  }

  auto write(array_view<uint8_t> memory) -> void {
    for(auto& byte : memory) write(byte);
  }

  template<typename... P> auto print(P&&... p) -> void {
    string s{forward<P>(p)...};
    for(auto& byte : s) write(byte);
  }

  auto flush() -> void {
    bufferFlush();
    fflush(fileHandle);
  }

  auto seek(int64_t offset, uint index_ = index::absolute) -> void {
    if(!fileHandle) return;
    bufferFlush();

    int64_t seekOffset = fileOffset;
    switch(index_) {
    case index::absolute: seekOffset  = offset; break;
    case index::relative: seekOffset += offset; break;
    }

    if(seekOffset < 0) seekOffset = 0;  //cannot seek before start of file
    if(seekOffset > fileSize) {
      if(fileMode == mode::read) {      //cannot seek past end of file
        seekOffset = fileSize;
      } else {                          //pad file to requested location
        fileOffset = fileSize;
        while(fileSize < seekOffset) write(0);
      }
    }

    fileOffset = seekOffset;
  }

  auto offset() const -> uint64_t {
    if(!fileHandle) return 0;
    return fileOffset;
  }

  auto size() const -> uint64_t {
    if(!fileHandle) return 0;
    return fileSize;
  }

  auto truncate(uint64_t size) -> bool {
    if(!fileHandle) return false;
    #if defined(API_POSIX)
    return ftruncate(fileno(fileHandle), size) == 0;
    #elif defined(API_WINDOWS)
    return _chsize(fileno(fileHandle), size) == 0;
    #endif
  }

  auto end() const -> bool {
    if(!fileHandle) return true;
    return fileOffset >= fileSize;
  }

  auto open(const string& filename, uint mode_) -> bool {
    close();

    switch(fileMode = mode_) {
    #if defined(API_POSIX)
    case mode::read:   fileHandle = fopen(filename, "rb" ); break;
    case mode::write:  fileHandle = fopen(filename, "wb+"); break;  //need read permission for buffering
    case mode::modify: fileHandle = fopen(filename, "rb+"); break;
    case mode::append: fileHandle = fopen(filename, "wb+"); break;
    #elif defined(API_WINDOWS)
    case mode::read:   fileHandle = _wfopen(utf16_t(filename), L"rb" ); break;
    case mode::write:  fileHandle = _wfopen(utf16_t(filename), L"wb+"); break;
    case mode::modify: fileHandle = _wfopen(utf16_t(filename), L"rb+"); break;
    case mode::append: fileHandle = _wfopen(utf16_t(filename), L"wb+"); break;
    #endif
    }
    if(!fileHandle) return false;

    bufferOffset = -1;
    fileOffset = 0;
    fseek(fileHandle, 0, SEEK_END);
    fileSize = ftell(fileHandle);
    fseek(fileHandle, 0, SEEK_SET);
    return true;
  }

  auto close() -> void {
    if(!fileHandle) return;
    bufferFlush();
    fclose(fileHandle);
    fileHandle = nullptr;
  }

private:
  array<uint8_t[4096]> buffer;
  int bufferOffset = -1;
  bool bufferDirty = false;
  FILE* fileHandle = nullptr;
  uint64_t fileOffset = 0;
  uint64_t fileSize = 0;
  uint fileMode = mode::read;

  auto bufferSynchronize() -> void {
    if(!fileHandle) return;
    if(bufferOffset == (fileOffset & ~(buffer.size() - 1))) return;

    bufferFlush();
    bufferOffset = fileOffset & ~(buffer.size() - 1);
    fseek(fileHandle, bufferOffset, SEEK_SET);
    uint64_t length = bufferOffset + buffer.size() <= fileSize ? buffer.size() : fileSize & buffer.size() - 1;
    if(length) (void)fread(buffer.data(), 1, length, fileHandle);
  }

  auto bufferFlush() -> void {
    if(!fileHandle) return;             //file not open
    if(fileMode == mode::read) return;  //buffer cannot be written to
    if(bufferOffset < 0) return;        //buffer unused
    if(!bufferDirty) return;            //buffer unmodified since read

    fseek(fileHandle, bufferOffset, SEEK_SET);
    uint64_t length = bufferOffset + buffer.size() <= fileSize ? buffer.size() : fileSize & buffer.size() - 1;
    if(length) (void)fwrite(buffer.data(), 1, length, fileHandle);
    bufferOffset = -1;
    bufferDirty = false;
  }
};

}
