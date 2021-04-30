#pragma once

namespace nall {

//launch a new process and inject specified DLL into it

auto launch(const char* applicationName, const char* libraryName, uint32 entryPoint) -> bool {
  //if a launcher does not send at least one message, a wait cursor will appear
  PostThreadMessage(GetCurrentThreadId(), WM_USER, 0, 0);
  MSG msg;
  GetMessage(&msg, 0, 0, 0);

  STARTUPINFOW si;
  PROCESS_INFORMATION pi;

  memset(&si, 0, sizeof(STARTUPINFOW));
  BOOL result = CreateProcessW(
    utf16_t(applicationName), GetCommandLineW(), NULL, NULL, TRUE,
    DEBUG_PROCESS | DEBUG_ONLY_THIS_PROCESS,  //do not break if application creates its own processes
    NULL, NULL, &si, &pi
  );
  if(result == false) return false;

  uint8 entryData[1024], entryHook[1024] = {
    0x68, 0x00, 0x00, 0x00, 0x00,  //push libraryName
    0xb8, 0x00, 0x00, 0x00, 0x00,  //mov eax,LoadLibraryW
    0xff, 0xd0,                    //call eax
    0xcd, 0x03,                    //int 3
  };

  entryHook[1] = (uint8)((entryPoint + 14) >>  0);
  entryHook[2] = (uint8)((entryPoint + 14) >>  8);
  entryHook[3] = (uint8)((entryPoint + 14) >> 16);
  entryHook[4] = (uint8)((entryPoint + 14) >> 24);

  auto pLoadLibraryW = (uint32)GetProcAddress(GetModuleHandleW(L"kernel32"), "LoadLibraryW");
  entryHook[6] = pLoadLibraryW >>  0;
  entryHook[7] = pLoadLibraryW >>  8;
  entryHook[8] = pLoadLibraryW >> 16;
  entryHook[9] = pLoadLibraryW >> 24;

  utf16_t buffer = utf16_t(libraryName);
  memcpy(entryHook + 14, buffer, 2 * wcslen(buffer) + 2);

  while(true) {
    DEBUG_EVENT event;
    WaitForDebugEvent(&event, INFINITE);

    if(event.dwDebugEventCode == EXIT_PROCESS_DEBUG_EVENT) break;

    if(event.dwDebugEventCode == EXCEPTION_DEBUG_EVENT) {
      if(event.u.Exception.ExceptionRecord.ExceptionCode == EXCEPTION_BREAKPOINT) {
        if(event.u.Exception.ExceptionRecord.ExceptionAddress == (void*)(entryPoint + 14 - 1)) {
          HANDLE hProcess = OpenProcess(0, FALSE, event.dwProcessId);
          HANDLE hThread = OpenThread(THREAD_ALL_ACCESS, FALSE, event.dwThreadId);

          CONTEXT context;
          context.ContextFlags = CONTEXT_FULL;
          GetThreadContext(hThread, &context);

          WriteProcessMemory(pi.hProcess, (void*)entryPoint, (void*)&entryData, sizeof entryData, NULL);
          context.Eip = entryPoint;
          SetThreadContext(hThread, &context);

          CloseHandle(hThread);
          CloseHandle(hProcess);
        }

        ContinueDebugEvent(event.dwProcessId, event.dwThreadId, DBG_CONTINUE);
        continue;
      }

      ContinueDebugEvent(event.dwProcessId, event.dwThreadId, DBG_EXCEPTION_NOT_HANDLED);
      continue;
    }

    if(event.dwDebugEventCode == CREATE_PROCESS_DEBUG_EVENT) {
      ReadProcessMemory(pi.hProcess, (void*)entryPoint, (void*)&entryData, sizeof entryData, NULL);
      WriteProcessMemory(pi.hProcess, (void*)entryPoint, (void*)&entryHook, sizeof entryHook, NULL);

      ContinueDebugEvent(event.dwProcessId, event.dwThreadId, DBG_CONTINUE);
      continue;
    }

    ContinueDebugEvent(event.dwProcessId, event.dwThreadId, DBG_CONTINUE);
  }

  return true;
}

}
