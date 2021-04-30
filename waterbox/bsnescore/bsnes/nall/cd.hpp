#pragma once

/* CD-ROM sector functions.
 *
 * Implemented:
 *   eight-to-fourteen modulation (encoding and decoding)
 *   sync header creation and verification
 *   error detection code creation and verification
 *   reed-solomon product-code creation and verification
 *   sector scrambling and descrambling (currently unverified)
 *
 * Unimplemented:
 *    reed-solomon product-code correction
 *    cross-interleave reed-solomon creation, verification, and correction
 *    CD-ROM XA mode 2 forms 1 & 2 support
 *    subcode insertion and removal
 *    subcode decoding from CUE files
 *    channel frame expansion and reduction
 */

#include <nall/galois-field.hpp>
#include <nall/matrix.hpp>
#include <nall/reed-solomon.hpp>

#include <nall/cd/crc16.hpp>
#include <nall/cd/efm.hpp>
#include <nall/cd/sync.hpp>
#include <nall/cd/edc.hpp>
#include <nall/cd/rspc.hpp>
#include <nall/cd/scrambler.hpp>
#include <nall/cd/session.hpp>
