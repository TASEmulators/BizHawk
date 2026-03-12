rmdir /s /q build
mkdir build
cd build
cmake ..\encore -DCMAKE_BUILD_TYPE=Release -DENABLE_LTO=ON -DCMAKE_C_COMPILER=cl ^
 -DCMAKE_CXX_COMPILER=cl -DCMAKE_MSVC_RUNTIME_LIBRARY=MultiThreaded -DENABLE_VULKAN=OFF ^
 -DCMAKE_POLICY_VERSION_MINIMUM=3.5 -G Ninja
ninja
cd ..
