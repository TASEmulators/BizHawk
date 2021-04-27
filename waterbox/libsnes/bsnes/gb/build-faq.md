# macOS Specific Issues
## Attempting to build the Cocoa frontend fails with NSInternalInconsistencyException

When building on macOS, the build system will make a native Cocoa app by default. In this case, the build system uses the Xcode `ibtool` command to build user interface files. If this command fails, you can fix this issue by starting Xcode and letting it install components. After this is done, you should be able to close Xcode and build successfully.

## Attempting to build the SDL frontend on macOS fails on linking

SameBoy on macOS expects you to have SDL2 installed via Brew, and not as a framework. Older versions expected it to be installed as a framework, but this is no longer the case.

# Windows Build Process

## Tools and Libraries Installation

For the various tools and libraries, follow the below guide to ensure easy, proper configuration for the build environment:

### SDL2

For [libSDL2](https://libsdl.org/download-2.0.php), download the Visual C++ Development Library pack. Place the extracted files within a known folder for later. Both the `\x86\` and `\include\` paths will be needed.  

The following examples will be referenced later: 

- `C:\SDL2\lib\x86\*`
- `C:\SDL2\include\*`

### rgbds

After downloading [rgbds](https://github.com/bentley/rgbds/releases/), ensure that it is added to the `%PATH%`. This may be done by adding it to the user's or SYSTEM's Environment Variables, or may be added to the command line at compilation time via `set path=%path%;C:\path\to\rgbds`.  

### GnuWin

Ensure that the `gnuwin32\bin\` directory is included in `%PATH%`. Like rgbds above, this may instead be manually included on the command line before installation: `set path=%path%;C:\path\to\gnuwin32\bin`. 

## Building

Within a command prompt in the project directory:

```
vcvars32
set lib=%lib%;C:\SDL2\lib\x86
set include=%include%;C:\SDL2\include
make
```
Please note that these directories (`C:\SDL2\*`) are the examples given within the "SDL Port" section above. Ensure that your `%PATH%` properly includes `rgbds` and `gnuwin32\bin`, and that the `lib` and `include` paths include the appropriate SDL2 directories.

## Common Errors

### Error -1073741819

If encountering an error that appears as follows:

``` make: *** [build/bin/BootROMs/dmg_boot.bin] Error -1073741819```

Simply run `make` again, and the process will continue. This appears to happen occasionally with `build/bin/BootROMs/dmg_boot.bin` and `build/bin/BootROMs/sgb2_boot.bin`. It does not affect the compiled output. This appears to be an issue with GnuWin.

### The system cannot find the file specified (`usr/bin/mkdir`)

If errors arise (i.e., particularly with the `CREATE_PROCESS('usr/bin/mkdir')` calls, also verify that Git for Windows has not been installed with full Linux support. If it has, remove `C:\Program Files\Git\usr\bin` from the SYSTEM %PATH% until after compilation. This happens because the Git for Windows version of `which` is used instead of the GnuWin one, and it returns a Unix-style path instead of a Windows one.
