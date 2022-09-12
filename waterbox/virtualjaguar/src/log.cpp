//
// Log handler
//
// Originally by David Raingeard (Cal2)
// GCC/SDL port by Niels Wagenaar (Linux/WIN32) and Caz (BeOS)
// Cleanups/new stuff by James Hammons
// (C) 2010 Underground Software
//
// JLH = James Hammons <jlhamm@acm.org>
//
// Who  When        What
// ---  ----------  -------------------------------------------------------------
// JLH  01/16/2010  Created this log ;-)
// JLH  07/11/2011  Instead of dumping out on max log file size being reached, we
//                  now just silently ignore any more output. 10 megs ought to be
//                  enough for anybody. ;-) Except when it isn't. :-P
//

#include "log.h"

#include <stdlib.h>
#include <stdarg.h>
#include <stdint.h>


//#define MAX_LOG_SIZE		10000000				// Maximum size of log file (10 MB)
#define MAX_LOG_SIZE		100000000				// Maximum size of log file (100 MB)

static FILE * log_stream = NULL;
static uint32_t logSize = 0;

int LogInit(const char * path)
{
	log_stream = fopen(path, "w");

	if (log_stream == NULL)
		return 0;

	return 1;
}

FILE * LogGet(void)
{
	return log_stream;
}

void LogDone(void)
{
	if (log_stream != NULL)
		fclose(log_stream);
}

//
// This logger is used mainly to ensure that text gets written to the log file
// even if the program crashes. The performance hit is acceptable in this case!
//
void WriteLog(const char * text, ...)
{
	va_list arg;
	va_start(arg, text);

	if (log_stream == NULL)
	{
		va_end(arg);
		return;
	}

	logSize += vfprintf(log_stream, text, arg);

	if (logSize > MAX_LOG_SIZE)
	{
		// Instead of dumping out, we just close the file and ignore any more output.
		fflush(log_stream);
		fclose(log_stream);
		log_stream = NULL;
	}

	va_end(arg);
	fflush(log_stream);					// Make sure that text is written!
}
