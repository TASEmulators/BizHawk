#pragma once

#include <nall/xorg/xorg.hpp>

namespace nall::Clipboard {

inline auto clear() -> void {
  XDisplay display;
  if(auto atom = XInternAtom(display, "CLIPBOARD", XlibTrue)) {
    XSetSelectionOwner(display, atom, XlibNone, XlibCurrentTime);
  }
}

}
