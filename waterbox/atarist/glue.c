#include <stdio.h>
#include <stdbool.h>
#include <string.h>

#ifndef TRUE
#define TRUE 1
#endif

/* prototypes for dummy log/alert functions below */
#include "hatari/src/includes/dialog.h"
#include "hatari/src/debug/log.h"

/**
 * Print suitable output prefix based on log level
 */
static void print_prefix(LOGTYPE nType)
{
	const char *sType;
	switch (nType) {
	case LOG_FATAL:
	case LOG_ERROR:
		sType = "ERROR: ";
		break;
	case LOG_WARN:
		sType = "WARNING: ";
		break;
	default:
		return;
	}
	fputs(sType, stdout);
}

/* output newline if it's missing from text */
static void do_newline(const char *text)
{
	if (text[strlen(text)-1] != '\n')
		fputs("\n", stdout);
}

/**
 * Output Hatari log string.
 */
void Log_Printf(LOGTYPE nType, const char *psFormat, ...)
{
	va_list argptr;
	print_prefix(nType);
	va_start(argptr, psFormat);
	vfprintf(stdout, psFormat, argptr);
	va_end(argptr);
}

/**
 * Output Hatari Alert dialog string.
 */
void Log_AlertDlg(LOGTYPE nType, const char *psFormat, ...)
{
	va_list argptr;
	print_prefix(nType);
	va_start(argptr, psFormat);
	vfprintf(stdout, psFormat, argptr);
	va_end(argptr);
	do_newline(psFormat);
}

/**
 * Output Hatari Query dialog string.
 */
int DlgAlert_Query(const char *text)
{
	puts(text);
	do_newline(text);
	return TRUE;
}
