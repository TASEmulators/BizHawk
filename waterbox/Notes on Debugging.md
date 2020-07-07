# How to Debug Waterbox and Cores

Bring lots of tools, and lots of self loathing.

## Windows

### gdb

* Usually comes from mingw, or something
* Example script to attach to a running BizHawk instance:
	```
	#!/bin/bash
	PSLINE=$(eval "ps -W | grep EmuHawk")

	if [[ $PSLINE =~ [0-9]+[[:space:]]+[0-9]+[[:space:]]+[0-9]+[[:space:]]+([0-9]+) ]]; then
		gdb --pid=${BASH_REMATCH[1]}
	fi
	```
* Thinks we're in x86-32 mode because of the PE header on EmuHawk.exe.  We're not.
	You can put this in `~/.gdbinit` (or maybe add it to the startgdb script?)
	```
	set arch i386:x86-64
	```
* The waterbox files have DWARF information, which gdb can read.  If you build a waterbox core in debug mode,
	you even get full source level debugging.  With the new rust based waterboxhost, these symbol files should automatically
	be registered for you as waterboxhost hits a gdb hook.
	* The gdb hook is experimental, so be ready with `add-sym foobar.wbx` if needed.
* Has no way to understand first chance vs second chance exceptions.  Since lazystates was added, the cores now
	emit lots of benign SIGSEGVs as the waterboxhost discovers what memory space they use.  You can suppress these exceptions:
	```
	han SIGSEGV nos nopr
	```
	But if the real exception you're trying to break on is a SIGSEGV, this leaves you defenseless.
	You probably want to use the `no-dirty-detection` feature in waterboxhost to turn off these
	SIGSEGVs for some kinds of debugging.
* Also understands symbols for waterboxhost.dll, since that was actually built with MINGW.
* `b rust_panic` to examine rust unwinds before they explode your computer.
* Breakpoints on symbols in the wbx file just don't work a lot of the time.
	* This is the single worst part of modern waterbox debugging.  I have no idea what gdb is doing wrong.  It sees the wbx
		symbols and can print stack information and tell you what function you're in, and print globals, but `b some_emu_core_function`
		just doesn't get hit.  I think it might have something to do with how we map memory.  This worked in some previous
		waterbox editions, but I never got to the bottom.
	* Recompile cores with lots of `__asm__("int3")` in them?  Heh.

### windbg

* `!address`, `!vprot` are useful for examining VirtualQuery information.
* Can't read any symbols, which makes it mostly useless until you've narrowed things down heavily.
* Has more useful stack traces than gdb, as gdb usually can't see the stack outside DWARF land.
* Understands first and second chance exceptions.
	* `sxd av` will do exactly what we need for lazystate page mapping; break only when our handled decides not to handle it.
* Can give reasonable information on random WIN32 exceptions with `!analyze`

### OmniSharp

* Great visibility into C# exceptions, pretty worthless otherwise.

### General unwinding hazards

* Within the guest, DWARF unwinding information is available and libunwind is used.  Guest C++ programs can freely use exceptions,
	and Mednafen does so without issue.
	* Unwinding through any stack that is not guest content will likely explode.
* Within the host, rust unwinds usually make some SEH explosion that crashes the application.
	* None of them were meant to be recoverable anyway.
* C# exceptions that occur when there is guest code on the stack usually cause a complete termination.
	* So a callback to managed from within a waterbox core, etc.
	* I never figured this one out.  Guest execution is fully done on vanilla host stacks, and we should be doing nothing
		that would make this fail.  But it does.
	* Since the rust waterboxhost, this now will only happen from emulator core specific callbacks, since the rust waterboxhost
		does not call out to C# from within a syscall handler, ever.
* Any unwinding on a libco cothread will likely make me laugh.

## Linux

### gdb

* Only game in town here.
* Mono seems to use a lot of custom signals for... something.
	* This script will start gdb, ignore those signals, and start EmuHawk:
		```
		#!/bin/bash
		gdb -iex "han SIG35 nos" -iex "han SIG36 nos" --args mono ./EmuHawk.exe "$@"
		```
* Because you're actually in linux, beware function names: `b mmap` can mean the host libc's mmap, the rust implementation of the guest mmap,
	or the guest libc's mmap.
* Same general problem with intermittently functional guest breakpoints as Windows.  Heh.
* Same lazystate SIGSEGV problem (and same solutions) as Windows.
* Can see some mono symbols, which is sometimes useful.
* If you're looking for a pure core bug, this might be a better environment to test it on than Windows.  It depends.
