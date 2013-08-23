64bit building:

( http://stackoverflow.com/questions/10325696/mingw-64-bit-install-trouble )

install http://code.google.com/p/mingw-builds/downloads/detail?name=i686-mingw32-gcc-4.7.0-release-c,c%2b%2b,fortran-sjlj.zip&can=2&q= to c:\mingw64

copy msys from c:\mingw into c:\mingw64; edit the new msys's fstab file to point to mingw64 instead of mingw

run bizwinmakedistro64.sh from the mingw64's msys