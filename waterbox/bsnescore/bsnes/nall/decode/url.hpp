#pragma once

namespace nall::Decode {

//returns empty string on malformed content
inline auto URL(string_view input) -> string {
  string output;
  for(uint n = 0; n < input.size();) {
    char c = input[n];

    //unreserved characters
    if(c >= 'A' && c <= 'Z') { output.append(c); n++; continue; }
    if(c >= 'a' && c <= 'z') { output.append(c); n++; continue; }
    if(c >= '0' && c <= '9') { output.append(c); n++; continue; }
    if(c == '-' || c == '_' || c == '.' || c == '~') { output.append(c); n++; continue; }

    //special characters
    if(c == '+') { output.append(' '); n++; continue; }

    //reserved characters
    if(c != '%' || n + 2 >= input.size()) return "";
    char hi = input[n + 1];
    char lo = input[n + 2];
         if(hi >= '0' && hi <= '9') hi -= '0';
    else if(hi >= 'A' && hi <= 'F') hi -= 'A' - 10;
    else if(hi >= 'a' && hi <= 'f') hi -= 'a' - 10;
    else return "";
         if(lo >= '0' && lo <= '9') lo -= '0';
    else if(lo >= 'A' && lo <= 'F') lo -= 'A' - 10;
    else if(lo >= 'a' && lo <= 'f') lo -= 'a' - 10;
    else return "";
    char byte = hi * 16 + lo;
    output.append(byte);
    n += 3;
  }
  return output;
}

}
