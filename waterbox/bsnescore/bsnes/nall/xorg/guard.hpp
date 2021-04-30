#ifndef NALL_XORG_GUARD_HPP
#define NALL_XORG_GUARD_HPP

#define Atom XlibAtom
#define Display XlibDisplay
#define Font XlibFont
#define Screen XlibScreen
#define Window XlibWindow

#else
#undef NALL_XORG_GUARD_HPP

#undef Atom
#undef Display
#undef Font
#undef Screen
#undef Window

#undef Above
#undef Below
#undef Bool

#ifndef NALL_XORG_GUARD_CONSTANTS
#define NALL_XORG_GUARD_CONSTANTS
enum XlibConstants : int {
  XlibButton1 = Button1,
  XlibButton2 = Button2,
  XlibButton3 = Button3,
  XlibButton4 = Button4,
  XlibButton5 = Button5,
  XlibCurrentTime = CurrentTime,
  XlibFalse = False,
  XlibNone = None,
  XlibTrue = True,
};
#endif

#undef Button1
#undef Button2
#undef Button3
#undef Button4
#undef Button5
#undef CurrentTime
#undef False
#undef None
#undef True

#undef MAX
#undef MIN

#endif
