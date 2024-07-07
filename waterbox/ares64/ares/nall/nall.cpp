#if !defined(NALL_HEADER_ONLY)

#include <nall/intrinsics.hpp>

#if defined(PLATFORM_WINDOWS)
  #include <nall/windows/windows.hpp>
#endif

#include <nall/directory.cpp>
#include <nall/dl.cpp>
#include <nall/file-map.cpp>
#include <nall/inode.cpp>
#include <nall/memory.cpp>
#include <nall/path.cpp>
#include <nall/platform.cpp>
#include <nall/random.cpp>
#include <nall/run.cpp>
#include <nall/terminal.cpp>
#include <nall/thread.cpp>
#include <nall/tcptext/tcp-socket.cpp>
#include <nall/tcptext/tcptext-server.cpp>
//currently unused by ares
//#include <nall/smtp.cpp>
//#include <nall/http/client.cpp>
//#include <nall/http/server.cpp>
#if defined(PLATFORM_WINDOWS)
  //#include <nall/windows/detour.cpp>
  //#include <nall/windows/guid.cpp>
  //#include <nall/windows/launcher.cpp>
  #include <nall/windows/registry.cpp>
  #include <nall/windows/utf8.cpp>
#endif

#endif
