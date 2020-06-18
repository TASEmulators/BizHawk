#include <stdio.h>
#include <stdbool.h>
#include <string.h>

#ifndef TRUE
#define TRUE 1
#endif

/* prototypes for dummy log/alert functions below */
#include "dialog.h"
#include "log.h"

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

int ExceptionDebugMask = 0;
// extern const char* Log_SetExceptionDebugMask(const char *OptionsStr);
Uint64 LogTraceFlags = 0;
void Log_SetLevels(void)
{}

#include "statusbar.h"

void Statusbar_EnableHDLed(drive_led_t state)
{}
void Statusbar_SetFloppyLed(drive_index_t drive, drive_led_t state)
{}

#include "rs232.h"
#include "ioMem.h"
#include "m68000.h"
#define Dprintf(a)

void RS232_SCR_ReadByte(void)
{
	M68000_WaitState(4);
}
void RS232_SCR_WriteByte(void)
{
	M68000_WaitState(4);
	/*Dprintf(("RS232: Write to SCR: $%x\n", (int)IoMem[0xfffa27]));*/
}
void RS232_UCR_ReadByte(void)
{
	M68000_WaitState(4);
	Dprintf(("RS232: Read from UCR: $%x\n", (int)IoMem[0xfffa29]));
}
void RS232_UCR_WriteByte(void)
{
	M68000_WaitState(4);
	Dprintf(("RS232: Write to UCR: $%x\n", (int)IoMem[0xfffa29]));
	// RS232_HandleUCR(IoMem[0xfffa29]);
}
void RS232_RSR_ReadByte(void)
{
	M68000_WaitState(4);
	IoMem[0xfffa2b] &= ~0x80;       /* Buffer not full */
	Dprintf(("RS232: Read from RSR: $%x\n", (int)IoMem[0xfffa2b]));
}
void RS232_RSR_WriteByte(void)
{
	M68000_WaitState(4);
	Dprintf(("RS232: Write to RSR: $%x\n", (int)IoMem[0xfffa2b]));
}
void RS232_TSR_ReadByte(void)
{
	M68000_WaitState(4);
	IoMem[0xfffa2d] |= 0x80;        /* Buffer empty */
	Dprintf(("RS232: Read from TSR: $%x\n", (int)IoMem[0xfffa2d]));
}
void RS232_TSR_WriteByte(void)
{
	M68000_WaitState(4);
	Dprintf(("RS232: Write to TSR: $%x\n", (int)IoMem[0xfffa2d]));
}
void RS232_UDR_ReadByte(void)
{
	Uint8 InByte = 0;
	M68000_WaitState(4);
	// RS232_ReadBytes(&InByte, 1);
	IoMem[0xfffa2f] = InByte;
	Dprintf(("RS232: Read from UDR: $%x\n", (int)IoMem[0xfffa2f]));
}
void RS232_UDR_WriteByte(void)
{
	Uint8 OutByte;
	M68000_WaitState(4);
	OutByte = IoMem[0xfffa2f];
	// RS232_TransferBytesTo(&OutByte, 1);
	Dprintf(("RS232: Write to UDR: $%x\n", (int)IoMem[0xfffa2f]));
}
void RS232_Init(void)
{}
void RS232_UnInit(void)
{}
void RS232_SetBaudRateFromTimerD(void)
{}

#include "main.h"

bool bQuitProgram = false;
