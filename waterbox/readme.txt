This is the native side of the experimental "waterbox" project for bizhawk.
It consists of a modified musl libc, and build scripts to tie it all together.


1. Set up your platform.
	The prescribed possibilities are not exhaustive. Other platforms may work.
	Here are the supported alternatives:

	PREPARE A WIN10 WORKSTATION:
	Using the guidance at (https://docs.microsoft.com/en-us/windows/wsl/wsl2-kernel & https://docs.microsoft.com/en-us/windows/wsl/install-win10):
	1. Install WSL2 
	2. Install Ubuntu 20.04 LTS (https://www.microsoft.com/en-us/p/ubuntu-2004-lts/9n6svws3rx71)
	3. Clone the bizhawk repository. You can use it through /mnt or /home if you really like 
	4. Install build tools: sudo apt-get update && sudo apt-get install gcc g++ make cmake

	PREPARE A WIN10 VM:
	1. Make sure the VM has virtualization enabled on the guest. For example in VMWare Workstation, "Virtualize Intel VT-X/EPT or AMD-V/RVI"
	2. Follow WIN10 Workstation preparation guide
	3. If you wish to clone bizhawk on your host system, you can use a VMWare shared folder and: `sudo mount -t drvfs Z: /mnt/z -o metadata` (WSL won't auto-mount the shared drive)
	3b. NO: it doesnt work. using `sudo nano /etc/fstab` you can add a line in this format: `Z: /mnt/z drvfs rw,relatime 0 0` (and then reboot your windows system) to get it to auto-mount

	PREPARE A LINUX WORKSTATION:
	1. TODO. This should work, but no one has tested it yet

2. Build libraries.
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

3. You are now ready to start building cores. Each supports `make` and `make install`, as well as `make debug` and `make install-debug` for local development.  From the root directory, the following should all be valid:
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
