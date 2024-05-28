#pragma once

#include <initializer_list>
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
  using std::initializer_list;
  using std::is_array;
  using std::is_array_v;
  using std::is_base_of;
  using std::is_base_of_v;
  using std::is_function;
  using std::is_pointer;
  using std::is_pointer_v;
  using std::is_same;
  using std::is_same_v;
  using std::nullptr_t;
  using std::remove_extent;
  using std::remove_extent_t;
  using std::remove_reference;
  using std::remove_reference_t;
  using std::swap;
  using std::true_type;

  //directly specializing std traits would result in undefined behavior
  template<typename T> struct is_integral : std::is_integral<T> {};
  template<typename T> struct is_signed   : std::is_signed  <T> {};
  template<typename T> struct is_unsigned : std::is_unsigned<T> {};

  template<typename T> inline constexpr bool is_integral_v = is_integral<T>::value;
  template<typename T> inline constexpr bool is_signed_v   = is_signed  <T>::value;
  template<typename T> inline constexpr bool is_unsigned_v = is_unsigned<T>::value;

  //defined in arithmetic.hpp when unavailable as a builtin
  template<> struct is_integral<u128> : true_type {};
  template<> struct is_unsigned<u128> : true_type {};

  #if defined(__SIZEOF_INT128__)
  template<> struct is_integral<s128> : true_type {};
  template<> struct is_signed  <s128> : true_type {};
  #endif
}
