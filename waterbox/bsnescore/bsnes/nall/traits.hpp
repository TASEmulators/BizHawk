#pragma once

#include <type_traits>
#include <nall/stdint.hpp>

//pull all type traits used by nall from std namespace into nall namespace
//this removes the requirement to prefix type traits with std:: within nall

namespace nall {
  using std::add_const;
  using std::conditional;
  using std::conditional_t;
  using std::decay;
  using std::declval;
  using std::enable_if;
  using std::enable_if_t;
  using std::false_type;
  using std::is_floating_point;
  using std::is_floating_point_v;
  using std::forward;
  using std::initializer_list;
  using std::is_array;
  using std::is_base_of;
  using std::is_base_of_v;
  using std::is_function;
  using std::is_integral;
  using std::is_integral_v;
  using std::is_same;
  using std::is_same_v;
  using std::is_signed;
  using std::is_signed_v;
  using std::is_unsigned;
  using std::is_unsigned_v;
  using std::move;
  using std::nullptr_t;
  using std::remove_extent;
  using std::remove_reference;
  using std::swap;
  using std::true_type;
}

namespace std {
  #if INTMAX_BITS >= 128
  template<> struct is_signed<int128_t> : true_type {};
  template<> struct is_unsigned<uint128_t> : true_type {};
  #endif
}
