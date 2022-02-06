#pragma once

#include <nall/foreach.hpp>
#include <nall/platform.hpp>
#include <nall/stdint.hpp>
#include <nall/string.hpp>
#include <nall/utf8.hpp>

namespace nall {

#define Copy    0
#define RelNear 1

struct detour {
  static auto insert(const string& moduleName, const string& functionName, void*& source, void* target) -> bool;
  static auto remove(const string& moduleName, const string& functionName, void*& source) -> bool;

protected:
  static auto length(const u8* function) -> u32;
  static auto mirror(u8* target, const u8* source) -> u32;

  struct opcode {
    u16 prefix;
    u32 length;
    u32 mode;
    u16 modify;
  };
  static opcode opcodes[];
};

//TODO:
//* fs:, gs: should force another opcode copy
//* conditional branches within +5-byte range should fail
detour::opcode detour::opcodes[] = {
  {0x50, 1},                   //push eax
  {0x51, 1},                   //push ecx
  {0x52, 1},                   //push edx
  {0x53, 1},                   //push ebx
  {0x54, 1},                   //push esp
  {0x55, 1},                   //push ebp
  {0x56, 1},                   //push esi
  {0x57, 1},                   //push edi
  {0x58, 1},                   //pop eax
  {0x59, 1},                   //pop ecx
  {0x5a, 1},                   //pop edx
  {0x5b, 1},                   //pop ebx
  {0x5c, 1},                   //pop esp
  {0x5d, 1},                   //pop ebp
  {0x5e, 1},                   //pop esi
  {0x5f, 1},                   //pop edi
  {0x64, 1},                   //fs:
  {0x65, 1},                   //gs:
  {0x68, 5},                   //push dword
  {0x6a, 2},                   //push byte
  {0x74, 2, RelNear, 0x0f84},  //je near      -> je far
  {0x75, 2, RelNear, 0x0f85},  //jne near     -> jne far
  {0x89, 2},                   //mov reg,reg
  {0x8b, 2},                   //mov reg,reg
  {0x90, 1},                   //nop
  {0xa1, 5},                   //mov eax,[dword]
  {0xeb, 2, RelNear,   0xe9},  //jmp near     -> jmp far
};

inline auto detour::insert(const string& moduleName, const string& functionName, void*& source, void* target) -> bool {
  HMODULE module = GetModuleHandleW(utf16_t(moduleName));
  if(!module) return false;

  u8* sourceData = (u8*)GetProcAddress(module, functionName);
  if(!sourceData) return false;

  u32 sourceLength = detour::length(sourceData);
  if(sourceLength < 5) {
    //unable to clone enough bytes to insert hook
    #if 1
    string output = {"detour::insert(", moduleName, "::", functionName, ") failed: "};
    for(u32 n = 0; n < 16; n++) output.append(hex<2>(sourceData[n]), " ");
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
}

inline auto detour::remove(const string& moduleName, const string& functionName, void*& source) -> bool {
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

inline auto detour::length(const u8* function) -> u32 {
  u32 length = 0;
  while(length < 5) {
    detour::opcode *opcode = 0;
    foreach(op, detour::opcodes) {
      if(function[length] == op.prefix) {
        opcode = &op;
        break;
      }
    }
    if(opcode == 0) break;
    length += opcode->length;
  }
  return length;
}

inline auto detour::mirror(u8* target, const u8* source) -> u32 {
  const u8* entryPoint = source;
  for(u32 n = 0; n < 256; n++) target[256 + n] = source[n];

  u32 size = detour::length(source);
  while(size) {
    detour::opcode* opcode = nullptr;
    foreach(op, detour::opcodes) {
      if(*source == op.prefix) {
        opcode = &op;
        break;
      }
    }

    switch(opcode->mode) {
    case Copy:
      for(u32 n = 0; n < opcode->length; n++) *target++ = *source++;
      break;
    case RelNear: {
      source++;
      u64 sourceAddress = (u64)source + 1 + (s8)*source;
      *target++ = opcode->modify;
      if(opcode->modify >> 8) *target++ = opcode->modify >> 8;
      u64 targetAddress = (u64)target + 4;
      u64 address = sourceAddress - targetAddress;
      *target++ = address >>  0;
      *target++ = address >>  8;
      *target++ = address >> 16;
      *target++ = address >> 24;
      source += 2;
    } break;
    }

    size -= opcode->length;
  }

  u64 address = (entryPoint + detour::length(entryPoint)) - (target + 5);
  *target++ = 0xe9;  //jmp entryPoint
  *target++ = address >>  0;
  *target++ = address >>  8;
  *target++ = address >> 16;
  *target++ = address >> 24;

  return source - entryPoint;
}

#undef Implied
#undef RelNear

}
