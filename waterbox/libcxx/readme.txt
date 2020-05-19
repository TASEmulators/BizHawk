Building and installing libcxx:

1. Clone llvm-project into $BIZHAWKGITROOT/../llvm-project
	* I used eaae6dfc545000e335e6f89abb9c78818383d7ad, which was the tip of origin/release/10.x at the time
2. Come to this folder
3. Execute some commands:

./configure-for-waterbox-phase-1
cd build1
make
make install
cd ..
./configure-for-waterbox-phase-2
cd build2
make
make install
cd ..
