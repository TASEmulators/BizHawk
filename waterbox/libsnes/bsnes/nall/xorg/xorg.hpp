#pragma once

#include <nall/xorg/guard.hpp>
#include <sys/ipc.h>
#include <sys/shm.h>
#include <X11/Xlib.h>
#include <X11/Xutil.h>
#include <X11/Xatom.h>
#include <nall/xorg/guard.hpp>

struct XDisplay {
  XDisplay() { _display = XOpenDisplay(nullptr); }
  ~XDisplay() { XCloseDisplay(_display); }
  operator XlibDisplay*() const { return _display; }

private:
  XlibDisplay* _display;
};
