/*  src/psp/satopt-sh2.h: Saturn-specific SH-2 optimization header
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

#ifndef PSP_SATOPT_SH2_H
#define PSP_SATOPT_SH2_H

/*************************************************************************/

/**
 * saturn_optimize_sh2:  Search for and return, if available, a native
 * implementation of the SH-2 routine starting at the given address.
 *
 * [Parameters]
 *        state: Processor state block pointer
 *      address: Address from which to translate
 *        fetch: Pointer corresponding to "address" from which opcodes can
 *                  be fetched
 *     func_ret: Pointer to variable to receive address of native function
 *                  implementing this routine if return value is nonzero
 *     for_fold: Nonzero if the callback is being called to look up a
 *                  subroutine for folding, zero if being called for a
 *                  full block translation
 * [Return value]
 *     Length of translated block in instructions (nonzero) if optimized
 *     code was generated, else zero
 */
extern unsigned int saturn_optimize_sh2(SH2State *state, uint32_t address,
                                        const uint16_t *fetch,
                                        SH2NativeFunctionPointer *func_ret,
                                        int for_fold);

/*************************************************************************/

#endif  // PSP_SATOPT_SH2_H

/*
 * Local variables:
 *   c-file-style: "stroustrup"
 *   c-file-offsets: ((case-label . *) (statement-case-intro . *))
 *   indent-tabs-mode: nil
 * End:
 *
 * vim: expandtab shiftwidth=4:
 */
