#define _GNU_SOURCE
#include <stdint.h>
#include <stdlib.h>
#include <unistd.h>
#include <sys/types.h>
#include <signal.h>
#include <sys/mman.h>
#include <stdio.h>
#include <errno.h>

#define MAX_TRIPS 64

typedef struct {
	uintptr_t start;
	uintptr_t length;
	uint8_t tripped[0];
} tripwire_t;

static tripwire_t* Trips[MAX_TRIPS];
static int HandlerInstalled;

static char altstack[SIGSTKSZ];

static struct sigaction sa_old;

static void SignalHandler(int sig, siginfo_t* info, void* ucontext)
{
	uintptr_t faultAddress = (uintptr_t)info->si_addr;
	for (int i = 0; i < MAX_TRIPS; i++)
	{
		if (Trips[i] && faultAddress >= Trips[i]->start && faultAddress < Trips[i]->start + Trips[i]->length)
		{
			uintptr_t page = (faultAddress - Trips[i]->start) >> 12;
			if (Trips[i]->tripped[page] & 1) // should change
			{
				if (mprotect((void*)(faultAddress & ~0xffful), 0x1000, PROT_READ | PROT_WRITE) != 0)
				{
					abort();
					while (1)
						;
				}

				Trips[i]->tripped[page] = 3; // did change
				return;
			}
			else
			{
				break;
			}
		}
	}

	if (sa_old.sa_flags & SA_SIGINFO)
		sa_old.sa_sigaction(sig, info, ucontext);
	else 
		sa_old.sa_handler(sig);
}

static int InstallHandler()
{
	stack_t ss;
	ss.ss_flags = 0;
	ss.ss_sp = altstack;
	ss.ss_size = sizeof(altstack);

	if (sigaltstack(&ss, NULL) != 0)
	{
		fprintf(stderr, "sigaltstack: %i\n", errno);
		return 0;
	}

	struct sigaction sa;
	sa.sa_sigaction = SignalHandler;
	sa.sa_flags = SA_ONSTACK | SA_SIGINFO;
	sigfillset(&sa.sa_mask);

	if (sigaction(SIGSEGV, &sa, &sa_old) != 0)
	{
		fprintf(stderr, "sigaction: %i\n", errno);
		return 0;
	}
	return 1;
}

uint8_t* AddTripGuard(uintptr_t start, uintptr_t length)
{
	if (!HandlerInstalled)
	{
		if (!InstallHandler())
			return NULL;
		HandlerInstalled = 1;
	}

	uintptr_t npage = length >> 12;
	for (int i = 0; i < MAX_TRIPS; i++)
	{
		if (!Trips[i])
		{
			Trips[i] = calloc(1, sizeof(*Trips[i]) + npage);
			if (!Trips[i])
				return NULL;
			Trips[i]->start = start;
			Trips[i]->length = length;
			return &Trips[i]->tripped[0];
		}
	}
	return NULL;
}

int64_t RemoveTripGuard(uintptr_t start, uintptr_t length)
{
	for (int i = 0; i < MAX_TRIPS; i++)
	{
		if (Trips[i] && Trips[i]->start == start && Trips[i]->length == length)
		{
			free(Trips[i]);
			Trips[i] = NULL;
			return 1;
		}
	}
	return 0;
}

uint8_t* ExamineTripGuard(uintptr_t start, uintptr_t length)
{
	for (int i = 0; i < MAX_TRIPS; i++)
	{
		if (Trips[i] && Trips[i]->start == start && Trips[i]->length == length)
			return &Trips[i]->tripped[0];
	}
	return NULL;
}


/*

#define _GNU_SOURCE
#include <stdio.h>
#include <stdlib.h>
#include <sys/mman.h>
#include <unistd.h>
#include <sys/types.h>
#include <errno.h>
#include <string.h>
#include <stdint.h>
#include <stdlib.h>
#include <unistd.h>
#include <sys/syscall.h>
#include <sys/types.h>
#include <linux/userfaultfd.h>
#include <pthread.h>
#include <sys/ioctl.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <fcntl.h>

			struct uffdio_writeprotect wp;
			wp.range.start = start;
			wp.range.len = length;
			wp.mode = 0;
			if (ioctl(fd, UFFDIO_WRITEPROTECT, &wp) == -1)
			{
				free(Trips[i]);
				Trips[i] = NULL;
				return NULL;
			}

static void v(const char* msg, int value)
{
	if (v < 0)
	{
		printf("ERROR: %s %i\n", msg, errno);
		exit(1);
	}
}

const uint64_t addr = 0x36f00000000ul;
const uint64_t size = 0x100000ul;

static void* threadproc(void* arg)
{
	int wpfd = (int)(long)arg;
	// should be able to register once for a large range,
	// then wp smaller ranges
	struct uffdio_register uffdio_register;
	uffdio_register.range.start = addr;
	uffdio_register.range.len = size;

	uffdio_register.mode = UFFDIO_REGISTER_MODE_WP;

	v("ioctl:UFFDIO_REGISTER", ioctl(wpfd, UFFDIO_REGISTER, &uffdio_register));
	v("uffdio_register.ioctls", (uffdio_register.ioctls & UFFD_API_RANGE_IOCTLS) == UFFD_API_RANGE_IOCTLS ? 0 : -1);

	// wp
	{
		struct uffdio_writeprotect wp;
		wp.range.start = addr;
		wp.range.len = size;
		wp.mode = UFFDIO_WRITEPROTECT_MODE_WP;
		v("ioctl:UFFDIO_WRITEPROTECT", ioctl(wpfd, UFFDIO_WRITEPROTECT, &wp));
	}

	while (1)
	{
		struct uffd_msg msg;
	
		int nb = read(wpfd, &msg, sizeof(msg));
		if (nb == -1)
		{
			if (errno == EAGAIN)
				continue;
			v("read", errno);
		}
		if (nb != sizeof(msg))
			v("sizeof(msg)", -1);
		if (msg.event & UFFD_EVENT_PAGEFAULT)
		{
			printf("==> Event is pagefault on %p flags 0x%llx write? 0x%llx wp? 0x%llx\n"
				, (void *)msg.arg.pagefault.address
				, msg.arg.pagefault.flags
				, msg.arg.pagefault.flags & UFFD_PAGEFAULT_FLAG_WRITE
				, msg.arg.pagefault.flags & UFFD_PAGEFAULT_FLAG_WP
				);
		}
		if (msg.arg.pagefault.flags & UFFD_PAGEFAULT_FLAG_WP)
		{
			//  send write unlock
			struct uffdio_writeprotect wp;
			wp.range.start = addr;
			wp.range.len = size;
			wp.mode = 0;
			printf("sending !UFFDIO_WRITEPROTECT event to userfaultfd\n");
			v("ioctl:UFFDIO_WRITEPROTECT", ioctl(wpfd, UFFDIO_WRITEPROTECT, &wp));
		}
	}
	return NULL;
}

int main(void)
{
	int fd = memfd_create("pewps", MFD_CLOEXEC);
	v("memfd_create", fd);
	printf("fd: %i\n", fd);
	v("ftruncate", ftruncate(fd, size));
	char* ptr = mmap((void*)addr, size, PROT_READ | PROT_WRITE | PROT_EXEC, MAP_SHARED | MAP_FIXED, fd, 0);
	if (ptr == MAP_FAILED || ptr != (void*)addr)
		v("mmap", (int)(long)ptr);
	v("mprotect", mprotect(ptr, size, PROT_READ | PROT_WRITE));

	int wpfd = syscall(SYS_userfaultfd, O_CLOEXEC);
	v("SYS_userfaultfd", wpfd);

	struct uffdio_api uffdio_api;
	uffdio_api.api = UFFD_API;
	uffdio_api.features = 0;
	v("ioctl:UFFDIO_API", ioctl(wpfd, UFFDIO_API, &uffdio_api));

	v("uffdio_api.api", uffdio_api.api == UFFD_API ? 0 : -1);


	pthread_t thr;
	v("pthread_create", pthread_create(&thr, NULL, threadproc, (void*)(long)wpfd));

	for (uint64_t a = addr; a < addr + size; a += 4096)
	{
		sleep(1);
		printf("Gonna read");
		if (*(uint64_t*)a == 77777)
		{
			printf("Lucky!");
		}
		sleep(1);
		printf("Gonna write");
		strcpy((void*)a, "string in mem area");
		printf("%s\n", (char*)a);
	}
}

*/
