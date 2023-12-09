rmdir /s /q build
mkdir build
cd build
cmake .. -DCMAKE_BUILD_TYPE=Release -DCMAKE_INTERPROCEDURAL_OPTIMIZATION=ON -DCMAKE_C_COMPILER=clang-cl -G Ninja
ninja
cd ..
