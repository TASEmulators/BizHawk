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
cd ../<insert your favourite core here>
make
make install
