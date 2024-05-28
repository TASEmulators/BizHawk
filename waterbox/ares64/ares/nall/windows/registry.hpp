#pragma once

#include <nall/platform.hpp>
#include <nall/string.hpp>

namespace nall {

struct registry {
  static auto exists(const string& name) -> bool;

  static auto read(const string& name) -> string;

  static auto write(const string& name, const string& data = "") -> void;

  static auto remove(const string& name) -> bool;

  static auto contents(const string& name) -> vector<string>;

private:
  static auto root(const string& name);
};

}

#if defined(NALL_HEADER_ONLY)
  #include <nall/windows/registry.cpp>
#endif
