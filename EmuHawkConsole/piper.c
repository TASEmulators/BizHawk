// why tf don't shells have this capability built in?

#include <windows.h>
#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#include <time.h>

#include <io.h> // _open_osfhandle

typedef struct
{
	FILE* fin;
	FILE* fout;
} threadparam_t;

DWORD WINAPI threadproc(LPVOID lpParam)
{
	threadparam_t* p = (threadparam_t*)lpParam;
	char buff[1];
	while (fwrite(buff, 1, fread(buff, 1, 1, p->fin), p->fout) == 1)
		;
	return 1;
}

int main(int argc, char** argv)
{
	int argslen = 2;
	for (int i = 1; argv[i]; i++)
	{
		argslen += strlen(argv[i]) + 2;
	}
	char* args = calloc(argslen, 1);
	if (!args)
		return 1;
	char* pp = args;
	for (int i = 1; argv[i]; i++)
	{
		strcpy(pp, argv[i]);
		pp += strlen(argv[i]);
		strcpy(pp, " ");
		pp++;
	}

	FILE* fin = NULL;
	FILE* fout = NULL;
	FILE* ferr = NULL;
	HANDLE child_hin = INVALID_HANDLE_VALUE;
	HANDLE child_hout = INVALID_HANDLE_VALUE;
	HANDLE child_herr = INVALID_HANDLE_VALUE;
	HANDLE parent_hin = INVALID_HANDLE_VALUE;
	HANDLE parent_hout = INVALID_HANDLE_VALUE;
	HANDLE parent_herr = INVALID_HANDLE_VALUE;

	HANDLE thread_hout = INVALID_HANDLE_VALUE;
	HANDLE thread_herr = INVALID_HANDLE_VALUE;
	HANDLE thread_hin = INVALID_HANDLE_VALUE;

	PROCESS_INFORMATION piProcInfo;
	STARTUPINFO siStartInfo;
	SECURITY_ATTRIBUTES sa;

	sa.nLength = sizeof(sa);
	sa.bInheritHandle = 1;
	sa.lpSecurityDescriptor = NULL;
	if (!CreatePipe(&child_hin, &parent_hin, &sa, 0))
		return 1;
	if (!CreatePipe(&parent_hout, &child_hout, &sa, 0))
		return 1;
	if (!CreatePipe(&parent_herr, &child_herr, &sa, 0))
		return 1;

	if (!SetHandleInformation(parent_hin, HANDLE_FLAG_INHERIT, 0))
		return 1;
	if (!SetHandleInformation(parent_hout, HANDLE_FLAG_INHERIT, 0))
		return 1;
	if (!SetHandleInformation(parent_herr, HANDLE_FLAG_INHERIT, 0))
		return 1;

	ZeroMemory (&siStartInfo, sizeof (STARTUPINFO));
	siStartInfo.cb = sizeof (STARTUPINFO);
	siStartInfo.hStdInput = child_hin;
	siStartInfo.hStdOutput = child_hout;
	siStartInfo.hStdError = child_herr;
	siStartInfo.dwFlags = STARTF_USESTDHANDLES;

	if (!CreateProcess("EmuHawk.exe", // application name
		(LPTSTR)args, // command line
		NULL, // process security attributes
		NULL, // primary thread security attributes
		TRUE, // handles are inherited
		DETACHED_PROCESS, // creation flags
		NULL, // use parent's environment
		NULL, // use parent's current directory
		&siStartInfo, // STARTUPINFO pointer
		&piProcInfo)) // receives PROCESS_INFORMATION
	{
		return 1;
	}

	if (NULL == (fin = _fdopen(_open_osfhandle((intptr_t)parent_hin, 0), "wb")))
		return 1;
	if (NULL == (fout = _fdopen(_open_osfhandle((intptr_t)parent_hout, 0), "rb")))
		return 1;
	if (NULL == (ferr = _fdopen(_open_osfhandle((intptr_t)parent_herr, 0), "rb")))
		return 1;
	// after fdopen(osf()), we don't need to keep track of parent handles anymore
	// fclose on the FILE struct will automatically free them

	// spawn child information
	threadparam_t pin, pout, perr;
	pin.fin = stdin;
	pin.fout = fin;
	pout.fin = fout;
	pout.fout = stdout;
	perr.fin = ferr;
	perr.fout = stderr;

	thread_hin = CreateThread(NULL, 0, threadproc, &pin, 0, NULL);
	if (!thread_hin)
		return 1;
	thread_hout = CreateThread(NULL, 0, threadproc, &pout, 0, NULL);
	if (!thread_hout)
		return 1;
	thread_herr = CreateThread(NULL, 0, threadproc, &perr, 0, NULL);
	if (!thread_herr)
		return 1;

	CloseHandle(child_hin);
	CloseHandle(child_hout);
	CloseHandle(child_herr);

	while(WaitForSingleObject(piProcInfo.hProcess, INFINITE))
		;
	abort();
	// fclose(stdin);
	// Sleep(500);
	// fclose(fout);
	// fclose(ferr);
	// Sleep(500);
	// exit(0);
}
