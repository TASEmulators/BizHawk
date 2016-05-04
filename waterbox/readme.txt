This is the experimental "waterbox" project for bizhawk.
Build has been tested only on a recent Debian, but many Linuxes will probably work.  amd64 is the only supported platform.

libc:  This is pdclib, with the jam makesystem butchered and replaced by a custom Makefile, and some things removed.
libm from musl is added.  sjlj from newlib is added.

gpgx:  This is more or less our current gpgx core.  Not much has been changed.


To build:

cd libc
make
cd ../gpgx
make

Copy gpgx.elf to Bizhawk's output64\dll folder.

Everything is still very much WIP.
Notes:
1. Remember ms-abi vs systemv!
2. gpgx codeblocks project isn't for building.
3. SJLJ might be busted.
4. VA_ARGS is probably busted.
5. STDIO isn't hooked up yet.
