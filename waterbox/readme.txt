This is the native side of the experimental "waterbox" project for bizhawk.
It consists of a modified musl libc, and build scripts to tie it all together.

How to use:

1. Get a full Bizhawk checkout.
	* This needs to be in an NTFS path which is then foreign mounted in WSL2
2. Get WSL2 + Ubuntu 20.4LTS
	* Other combinations may work.  Shrug.
3. Start running commands:

cd musl
./configure-for-waterbox
make
make install
cd ../emulibc
make
cd ../libco
make
cd ../libcxx
./do-everything.sh
cd ..

4. You are now ready to start building cores. Each supports `make` and `make install`, as well as `make debug` and `make install-debug` for local development.  From the root directory, the following should all be valid:

cd gpgx && make install
cd libsnes && make install
cd nyma && make -f faust.mak install
cd nyma && make -f ngp.mak install
cd nyma && make -f pce.mak install
cd pcfx && make install
cd picodrive && make install
cd sameboy && make install
cd snes9x && make install
cd ss && make install
cd uzem && make install
cd vb && make install

