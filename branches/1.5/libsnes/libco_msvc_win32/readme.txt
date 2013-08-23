* Why does this exist?

Because zeromus can't figure out how to successfully compile this code in mingw32.
My efforts are in trying-to-port-to-mingw-win32.c; I don't know why it isn't working.

* Why do we need this code?

Because libco needs to be a bit more properly win32 in order for it to get used from .net code.
.net throws exceptions in each thread when it needs to suspend them for GC. 
Those exceptions get garbled without more proper win32 stack frame setup, and the process terminates.
Additionally, you wont be able to debug very well from callbacks out of a coroutine into c# without this.
 * Note: you can't debug very well anyway due to mingw code having no debug symbols in the callstack.