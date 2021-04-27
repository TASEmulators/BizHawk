#include <emulator/emulator.hpp>

#include "7zAlloc.c"
#include "7zArcIn.c"
#include "7zBuf.c"
#include "7zCrc.c"
#include "7zCrcOpt.c"
#include "7zDec.c"
#include "7zFile.c"
#include "7zStream.c"
#include "Bcj2.c"
#include "Bra.c"
#include "Bra86.c"
#include "BraIA64.c"
#include "CpuArch.c"
#include "Delta.c"
#undef kBitModelTotal
#undef kTopValue
#include "LzmaDec.c"
#include "Lzma2Dec.c"
#define kInputBufSize ((size_t)1 << 18)

namespace LZMA {

auto extract(string_view filename) -> vector<uint8_t> {
  vector<uint8_t> result;

  static bool initialized = false;
  if(!initialized) {
    initialized = true;
    CrcGenerateTable();
  }

  static ISzAlloc allocate = {SzAlloc, SzFree};
  static ISzAlloc allocateTemporary = {SzAllocTemp, SzFreeTemp};

  CFileInStream archive;
  #if defined(PLATFORM_WINDOWS)
  if(InFile_OpenW(&archive.file, (const wchar_t*)utf16_t(filename)) != SZ_OK) return result;
  #else
  if(InFile_Open(&archive.file, (const char*)filename) != SZ_OK) return result;
  #endif
  FileInStream_CreateVTable(&archive);

  CLookToRead2 look;
  LookToRead2_CreateVTable(&look, false);
  look.buf = (Byte*)ISzAlloc_Alloc(&allocate, kInputBufSize);
  look.bufSize = kInputBufSize;
  look.realStream = &archive.vt;
  LookToRead2_Init(&look);

  CSzArEx db;
  SzArEx_Init(&db);
  if(SzArEx_Open(&db, &look.vt, &allocate, &allocateTemporary) != SZ_OK || db.NumFiles == 0) {
    SzArEx_Free(&db, &allocate);
    return result;
  }

  for(uint index : range(db.NumFiles)) {
    if(SzArEx_IsDir(&db, index)) continue;

    UInt32 blockIndex = -1;
    Byte* filedata = nullptr;
    size_t filesize = 0;
    size_t offset = 0;
    size_t count = 0;
    if(SzArEx_Extract(&db, &look.vt, index, &blockIndex,
      &filedata, &filesize, &offset, &count, &allocate, &allocateTemporary
    ) == SZ_OK) {
      result.resize(filesize);
      memory::copy(result.data(), filedata, filesize);
      ISzAlloc_Free(&allocate, filedata);
      break;
    }
  }

  SzArEx_Free(&db, &allocate);
  return result;
}

}
