#include <sys/types.h>
#include <sys/stat.h>
#include <unistd.h>
#include <fcntl.h>
#include <stdlib.h>
#include <stdint.h>
#include <signal.h>
#include "util.h"
#include "terminal.h"

pid_t child;

void cleanup_terminal()
{
	kill(child, SIGKILL);
	unlink(INPUT_PATH);
	unlink(OUTPUT_PATH);
}

static char init_done;

void force_no_terminal()
{
	init_done = 1;
}

void init_terminal()
{
	if (!init_done) {
		if (!(isatty(STDIN_FILENO) && isatty(STDOUT_FILENO))) {
#ifndef __APPLE__
			//check to see if x-terminal-emulator exists, just use xterm if it doesn't
			char *term = system("which x-terminal-emulator > /dev/null") ? "xterm" : "x-terminal-emulator";
#endif
			//get rid of FIFO's if they already exist
			unlink(INPUT_PATH);
			unlink(OUTPUT_PATH);
			//create FIFOs for talking to helper process in terminal app
			mkfifo(INPUT_PATH, 0666);
			mkfifo(OUTPUT_PATH, 0666);

			//close existing file descriptors
			close(STDIN_FILENO);
			close(STDOUT_FILENO);
			close(STDERR_FILENO);

			child = fork();
			if (child == -1) {
				//error, oh well
				warning("Failed to fork for terminal spawn");
			} else if (!child) {
				//child process, exec our terminal emulator
#ifdef __APPLE__
				execlp("open", "open", "./termhelper", NULL);
#else
				execlp(term, term, "-title", "BlastEm Debugger", "-e", "./termhelper", NULL);
#endif
			} else {
				//connect to the FIFOs, these will block so order is important
				open(INPUT_PATH, O_RDONLY);
				open(OUTPUT_PATH, O_WRONLY);
				atexit(cleanup_terminal);
				if (-1 == dup(STDOUT_FILENO)) {
					fatal_error("failed to dup STDOUT to STDERR after terminal fork");
				}
			}
		}

		init_done = 1;
	}
}
