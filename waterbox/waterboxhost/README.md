# Waterboxhost

This is the native support code for Waterbox.  It's intended to be consumed as a shared library from the host environment
with a C api.  For most work with Waterbox cores, you don't need to get into this at all.

## API
The public api is mostly all in `src/cinterface.rs` and has basic documentation on it.  Bare minimum sequence of calls to
get going:

0. (Optional) In a release environment, turn off certain checks to speed things up
	`wbx_set_always_evict_blocks()`
1. Create an environment, and load the ELF into it
	`wbx_create_host()`
	`wbx_activate_host()`
2. Connect exports from the guest executable to your host system
	`wbx_get_proc_addr()`
3. Run the guest system's init, using function pointers it exposed through `wbx_get_proc_addr()`
4. Get ready to take savestates
	`wbx_seal()`
5. Run emulation, using frameadvance or other advance functions exposed by the guest through `wbx_get_proc_addr()`
6. Save and load states as needed
	`wbx_save_state()`
	`wbx_load_state()`
7. Tear down the environment when done with it.  (One shot processes that are about to exit can skip this; the OS will clean everything up)
	`wbx_deactivate_host()`
	`wbx_destroy_host()`

Some more advanced features:

* If you're keeping around multiple hosts that may compete for the same address space,
	use `wbx_activate_host()` and `wbx_deactivate_host()` to switch between them.
* If you'd like to expose files to the virtual filesystem, see `wbx_mount_file()` and `wbx_unmount_file()`.
* If you need to call dynamically exposed functions that are not part of the static exports, see `wbx_get_callin_addr()`.
* If you'd like the guest code to be able to call callbacks that you pass to it, see `wbx_get_callback_addr()`.

## Building

Standard rust build infrastructure is used and can be installed with `rustup`.  At the moment, we're using the `nightly-x86_64-pc-windows-msvc`
chain on Windows, and the `nightly-x86_64-unknown-linux-gnu` chain on linux.  I don't know much about crosspiling, but presumably that will work.
The linux chain works fine in WSL, anyway.  When used in a Windows environment with the right default chain, `build-release.bat` will build
waterboxhost.dll and copy it to the right place.  When used in a Linux (or WSL) environment with the right default chain, `build-release.sh`
will build libwaterboxhost.so and copy it to the right place.
