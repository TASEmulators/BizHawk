# paraLLEl-RDP

This project is a revival and complete rewrite of the old, defunct paraLLEl-RDP project.

The goal is to implement the Nintendo 64 RDP graphics chip as accurately as possible using Vulkan compute.
The implementation aims to be bitexact with the
[Angrylion-Plus](https://github.com/ata4/angrylion-rdp-plus) reference renderer where possible.

## Disclaimer

While paraLLEl-RDP uses [Angrylion-Plus](https://github.com/ata4/angrylion-rdp-plus)
as an implementation reference, it is not a port, and not a derived codebase of said project.
It is written from scratch by studying [Angrylion-Plus](https://github.com/ata4/angrylion-rdp-plus)
and trying to understand what is going on.
The test suite uses [Angrylion-Plus](https://github.com/ata4/angrylion-rdp-plus) as a reference
to validate implementation and cross-checking behavior.

## Use cases

- **Much** faster LLE RDP emulation of N64 compared to a CPU implementation
  as parallel graphics workloads are offloaded to the GPU.
  Emulation performance is now completely bound by CPU and LLE RSP performance.
  Early benchmarking results suggest 2000 - 5000 VI/s being achieved on mid-range desktop GPUs based on timestamp data.
  There is no way the CPU emulation can keep up with that, but that means this should
  scale down to fairly gimped GPUs as well, assuming the driver requirements are met.
- A backend renderer for standalone engines which aim to efficiently reproduce faithful N64 graphics.
- Hopefully, an easier to understand implementation than the reference renderer.
- An esoteric use case of advanced Vulkan compute programming.

## Missing features

The implementation is quite complete, and compatibility is very high in the limited amount of content I've tested.
However, not every single feature is supported at this moment.
Ticking the last boxes depends mostly on real content making use of said features.

- Color combiner chroma keying
- Various "bugs" / questionable behavior that seems meaningless to emulate
- Certain extreme edge cases in TMEM upload. The implementation has tests for many "crazy" edge cases though.
- ... possibly other obscure features

The VI is essentially complete. A fancy deinterlacer might be useful to add since we have plenty of GPU cycles to spare in the graphics queue.
The VI filtering is always turned on if game requests it, but features can selectively be turned off for the pixel purists.

## Environment variables for development / testing

### `RDP_DEBUG` / `RDP_DEBUG_X` / `RDP_DEBUG_Y`

Supports printf in shaders, which is extremely useful to drill down difficult bugs.
Only printfs from certain pixels can be filtered through to avoid spam.

### `VI_DEBUG` / `VI_DEBUG_X` / `VI_DEBUG_Y`

Same as `RDP_DEBUG` but for the VI.

### `PARALLEL_RDP_MEASURE_SYNC_TIME`

Measures time stalled in `CommandProcessor::wait_for_timeline`. Useful to measure
CPU overhead in hard-synced emulator integrations.

### `PARALLEL_RDP_SMALL_TYPES=0`

Force-disables 8/16-bit arithmetic support. Useful when suspecting driver bugs.

### `PARALLEL_RDP_UBERSHADER=1`

Forces the use of ubershaders. Can be extremely slow depending on the shader compiler.

### `PARALLEL_RDP_FORCE_SYNC_SHADER=1`

Disabled async pipeline optimization, and blocks for every shader compiler.
Only use if the ubershader crashes, since this adds the dreaded shader compilation stalls.

### `PARALLEL_RDP_BENCH=1`

Measures RDP rendering time spent on GPU using Vulkan timestamps.
At end of a run, reports average time spent per render pass,
and how many render passes are flushed per frame.

### `PARALLEL_RDP_SUBGROUP=0`

Force-disables use of Vulkan subgroup operations,
which are used to optimize the tile binning algorithm.

### `PARALLEL_RDP_ALLOW_EXTERNAL_HOST=0`

Disables use of `VK_EXT_external_memory_host`. For testing.

## Vulkan driver requirements

paraLLEl-RDP requires up-to-date Vulkan implementations. A lot of the great improvements over the previous implementation
comes from the idea that we can implement N64's UMA by simply importing RDRAM directly as an SSBO and perform 8 and 16-bit
data access over the bus. With the tile based architecture in paraLLEl-RDP, this works very well and actual
PCI-e traffic is massively reduced. The bandwidth for doing this is also trivial. On iGPU systems, this also works really well, since
it's all the same memory anyways.

Thus, the requirements are as follows. All of these features are widely supported, or will soon be in drivers.
paraLLEl-RDP does not aim for compatibility with ancient hardware and drivers.
Just use the reference renderer for that. This is enthusiast software for a niche audience.

- Vulkan 1.1
- VK_KHR_8bit_storage / VK_KHR_16bit_storage
- Optionally VK_KHR_shader_float16_int8 which enables small integer arithmetic
- Optionally subgroup support with VK_EXT_subgroup_size_control
- For integration in emulators, VK_EXT_external_memory_host is currently required (may be relaxed later at some performance cost)

### Tested drivers

paraLLEl-RDP has been tested on Linux and Windows on all desktop vendors.

- Intel Mesa (20.0.6) - Passes conformance
- Intel Windows - Passes conformance (**CAVEAT**. Intel Windows requires 64 KiB alignment for host memory import, make sure to add some padding around RDRAM in an emulator to make this work well.)
- AMD RADV LLVM (20.0.6) - Passes conformance
- AMD RADV ACO - Passes conformance with bleeding edge drivers and `PARALLEL_RDP_SMALL_TYPES=0`.
- Linux AMDGPU-PRO - Passes conformance, with caveat that 8/16-bit arithmetic does not work correctly for some tests.
  paraLLEl-RDP automatically disables small integer arithmetic for proprietary AMD driver.
- AMD Windows - Passes conformance with same caveat and workaround as AMDGPU-PRO.
- NVIDIA Linux - Passes conformance (**MAJOR CAVEAT**, NVIDIA Linux does not support VK_EXT_external_memory_host as of 2020-05-12.)
- NVIDIA Windows - Passes conformance

## Implementation strategy

This project uses Vulkan compute shaders to implement a fully programmable rasterization pipeline.
The overall rendering architecture is reused from [RetroWarp](https://github.com/Themaister/RetroWarp)
with some further refinements.

The lower level Vulkan backend comes from [Granite](https://github.com/Themaister/Granite).

### Asynchronous pipeline optimization

Toggleable paths in RDP state is expressed as specialization constants. The rendering thread will
detect new state combinations and kick off building pipelines which only specify exact state needed to render.
This is a massive performance optimization.

The same shaders are used for an "ubershader" fallback when pipelines are not ready.
In this case, specialization constants are simply not used.
The same SPIR-V modules are reused to great effect using this Vulkan feature.

### Tile-based rendering

See [RetroWarp](https://github.com/Themaister/RetroWarp) for more details.

### GPU-driven TMEM management

TMEM management is fully GPU-driven, but this is a very complicated implementation.
Certain combinations of formats are not supported, but such cases would produce
meaningless results, and it is unclear that applications can make meaningful use of these "weird" uploads.

### Synchronization

Synchronizing the GPU and CPU emulation is one of the hot button issues of N64 emulation.
The integration code is designed around a timeline of synchronization points which can be waited on by the CPU
when appropriate. For accurate emulation, an OpSyncFull is generally followed by a full wait,
but most games can be more relaxed and only synchronize with the CPU N frames later.
Implementation of this behavior is outside the scope of paraLLEl-RDP, and is left up to the integration code.

### Asynchronous compute

GPUs with a dedicated compute queue is recommended for optimal performance since
RDP shading work can happen on the compute queue, and won't be blocked by graphics workloads happening
in the graphics queue, which will typically be VI scanout and frontend applying shaders on top.

## Project structure

This project implements several submodules which are quite useful.

### rdp-replayer

This app replays RDP dump files, which are produced by running content through an RDP dumper.
An implementation can be found in e.g. parallel-N64. The file format is very simple and essentially
contains a record of RDRAM changes and RDP command streams.
This dump is replayed and a live comparison between the reference renderer can be compared to paraLLEl-RDP
with visual output. The UI is extremely crude, and is not user-friendly, but good enough for my use.

### rdp-conformance

I made a somewhat comprehensive test suite for the RDP, with a custom higher level RDP command stream generator.
There are roughly ~150 fuzz tests which exercise many aspects of the RDP.
In order to pass the test, paraLLEl-RDP must produce bit-exact results compared to Angrylion,
so the test condition is as stringent as possible.

#### A note on bitexactness

There are a few cases where bit-exactness is a meaningless term, such as the noise feature of the RDP.
It is not particularly meaningful to exactly reproduce noise, since it is by its very nature unpredictable.
For that reason, this repo references a fork of the reference renderer which implements deterministic "undefined behavior"
where appropriate. The exact formulation of the noise generator is not very interesting as long as
correct entropy and output range is reproduced.

##### Intentional differences from reference renderer

Certain effects invoke "undefined behavior" in the RDP and requires cycle accuracy to resolve bit-accurately with real RDP.
Reference renderer attempts to emulate these effects, but to reproduce this behavior breaks any form of multi-threading.
To be able to validate dumps in a sensible way with buggy content, I modified the reference slightly to make certain
"undefined behavior" deterministic. This doesn't meaningfully change the rendered output in the cases I've seen in the wild.
Some of these effects would be possible to emulate,
but at the cost of lots of added complexity and it wouldn't be quite correct anyways given the cycle accuracy issue.

- CombinedColor/Alpha in first cycle is cleared to zero. Some games read this in first cycle,
  and reference renderer will read whatever was generated last pixel.
  This causes issues in some cases, where cycle accuracy would have caused the feedback to converge to zero over time.
- Reading LODFrac in 1 cycle mode. This is currently ignored. The results generated seem non-sensical. Never seen this in the wild.
- Using TexLOD in copy mode. This is currently ignored. The results generated seem non-sensical. Never seen this in the wild.
- Reading MemoryColor in first blender cycle in 2-cycle mode. Reference seems to wait until the second cycle before updating this value,
  despite memory coverage being updated right away. The sensible thing to do is to allow reading memory color in first cycle.
- Alpha testing in 2-cycle mode reads combined alpha from next pixel in reference.
  Just doing alpha testing in first cycle on current pixel is good enough.
  If this is correct hardware behavior, I consider this a hardware bug.
- Reading Texel1 in cycle 1 of 2-cycle mode reads the Texel0 from next pixel.
  In the few cases I've seen this, the rendered output is slightly buggy, but it's hardly visible in motion.
  The workaround is just to read Texel0 from current pixel which still renders fine.

### vi-conformance

This is a conformance suite, except for the video interface (VI) unit.

### rdp-validate-dump

This tool replays an RDP dump headless and compares outputs between reference renderer and paraLLEl-RDP.
To pass, bitexact output must be generated.

## Build

Checkout submodules. This pulls in Angrylion-Plus as well as Granite.

```
git submodule update --init --recursive
```

Standard CMake build.

```
mkdir build
cd build
cmake ..
cmake --build . --parallel (--config Release on MSVC)
```

### Run test suite

You can run rdp-conformance and vi-conformance with ctest to verify if your driver is behaving correctly.

```
ctest (-C Release on MSVC)
```

### Embedding shaders in a C++ header

If embedding paraLLEl-RDP in an emulator project, it is helpful to pre-compile and bake SPIR-V shaders in a C++ header.
Build slangmosh from Granite, and then run:

```
slangmosh parallel-rdp/shaders/slangmosh.json --output slangmosh.hpp --vk11 --strip -O --namespace RDP
```

### Generating a standalone code base for emulator integration

Run the `generate_standalone_codebase.sh $OUTDIR` script with an output directory `$OUTDIR/` as argument to generate a standalone code base which can be built without any special build system support.
Include `$OUTDIR/config.mk` if building with Make to make your life easier.
Note that `slangmosh` must be in your path for this script to run. It executes the command above to build `slangmosh.hpp`.

## License

paraLLEl-RDP is licensed under the permissive license MIT. See included LICENSE file.
This implementation builds heavily on the knowledge (but not code) gained from studying the reference implementation,
thus it felt fair to release it under a permissive license, so my work could be reused more easily.
