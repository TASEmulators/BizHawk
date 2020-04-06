#pragma once

#include "emuware/emuware.h"

#define SIZEOF_DOUBLE 8

#define LSB_FIRST

EW_EXPORT int os_test();

#include <type_traits>

template<typename T> typename std::remove_all_extents<T>::type* MDAP(T* v) { return (typename std::remove_all_extents<T>::type*)v; }
