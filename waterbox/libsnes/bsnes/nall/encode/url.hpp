#pragma once

namespace nall::Encode {

inline auto URL(string_view input) -> string {
  string output;
  for(auto c : input) {
    //unreserved characters
    if(c >= 'A' && c <= 'Z') { output.append(c); continue; }
    if(c >= 'a' && c <= 'z') { output.append(c); continue; }
    if(c >= '0' && c <= '9') { output.append(c); continue; }
    if(c == '-' || c == '_' || c == '.' || c == '~') { output.append(c); continue; }

    //special characters
    if(c == ' ') { output.append('+'); continue; }

    //reserved characters
    uint hi = (c >> 4) & 15;
    uint lo = (c >> 0) & 15;
    output.append('%');
    output.append((char)(hi < 10 ? ('0' + hi) : ('a' + hi - 10)));
    output.append((char)(lo < 10 ? ('0' + lo) : ('a' + lo - 10)));
  }
  return output;
}

}
