#include "thread_name.hpp"

#ifdef __linux__
#include <pthread.h>
#endif

namespace Util
{
void set_current_thread_name(const char *name)
{
#ifdef __linux__
	pthread_setname_np(pthread_self(), name);
#else
	// TODO: Kinda messy.
	(void)name;
#endif
}
}