#pragma once

namespace nall {

inline constexpr auto operator"" _Kibit(unsigned long long value) { return value * 1024 / 8; }
inline constexpr auto operator"" _Mibit(unsigned long long value) { return value * 1024 * 1024 / 8; }
inline constexpr auto operator"" _Gibit(unsigned long long value) { return value * 1024 * 1024 * 1024 / 8; }
inline constexpr auto operator"" _Tibit(unsigned long long value) { return value * 1024 * 1024 * 1024 * 1024 / 8; }

inline constexpr auto operator"" _KiB(unsigned long long value) { return value * 1024; }
inline constexpr auto operator"" _MiB(unsigned long long value) { return value * 1024 * 1024; }
inline constexpr auto operator"" _GiB(unsigned long long value) { return value * 1024 * 1024 * 1024; }
inline constexpr auto operator"" _TiB(unsigned long long value) { return value * 1024 * 1024 * 1024 * 1024; }

inline constexpr auto operator"" _KHz(unsigned long long value) { return value * 1000; }
inline constexpr auto operator"" _MHz(unsigned long long value) { return value * 1000 * 1000; }
inline constexpr auto operator"" _GHz(unsigned long long value) { return value * 1000 * 1000 * 1000; }
inline constexpr auto operator"" _THz(unsigned long long value) { return value * 1000 * 1000 * 1000 * 1000; }

}
