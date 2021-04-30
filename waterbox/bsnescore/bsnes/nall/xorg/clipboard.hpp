#pragma once

#include <nall/xorg/xorg.hpp>

namespace nall::Clipboard {

auto clear() -> void {
  XDisplay display;
  if(auto atom = XInternAtom(display, "CLIPBOARD", XlibTrue)) {
    XSetSelectionOwner(display, atom, XlibNone, XlibCurrentTime);
  }
}

}
