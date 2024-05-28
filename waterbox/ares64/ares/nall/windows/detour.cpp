#include <nall/windows/detour.hpp>

namespace nall {

NALL_HEADER_INLINE auto detour::insert(const string& moduleName, const string& functionName, void*& source, void* target) -> bool {
  #if defined(ARCHITECTURE_X86)
  HMODULE module = GetModuleHandleW(utf16_t(moduleName));
  if(!module) return false;

  u8* sourceData = (u8*)GetProcAddress(module, functionName);
  if(!sourceData) return false;

  u32 sourceLength = detour::length(sourceData);
  if(sourceLength < 5) {
    //unable to clone enough bytes to insert hook
    #if 1
    string output = {"detour::insert(", moduleName, "::", functionName, ") failed: "};
    for(u32 n = 0; n < 16; n++) output.append(hex(sourceData[n], 2L), " ");
    output.trimRight(" ", 1L);
    MessageBoxA(0, output, "nall::detour", MB_OK);
    #endif
    return false;
  }

  auto mirrorData = new u8[512]();
  detour::mirror(mirrorData, sourceData);

  DWORD privileges;
  VirtualProtect((void*)mirrorData, 512, PAGE_EXECUTE_READWRITE, &privileges);
  VirtualProtect((void*)sourceData, 256, PAGE_EXECUTE_READWRITE, &privileges);
  u64 address = (u64)target - ((u64)sourceData + 5);
  sourceData[0] = 0xe9;  //jmp target
  sourceData[1] = address >>  0;
  sourceData[2] = address >>  8;
  sourceData[3] = address >> 16;
  sourceData[4] = address >> 24;
  VirtualProtect((void*)sourceData, 256, privileges, &privileges);

  source = (void*)mirrorData;
  return true;
  #else
  return false;
  #endif
}

NALL_HEADER_INLINE auto detour::remove(const string& moduleName, const string& functionName, void*& source) -> bool {
  HMODULE module = GetModuleHandleW(utf16_t(moduleName));
  if(!module) return false;

  auto sourceData = (u8*)GetProcAddress(module, functionName);
  if(!sourceData) return false;

  auto mirrorData = (u8*)source;
  if(mirrorData == sourceData) return false;  //hook was never installed

  u32 length = detour::length(256 + mirrorData);
  if(length < 5) return false;

  DWORD privileges;
  VirtualProtect((void*)sourceData, 256, PAGE_EXECUTE_READWRITE, &privileges);
  for(u32 n = 0; n < length; n++) sourceData[n] = mirrorData[256 + n];
  VirtualProtect((void*)sourceData, 256, privileges, &privileges);

  source = (void*)sourceData;
  delete[] mirrorData;
  return true;
}

}
