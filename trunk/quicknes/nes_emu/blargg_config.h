
// Nes_Emu 0.7.0 user configuration file. Don't replace when updating library.

#ifndef BLARGG_CONFIG_H
#define BLARGG_CONFIG_H

// Uncomment to transparently decompress files using zlib
#define HAVE_ZLIB_H

// Uncomment to enable platform-specific (and possibly non-portable) optimizations.
#define BLARGG_NONPORTABLE 1

// Uncomment if automatic byte-order determination doesn't work
//#define BLARGG_BIG_ENDIAN 1

// Uncomment if you get errors in the bool section of blargg_common.h
//#define BLARGG_COMPILER_HAS_BOOL 1

// Uncomment to disable out-of-memory exceptions
//#include <memory>
//#define BLARGG_NEW new (std::nothrow)

#define DISABLE_AUTO_FILE 1

#define HAVE_STDINT_H

// Use standard config.h if present
#ifdef HAVE_CONFIG_H
	#include "config.h"
#endif

#endif

