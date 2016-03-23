#ifndef _EMULIBC_H
#define _EMULIBC_H

// mark an entry point or callback pointer
#define ECL_ENTRY __attribute__((ms_abi))
// mark a visible symbol
#define ECL_EXPORT __attribute__((visibility("default")))

#endif
