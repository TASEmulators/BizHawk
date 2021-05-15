#pragma once

namespace nall {

template<bool Insensitive, bool Quoted>
auto vector<string>::_split(string_view source, string_view find, long limit) -> type& {
  reset();
  if(limit <= 0 || find.size() == 0) return *this;

  const char* p = source.data();
  int size = source.size();
  int base = 0;
  int matches = 0;

  for(int n = 0, quoted = 0; n <= size - (int)find.size();) {
    if(Quoted) { if(p[n] == '\"') { quoted ^= 1; n++; continue; } if(quoted) { n++; continue; } }
    if(string::_compare<Insensitive>(p + n, size - n, find.data(), find.size())) { n++; continue; }
    if(matches >= limit) break;

    string& s = operator()(matches);
    s.resize(n - base);
    memory::copy(s.get(), p + base, n - base);

    n += find.size();
    base = n;
    matches++;
  }

  string& s = operator()(matches);
  s.resize(size - base);
  memory::copy(s.get(), p + base, size - base);

  return *this;
}

auto string::split(string_view on, long limit) const -> vector<string> { return vector<string>()._split<0, 0>(*this, on, limit); }
auto string::isplit(string_view on, long limit) const -> vector<string> { return vector<string>()._split<1, 0>(*this, on, limit); }
auto string::qsplit(string_view on, long limit) const -> vector<string> { return vector<string>()._split<0, 1>(*this, on, limit); }
auto string::iqsplit(string_view on, long limit) const -> vector<string> { return vector<string>()._split<1, 1>(*this, on, limit); }

}
