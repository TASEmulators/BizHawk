#pragma once

//auto execute(const string& name, const string& args...) -> string;
//[[synchronous]]
//executes program, waits for completion, and returns data written to stdout

//auto invoke(const string& name, const string& args...) -> void;
//[[asynchronous]]
//if a program is specified, it is executed with the arguments provided
//if a file is specified, the file is opened using the program associated with said file type
//if a folder is specified, the folder is opened using the associated file explorer
//if a URL is specified, the default web browser is opened and pointed at the URL requested

#include <nall/intrinsics.hpp>
#include <nall/string.hpp>

namespace nall {

struct execute_result_t {
  explicit operator bool() const { return code == EXIT_SUCCESS; }

  int code = EXIT_FAILURE;
  string output;
  string error;
};

#if defined(PLATFORM_MACOS) || defined(PLATFORM_LINUX) || defined(PLATFORM_BSD)

template<typename... P> inline auto execute(const string& name, P&&... p) -> execute_result_t {
  int fdout[2];
  int fderr[2];
  if(pipe(fdout) == -1) return {};
  if(pipe(fderr) == -1) return {};

  pid_t pid = fork();
  if(pid == 0) {
    const char* argv[1 + sizeof...(p) + 1];
    const char** argp = argv;
    vector<string> argl(std::forward<P>(p)...);
    *argp++ = (const char*)name;
    for(auto& arg : argl) *argp++ = (const char*)arg;
    *argp++ = nullptr;

    dup2(fdout[1], STDOUT_FILENO);
    dup2(fderr[1], STDERR_FILENO);
    close(fdout[0]);
    close(fderr[0]);
    close(fdout[1]);
    close(fderr[1]);
    execvp(name, (char* const*)argv);
    //this is called only if execvp fails:
    //use _exit instead of exit, to avoid destroying key shared file descriptors
    _exit(EXIT_FAILURE);
  } else {
    close(fdout[1]);
    close(fderr[1]);

    char buffer[256];
    execute_result_t result;

    while(true) {
      auto size = read(fdout[0], buffer, sizeof(buffer));
      if(size <= 0) break;

      auto offset = result.output.size();
      result.output.resize(offset + size);
      memory::copy(result.output.get() + offset, buffer, size);
    }

    while(true) {
      auto size = read(fderr[0], buffer, sizeof(buffer));
      if(size <= 0) break;

      auto offset = result.error.size();
      result.error.resize(offset + size);
      memory::copy(result.error.get() + offset, buffer, size);
    }

    close(fdout[0]);
    close(fderr[0]);

    int status = 0;
    waitpid(pid, &status, 0);
    if(!WIFEXITED(status)) return {};
    result.code = WEXITSTATUS(status);
    return result;
  }
}

template<typename... P> inline auto invoke(const string& name, P&&... p) -> void {
  pid_t pid = fork();
  if(pid == 0) {
    const char* argv[1 + sizeof...(p) + 1];
    const char** argp = argv;
    vector<string> argl(std::forward<P>(p)...);
    *argp++ = (const char*)name;
    for(auto& arg : argl) *argp++ = (const char*)arg;
    *argp++ = nullptr;

    if(execvp(name, (char* const*)argv) < 0) {
      #if defined(PLATFORM_MACOS)
      execlp("open", "open", (const char*)name, nullptr);
      #else
      execlp("xdg-open", "xdg-open", (const char*)name, nullptr);
      #endif
    }
    exit(0);
  }
}

#elif defined(PLATFORM_WINDOWS)

auto execute(const string& name, vector<string> argl) -> execute_result_t;

template<typename... P> inline auto execute(const string& name, P&&... p) -> execute_result_t {
  vector<string> argl(name, std::forward<P>(p)...);
  return execute(name, std::move(argl));
}

auto invoke(const string& name, vector<string> argl) -> void;

template<typename... P> inline auto invoke(const string& name, P&&... p) -> void {
  vector<string> argl(std::forward<P>(p)...);
  invoke(name, std::move(argl));
}

#else

template<typename... P> inline auto execute(const string& name, P&&... p) -> string {
  return "";
}

template<typename... P> inline auto invoke(const string& name, P&&... p) -> void {
}

#endif

}

#if defined(NALL_HEADER_ONLY)
  #include <nall/run.cpp>
#endif
