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
  struct mode  { enum : u32 { read, write, modify, append }; };
  struct index { enum : u32 { absolute, relative }; };

  file_buffer(const file_buffer&) = delete;
  auto operator=(const file_buffer&) -> file_buffer& = delete;

  file_buffer() = default;
  file_buffer(const string& filename, u32 mode) { open(filename, mode); }

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

    source.bufferOffset = -1LL;
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

  auto read() -> u8 {
    if(!fileHandle) return 0;             //file not open
    if(fileOffset >= fileSize) return 0;  //cannot read past end of file
    bufferSynchronize();
    return buffer[fileOffset++ & buffer.size() - 1];
  }

  template<typename T = u64> auto readl(u32 length = 1) -> T {
    T data = 0;
    for(u32 n : range(length)) {
      data |= (T)read() << n * 8;
    }
    return data;
  }

  template<typename T = u64> auto readm(u32 length = 1) -> T {
    T data = 0;
    while(length--) {
      data <<= 8;
      data |= read();
    }
    return data;
  }

  auto reads(u64 length) -> string {
    string result;
    result.resize(length);
    for(auto& byte : result) byte = read();
    return result;
  }

  auto read(array_span<u8> memory) -> void {
    for(auto& byte : memory) byte = read();
  }

  auto write(u8 data) -> void {
    if(!fileHandle) return;             //file not open
    if(fileMode == mode::read) return;  //writes not permitted
    bufferSynchronize();
    buffer[fileOffset++ & buffer.size() - 1] = data;
    bufferDirty = true;
    if(fileOffset > fileSize) fileSize = fileOffset;
  }

  template<typename T = u64> auto writel(T data, u32 length = 1) -> void {
    while(length--) {
      write(u8(data));
      data >>= 8;
    }
  }

  template<typename T = u64> auto writem(T data, u32 length = 1) -> void {
    for(u32 n : reverse(range(length))) {
      write(u8(data >> n * 8));
    }
  }

  auto writes(const string& s) -> void {
    for(auto& byte : s) write(byte);
  }

  auto write(array_view<u8> memory) -> void {
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

  auto seek(s64 offset, u32 index_ = index::absolute) -> void {
    if(!fileHandle) return;
    bufferFlush();

    s64 seekOffset = fileOffset;
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

  auto offset() const -> u64 {
    if(!fileHandle) return 0;
    return fileOffset;
  }

  auto size() const -> u64 {
    if(!fileHandle) return 0;
    return fileSize;
  }

  auto truncate(u64 size) -> bool {
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

  auto open(const string& filename, u32 mode_) -> bool {
    close();

    switch(fileMode = mode_) {
    #if defined(API_POSIX)
    case mode::read:   fileHandle = fopen(filename, "rb" ); break;
    case mode::write:  fileHandle = fopen(filename, "wb+"); break;  //need read permission for buffering
    case mode::modify: fileHandle = fopen(filename, "rb+"); break;
    case mode::append: fileHandle = fopen(filename, "ab+"); break;
    #elif defined(API_WINDOWS)
    case mode::read:   fileHandle = _wfopen(utf16_t(filename), L"rb" ); break;
    case mode::write:  fileHandle = _wfopen(utf16_t(filename), L"wb+"); break;
    case mode::modify: fileHandle = _wfopen(utf16_t(filename), L"rb+"); break;
    case mode::append: fileHandle = _wfopen(utf16_t(filename), L"ab+"); break;
    #endif
    }
    if(!fileHandle) return false;

    bufferOffset = -1LL;
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
  array<u8[4096]> buffer;
  s64 bufferOffset = -1LL;
  bool bufferDirty = false;
  FILE* fileHandle = nullptr;
  u64 fileOffset = 0;
  u64 fileSize = 0;
  u32 fileMode = mode::read;

  auto bufferSynchronize() -> void {
    if(!fileHandle) return;
    if(bufferOffset == (fileOffset & ~(buffer.size() - 1))) return;

    bufferFlush();
    bufferOffset = fileOffset & ~(buffer.size() - 1);
    fseek(fileHandle, bufferOffset, SEEK_SET);
    u64 length = bufferOffset + buffer.size() <= fileSize ? buffer.size() : fileSize & buffer.size() - 1;
    if(length) (void)fread(buffer.data(), 1, length, fileHandle);
  }

  auto bufferFlush() -> void {
    if(!fileHandle) return;             //file not open
    if(fileMode == mode::read) return;  //buffer cannot be written to
    if(bufferOffset < 0) return;        //buffer unused
    if(!bufferDirty) return;            //buffer unmodified since read

    fseek(fileHandle, bufferOffset, SEEK_SET);
    u64 length = bufferOffset + buffer.size() <= fileSize ? buffer.size() : fileSize & buffer.size() - 1;
    if(length) (void)fwrite(buffer.data(), 1, length, fileHandle);
    bufferOffset = -1LL;
    bufferDirty = false;
  }
};

}
