# `chd-capi`

⚠️*The C API has not been heavily tested. Use at your own risk.* ⚠️

chd-rs provides a C API compatible with [chd.h](https://github.com/rtissera/libchdr/blob/6eeb6abc4adc094d489c8ba8cafdcff9ff61251b/include/libchdr/chd.h).
ABI compatibility is detailed below but is untested when compiling as a dynamic library. The intended consumption for this crate is not via cargo, but by vendoring
the [sources of the C API](https://github.com/SnowflakePowered/chd-rs/tree/master/chd-rs-capi) in tree, along with a compatible `libchdcorefile` implementation
for your platform.

## Features
### `verify_block_crc`
Enables the `verify_block_crc` of the `chd` crate to verify decompressed CHD hunks with their internal hash.

### `chd_core_file`
Enables `core_file*` and the`chd_open_file`, and `chd_core_file` APIs. This feature requires a `libchdcorefile` implementation,
or the default POSIX compatible implementation (where `core_file*` is `FILE*`) will be used.

Note that by default, `core_file*` is not an opaque pointer and is a C `FILE*` stream. This allows the underlying 
file pointer to be changed unsafely beneath the memory safety guarantees of chd-rs. We strongly encourage using 
`chd_open` instead of `chd_open_file`.

If you need `core_file*` support, chd-capi should have the `chd_core_file` feature enabled, which will wrap
`FILE*` to be usable in Rust with a lightweight wrapper in `libchdcorefile`. If the default implementation
is not suitable, you may need to implement `libchdcorefile` yourself. The `chd_core_file` feature requires
CMake and Clang to be installed.

### `chd_virtio`
Enables the [virtual I/O](https://github.com/rtissera/libchdr/pull/78) functions `chd_open_core_file`. 
Because this C API requires `core_file` to be an opaque pointer, there is no difference between `chd_open_file` and
`chd_open_core_file` unlike libchdr, and `chd_open_core_file` is simply an alias for `chd_open_file`. All functions that
take `core_file*` require a `libchdcorefile` implementation.

### `chd_precache`
Enables precaching of the underlying file into memory with the `chd_precache_progress` and `chd_precache` functions. 

## ABI compatibility

chd-rs makes the following ABI-compatibility guarantees compared to libchdr when compiled statically.
* `chd_error` is ABI and API-compatible with [chd.h](https://github.com/rtissera/libchdr/blob/cdcb714235b9ff7d207b703260706a364282b063/include/libchdr/chd.h#L258)
* `chd_header` is ABI and API-compatible [chd.h](https://github.com/rtissera/libchdr/blob/cdcb714235b9ff7d207b703260706a364282b063/include/libchdr/chd.h#L302)
* `chd_file *` is an opaque pointer. It is **not layout compatible** with [chd.c](https://github.com/rtissera/libchdr/blob/cdcb714235b9ff7d207b703260706a364282b063/src/libchdr_chd.c#L265)
* The layout of `core_file *` is user-defined when the `chd_core_file` feature is enabled.
* Freeing any pointer returned by chd-rs with `free` is undefined behaviour. The exception are `chd_file *` pointers which can be safely freed with `chd_close`.
