/*
 Copyright 2013 Michael Pavone
 This file is part of BlastEm.
 BlastEm is free software distributed under the terms of the GNU General Public License version 3 or greater. See COPYING for full license text.
*/
#ifdef _WIN32
#define WINVER 0x501
#include <winsock2.h>
#include <ws2tcpip.h>

int gdb_sock;
#define GDB_IN_FD gdb_sock
#define GDB_OUT_FD gdb_sock
#define GDB_READ(fd, buf, bufsize) recv(fd, buf, bufsize, 0)
#define GDB_WRITE(fd, buf, bufsize) send(fd, buf, bufsize, 0)
#else
#define GDB_IN_FD STDIN_FILENO
#define GDB_OUT_FD STDOUT_FILENO
#define GDB_READ read
#define GDB_WRITE write
#include <unistd.h>
#endif

#include "gdb_remote.h"
#include "68kinst.h"
#include "debug.h"
#include "util.h"
#include <fcntl.h>
#include <stddef.h>
#include <stdlib.h>
#include <stdio.h>
#include <string.h>



#define INITIAL_BUFFER_SIZE (16*1024)

#ifdef DO_DEBUG_PRINT
#define dfprintf fprintf
#else
#define dfprintf
#endif

char * buf = NULL;
char * curbuf = NULL;
char * end = NULL;
size_t bufsize;
int cont = 0;
int expect_break_response=0;
uint32_t resume_pc;


static uint16_t branch_t;
static uint16_t branch_f;

static bp_def * breakpoints = NULL;
static uint32_t bp_index = 0;


void hex_32(uint32_t num, char * out)
{
	for (int32_t shift = 28; shift >= 0; shift -= 4)
	{
		uint8_t nibble = num >> shift & 0xF;
		*(out++) = nibble > 9 ? nibble - 0xA + 'A' : nibble + '0';
	}
}

void hex_16(uint16_t num, char * out)
{
	for (int16_t shift = 14; shift >= 0; shift -= 4)
	{
		uint8_t nibble = num >> shift & 0xF;
		*(out++) = nibble > 9 ? nibble - 0xA + 'A' : nibble + '0';
	}
}

void hex_8(uint8_t num, char * out)
{
	uint8_t nibble = num >> 4;
	*(out++) = nibble > 9 ? nibble - 0xA + 'A' : nibble + '0';
	nibble = num & 0xF;
	*out = nibble > 9 ? nibble - 0xA + 'A' : nibble + '0';
}

void gdb_calc_checksum(char * command, char *out)
{
	uint8_t checksum = 0;
	while (*command)
	{
		checksum += *(command++);
	}
	hex_8(checksum, out);
}

void write_or_die(int fd, const void *buf, size_t count)
{
	if (GDB_WRITE(fd, buf, count) < count) {
		fatal_error("Error writing to stdout\n");
	}
}

void gdb_send_command(char * command)
{
	char end[3];
	write_or_die(GDB_OUT_FD, "$", 1);
	write_or_die(GDB_OUT_FD, command, strlen(command));
	end[0] = '#';
	gdb_calc_checksum(command, end+1);
	write_or_die(GDB_OUT_FD, end, 3);
	dfprintf(stderr, "Sent $%s#%c%c\n", command, end[1], end[2]);
}

uint32_t calc_status(m68k_context * context)
{
	uint32_t status = context->status << 3;
	for (int i = 0; i < 5; i++)
	{
		status <<= 1;
		status |= context->flags[i];
	}
	return status;
}

void update_status(m68k_context * context, uint16_t value)
{
	context->status = value >> 8;
	for (int i = 4; i >= 0; i--)
	{
		context->flags[i] = value & 1;
		value >>= 1;
	}
}

static uint8_t m68k_read_byte(m68k_context *context, uint32_t address)
{
	//TODO: share this implementation with builtin debugger
	return read_byte(address, (void **)context->mem_pointers, &context->options->gen, context);
}

void m68k_write_byte(m68k_context * context, uint32_t address, uint8_t value)
{
	genesis_context *gen = context->system;
	//TODO: Use generated read/write functions so that memory map is properly respected
	uint16_t * word = get_native_pointer(address & 0xFFFFFFFE, (void **)context->mem_pointers, &context->options->gen);
	if (word) {
		if (address & 1) {
			*word = (*word & 0xFF00) | value;
		} else {
			*word = (*word & 0xFF) | value << 8;
		}
		//TODO: Deal with this more generally once m68k_handle_code_write can handle it
		if (address >= 0xE00000) {
			m68k_handle_code_write(address, context);
		}
		return;
	}
	if (address >= 0xA00000 && address < 0xA04000) {
		gen->zram[address & 0x1FFF] = value;
		genesis_context * gen = context->system;
#if !defined(NO_Z80) && !defined(NEW_CORE)
		z80_handle_code_write(address & 0x1FFF, gen->z80);
#endif
		return;
	} else {
		return;
	}
}

void gdb_run_command(m68k_context * context, uint32_t pc, char * command)
{
	char send_buf[512];
	dfprintf(stderr, "Received command %s\n", command);
	switch(*command)
	{

	case 'c':
		if (*(command+1) != 0) {
			//TODO: implement resuming at an arbitrary address
			goto not_impl;
		}
		cont = 1;
		expect_break_response = 1;
		break;
	case 's': {
		if (*(command+1) != 0) {
			//TODO: implement resuming at an arbitrary address
			goto not_impl;
		}
		m68kinst inst;
		genesis_context *gen = context->system;
		uint16_t * pc_ptr = get_native_pointer(pc, (void **)context->mem_pointers, &context->options->gen);
		if (!pc_ptr) {
			fatal_error("Entered gdb remote debugger stub at address %X\n", pc);
		}
		uint16_t * after_pc = m68k_decode(pc_ptr, &inst, pc & 0xFFFFFF);
		uint32_t after = pc + (after_pc-pc_ptr)*2;

		if (inst.op == M68K_RTS) {
			after = (read_dma_value(context->aregs[7]/2) << 16) | read_dma_value(context->aregs[7]/2 + 1);
		} else if (inst.op == M68K_RTE || inst.op == M68K_RTR) {
			after = (read_dma_value((context->aregs[7]+2)/2) << 16) | read_dma_value((context->aregs[7]+2)/2 + 1);
		} else if(m68k_is_branch(&inst)) {
			if (inst.op == M68K_BCC && inst.extra.cond != COND_TRUE) {
				branch_f = after;
				branch_t = m68k_branch_target(&inst, context->dregs, context->aregs) & 0xFFFFFF;
				insert_breakpoint(context, branch_t, gdb_debug_enter);
			} else if(inst.op == M68K_DBCC && inst.extra.cond != COND_FALSE) {
				branch_t = after;
				branch_f = m68k_branch_target(&inst, context->dregs, context->aregs) & 0xFFFFFF;
				insert_breakpoint(context, branch_f, gdb_debug_enter);
			} else {
				after = m68k_branch_target(&inst, context->dregs, context->aregs) & 0xFFFFFF;
			}
		}
		insert_breakpoint(context, after, gdb_debug_enter);

		cont = 1;
		expect_break_response = 1;
		break;
	}
	case 'H':
		if (command[1] == 'g' || command[1] == 'c') {;
			//no thread suport, just acknowledge
			gdb_send_command("OK");
		} else {
			goto not_impl;
		}
		break;
	case 'Z': {
		uint8_t type = command[1];
		if (type < '2') {
			uint32_t address = strtoul(command+3, NULL, 16);
			insert_breakpoint(context, address, gdb_debug_enter);
			bp_def *new_bp = malloc(sizeof(bp_def));
			new_bp->next = breakpoints;
			new_bp->address = address;
			new_bp->index = bp_index++;
			breakpoints = new_bp;
			gdb_send_command("OK");
		} else {
			//watchpoints are not currently supported
			gdb_send_command("");
		}
		break;
	}
	case 'z': {
		uint8_t type = command[1];
		if (type < '2') {
			uint32_t address = strtoul(command+3, NULL, 16);
			remove_breakpoint(context, address);
			bp_def **found = find_breakpoint(&breakpoints, address);
			if (*found)
			{
				bp_def * to_remove = *found;
				*found = to_remove->next;
				free(to_remove);
			}
			gdb_send_command("OK");
		} else {
			//watchpoints are not currently supported
			gdb_send_command("");
		}
		break;
	}
	case 'g': {
		char * cur = send_buf;
		for (int i = 0; i < 8; i++)
		{
			hex_32(context->dregs[i], cur);
			cur += 8;
		}
		for (int i = 0; i < 8; i++)
		{
			hex_32(context->aregs[i], cur);
			cur += 8;
		}
		hex_32(calc_status(context), cur);
		cur += 8;
		hex_32(pc, cur);
		cur += 8;
		*cur = 0;
		gdb_send_command(send_buf);
		break;
	}
	case 'm': {
		char * rest;
		uint32_t address = strtoul(command+1, &rest, 16);
		uint32_t size = strtoul(rest+1, NULL, 16);
		if (size > (sizeof(send_buf)-1)/2) {
			size = (sizeof(send_buf)-1)/2;
		}
		char *cur = send_buf;
		while (size)
		{
			hex_8(m68k_read_byte(context, address), cur);
			cur += 2;
			address++;
			size--;
		}
		*cur = 0;
		gdb_send_command(send_buf);
		break;
	}
	case 'M': {
		char * rest;
		uint32_t address = strtoul(command+1, &rest, 16);
		uint32_t size = strtoul(rest+1, &rest, 16);

		char *cur = rest+1;
		while (size)
		{
			char tmp[3];
			tmp[0] = *(cur++);
			tmp[1] = *(cur++);
			tmp[2] = 0;
			m68k_write_byte(context, address, strtoul(tmp, NULL, 16));
			address++;
			size--;
		}
		gdb_send_command("OK");
		break;
	}
	case 'X':
		//binary transfers aren't supported currently as I don't feel like dealing with the escaping
		gdb_send_command("");
		break;
	case 'p': {
		unsigned long reg = strtoul(command+1, NULL, 16);

		if (reg < 8) {
			hex_32(context->dregs[reg], send_buf);
		} else if (reg < 16) {
			hex_32(context->aregs[reg-8], send_buf);
		} else if (reg == 16) {
			hex_32(calc_status(context), send_buf);
		} else if (reg == 17) {
			hex_32(pc, send_buf);
		} else {
			send_buf[0] = 0;
		}
		send_buf[8] = 0;
		gdb_send_command(send_buf);
		break;
	}
	case 'P': {
		char *after = NULL;
		unsigned long reg = strtoul(command+1, &after, 16);
		uint32_t value = strtoul(after+1, NULL, 16);

		if (reg < 8) {
			context->dregs[reg] = value;
		} else if (reg < 16) {
			context->aregs[reg-8] = value;
		} else if (reg == 16) {
			update_status(context, value);
		} else {
			//supporting updates to PC is going to be a pain
			gdb_send_command("E01");
			break;
		}
		gdb_send_command("OK");
		break;
	}
	case 'q':
		if (!memcmp("Supported", command+1, strlen("Supported"))) {
			sprintf(send_buf, "PacketSize=%X", (int)bufsize);
			gdb_send_command(send_buf);
		} else if (!memcmp("Attached", command+1, strlen("Attached"))) {
			//not really meaningful for us, but saying we spawned a new process
			//is probably closest to the truth
			gdb_send_command("0");
		} else if (!memcmp("Offsets", command+1, strlen("Offsets"))) {
			//no relocations, so offsets are all 0
			gdb_send_command("Text=0;Data=0;Bss=0");
		} else if (!memcmp("Symbol", command+1, strlen("Symbol"))) {
			gdb_send_command("");
		} else if (!memcmp("TStatus", command+1, strlen("TStatus"))) {
			//TODO: actual tracepoint support
			gdb_send_command("T0;tnotrun:0");
		} else if (!memcmp("TfV", command+1, strlen("TfV")) || !memcmp("TfP", command+1, strlen("TfP"))) {
			//TODO: actual tracepoint support
			gdb_send_command("");
		} else if (command[1] == 'C') {
			//we only support a single thread currently, so send 1
			gdb_send_command("QC1");
		} else if (!strcmp("fThreadInfo", command + 1)) {
			//we only support a single thread currently, so send 1
			gdb_send_command("m1");
		} else if (!strcmp("sThreadInfo", command + 1)) {
			gdb_send_command("l");
		} else if (!memcmp("ThreadExtraInfo", command+1, strlen("ThreadExtraInfo"))) {
			gdb_send_command("");
		} else if (command[1] == 'P') {
			gdb_send_command("");
		} else {
			goto not_impl;
		}
		break;
	case 'v':
		if (!memcmp("Cont?", command+1, strlen("Cont?"))) {
			gdb_send_command("vCont;c;C;s;S");
		} else if (!strcmp("MustReplyEmpty", command + 1)) {
			gdb_send_command("");
		} else if (!memcmp("Cont;", command+1, strlen("Cont;"))) {
			switch (*(command + 1 + strlen("Cont;")))
			{
			case 'c':
			case 'C':
				//might be interesting to have continue with signal fire a
				//trap exception or something, but for no we'll treat it as
				//a normal continue
				cont = 1;
				expect_break_response = 1;
				break;
			case 's':
			case 'S': {
				m68kinst inst;
				genesis_context *gen = context->system;
				uint16_t * pc_ptr = get_native_pointer(pc, (void **)context->mem_pointers, &context->options->gen);
				if (!pc_ptr) {
					fatal_error("Entered gdb remote debugger stub at address %X\n", pc);
				}
				uint16_t * after_pc = m68k_decode(pc_ptr, &inst, pc & 0xFFFFFF);
				uint32_t after = pc + (after_pc-pc_ptr)*2;

				if (inst.op == M68K_RTS) {
					after = (read_dma_value(context->aregs[7]/2) << 16) | read_dma_value(context->aregs[7]/2 + 1);
				} else if (inst.op == M68K_RTE || inst.op == M68K_RTR) {
					after = (read_dma_value((context->aregs[7]+2)/2) << 16) | read_dma_value((context->aregs[7]+2)/2 + 1);
				} else if(m68k_is_branch(&inst)) {
					if (inst.op == M68K_BCC && inst.extra.cond != COND_TRUE) {
						branch_f = after;
						branch_t = m68k_branch_target(&inst, context->dregs, context->aregs) & 0xFFFFFF;
						insert_breakpoint(context, branch_t, gdb_debug_enter);
					} else if(inst.op == M68K_DBCC && inst.extra.cond != COND_FALSE) {
						branch_t = after;
						branch_f = m68k_branch_target(&inst, context->dregs, context->aregs) & 0xFFFFFF;
						insert_breakpoint(context, branch_f, gdb_debug_enter);
					} else {
						after = m68k_branch_target(&inst, context->dregs, context->aregs) & 0xFFFFFF;
					}
				}
				insert_breakpoint(context, after, gdb_debug_enter);

				cont = 1;
				expect_break_response = 1;
				break;
			}
			default:
				goto not_impl;
			}
		} else {
			goto not_impl;
		}
		break;
	case '?':
		gdb_send_command("S05");
		break;
	default:
		goto not_impl;

	}
	return;
not_impl:
	fatal_error("Command %s is not implemented, exiting...\n", command);
}

void  gdb_debug_enter(m68k_context * context, uint32_t pc)
{
	dfprintf(stderr, "Entered debugger at address %X\n", pc);
	if (expect_break_response) {
		gdb_send_command("S05");
		expect_break_response = 0;
	}
	if ((pc & 0xFFFFFF) == branch_t) {
		bp_def ** f_bp = find_breakpoint(&breakpoints, branch_f);
		if (!*f_bp) {
			remove_breakpoint(context, branch_f);
		}
		branch_t = branch_f = 0;
	} else if((pc & 0xFFFFFF) == branch_f) {
		bp_def ** t_bp = find_breakpoint(&breakpoints, branch_t);
		if (!*t_bp) {
			remove_breakpoint(context, branch_t);
		}
		branch_t = branch_f = 0;
	}
	//Check if this is a user set breakpoint, or just a temporary one
	bp_def ** this_bp = find_breakpoint(&breakpoints, pc & 0xFFFFFF);
	if (!*this_bp) {
		remove_breakpoint(context, pc & 0xFFFFFF);
	}
	resume_pc = pc;
	cont = 0;
	uint8_t partial = 0;
	while(!cont)
	{
		if (!curbuf) {
			int numread = GDB_READ(GDB_IN_FD, buf, bufsize);
			if (numread < 0) {
				fatal_error("Failed to read on GDB input file descriptor\n");
			}
			dfprintf(stderr, "read %d bytes\n", numread);
			curbuf = buf;
			end = buf + numread;
		} else if (partial) {
			if (curbuf != buf) {
				memmove(curbuf, buf, end-curbuf);
				end -= curbuf - buf;
			}
			int numread = GDB_READ(GDB_IN_FD, end, bufsize - (end-buf));
			end += numread;
			curbuf = buf;
		}
		for (; curbuf < end; curbuf++)
		{
			if (*curbuf == '$')
			{
				curbuf++;
				char * start = curbuf;
				while (curbuf < end && *curbuf != '#') {
					curbuf++;
				}
				if (*curbuf == '#') {
					//check to make sure we've received the checksum bytes
					if (end-curbuf >= 2) {
						//TODO: verify checksum
						//Null terminate payload
						*curbuf = 0;
						//send acknowledgement
						if (GDB_WRITE(GDB_OUT_FD, "+", 1) < 1) {
							fatal_error("Error writing to stdout\n");
						}
						gdb_run_command(context, pc, start);
						curbuf += 2;
					}
				} else {
					curbuf--;
					partial = 1;
					break;
				}
			} else {
				dfprintf(stderr, "Ignoring character %c\n", *curbuf);
			}
		}
		if (curbuf == end) {
			curbuf = NULL;
		}
	}
}

void gdb_remote_init(void)
{
	buf = malloc(INITIAL_BUFFER_SIZE);
	curbuf = NULL;
	bufsize = INITIAL_BUFFER_SIZE;
#ifdef _WIN32
	socket_init();

	struct addrinfo request, *result;
	memset(&request, 0, sizeof(request));
	request.ai_family = AF_INET;
	request.ai_socktype = SOCK_STREAM;
	request.ai_flags = AI_PASSIVE;
	getaddrinfo("localhost", "1234", &request, &result);

	int listen_sock = socket(result->ai_family, result->ai_socktype, result->ai_protocol);
	if (listen_sock < 0) {
		fatal_error("Failed to open GDB remote debugging socket");
	}
	if (bind(listen_sock, result->ai_addr, result->ai_addrlen) < 0) {
		fatal_error("Failed to bind GDB remote debugging socket");
	}
	freeaddrinfo(result);
	if (listen(listen_sock, 1) < 0) {
		fatal_error("Failed to listen on GDB remote debugging socket");
	}
	gdb_sock = accept(listen_sock, NULL, NULL);
	if (gdb_sock < 0) {
		fatal_error("accept returned an error while listening on GDB remote debugging socket");
	}
	socket_close(listen_sock);
#else
	disable_stdout_messages();
#endif
}
