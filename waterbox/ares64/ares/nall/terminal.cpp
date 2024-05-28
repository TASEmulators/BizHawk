#include <nall/terminal.hpp>

namespace nall::terminal {

NALL_HEADER_INLINE auto redirectStdioToTerminal(bool create) -> void {
#if defined(PLATFORM_WINDOWS)
  if(create) {
    FreeConsole();
    if(!AllocConsole()) return;
  } else if(!AttachConsole(ATTACH_PARENT_PROCESS)) {
    return;
  }

  //unless a new terminal was requested, do not reopen already valid handles (allow redirection to/from file)
  if(create || _get_osfhandle(_fileno(stdin )) < 0) freopen("CONIN$" , "r", stdin );
  if(create || _get_osfhandle(_fileno(stdout)) < 0) freopen("CONOUT$", "w", stdout);
  if(create || _get_osfhandle(_fileno(stderr)) < 0) freopen("CONOUT$", "w", stderr);
#endif
}

}
