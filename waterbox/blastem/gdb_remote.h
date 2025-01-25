#ifndef GDB_REMOTE_H_
#define GDB_REMOTE_H_
#include "genesis.h"

void gdb_remote_init(void);
void gdb_debug_enter(m68k_context * context, uint32_t pc);

#endif //GDB_REMOTE_H_
