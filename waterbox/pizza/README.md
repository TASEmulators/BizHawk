# Emu-pizza
A new born Gameboy Classic/Color emulator....

Requirements
-----------
Emu-pizza requires libSDL2 to compile and run Space Invaders and Gameboy games. To install it

on an APT based distro:
```
sudo apt-get install libsdl2-dev
```

on a YUM based distro:
```
sudo yum install SDL2-devel
```

Compile
-------
```
make
```

Usage 
-----
```
emu-pizza [gameboy rom]
```

Gameboy keys
-------------------
* Arrows -- Arrows (rly?)
* Enter -- Start
* Space -- Select
* Z/X -- A/B buttons
* Q -- Exit

Supported ROMS
--------------
* Almost totality of Gameboy roms 

Todo
----
* Serial cable emulation 

Credits
-------

Thanks to [Emulator 101](http://www.emulator101.com), the source of all my current knowledge on 8080 emulation
