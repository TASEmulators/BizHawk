This is the native side of the experimental "waterbox" project for bizhawk.
It consists of a modified musl libc, and build scripts to tie it all together.


1. Set up your platform.
	The prescribed possibilities are not exhaustive. Other platforms may work.
	Here are the supported alternatives:

	PREPARE A WIN10 WORKSTATION:
	Using the guidance at (https://docs.microsoft.com/en-us/windows/wsl/wsl2-kernel & https://docs.microsoft.com/en-us/windows/wsl/install-win10):
	1. Install WSL2 
	2. Install Ubuntu 22.04.2 LTS (https://apps.microsoft.com/store/detail/ubuntu-22042-lts/9PN20MSR04DW)
	3. Clone the bizhawk repository. You can use it through /mnt or /home if you really like
	4. The waterbox toolchain has a choice between gcc and clang. Currently, clang is the preferred compiler due to its generally superior performance all around.
	4a. (Clang) Install build tools: sudo apt-get update && sudo apt-get install make cmake clang lld llvm zstd
	4b. (GCC) Install build tools: sudo apt-get update && sudo apt-get install make cmake gcc-12 g++-12 llvm zstd
	5. Currently, GCC 12 is required when using a gcc environment (older gcc versions fail at building libcxx), this may change in the future.

	PREPARE A WIN10 VM:
	1. Make sure the VM has "yo dawg" virtualization enabled on the guest. For example in VMWare Workstation, "Virtualize Intel VT-X/EPT or AMD-V/RVI"
	2. Follow WIN10 Workstation preparation guide
	3. If you wish to clone bizhawk on your host system (slower but maybe more convenient), you can use a VMWare shared folder and: `sudo mount -t drvfs Z: /mnt/z -o rw,relatime,metadata,uid=1000,gid=1000` (WSL won't auto-mount the shared drive)

	PREPARE A LINUX WORKSTATION:
	1. Debian/Ubuntu based distros work fine with the WIN10 Workstation instructions (confirmed working with Debian 11). Other distros should be able provide the needed programs in their package managers. make, cmake, gcc/g++ or clang/clang++, ld or lld (lld is required for clang), gcc-ar/gcc-ranlib or llvm-ar/llvm-ranlib (which pair you choose doesn't matter), llvm-config, zstd.
	2. For older Debian/Ubuntu based distros, the package manager might not provide a recent enough compiler for the waterbox toolchain. In these cases, it is recommended to obtain clang and other llvm tools from llvm's apt repository: https://apt.llvm.org/

2. Clone bizhawk sources
	* Make sure git's core.autocrlf is set to false, as the alternatives cause git to modify the line endings in .sh-looking files which WILL break the build process, from the very first step.
	* This is NOT git's default. You will need to change it!! Go ahead and set it false globally permanently, since do you really want git modifying files?
	* Make sure you have initialized and updated the needed submodules in the waterbox directory, a listing of these is here:
		* waterbox/musl (required for the entire waterbox toolchain)
		* waterbox/ares64/ares/thirdparty/angrylion-rdp (required for ares64)
		* submodules/sameboy/libsameboy (required for new BSNES)
		* waterbox/mame-arcade/mame (required for MAME)
		* waterbox/melon/melonDS (required for melonDS)
		* waterbox/nyma/mednafen (required for all Nyma cores)
		* waterbox/snes9x (required for Snes9x)
	* waterbox/llvm-project is needed for the entire waterbox toolchain, but initializing and updating this is handled automatically by later scripts, so you do not need to do anything about this

3. Consider whether it is time to update your build environment (i.e. sudo apt-get upgrade). Build environment tools are generally best kept at the latest version, to ensure top performance for our users.

4. Build libraries.
	cd musl
	./wbox_configure.sh
	./wbox_build.sh
	cd ../emulibc
	make
	cd ../libco
	make
	cd ../libcxx
	./do-everything.sh
	cd ..

4a. At ./wbox_configure, you may need to specify the compiler used. By default, it will try to use clang without a version suffix. If this is not present, it falls on the user to specify a CC variable (e.g. `CC=gcc-12 ./wbox_configure`). This is the only stage which the build tool may need to be manually specified, all future steps will remember this specification and thus need no manual input.

4b. If errors happen in the libcxx part, it can be due to musl mismatching your current build environment. This happens when your build environment is updated; musl does not track its build dependencies correctly. do `make clean` on musl (and delete the non-checkedin directories just to be safe) and try again from the musl step.

5. Some additional preparation is required before all the cores can be built:
	cd nyma && ./build-and-install-zlib.sh

6. You are now ready to start building cores. Each supports `make` and `make install`, as well as `make debug` and `make install-debug` for local development.  From the root directory, the following should all be valid:
	cd ares64 && make install
	cd bsnescore && make install
	cd gpgx && make install
	cd libsnes && make install
	cd mame-arcade && make install
	cd melon && make install
	cd nyma && make -f faust.mak install
	cd nyma && make -f ngp.mak install
	cd nyma && make -f turbo.mak install
	cd nyma && make -f hyper.mak install
	cd nyma && make -f pcfx.mak install
	cd nyma && make -f ss.mak install
	cd nyma && make -f shock.mak install
	cd nyma && make -f vb.mak install
	cd picodrive && make install
	cd snes9x && make install
	cd tic80 && make install
	cd uzem && make install
	cd virtualjaguar && make install

Be aware MAME takes a very long while to build. Following suit, the provided make-all-cores.sh will only make MAME if INCLUDE_MAME is exported (e.g. `INCLUDE_MAME=1 ./make-all-cores.sh`).
