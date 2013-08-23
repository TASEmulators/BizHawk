ppsspp-ffmpeg
=============

A private copy of FFMPEG used in PPSSPP.

Building
========

If on Mac, iOS, or Linux, just run the corresponding build script (if it's written...)


If on Windows and building for Windows, use these instructions:

https://ffmpeg.org/platform.html#Microsoft-Visual-C_002b_002b

Then except instead of ./configure, just run ./windows_x86-build.sh.


If on Windows and building for Android, just run ./android-build.sh . Like for the
Windows build, you may need real msys environment for that, and you may need
to adjust some paths in android-build.sh.

Errors
==============

If you get `*** missing separator. Stop`, it's a line ending problem.  You can fix
this by running the following commands (WARNING: this will delete all your changes.)

    git config core.autocrlf false
    git rm --cached -r .
    git reset --hard

This won't affect anything other than this repo, though.  See also
[ffmpeg ticket #1209] (https://ffmpeg.org/trac/ffmpeg/ticket/1209).
