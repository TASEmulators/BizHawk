BlastEm 0.6.0
-------------

Installation
------------

Extract this archive to a directory of your choosing.

NOTE: Prior to version 0.4.1, BlastEm was still using Unixy locations for config
and save files. If you're upgrading from a previous version on Windows, you will
need to move them manually. For config files, the relevant paths are in the
previous paragraph. For save files, move all the directories found in 
%userprofile%\.local\share\blastem to %localappdata%\blastem

Usage
-----

This version of BlastEm has a GUI that allows access to most configuration options.
Simply start BlastEm without passing a ROM filename on the command line to access
the main menu. You can also access the menu by hitting the button mapped to the ui.exit
action (default Esc).

If Open GL is disabled or unavaible, or you explicitly request it, the old ROM-based UI
will be used instead. This UI does not support configuration so you will need to modify
the configuration file manually if you use it. See the rest of this README for instructions
on modifying the configuration file.

Some operations are currently only supported through the command line. To get a
list of supported command line options on Linux or OSX type:

    ./blastem -h
    
From within your BlastEm directory. On Windows type:
    
    blastem.exe -h
    
Lock-On Support
---------------

This version of BlastEm has some preliminary support for Sonic & Knuckles lock
on technology. This is available via both the menu and the command line. To use
it from the menu, first load Sonic & Knuckles normally. Enter the menu (mapped
to the Escape key by default) and select the "Lock On" option to select a ROM
to lock on. The system will then reload with the combined game. To use it from
the command line, specify the Sonic & Knuckles ROM as the primary ROM and
specify the ROM to be locked on using the -o option. As an example:

    ./blastem ~/romz/sonic_and_knuckles.bin -o ~/romz/sonic3.bin
    
Please note that Sonic 2 lock-on does not work at this time.

Configuration
-------------

Configuration is read from the file at $HOME/.config/blastem/blastem.cfg on
Unix-like systems and %localappdata%\blastem\blastem.cfg if it exists.
Othwerise it is read from default.cfg from the same directory as the BlastEm
executable. Sections are denoted by a section name followed by an open curly 
bracket, the section's contents and a closing curly bracket. Individual
configuration values are set by entering the value's name followed by a space
or tab and followed by the desired value.

Bindings
--------

The keys subsection of bindings maps keyboard keys to gamepad buttons or UI
actions. The key name goes on the left and the action is on the right.
Most keys are named for the character they produce when pressed. For keys that
don't correspond to a normal character, check the list below:

  Name       | Description
  -----------------
  up           Up arrow
  down         Down arrow
  left         Left arrow
  right        Right arrow
  space
  tab
  backspace    Backspace on PC keyboards, Delete on Mac keyboards
  esc
  delete
  lshift       Left shift
  rshift       Right shift
  lctrl        Left control
  rctrl        Right control
  lalt         Left alt on PC keyboards, Option on Mac keyboards
  ralt         Right alt on PC keyboards, Option on Mac keyboards
  home
  end
  pageup
  pagedown
  f1
  f2
  f3
  f4
  f5
  f6
  f7
  f8
  f9
  f10
  f11
  f12
  select
  play
  search
  back

The pads subsection is used to map gamepads and joysticks. Gamepads that are
recognized, can have their buttons and axes mapped with semantic names. 
Xbox 360, PS4 and PS3 style names are supported. Unrecognized gamepads can be 
mapped using numeric button and axis ids. The following button names are
recognized by BlastEm:
	a, cross
	b, circle
	x, square
	y, trinagle
	start, options
	back, select, share
	guide
	leftbutton, l1
	rightbutton, r1
	leftstick, l3
	rightstick, r3
The following axis names are recognized by BlastEm:
	leftx
	lefty
	rightx
	righty
	lefttrigger, l2
	righttrigger, r2
	

The mice subsection is used to map mice to emulated Mega/Sega mice. The default
configuration maps both the first and second host mice to the first emulated
mouse. This should not need modification for most users.

One special mapping deserves a mention. By default, the 'r' key is mapped to
ui.release_mouse. When operating in windowed mode the mouse has a capture
behavior. Mouse events are ignored until you click in the window. The mouse
will then be "captured" and the cursor will be both made invisible and locked
to the window. The ui.release_mouse binding releases the mouse so it can be
used normally.

UI Actions
----------

This section lists the various "UI" actions that can be triggered by a key or
gamepad binding.

ui.release_mouse             Releases the mouse if it is currently captured
ui.plane_debug               Toggles the VDP plane debug view
ui.vram_debug                Toggles the VDP VRAM debug view
ui.cram_debug                Toggles the VDP CRAM debug view
ui.compositing_debug         Toggles the VDP compositing debug view
ui.vdp_debug_mode            Cycles the mode/palette of the VDP debug view
                             that currently has focus
ui.enter_debugger            Enters the debugger for the main CPU of the
							 currently emulated system
ui.screenshot                Takes an internal screenshot
ui.exit                      Returns to the menu ROM if currently in a game
                             that was launched from the menu. Exits otherwise
ui.save_state                Saves a savestate to the quicksave slot
ui.set_speed.N               Selects a specific machine speed specified by N
                             which should be a number between 0-9. Speeds are
                             specified in the "clocks" section of the config				
ui.next_speed                Selects the next machine speed
ui.prev_speed                Selects the previous machine speed
ui.toggle_fullscreen         Toggles between fullscreen and windowed mode
ui.soft_reset                Resets a portion of the emulated machine
                             Equivalent to pushing the reset button on the
                             emulated device
ui.reload                    Reloads the current ROM from a file and performs
                             a hard reset of the emulated device
ui.sms_pause                 Triggers a press of the pause button when in SMS
                             mode
ui.toggle_keyboard_captured  Toggles the capture state of the host keyboard
                             when an emulated keyboard is present
		
IO
--

This section controls which peripherals are attached to the emulated console.
IO assignments can be overridden by the ROM database when appropriate. For
instance, games with mouse support can automatically use the mouse and games
that only support 3-button pads can automatically force an appropriate pad.
Unforunately, the ROM database is not yet exhaustive so manual configuration
may be needed here in some cases.

Video
-----

The video section contains settings that affect the visual output of BlastEm.

"aspect" is used to control the aspect ratio of the emulated display. The
default of 4:3 matches that of a standard definition television.

"width" is used to control the window width when not in fullscreen mode.

"height" is used to control the window height when not in fullscreen mode. If
left unspecified, it will be calculated from "width" and "aspect".

"vertex_shader" and "fragment_shader" define the GLSL shader program that
produces the final image for each frame. Shaders can be used to add various
visual effects or enhancements. Currently BlastEm only ships with the default
shader and a "subtle" crt shader. If you write your own shaders, place them in 
$HOME/.config/blastem/shaders and then specify the basename of the new shader
files in the "vertex_shader" and "fragment_shader" config options. Note that
shaders are not available in the SDL fallback renderer.

"scanlines" controls whether there is any emulation of the gaps between display
lines that are present when driving a CRT television with a 240p signal. This
emulation is very basic at the moment so this option is off by default.

"vsync" controls whether the drawing of frames is synchronized to the monitor
refresh rate. Valid values for this setting are "off", "on" and "tear". The
latter will attempt to use the "late tear" option if it's available and normal
vsync otherwise. Currently it's recommended to leave this at the default of
"off" as it may not work well with the default "audio" sync method and the
"video" sync method will automatically enable "vsync". See "Sync Source and
VSync" for more details.

"fullscreen" controls whether BlastEm starts in fullscreen or windowed mode.
This can be overridden on the command line with the -f flag. If fullscreen
is set to "off", -f will turn it on. Conversely, if fullscreen is set to "on"
in the config, -f will turn it off.

"gl" controls whether OpenGL is used for rendering. The default value is on.
If it is set to off instead, the fallback renderer which uses SDL2's render API
will be used instead. This option is mostly useful for users on hardware that
lacks OpenGL 2 support. While BlastEm will fall back automatically even if gl
is set to on there will be a warning. Disabling gl eliminates this warning.

"scaling" controls the type of scaling used for textures in both the GL and
SDL renderers. Valid values are "nearest" and "linear". Note that shaders also
impact how pixels are scaled.

The "ntsc" and "pal" sub-sections control overscan settings for the emulated
video output for NTSC and PAL consoles respectively. More details are available
in the Overscan section.

Overscan
--------

Analog televisions generally don't display the entirety of a video frame. Some
portion is cropped at the edges of the display. This is called overscan.
Unfortunately, the amount of cropping performed varies considerably and is even
adjustable on many TV sets. To deal with this, BlastEm allows overscan to be
customized.

Overscan values are specified in the "ntsc" and "pal" sub-sections of the
"video" section of the config file. The "overscan" sub-section contains four
settings for specifying the number of pixels cropped on each side of the
display: "top", "bottom", "left" and "right".

The default settings hide the horizontal border completely for both NTSC and
PAL consoles. For the vertical borders, the NTSC overscan settings are chosen
to give square pixels with the default aspect ratio of 4:3. For PAL, the
default settings are set so that the PAL-exclusive V30 mode will produce a
visible border that is the same size as what is shown in V28 mode in NTSC. This
results in a slightly squished picture compared to NTSC which is probably
appropriate given that a PAL display has more lines than an NTSC one.

Audio
-----

The audio section contains settings that affect the audio output of BlastEm.

"rate" selects the preferred sample rate for audio output. Your operating
system may not accept this value in which case a different rate will be chosen.
This should generally be either the native sample rate of your sound card or an
integral divisor of it. Most modern sound cards have a native output rate that
is a multiple of 48000 Hz so the default setting should work well for most users.

"buffer size" controls how large of a buffer uses for audio data. Smaller values
will reduce latency, but too small of a value can lead to dropouts. 512 works
well for me, but a higher or lower value may be more appropriate for your system.

"lowpass_cutoff" controls the cutoff, or knee, frequency of the RC-style
low-pass filter. The default value of 3390 Hz is supposedly what is present in
at least some Genesis/Megadrive models. Other models reportedly use an even
lower value.

"gain" specifies the gain in decibels to be applied to the overall output.

"fm_gain" specifies the gain to be applied to the emulated FM output before
mixing with the PSG.

"psg_gain" specifies the gain to be applied to the emulated PSG output before
mixing with the FM chip.

"fm_dac" controls the characteristics of the DAC in the emulated FM chip. If
this is set to "linear", then the DAC will have precise linear output similar
to the integrated YM3438 in later Gen/MD consoles. If it is set to "zero_offset",
there will be a larger gap between -1 and 0. This is commonly referred to as the
"ladder effect". This will also cause "leakage" on channels that are muted or
panned to one side in a similar manner to a discrete YM2612.


Clocks
------

The clocks section contains settings that affect how fast things run.

"m68k_divider" describes the relationsip between the master clock (which is
53693175 Hz for NTSC mode and 53203395 Hz for PAL mode). The default value of 7
matches the real hardware. Set this to a lower number to overclock the 68000
and set it to a higher number to underclock it.

"max_cycles" controls how often the system is forced to synchronize all
hardware. BlastEm generally uses a sync on demand approach to synchronizing
components in the system. This can provide perfect synchronization for most
components, but since the Z80 can steal cycles from the 68000 at unpredictable
times 68000/Z80 synchronization is imperfect. The default value of 3420
corresponds to the number of master clock cycles per line. Larger numbers may
produce a modest performance improvement whereas smaller numbers will improve
68000/Z80 synchronization.

"speeds" controls the speed of the overall emulated console at different
presets. Preset 0 is the default speed and should normally be set to 100. The
other presets enable the slow/turbo mode functionality.

UI
--

The UI section contains settings that affect the user interface.

"rom" determines the path of the Genesis/Megadrive ROM that implements the UI.
Relative paths will be loaded relative to the BlastEm executable.

"initial_path" specifies the starting path for the ROM browser. It can contain
the following special variables: $HOME, $EXEDIR. Additionally, variables
defined in the OS environment can be used.

"remember_path" specifies whether BlastEm should remember the last path used in
the file browser. When it is set to "on", the last path will be remembered and
used instead of "initial_path" in subsequent runs. If it is set to "off", 
"initial_path" will always be used.

"screenshot_path" specifies the directory "internal" screenshots will be saved
in. It accepts the same special variables as "initial_path".

"screenshot_template" specifies a template for creating screenshot filenames.
It is specified as a format string for the C library function strftime

"save_path" specifies the directory that savestates, SRAM and EEPROM data will
be saved in for a given game. It can contain the following special variables:
$HOME, $EXEDIR, $USERDATA, $ROMNAME. Like "initial_path" it can also reference
variables from the environment.

"extensions" specifies the file extensions that should be displayed in the file
browser.

"state_format" specifies the preferred format for saving save states. Valid
values are "native" (the default) and "gst". "native" save states do a better
job of preserving the state of the emulated system, but "gst" save states are
compatible with other emulators like Kega and Gens. This setting has no effect
for systems other than the Genesis/Mega Drive

Path Variables
--------------

This section explains the meaning of the special path variables referenced
in the previous section.

$HOME      The home directory of the current user. On most Unix variants, it
           will be a subdirectory of /home. On Windows it will typically be a 
           subdirectory of C:\Users
$EXEDIR    The directory the BlastEm executable is located in
$USERDATA  This is an OS-specific path used for storing application specific
           user data. On Unix variants, it will be  $HOME/.local/share/blastem
           On Windows it will be %LOCALDATA%/blastem
$ROMNAME   The name of the currently loaded ROM file without the extension

System
------

"ram_init" determines how the RAM in the emulated system is initialized. The
default value of "zero" will cause all RAM to be zeroed out before the system
is started. Alternatively, "random" can be used to initialize RAM with values
from a pseudo-random number generator. This option is mostly useful for
developers that want to debug initialization issues in their code.

"default_region" determines the console region that will be used when region
detection fails and when there are multiple valid regions. The default of 'U'
specifies a 60Hz "foreign" console.

"sync_source" controls whether BlastEm uses audio or video output to control
execution speed. "video" can provide a smoother experience when your display
has a similar refresh rate to the emulated system, but has some limitations
in the current version. The default value is "audio".

"megawifi" enables or disables support for MegaWiFi cart emulation. MegaWiFi
is a cartridge that contains WiFi hardware for network functionality. Enabling
this means that ROMs potentially have access to your network (and the internet)
which obviously has security implications. For this reason, it is disabled by
default. If you wish to try out MegaWiFi emulation, set this to "on". Note that
the support for MegaWiFi hardware is preliminary in this release.

Debugger
--------

BlastEm has an integrated command-line debugger loosely based on GDB's
interface. The interface is very rough at the moment. Available commands in the
68K debugger are:
    b ADDRESS            - Set a breakpoint at ADDRESS
    d BREAKPOINT         - Delete a 68K breakpoint
    co BREAKPOINT        - Run a list of debugger commands each time
                           BREAKPOINT is hit
    a ADDRESS            - Advance to address
    n                    - Advance to next instruction
    o                    - Advance to next instruction ignoring branches to
                           lower addresses (good for breaking out of loops)
    s                    - Advance to next instruction (follows bsr/jsr)
    c                    - Continue
    bt                   - Print a backtrace
    p[/(x|X|d|c)] VALUE  - Print a register or memory location
    di[/(x|X|d|c)] VALUE - Print a register or memory location each time
                           a breakpoint is hit
    vs                   - Print VDP sprite list
    vr                   - Print VDP register info
    zb ADDRESS           - Set a Z80 breakpoint
    zp[/(x|X|d|c)] VALUE - Display a Z80 value
    q                    - Quit BlastEm
Available commands in the Z80 debugger are:
    b  ADDRESS           - Set a breakpoint at ADDRESS
    de BREAKPOINT        - Delete a Z80 breakpoint
    a  ADDRESS           - Advance to address
    n                    - Advance to next instruction
    c                    - Continue
    p[/(x|X|d|c)] VALUE  - Print a register or memory location
    di[/(x|X|d|c)] VALUE - Print a register or memory location each time
                           a breakpoint is hit
    q                    - Quit BlastEm

The -d flag can be used to cause BlastEm to start in the debugger.
Alternatively, you can use the ui.enter_debugger action (mapped to the 'u' key
by default) to enter the debugger while a game is running. To debug the menu
ROM, use the -dm flag.

GDB Remote Debugging
--------------------

In addition to the native debugger, BlastEm can also act as a GDB remote
debugging stub. To use this, you'll want to configure your Makefile to produce
both an ELF executable and a raw binary. Invoke an m68k-elf targeted gdb with
the ELF file. Once inside the gdb session, type:

    target remote | BLASTEM_PATH/blastem ROM_FILE.bin -D

where BLASTEM_PATH is the relative or absolute path to your BlastEm
installation and ROM_FILE.bin is the name of the raw binary for your program.
BlastEm will halt at the beginning of your program's entry point and return
control to GDB. This will allow you to set breakpoints before your code runs.

On Windows, the procedure is slightly different. First run 
    blastem.exe ROM_FILE.bin -D
This will cause BlastEm to wait for a socket connection on port 1234. It will
appear to be frozen until gdb connects to it. Now open the ELF file in gdb
and type:

    target remote :1234

Trace points and watch points are not currently supported.

Included Tools
--------------

BlastEm ships with a few small utilities that leverage portions of the emulator
code.
    
    dis       - 68K disassembler
    zdis      - Z80 disassembler
    vgmplay   - Very basic VGM player
    stateview - GST save state viewer
    
Sync Source and VSync
-----

This section includes information about using VSync with BlastEm. Currently,
the best way to use VSync is to set the sync source to "video". This will force
VSync on and use video output for controlling the speed of emulation. In this
mode, audio will have it's rate automatically adjusted to keep pace with video.
The code for this is still a bit immature, so you may experience dropouts or
pitch changes in this mode.

If you experience problems, please switch back to the "audio" sync source,
which is the default. You can also enable vsync when using the "audio" sync
source by changing the "vsync" setting. This will generally work okay as long
as the emulated refresh rate is below your monitor refresh rate (even if only
slightly), but you will occassionally get a doubled frame (or frequently if
the refresh rates are very different).

Turbo mode will currently not work when vsync is on, regardless of which sync
source is used. Slow mode will work with "audio" sync, but not "video" sync.

--------------

My work has been made much easier by the contributions of those in the Genesis
community past and present. I'd like to thank the people below for their help.

Nemesis            - His work reverse engineering and documenting the VDP and
                     YM-2612 has saved me an immeasurable amount of time. I've
                     found both his sprite overflow test ROM and VDP FIFO
                     Testing ROM to be quite helpful.

Charles MacDonald  - While it hasn't been updated in a while, I still find his
                     VDP document to be my favorite reference. His Genesis
                     hardware document has also come in handy.

Eke-Eke            - Eke-Eke wrote a great document on the use of I2C EEPROM in
                     Genesis games and also left some useful very helpful 
                     comments about problematic games in Genesis Plus GX
					 
Sauraen            - Sauraen has analyzed the YM2203 and YM2612 dies and written
                     a VHDL operator implementation. These have been useful in
                     improving the accuracy of my YM2612 core.

Alexey Khokholov   - Alexey (aka Nuke.YKT) has analyzed the YM3438 die and written
                     a fairly direct C implementation from that analysis. This
                     has been a useful reference for verifying and improving my
                     YM2612 core.

Bart Trzynadlowski - His documents on the Genecyst save-state format and the
                     mapper used in Super Street Fighter 2 were definitely
                     appreciated.
                     
KanedaFR           - Kaneda's SpritesMind forum is a great resource for the
                     Sega development community.
					 
Titan              - Titan has created what are without a doubt the most
                     impressive demos on the Megadrive. Additionally, I am very
                     grateful for the documentation provided by Kabuto and the
                     assistance of Kabuto, Sik and Jorge in getting Overdrive 2
                     to run properly in BlastEm.
					 
flamewing          - flamewing created a very handy exhaustive test ROM for 68K
                     BCD instructions and documented the proper behavior for
                     certain BCD edge cases

r57shell           - r57shell created a test ROM for 68K instruction sizes that
                     was invaluable in fixing the remaining bugs in my 68K instruction
                     decoder

I'd also like to thank the following people who have performed compatibility
testing or submitted helpful bug reports

micky, Sasha, lol-frank, Sik, Tim Lawrence, ComradeOj, Vladikcomper

License
-------

BlastEm is free software distributed under the terms of the GNU General Public
License version 3 or higher. This gives you the right to redistribute and/or
modify the program as long as you follow the terms of the license. See the file
COPYING for full license details.

Binary releases of BlastEm are packaged with GLEW, SDL2 and zlib which have their
own licenses. See GLEW-LICENSE and SDL-LICENSE for details. For zlib license
information, please see zlib.h in the source code release.
