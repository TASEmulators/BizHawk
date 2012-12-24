/*
win32 implementation of libco using actual threads.  much slower than other implementations,
but far from glacial.  may be useful for debuggers that don't understand libco cothreads.

compiles in mingw.  try:
gcc libco_win32threads.c -o libco.dll -Wall -shared -O0 -g
*/

#include <stdlib.h>
#include <assert.h> // asserts don't happen in co_switch(), so no real performance gain from turning them off
#define LIBCO_C
#include "libco.h"
#define WIN32_LEAN_AND_MEAN
#include <windows.h>

typedef struct
{
  HANDLE thread;
  HANDLE sem;
  int wantdie;
  void (*entrypoint)(void);
} coprivate;

static __thread coprivate *libco_priv = NULL;


/*
Return handle to current cothread. Always returns a valid handle, even when called from the main program thread. 
*/
cothread_t co_active(void)
{
  if (!libco_priv)
  {
    // thread started out as a real thread, so we must make a libco_priv for it
	libco_priv = malloc(sizeof (*libco_priv));
	assert(libco_priv);
	DWORD ret = DuplicateHandle
	(
	  GetCurrentProcess(),
	  GetCurrentThread(),
	  GetCurrentProcess(),
	  &libco_priv->thread,
	  0,
	  FALSE,
	  DUPLICATE_SAME_ACCESS
	);
	assert(ret);
	libco_priv->sem = CreateSemaphore(NULL, 0, 1, NULL);
	assert(libco_priv->sem);
    libco_priv->wantdie = 0;
	libco_priv->entrypoint = NULL;
  }
  return (cothread_t) libco_priv;
}

void waittorun(void)
{
  // i suppose it would be possible to switch off the main thread
  // without ever having made a context for it.  then it would never
  // be able to activate again.
  coprivate *this = (coprivate *) co_active();
  WaitForSingleObject(this->sem, INFINITE);
  if (this->wantdie)
  {
    CloseHandle(this->sem);
	ExitThread(0);
  }
}

DWORD WINAPI thread_entry(LPVOID lpParameter)
{
  libco_priv = (coprivate *) lpParameter;
  waittorun();
  libco_priv->entrypoint();
  assert(0); // returning from entry point not allowed
  return 0;
}

/*
Create new cothread.
Heapsize is the amount of memory allocated for the cothread stack, specified in bytes. This is unfortunately impossible to make fully portable. It is recommended to specify sizes using `n * sizeof(void*)'. It is better to err on the side of caution and allocate more memory than will be needed to ensure compatibility with other platforms, within reason. A typical heapsize for a 32-bit architecture is ~1MB.
When the new cothread is first called, program execution jumps to coentry. This function does not take any arguments, due to portability issues with passing function arguments. However, arguments can be simulated by the use of global variables, which can be set before the first call to each cothread.
coentry() must not return, and should end with an appropriate co_switch() statement. Behavior is undefined if entry point returns normally.
Library is responsible for allocating cothread stack memory, to free the user from needing to allocate special memory capable of being used as program stack memory on platforms where this is required.
User is always responsible for deleting cothreads with co_delete().
Return value of null (0) indicates cothread creation failed. 
*/
cothread_t co_create(unsigned int heapsize, void (*entry)(void))
{
  coprivate *new_thread = malloc(sizeof (*new_thread));
  assert(new_thread);
  new_thread->sem = CreateSemaphore(NULL, 0, 1, NULL);
  assert(new_thread->sem);
  new_thread->wantdie = 0;
  new_thread->entrypoint = entry;
  assert(new_thread->entrypoint);
  new_thread->thread = CreateThread
  (
    NULL,
	heapsize,
	thread_entry,
	(void *)new_thread,
	0, // runs immediately
	NULL
  );
  assert(new_thread->thread);
  return (cothread_t) new_thread;
}

/*
Delete specified cothread.
Null (0) or invalid cothread handle is not allowed.
Passing handle of active cothread to this function is not allowed.
Passing handle of primary cothread is not allowed. 
*/
void co_delete(cothread_t _thread)
{
  coprivate *thread = (coprivate *) _thread;
  assert(thread);
  assert(thread->entrypoint); // Passing handle of primary cothread is not allowed
  thread->wantdie = 1;
  ReleaseSemaphore(thread->sem, 1, NULL);
  WaitForSingleObject(thread->thread, INFINITE);
  CloseHandle(thread->thread);
  free(thread);
}

/*
Switch to specified cothread.
Null (0) or invalid cothread handle is not allowed.
Passing handle of active cothread to this function is not allowed. 
*/
void co_switch(cothread_t _thread)
{
  coprivate *thread = (coprivate *) _thread;
  ReleaseSemaphore(thread->sem, 1, NULL);
  waittorun();
}
