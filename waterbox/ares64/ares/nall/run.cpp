#include <nall/run.hpp>
#include <nall/path.hpp>

#if defined(PLATFORM_WINDOWS)
  #include <shellapi.h>
#endif

namespace nall {

#if defined(PLATFORM_WINDOWS)

NALL_HEADER_INLINE auto execute(const string& name, vector<string> argl) -> execute_result_t {
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

NALL_HEADER_INLINE auto invoke(const string& name, vector<string> argl) -> void {
  for(auto& arg : argl) if(arg.find(" ")) arg = {"\"", arg, "\""};
  string arguments = argl.merge(" ");
  string directory = Path::program().replace("/", "\\");
  ShellExecute(nullptr, nullptr, utf16_t(name), utf16_t(arguments), utf16_t(directory), SW_SHOWNORMAL);
}

#endif

}
