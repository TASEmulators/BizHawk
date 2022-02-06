#pragma once

#include <nall/traits.hpp>

#undef min
#undef max

namespace nall {

template<typename T, typename U> constexpr auto min(const T& t, const U& u) -> T {
  return t < u ? t : (T)u;
}

template<typename T, typename U, typename... P> constexpr auto min(const T& t, const U& u, P&&... p) -> T {
  return t < u ? min(t, forward<P>(p)...) : min(u, forward<P>(p)...);
}

template<typename T, typename U> constexpr auto max(const T& t, const U& u) -> T {
  return t > u ? t : (T)u;
}

template<typename T, typename U, typename... P> constexpr auto max(const T& t, const U& u, P&&... p) -> T {
  return t > u ? max(t, forward<P>(p)...) : max(u, forward<P>(p)...);
}

}
