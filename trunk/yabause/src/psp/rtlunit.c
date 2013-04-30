/*  src/psp/rtlunit.c: Basic unit processing for RTL
    Copyright 2009 Andrew Church

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

/*************************************************************************/

/*
 * This source file contains the logic used to divide a stream of RTL
 * instructions into basic units (so-called "basic blocks"--we use the
 * term "block" to refer to a sequence of source instructions translated
 * into RTL as a group, so the term "units" is used here to differentiate).
 * Each basic unit has exactly one entry point and one exit point, though
 * there may be multiple paths into or out of the unit.  As such, the unit
 * can be optimized and translated to native code without the necessity of
 * tracking every possible path for reaching each instruction.
 */

/*************************************************************************/
/*************************** Required headers ****************************/
/*************************************************************************/

#include "common.h"

#include "rtl.h"
#include "rtl-internal.h"

/*************************************************************************/
/********************** External interface routines **********************/
/*************************************************************************/

/**
 * rtlunit_add:  Add a new, empty basic unit to the given block
 * at the end of the block->units[] array.
 *
 * [Parameters]
 *     block: RTL block
 * [Return value]
 *     Nonzero on success, zero on failure
 */
int rtlunit_add(RTLBlock *block)
{
    PRECOND(block != NULL, return 0);

    if (UNLIKELY(block->num_units >= block->units_size)) {
        unsigned int new_units_size = block->num_units + UNITS_EXPAND_SIZE;
        RTLUnit *new_units = realloc(block->units,
                                     sizeof(*block->units) * new_units_size);
        if (UNLIKELY(!new_units)) {
            DMSG("No memory to expand block %p to %d units", block,
                 new_units_size);
            return 0;
        }
        block->units = new_units;
        block->units_size = new_units_size;
    }

    const unsigned int index = block->num_units++;
    if (index > 0) {
        block->units[index-1].next_unit = index;
    }
    block->units[index].first_insn = 0;
    block->units[index].last_insn = -1;
    block->units[index].next_unit = -1;
    block->units[index].prev_unit = index-1;
    unsigned int i;
    for (i = 0; i < lenof(block->units[index].entries); i++) {
        block->units[index].entries[i] = -1;
    }
    for (i = 0; i < lenof(block->units[index].exits); i++) {
        block->units[index].exits[i] = -1;
    }
    block->units[index].next_call_unit = -1;
    block->units[index].prev_call_unit = -1;

    return 1;
}

/*************************************************************************/

/**
 * rtlunit_add_edge:  Add a new edge between two basic units.
 *
 * [Parameters]
 *          block: RTL block
 *     from_index: Index of dominating basic unit (in block->units[])
 *       to_index: Index of postdominating basic unit (in block->units[])
 * [Return value]
 *     Nonzero on success, zero on failure
 */
int rtlunit_add_edge(RTLBlock *block, unsigned int from_index,
                     unsigned int to_index)
{
    PRECOND(block != NULL, return 0);
    PRECOND(block->units != NULL, return 0);
    PRECOND(from_index < block->num_units, return 0);
    PRECOND(to_index < block->num_units, return 0);

    unsigned int i;

    /* Find an empty exit slot; also check whether this edge already exists,
     * and return successfully (without adding a duplicate edge) if so.
     * A unit can never have more than two exits, so bail if we try to add
     * a third. */
    for (i = 0; i < lenof(block->units[from_index].exits); i++) {
        if (block->units[from_index].exits[i] < 0) {
            break;
        } else if (block->units[from_index].exits[i] == to_index) {
            return 1;
        }
    }
    if (UNLIKELY(i >= lenof(block->units[from_index].exits))) {
        DMSG("%p: Too many exits from unit %u", block, from_index);
    }
    block->units[from_index].exits[i] = to_index;

    /* If we overflow the entry list, add a dummy unit to take some of the
     * entries out */
    for (i = 0; i < lenof(block->units[to_index].entries); i++) {
        if (block->units[to_index].entries[i] < 0) {
            break;
        }
    }
    if (UNLIKELY(i >= lenof(block->units[to_index].entries))) {
        /* No room in the entry list, so we need to create a dummy unit */
        if (UNLIKELY(!rtlunit_add(block))) {
            DMSG("%p: Failed to add dummy unit", block);
            return 0;
        }
        const unsigned int dummy_unit = block->num_units - 1;
        /* Move all the current edges over to the dummy unit */
        for (i = 0; i < lenof(block->units[to_index].entries); i++) {
            const unsigned int other_unit = block->units[to_index].entries[i];
            unsigned int j;
            for (j = 0; j < lenof(block->units[other_unit].exits); j++) {
                if (block->units[other_unit].exits[j] == to_index) {
                    break;
                }
            }
            if (UNLIKELY(j >= lenof(block->units[other_unit].exits))) {
                DMSG("%p: Internal compiler error: edge to unit %u missing"
                     " from unit %u", block, to_index, other_unit);
                return 0;
            }
            block->units[other_unit].exits[j] = dummy_unit;
            block->units[dummy_unit].entries[i] = other_unit;
        }
        /* Link to the original unit */
        block->units[dummy_unit].exits[0] = to_index;
        block->units[dummy_unit].exits[1] = -1;
        block->units[to_index].entries[0] = dummy_unit;
        /* The new entry will go into the second slot; clear out all
         * other edges */
        for (i = lenof(block->units[to_index].entries) - 1; i > 1; i--) {
            block->units[to_index].entries[i] = -1;
        }
    }
    block->units[to_index].entries[i] = from_index;

    return 1;
}

/*************************************************************************/

/**
 * rtlunit_remove_edge:  Remove an edge between two basic units.
 *
 * [Parameters]
 *          block: RTL block
 *     from_index: Index of dominating basic unit (in block->units[])
 *     exit_index: Index of exit edge to remove (in units[from_index].exits[])
 * [Return value]
 *     None
 */
void rtlunit_remove_edge(RTLBlock *block, const unsigned int from_index,
                         unsigned int exit_index)
{
    PRECOND(block != NULL, return);
    PRECOND(block->units != NULL, return);
    PRECOND(from_index < block->num_units, return);
    PRECOND(exit_index < lenof(block->units[from_index].exits), return);
    PRECOND(block->units[from_index].exits[exit_index] >= 0, return);

    RTLUnit * const from_unit = &block->units[from_index];
    const unsigned int to_index = from_unit->exits[exit_index];
    RTLUnit * const to_unit = &block->units[to_index];
    unsigned int entry_index;

    for (; exit_index < lenof(from_unit->exits) - 1; exit_index++) {
        from_unit->exits[exit_index] = from_unit->exits[exit_index + 1];
    }
    from_unit->exits[lenof(from_unit->exits) - 1] = -1;

    for (entry_index = 0; entry_index < lenof(to_unit->entries);
         entry_index++
    ) {
        if (to_unit->entries[entry_index] == from_index) {
            break;
        }
    }
    if (UNLIKELY(entry_index >= lenof(to_unit->entries))) {
        DMSG("BUG: edge %u->%u missing from %u.entries!",
             from_index, to_index, to_index);
        return;
    }

    for (; entry_index < lenof(to_unit->entries) - 1; entry_index++) {
        to_unit->entries[entry_index] = to_unit->entries[entry_index + 1];
    }
    to_unit->entries[lenof(to_unit->entries) - 1] = -1;
}

/*************************************************************************/

/**
 * rtlunit_dump_all:  Dump a list of all basic units in the block to
 * stderr.  Intended for debugging.
 *
 * [Parameters]
 *     block: RTL block
 *       tag: Tag to prepend to all lines, or NULL for none
 * [Return value]
 *     None
 */
void rtlunit_dump_all(const RTLBlock * const block, const char * const tag)
{
    PRECOND(block != NULL, return);
    PRECOND(block->units != NULL, return);

    unsigned int i;
    for (i = 0; i < block->num_units; i++) {
        const RTLUnit * const unit = &block->units[i];
        fprintf(stderr, "[RTL] %s%s%sUnit %4u: ",
                tag ? "[" : "", tag ? tag : "", tag ? "] " : "", i);
        if (unit->entries[0] < 0) {
            fprintf(stderr, "<none>");
        } else {
            unsigned int j;
            for (j = 0; j < lenof(unit->entries) && unit->entries[j]>=0; j++) {
                fprintf(stderr, "%s%u", j==0 ? "" : ",", unit->entries[j]);
            }
        }
        if (unit->first_insn <= unit->last_insn) {
            fprintf(stderr, " --> [%d,%d] --> ",
                    unit->first_insn, unit->last_insn);
        } else {
            fprintf(stderr, " --> [empty] --> ");
        }
        if (unit->exits[0] < 0) {
            fprintf(stderr, "<none>");
        } else {
            unsigned int j;
            for (j = 0; j < lenof(unit->exits) && unit->exits[j] >= 0; j++) {
                fprintf(stderr, "%s%u", j==0 ? "" : ",", unit->exits[j]);
            }
        }
        fprintf(stderr, "\n");
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
