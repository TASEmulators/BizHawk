#pragma once

#include <signal.h>

namespace nall {

struct service {
  explicit operator bool() const;
  auto command(const string& name, const string& command) -> bool;
  auto receive() -> string;
  auto name() const -> string;
  auto stop() const -> bool;

private:
  shared_memory shared;
  string _name;
  bool _stop = false;
};

inline service::operator bool() const {
  return (bool)shared;
}

//returns true on new service process creation (false is not necessarily an error)
inline auto service::command(const string& name, const string& command) -> bool {
  if(!name) return false;
  if(!command) return print("[{0}] usage: {service} command\n"
    "commands:\n"
    "  status  : query whether service is running\n"
    "  start   : start service if it is not running\n"
    "  stop    : stop service if it is running\n"
    "  remove  : remove semaphore lock if service crashed\n"
    "  {value} : send custom command to service\n"
    "", string_format{name}), false;

  if(shared.open(name, 4096)) {
    if(command == "start") {
      print("[{0}] already started\n", string_format{name});
    } else if(command == "status") {
      print("[{0}] running\n", string_format{name});
    }
    if(auto data = shared.acquire()) {
      if(command == "stop") print("[{0}] stopped\n", string_format{name});
      memory::copy(data, 4096, command.data(), command.size());
      shared.release();
    }
    if(command == "remove") {
      shared.remove();
      print("[{0}] removed\n", string_format{name});
    }
    return false;
  }

  if(command == "start") {
    if(shared.create(name, 4096)) {
      print("[{0}] started\n", string_format{name});
      auto pid = fork();
      if(pid == 0) {
        signal(SIGHUP, SIG_IGN);
        signal(SIGPIPE, SIG_IGN);
        _name = name;
        return true;
      }
      shared.close();
    } else {
      print("[{0}] start failed ({1})\n", string_format{name, strerror(errno)});
    }
    return false;
  }

  if(command == "status") {
    print("[{0}] stopped\n", string_format{name});
    return false;
  }

  return false;
}

inline auto service::receive() -> string {
  string command;
  if(shared) {
    if(auto data = shared.acquire()) {
      if(*data) {
        command.resize(4095);
        memory::copy(command.get(), data, 4095);
        memory::fill(data, 4096);
      }
      shared.release();
      if(command == "remove") {
        _stop = true;
        return "";
      } else if(command == "start") {
        return "";
      } else if(command == "status") {
        return "";
      } else if(command == "stop") {
        _stop = true;
        shared.remove();
        return "";
      }
    }
  }
  return command;
}

inline auto service::name() const -> string {
  return _name;
}

inline auto service::stop() const -> bool {
  return _stop;
}

}
