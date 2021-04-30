#pragma once

namespace nall::Encode {

inline auto HTML(const string& input) -> string {
  string output;
  for(char c : input) {
    if(c == '&' ) { output.append("&amp;" ); continue; }
    if(c == '<' ) { output.append("&lt;"  ); continue; }
    if(c == '>' ) { output.append("&gt;"  ); continue; }
    if(c == '"' ) { output.append("&quot;"); continue; }
    if(c == '\'') { output.append("&apos;"); continue; }
    output.append(c);
  }
  return output;
}

}
