#ifndef THREADDEP_SEM_H
#define THREADDEP_SEM_H

#include <stddef.h>
// #include <fs/thread.h>
#ifdef USE_SDL2
#include <SDL.h>
#endif

typedef uintptr_t uae_sem_t; // TODO

static inline int uae_sem_init(uae_sem_t *sem, int dummy, int init)
{
	// *sem = fs_semaphore_create(init);
	// return (*sem == 0);
	return 0;
}

static inline void uae_sem_destroy(uae_sem_t *sem)
{
	// if (*sem) {
	// 	fs_semaphore_destroy(*sem);
	// 	*sem = NULL;
	// }
}

static inline int uae_sem_post(uae_sem_t *sem)
{
	// return fs_semaphore_post(*sem);
	return 0;
}

static inline int uae_sem_wait(uae_sem_t *sem)
{
	// return fs_semaphore_wait(*sem);
	return 0;
}

static inline int uae_sem_trywait(uae_sem_t *sem)
{
	// return fs_semaphore_try_wait(*sem);
	return 0;
}

static inline int uae_sem_trywait_delay(uae_sem_t *sem, int millis)
{
	// int result = fs_semaphore_wait_timeout_ms(*sem, millis);
	// if (result == 0) {
	// 	return 0;
	// } else if (result == FS_SEMAPHORE_TIMEOUT) {
	// 	return -1;
	// }
	// return -3;
	return 0;
}

#endif // THREADDEP_SEM_H
