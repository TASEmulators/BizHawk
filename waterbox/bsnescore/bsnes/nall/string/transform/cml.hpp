#pragma once

/* CSS Markup Language (CML) v1.0 parser
 * revision 0.02
 */

#include <nall/location.hpp>

namespace nall {

struct CML {
  auto& setPath(const string& pathname) { settings.path = pathname; return *this; }
  auto& setReader(const function<string (string)>& reader) { settings.reader = reader; return *this; }

  auto parse(const string& filename) -> string;
  auto parse(const string& filedata, const string& pathname) -> string;

private:
  struct Settings {
    string path;
    function<string (string)> reader;
  } settings;

  struct State {
    string output;
  } state;

  struct Variable {
    string name;
    string value;
  };
  vector<Variable> variables;
  bool inMedia = false;
  bool inMediaNode = false;

  auto parseDocument(const string& filedata, const string& pathname, uint depth) -> bool;
};

inline auto CML::parse(const string& filename) -> string {
  if(!settings.path) settings.path = Location::path(filename);
  string document = settings.reader ? settings.reader(filename) : string::read(filename);
  parseDocument(document, settings.path, 0);
  return state.output;
}

inline auto CML::parse(const string& filedata, const string& pathname) -> string {
  settings.path = pathname;
  parseDocument(filedata, settings.path, 0);
  return state.output;
}

inline auto CML::parseDocument(const string& filedata, const string& pathname, uint depth) -> bool {
  if(depth >= 100) return false;  //prevent infinite recursion

  auto vendorAppend = [&](const string& name, const string& value) {
    state.output.append("  -moz-", name, ": ", value, ";\n");
    state.output.append("  -webkit-", name, ": ", value, ";\n");
  };

  for(auto& block : filedata.split("\n\n")) {
    auto lines = block.stripRight().split("\n");
    auto name = lines.takeFirst();

    if(name.beginsWith("include ")) {
      name.trimLeft("include ", 1L);
      string filename{pathname, name};
      string document = settings.reader ? settings.reader(filename) : string::read(filename);
      parseDocument(document, Location::path(filename), depth + 1);
      continue;
    }

    if(name == "variables") {
      for(auto& line : lines) {
        auto data = line.split(":", 1L).strip();
        variables.append({data(0), data(1)});
      }
      continue;
    }

    state.output.append(name, " {\n");
    inMedia = name.beginsWith("@media");

    for(auto& line : lines) {
      if(inMedia && !line.find(": ")) {
        if(inMediaNode) state.output.append("  }\n");
        state.output.append(line, " {\n");
        inMediaNode = true;
        continue;
      }

      auto data = line.split(":", 1L).strip();
      auto name = data(0), value = data(1);
      while(auto offset = value.find("var(")) {
        bool found = false;
        if(auto length = value.findFrom(*offset, ")")) {
          string name = slice(value, *offset + 4, *length - 4);
          for(auto& variable : variables) {
            if(variable.name == name) {
              value = {slice(value, 0, *offset), variable.value, slice(value, *offset + *length + 1)};
              found = true;
              break;
            }
          }
        }
        if(!found) break;
      }
      state.output.append(inMedia ? "    " : "  ", name, ": ", value, ";\n");
      if(name == "box-sizing") vendorAppend(name, value);
    }
    if(inMediaNode) {
      state.output.append("  }\n");
      inMediaNode = false;
    }
    state.output.append("}\n\n");
  }

  return true;
}

}
