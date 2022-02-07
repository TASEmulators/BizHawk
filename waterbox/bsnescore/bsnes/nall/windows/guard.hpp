#ifndef NALL_WINDOWS_GUARD_HPP
#define NALL_WINDOWS_GUARD_HPP

#define boolean WindowsBoolean
#define interface WindowsInterface

#undef UNICODE
#undef WINVER
#undef WIN32_LEAN_AND_LEAN
#undef _WIN32_WINNT
#undef _WIN32_IE
#undef __MSVCRT_VERSION__
#undef NOMINMAX
#undef PATH_MAX

#define UNICODE
#define WINVER 0x0601
#define WIN32_LEAN_AND_MEAN
#define _WIN32_WINNT WINVER
#define _WIN32_IE WINVER
#define __MSVCRT_VERSION__ WINVER
#define NOMINMAX
#define PATH_MAX 260

#else
#undef NALL_WINDOWS_GUARD_HPP

#undef boolean
#undef interface

#undef far
#undef near

#endif
