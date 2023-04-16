#pragma once

namespace nall {

template<typename... P> inline auto vector<string>::append(const string& data, P&&... p) -> type& {
  vector_base::append(data);
  append(forward<P>(p)...);
  return *this;
}

inline auto vector<string>::append() -> type& {
  return *this;
}

inline auto vector<string>::isort() -> type& {
  sort([](const string& x, const string& y) {
    return memory::icompare(x.data(), x.size(), y.data(), y.size()) < 0;
  });
  return *this;
}

inline auto vector<string>::find(string_view source) const -> maybe<u32> {
  for(u32 n = 0; n < size(); n++) {
    if(operator[](n).equals(source)) return n;
  }
  return {};
}

inline auto vector<string>::ifind(string_view source) const -> maybe<u32> {
  for(u32 n = 0; n < size(); n++) {
    if(operator[](n).iequals(source)) return n;
  }
  return {};
}

inline auto vector<string>::match(string_view pattern) const -> vector<string> {
  vector<string> result;
  for(u32 n = 0; n < size(); n++) {
    if(operator[](n).match(pattern)) result.append(operator[](n));
  }
  return result;
}

inline auto vector<string>::merge(string_view separator) const -> string {
  string output;
  for(u32 n = 0; n < size(); n++) {
    output.append(operator[](n));
    if(n < size() - 1) output.append(separator.data());
  }
  return output;
}

inline auto vector<string>::strip() -> type& {
  for(u32 n = 0; n < size(); n++) {
    operator[](n).strip();
  }
  return *this;
}

}
