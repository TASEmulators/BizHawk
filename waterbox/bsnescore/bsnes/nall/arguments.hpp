#pragma once

#include <nall/location.hpp>
#include <nall/path.hpp>
#include <nall/string.hpp>
#include <nall/vector.hpp>

namespace nall {

struct Arguments {
  Arguments(int argc, char** argv);
  Arguments(vector<string> arguments);

  explicit operator bool() const { return (bool)arguments; }
  auto size() const -> uint { return arguments.size(); }

  auto operator[](uint index) -> string& { return arguments[index]; }
  auto operator[](uint index) const -> const string& { return arguments[index]; }

  auto programPath() const -> string;
  auto programName() const -> string;
  auto programLocation() const -> string;

  auto find(string_view name) const -> bool;
  auto find(string_view name, bool& argument) const -> bool;
  auto find(string_view name, string& argument) const -> bool;

  auto begin() const { return arguments.begin(); }
  auto end() const { return arguments.end(); }

  auto rbegin() const { return arguments.rbegin(); }
  auto rend() const { return arguments.rend(); }

  auto take() -> string;
  auto take(string_view name) -> bool;
  auto take(string_view name, bool& argument) -> bool;
  auto take(string_view name, string& argument) -> bool;

  auto begin() { return arguments.begin(); }
  auto end() { return arguments.end(); }

  auto rbegin() { return arguments.rbegin(); }
  auto rend() { return arguments.rend(); }

private:
  auto construct() -> void;

  string programArgument;
  vector<string> arguments;
};

inline auto Arguments::construct() -> void {
  if(!arguments) return;

  //extract and pre-process program argument
  programArgument = arguments.takeFirst();
  programArgument = {Path::real(programArgument), Location::file(programArgument)};

  //normalize path and file arguments
  for(auto& argument : arguments) {
    if(directory::exists(argument)) argument.transform("\\", "/").trimRight("/").append("/");
    else if(file::exists(argument)) argument.transform("\\", "/").trimRight("/");
  }
}

inline Arguments::Arguments(int argc, char** argv) {
  #if defined(PLATFORM_WINDOWS)
  utf8_arguments(argc, argv);
  #endif
  for(uint index : range(argc)) arguments.append(argv[index]);
  construct();
}

inline Arguments::Arguments(vector<string> arguments) {
  this->arguments = arguments;
  construct();
}

inline auto Arguments::programPath() const -> string {
  return Location::path(programArgument);
}

inline auto Arguments::programName() const -> string {
  return Location::file(programArgument);
}

inline auto Arguments::programLocation() const -> string {
  return programArgument;
}

inline auto Arguments::find(string_view name) const -> bool {
  for(uint index : range(arguments.size())) {
    if(arguments[index].match(name)) {
      return true;
    }
  }
  return false;
}

inline auto Arguments::find(string_view name, bool& argument) const -> bool {
  for(uint index : range(arguments.size())) {
    if(arguments[index].match(name) && arguments.size() >= index
    && (arguments[index + 1] == "true" || arguments[index + 1] == "false")) {
      argument = arguments[index + 1] == "true";
      return true;
    }
  }
  return false;
}

inline auto Arguments::find(string_view name, string& argument) const -> bool {
  for(uint index : range(arguments.size())) {
    if(arguments[index].match(name) && arguments.size() >= index) {
      argument = arguments[index + 1];
      return true;
    }
  }
  return false;
}

//

inline auto Arguments::take() -> string {
  if(!arguments) return {};
  return arguments.takeFirst();
}

inline auto Arguments::take(string_view name) -> bool {
  for(uint index : range(arguments.size())) {
    if(arguments[index].match(name)) {
      arguments.remove(index);
      return true;
    }
  }
  return false;
}

inline auto Arguments::take(string_view name, bool& argument) -> bool {
  for(uint index : range(arguments.size())) {
    if(arguments[index].match(name) && arguments.size() > index + 1
    && (arguments[index + 1] == "true" || arguments[index + 1] == "false")) {
      arguments.remove(index);
      argument = arguments.take(index) == "true";
      return true;
    }
  }
  return false;
}

inline auto Arguments::take(string_view name, string& argument) -> bool {
  for(uint index : range(arguments.size())) {
    if(arguments[index].match(name) && arguments.size() > index + 1) {
      arguments.remove(index);
      argument = arguments.take(index);
      return true;
    }
  }
  return false;
}

}
