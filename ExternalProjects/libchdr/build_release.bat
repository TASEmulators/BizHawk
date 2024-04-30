rmdir /s /q build
mkdir build
cd build
:: cl must be used as clang fails to compile :(
cmake ..\libchdr -DCMAKE_BUILD_TYPE=Release -DCMAKE_C_COMPILER=cl -G Ninja ^
	-DBUILD_LTO=ON -DBUILD_SHARED_LIBS=ON -DINSTALL_STATIC_LIBS=OFF -DWITH_SYSTEM_ZLIB=OFF
ninja
xcopy .\chdr.dll ..\..\..\Assets\dll\ /Y
xcopy .\chdr.dll ..\..\..\output\dll\ /Y
cd ..
