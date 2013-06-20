/*  src/psp/psp-m68k.c: PSP M68k emulator interface (uses Q68)
    Copyright 2009-2010 Andrew Church

    This file is part of Yabause.

    Yabause is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation; either version 2 of the License, or
    (at your option) any later version.

    Yabause is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with Yabause; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301  USA
*/

#include "../core.h"
#include "../error.h"
#include "../m68kcore.h"
#include "../memory.h"
#include "../scsp.h"

#include "../q68/q68.h"
#include "../q68/q68-const.h"  // for Q68_JIT_PAGE_BITS

#include "common.h"
#include "config.h"
#include "psp-m68k.h"
#include "sys.h"

#include "me.h"
#include "me-utility.h"

/*************************************************************************/
/************************* Interface definition **************************/
/*************************************************************************/

/* Interface function declarations (must come before interface definition) */

static int psp_m68k_init(void);
static void psp_m68k_deinit(void);
static void psp_m68k_reset(void);

static FASTCALL s32 psp_m68k_exec(s32 cycles);
static void psp_m68k_sync(void);

static u32 psp_m68k_get_dreg(u32 num);
static u32 psp_m68k_get_areg(u32 num);
static u32 psp_m68k_get_pc(void);
static u32 psp_m68k_get_sr(void);
static u32 psp_m68k_get_usp(void);
static u32 psp_m68k_get_ssp(void);

static void psp_m68k_set_dreg(u32 num, u32 val);
static void psp_m68k_set_areg(u32 num, u32 val);
static void psp_m68k_set_pc(u32 val);
static void psp_m68k_set_sr(u32 val);
static void psp_m68k_set_usp(u32 val);
static void psp_m68k_set_ssp(u32 val);

static FASTCALL void psp_m68k_set_irq(s32 level);
static FASTCALL void psp_m68k_write_notify(u32 address, u32 size);

static void psp_m68k_set_fetch(u32 low_addr, u32 high_addr, pointer fetch_addr);
static void psp_m68k_set_readb(M68K_READ *func);
static void psp_m68k_set_readw(M68K_READ *func);
static void psp_m68k_set_writeb(M68K_WRITE *func);
static void psp_m68k_set_writew(M68K_WRITE *func);

/*-----------------------------------------------------------------------*/

/* Module interface definition */

M68K_struct M68KPSP = {
    .id          = M68KCORE_PSP,
    .Name        = "PSP M68k Emulator Interface",

    .Init        = psp_m68k_init,
    .DeInit      = psp_m68k_deinit,
    .Reset       = psp_m68k_reset,

    .Exec        = psp_m68k_exec,
    .Sync        = psp_m68k_sync,

    .GetDReg     = psp_m68k_get_dreg,
    .GetAReg     = psp_m68k_get_areg,
    .GetPC       = psp_m68k_get_pc,
    .GetSR       = psp_m68k_get_sr,
    .GetUSP      = psp_m68k_get_usp,
    .GetMSP      = psp_m68k_get_ssp,

    .SetDReg     = psp_m68k_set_dreg,
    .SetAReg     = psp_m68k_set_areg,
    .SetPC       = psp_m68k_set_pc,
    .SetSR       = psp_m68k_set_sr,
    .SetUSP      = psp_m68k_set_usp,
    .SetMSP      = psp_m68k_set_ssp,

    .SetIRQ      = psp_m68k_set_irq,
    .WriteNotify = psp_m68k_write_notify,

    .SetFetch    = psp_m68k_set_fetch,
    .SetReadB    = psp_m68k_set_readb,
    .SetReadW    = psp_m68k_set_readw,
    .SetWriteB   = psp_m68k_set_writeb,
    .SetWriteW   = psp_m68k_set_writew,
};

/*************************************************************************/

/* Virtual processor state block */
static Q68State *q68_state;

/* JIT code arena block pointer (for free() on cleanup) */
void *jit_arena_base;

/*----------------------------------*/

/* Local function declarations */

static void flush_cache(void);

static void local_malloc_init(void *base, uint32_t size);
static void *local_malloc(size_t size);
static void *local_realloc(void *ptr, size_t size);
static void local_free(void *ptr);

/*************************************************************************/
/************************** Interface functions **************************/
/*************************************************************************/

/**
 * psp_m68k_init:  Initialize the virtual processpr.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Zero on success, negative on failure
 */
static int psp_m68k_init(void)
{
    /* Allocate a memory block for the Q68 memory pool; make sure it's
     * 64-byte aligned to avoid cache line collisions */
    const uint32_t jit_arena_size = 2 * 1024 * 1024;
    jit_arena_base = malloc(jit_arena_size + (64*2-1));
    if (!jit_arena_base) {
        DMSG("Failed to allocate memory arena for JIT code");
        q68_destroy(q68_state);
        q68_state = NULL;
        return -1;
    }
    local_malloc_init((void *)(((uintptr_t)jit_arena_base + 0x3F) & -0x40),
                      jit_arena_size);

    if (!(q68_state = q68_create_ex(local_malloc, local_realloc, local_free))) {
        DMSG("Failed to create Q68 state block");
        return -1;
    }
    q68_set_irq(q68_state, 0);
    q68_set_jit_flush_func(q68_state, flush_cache);

    return 0;
}

/*-----------------------------------------------------------------------*/

/**
 * psp_m68k_deinit:  Destroy the virtual processor.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
static void psp_m68k_deinit(void)
{
    free(jit_arena_base);
    jit_arena_base = NULL;
    q68_destroy(q68_state);
    q68_state = NULL;
}

/*-----------------------------------------------------------------------*/

/**
 * psp_m68k_reset:  Reset the virtual processor.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
static void psp_m68k_reset(void)
{
    q68_reset(q68_state);
}

/*************************************************************************/

/**
 * psp_m68k_exec:  Execute instructions for the given number of clock cycles.
 *
 * [Parameters]
 *     cycles: Number of clock cycles to execute
 * [Return value]
 *     Number of clock cycles actually executed
 */
static FASTCALL s32 psp_m68k_exec(s32 cycles)
{
    return q68_run(q68_state, cycles);
}

/*-----------------------------------------------------------------------*/

/**
 * psp_m68k_sync:  Wait for background execution to finish.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
static void psp_m68k_sync(void)
{
    /* No-op */
}

/*************************************************************************/

/**
 * psp_m68k_get_{dreg,areg,pc,sr,usp,ssp}:  Return the current value of
 * the specified register.
 *
 * [Parameters]
 *     num: Register number (psp_m68k_get_dreg(), psp_m68k_get_areg() only)
 * [Return value]
 *     None
 */

static u32 psp_m68k_get_dreg(u32 num)
{
    return q68_get_dreg(q68_state, num);
}

static u32 psp_m68k_get_areg(u32 num)
{
    return q68_get_areg(q68_state, num);
}

static u32 psp_m68k_get_pc(void)
{
    return q68_get_pc(q68_state);
}

static u32 psp_m68k_get_sr(void)
{
    return q68_get_sr(q68_state);
}

static u32 psp_m68k_get_usp(void)
{
    return q68_get_usp(q68_state);
}

static u32 psp_m68k_get_ssp(void)
{
    return q68_get_ssp(q68_state);
}

/*-----------------------------------------------------------------------*/

/**
 * psp_m68k_set_{dreg,areg,pc,sr,usp,ssp}:  Set the value of the specified
 * register.
 *
 * [Parameters]
 *     num: Register number (psp_m68k_set_dreg(), psp_m68k_set_areg() only)
 *     val: Value to set
 * [Return value]
 *     None
 */

static void psp_m68k_set_dreg(u32 num, u32 val)
{
    q68_set_dreg(q68_state, num, val);
}

static void psp_m68k_set_areg(u32 num, u32 val)
{
    q68_set_areg(q68_state, num, val);
}

static void psp_m68k_set_pc(u32 val)
{
    q68_set_pc(q68_state, val);
}

static void psp_m68k_set_sr(u32 val)
{
    q68_set_sr(q68_state, val);
}

static void psp_m68k_set_usp(u32 val)
{
    q68_set_usp(q68_state, val);
}

static void psp_m68k_set_ssp(u32 val)
{
    q68_set_ssp(q68_state, val);
}

/*************************************************************************/

/**
 * psp_m68k_set_irq:  Deliver an interrupt to the processor.
 *
 * [Parameters]
 *     level: Interrupt level (0-7)
 * [Return value]
 *     None
 */
static FASTCALL void psp_m68k_set_irq(s32 level)
{
    q68_set_irq(q68_state, level);
}

/*-----------------------------------------------------------------------*/

/**
 * psp_m68k_write_notify:  Inform the 68k emulator that the given address
 * range has been modified.
 *
 * [Parameters]
 *     address: 68000 address of modified data
 *        size: Size of modified data in bytes
 * [Return value]
 *     None
 */
static FASTCALL void psp_m68k_write_notify(u32 address, u32 size)
{
    /* NOTE: If the SCSP/M68k is running in a separate thread, and the
     * main thread calls this function with the result of freeing an
     * allocated JIT block at the same time the M68k tries to allocate
     * or free a JIT block in its thread, a crash is likely.  Hopefully
     * there are no actual games that try writing to M68k code space
     * while the M68k is running. */
    q68_touch_memory(q68_state, address, size);
}

/*************************************************************************/

/**
 * psp_m68k_set_fetch:  Set the instruction fetch pointer for a region of
 * memory.  Not used by Q68.
 *
 * [Parameters]
 *       low_addr: Low address of memory region to set
 *      high_addr: High address of memory region to set
 *     fetch_addr: Pointer to corresponding memory region (NULL to disable)
 * [Return value]
 *     None
 */
static void psp_m68k_set_fetch(u32 low_addr, u32 high_addr, pointer fetch_addr)
{
}

/*-----------------------------------------------------------------------*/

/**
 * psp_m68k_set_{readb,readw,writeb,writew}:  Set functions for reading or
 * writing bytes or words in memory.
 *
 * [Parameters]
 *     func: Function to set
 * [Return value]
 *     None
 */

static void psp_m68k_set_readb(M68K_READ *func)
{
    q68_set_readb_func(q68_state, (Q68ReadFunc *)func);
}

static void psp_m68k_set_readw(M68K_READ *func)
{
    q68_set_readw_func(q68_state, (Q68ReadFunc *)func);
}

static void psp_m68k_set_writeb(M68K_WRITE *func)
{
    q68_set_writeb_func(q68_state, (Q68WriteFunc *)func);
}

static void psp_m68k_set_writew(M68K_WRITE *func)
{
    q68_set_writew_func(q68_state, (Q68WriteFunc *)func);
}

/*************************************************************************/
/**************************** Local routines *****************************/
/*************************************************************************/

/**
 * flush_cache:  Flush the data and instruction caches of the current CPU.
 * Called by Q68 after a new block of code has been translated.
 */
static void flush_cache(void)
{
    if (meUtilityIsME()) {
        meUtilityDcacheWritebackInvalidateAll();
        meUtilityIcacheInvalidateAll();
    } else {
        sceKernelDcacheWritebackInvalidateAll();
        sceKernelIcacheInvalidateAll();
    }
}

/*************************************************************************/

/* For the moment, we use a very simplistic memory allocator to manage JIT
 * memory.  Since we only allocate or free during 68k code translation,
 * this should hopefully be good enough. */

/* Memory management structure prefixed to each block */
typedef struct BlockInfo_ BlockInfo;
struct BlockInfo_ {
    BlockInfo *next, *prev;  // Next and previous blocks sorted by address
    uint32_t size;           // Size of this block (excluding this structure)
    int free;                // Nonzero if this is a free block
    uint32_t pad[12];        // Pad to 1 cache line (64 bytes)
};

/* Base of memory region used for local_malloc() and friends */
static BlockInfo *local_malloc_base;

/*-----------------------------------------------------------------------*/

/**
 * local_malloc_init:  Initialize the memory arena used by local_malloc()
 * and friends.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
static void local_malloc_init(void *base, uint32_t size)
{
    local_malloc_base = base;
    local_malloc_base->next = NULL;
    local_malloc_base->prev = NULL;
    local_malloc_base->size = size - sizeof(BlockInfo);
    local_malloc_base->free = 1;
}

/*-----------------------------------------------------------------------*/

/**
 * local_malloc:  Allocate a block of local memory.
 *
 * [Parameters]
 *     size: Size of memory block to allocate
 * [Return value]
 *     Allocated memory block, or NULL on failure
 */
static void *local_malloc(size_t size)
{
    /* Round the size up to a multiple of sizeof(BlockInfo) for simplicity */
    size = (size + sizeof(BlockInfo)-1)
           / sizeof(BlockInfo) * sizeof(BlockInfo);

    /* Find a free block of sufficient size and return it, splitting the
     * block if appropriate */
    BlockInfo *block;
    for (block = local_malloc_base; block; block = block->next) {
        if (!block->free) {
            continue;
        }
        if (block->size >= size) {
            void *ptr = (void *)((uintptr_t)block + sizeof(BlockInfo));
            if (block->size > size) {
                BlockInfo *split_block = (BlockInfo *)((uintptr_t)ptr + size);
                split_block->next = block->next;
                if (split_block->next) {
                    split_block->next->prev = split_block;
                }
                split_block->prev = block;
                block->next = split_block;
                split_block->size = block->size - (size + sizeof(BlockInfo));
                split_block->free = 1;
            }
            block->size = size;
            block->free = 0;
            return ptr;
        }
    }

    return NULL;
}

/*-----------------------------------------------------------------------*/

/**
 * local_realloc:  Adjust the size of a block of local memory.
 *
 * [Parameters]
 *      ptr: Pointer to memory block to adjust
 *     size: New size of memory block
 * [Return value]
 *     Allocated memory block, or NULL on failure
 */
static void *local_realloc(void *ptr, size_t size)
{
    if (size == 0) {
        local_free(ptr);
        return NULL;
    }

    if (ptr == NULL) {
        return local_malloc(size);
    }

    BlockInfo *block = (BlockInfo *)((uintptr_t)ptr - sizeof(BlockInfo));
    const size_t oldsize = block->size;
    size = (size + sizeof(BlockInfo)-1)
           / sizeof(BlockInfo) * sizeof(BlockInfo);
    if (size < oldsize - sizeof(BlockInfo)) {
        /* Adjust the block size downward and split off the remaining space
         * into a new free block (or add it to the next block, if the next
         * block is a free block) */
        block->size = size;
        BlockInfo *newblock = (BlockInfo *)
            ((uintptr_t)block + sizeof(BlockInfo) + size);
        if (block->next && block->next->free) {
            newblock->next = block->next->next;
            newblock->prev = block;
            newblock->size = block->next->size;
            newblock->free = 1;
            if (newblock->next) {
                newblock->next->prev = newblock;
            }
            block->next = newblock;
            newblock->size += oldsize - size;
        } else {
            newblock->next = block->next;
            newblock->prev = block;
            if (block->next) {
                block->next->prev = newblock;
            }
            block->next = newblock;
            newblock->size = oldsize - size - sizeof(BlockInfo);
            newblock->free = 1;
        }
        return ptr;
    } else if (size <= sizeof(BlockInfo)) {
        /* No need to adjust anything; just return the same block */
        return ptr;
    } else if (block->next && block->next->free
               && (sizeof(BlockInfo) + block->next->size) >= size - oldsize) {
        /* Append the next block to this one, then resize downward with a
         * recursive call */
        block->size += sizeof(BlockInfo) + block->next->size;
        block->next = block->next->next;
        if (block->next) {
            block->next->prev = block;
        }
        return local_realloc(ptr, size);
    } else {
        /* No simple path, so alloc/copy/free */
        void *newptr = local_malloc(size);
        if (!newptr) {
            return NULL;
        }
        const size_t copysize = (oldsize < size) ? oldsize : size;
        memcpy(newptr, ptr, copysize);
        local_free(ptr);
        return newptr;
    }
}

/*-----------------------------------------------------------------------*/

/**
 * local_free:  Free a block of local memory.
 *
 * [Parameters]
 *     ptr: Pointer to memory block to free
 * [Return value]
 *     None
 */
static void local_free(void *ptr)
{
    if (ptr != NULL) {
        BlockInfo *block = (BlockInfo *)((uintptr_t)ptr - sizeof(BlockInfo));
        block->free = 1;
        if (block->prev && block->prev->free) {
            block->prev->next = block->next;
            if (block->next) {
                block->next->prev = block->prev;
            }
            block->prev->size += block->size + sizeof(BlockInfo);
            block = block->prev;
        }
        if (block->next && block->next->free) {
            block->size += block->next->size + sizeof(BlockInfo);
            block->next = block->next->next;
            if (block->next) {
                block->next->prev = block;
            }
        }
    }
}

/*************************************************************************/
/*************************************************************************/

/*
 * Local variables:
 *   c-file-style: "stroustrup"
 *   c-file-offsets: ((case-label . *) (statement-case-intro . *))
 *   indent-tabs-mode: nil
 * End:
 *
 * vim: expandtab shiftwidth=4:
 */
