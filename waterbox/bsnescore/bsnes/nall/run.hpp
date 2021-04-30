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
    vector<string> argl(forward<P>(p)...);
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
    vector<string> argl(forward<P>(p)...);
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

template<typename... P> inline auto execute(const string& name, P&&... p) -> execute_result_t {
  vector<string> argl(name, forward<P>(p)...);
  for(auto& arg : argl) if(arg.find(" ")) arg = {"\"", arg, "\""};
  string arguments = argl.merge(" ");

  SECURITY_ATTRIBUTES sa;
  ZeroMemory(&sa, sizeof(SECURITY_ATTRIBUTES));
  sa.nLength = sizeof(SECURITY_ATTRIBUTES);
  sa.bInheritHandle = true;
  sa.lpSecurityDescriptor = nullptr;

  HANDLE stdoutRead;
  HANDLE stdoutWrite;
  if(!CreatePipe(&stdoutRead, &stdoutWrite, &sa, 0)) return {};
  if(!SetHandleInformation(stdoutRead, HANDLE_FLAG_INHERIT, 0)) return {};

  HANDLE stderrRead;
  HANDLE stderrWrite;
  if(!CreatePipe(&stderrRead, &stderrWrite, &sa, 0)) return {};
  if(!SetHandleInformation(stderrRead, HANDLE_FLAG_INHERIT, 0)) return {};

  HANDLE stdinRead;
  HANDLE stdinWrite;
  if(!CreatePipe(&stdinRead, &stdinWrite, &sa, 0)) return {};
  if(!SetHandleInformation(stdinWrite, HANDLE_FLAG_INHERIT, 0)) return {};

  STARTUPINFO si;
  ZeroMemory(&si, sizeof(STARTUPINFO));
  si.cb = sizeof(STARTUPINFO);
  si.hStdOutput = stdoutWrite;
  si.hStdError = stderrWrite;
  si.hStdInput = stdinRead;
  si.dwFlags = STARTF_USESTDHANDLES;

  PROCESS_INFORMATION pi;
  ZeroMemory(&pi, sizeof(PROCESS_INFORMATION));

  if(!CreateProcess(
    nullptr, utf16_t(arguments),
    nullptr, nullptr, true, CREATE_NO_WINDOW,
    nullptr, nullptr, &si, &pi
  )) return {};

  DWORD exitCode = EXIT_FAILURE;
  if(WaitForSingleObject(pi.hProcess, INFINITE)) return {};
  if(!GetExitCodeProcess(pi.hProcess, &exitCode)) return {};
  CloseHandle(pi.hThread);
  CloseHandle(pi.hProcess);

  char buffer[256];
  execute_result_t result;
  result.code = exitCode;

  while(true) {
    DWORD read, available, remaining;
    if(!PeekNamedPipe(stdoutRead, nullptr, sizeof(buffer), &read, &available, &remaining)) break;
    if(read == 0) break;

    if(!ReadFile(stdoutRead, buffer, sizeof(buffer), &read, nullptr)) break;
    if(read == 0) break;

    auto offset = result.output.size();
    result.output.resize(offset + read);
    memory::copy(result.output.get() + offset, buffer, read);
  }

  while(true) {
    DWORD read, available, remaining;
    if(!PeekNamedPipe(stderrRead, nullptr, sizeof(buffer), &read, &available, &remaining)) break;
    if(read == 0) break;

    if(!ReadFile(stderrRead, buffer, sizeof(buffer), &read, nullptr)) break;
    if(read == 0) break;

    auto offset = result.error.size();
    result.error.resize(offset + read);
    memory::copy(result.error.get() + offset, buffer, read);
  }

  return result;
}

template<typename... P> inline auto invoke(const string& name, P&&... p) -> void {
  vector<string> argl(forward<P>(p)...);
  for(auto& arg : argl) if(arg.find(" ")) arg = {"\"", arg, "\""};
  string arguments = argl.merge(" ");
  string directory = Path::program().replace("/", "\\");
  ShellExecute(nullptr, nullptr, utf16_t(name), utf16_t(arguments), utf16_t(directory), SW_SHOWNORMAL);
}

#else

template<typename... P> inline auto execute(const string& name, P&&... p) -> string {
  return "";
}

template<typename... P> inline auto invoke(const string& name, P&&... p) -> void {
}

#endif

}
