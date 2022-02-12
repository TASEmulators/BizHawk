#ifndef NALL_MACOS_GUARD_HPP
#define NALL_MACOS_GUARD_HPP

#define Boolean CocoaBoolean
#define decimal CocoaDecimal
#define DEBUG   CocoaDebug

#else
#undef NALL_MACOS_GUARD_HPP

#undef Boolean
#undef decimal
#undef DEBUG

#endif
