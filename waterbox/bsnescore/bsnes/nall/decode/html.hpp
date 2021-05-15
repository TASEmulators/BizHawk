#pragma once

namespace nall::Decode {

inline auto HTML(const string& input) -> string {
  string output;
  for(uint n = 0; n < input.size();) {
    if(input[n] == '&') {
      if(input(n + 1) == 'a' && input(n + 2) == 'm' && input(n + 3) == 'p' && input(n + 4) == ';') {
        output.append('&');
        n += 5;
        continue;
      }
      if(input(n + 1) == 'l' && input(n + 2) == 't' && input(n + 3) == ';') {
        output.append('<');
        n += 4;
        continue;
      }
      if(input(n + 1) == 'g' && input(n + 2) == 't' && input(n + 3) == ';') {
        output.append('>');
        n += 4;
        continue;
      }
      if(input(n + 1) == 'q' && input(n + 2) == 'u' && input(n + 3) == 'o' && input(n + 4) == 't' && input(n + 5) == ';') {
        output.append('"');
        n += 6;
        continue;
      }
      if(input(n + 1) == 'a' && input(n + 2) == 'p' && input(n + 3) == 'o' && input(n + 4) == 's' && input(n + 5) == ';') {
        output.append('\'');
        n += 6;
        continue;
      }
    }
    output.append(input[n++]);
  }
  return output;
}

}
