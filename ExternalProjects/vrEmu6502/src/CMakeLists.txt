cmake_minimum_required(VERSION 3.16)

set(CMAKE_RUNTIME_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/../../Assets/dll)

include(CheckSymbolExists)

add_library(vrEmu6502)
target_sources(vrEmu6502 PRIVATE vrEmu6502.c)
target_sources(vrEmu6502 PUBLIC vrEmu6502.h)

if (WIN32)
  if (BUILD_SHARED_LIBS)
     add_definitions(-DVR_EMU_6502_COMPILING_DLL)
   endif()
endif()

target_include_directories (vrEmu6502 INTERFACE ${CMAKE_CURRENT_SOURCE_DIR})


