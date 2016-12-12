The Linux port is hightly experimental and may or may not work (more likely not).

## Current status

The build ends without problem, but no client runs at the moment.

### EmuHawk.exe

    Initialization of Direct3d 9 Display Method failed; falling back to GDI+ ---> System.TypeInitializationException: The type initializer for '<Module>' threw an exception. ---> System.NotImplementedException: The method or operation is not implemented.

This is totally expected, DirectX is a Windows API and will likely never be supported by Mono. This only seems to be a warning though.

Maybe Mesa with d3d9 may do the trick here. I did not test and it will likely not be enough.

    System.BadImageFormatException: Corrupt .resources file. Unable to read resources from this file because of invalid header information. Try regenerating the .resources file. ---> System.IO.EndOfStreamException: Unable to read beyond the end of the stream.

This one is curious. There is either a resource incompatibility in Mono, or my Mono installation is broken. Anyway, this crashes the program.

### DiscoHawk.exe

Runs, outputs the main window, but have not been tested.

### MultiHawk.exe

    System.NullReferenceException: Object reference not set to an instance of an object at BizHawk.Client.MultiHawk.Mainform.ProgramRunLoop() [0x0000f]

A bug that needs to be tracked...


## Installation

If you want to try anyway, here is how.

### Dependencies

* Mono >= 4.0.0
* [binfmt_misc configured](http://www.mono-project.com/archived/guiderunning_mono_applications/#registering-exe-as-non-native-binaries-linux-only)

### Building

Execute at the root of the project:

    xbuild /property:Configuration=Release /property:SolutionDir=$(pwd)/ BizHawk.sln

### Running

Go to the `output` folder or the `output64` depending on your system type and execute one of the clients with `./<client>`

If you are unsure about the right folder to pick, run `uname -m`:
* if the output is x86, you are running a 32 bit system, use the `output` folder
* if the output is x86_64, you are running a 64 bit system, use the `output64` folder
