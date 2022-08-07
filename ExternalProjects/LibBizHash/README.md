LibBizHash is the unmanaged side for BizHawk's hashing.

CRC32 code is taken from [zlib-ng](https://github.com/zlib-ng/zlib-ng) with massive slashing of code and various tweaks. This code is licensed under the zlib license.
SHA1 is code is taken from [SHA-Intrinsics](https://github.com/noloader/SHA-Intrinsics) with some tweaks. This code is under the public domain.

To build, just do `make` in this directory. Note gcc 10 or later is required (due to missing intrinsics in older gcc versions)

zlib-ng's license:

```
(C) 1995-2013 Jean-loup Gailly and Mark Adler

This software is provided 'as-is', without any express or implied warranty. In no event will the authors be held liable for any damages arising from the use of this software.

Permission is granted to anyone to use this software for any purpose, including commercial applications, and to alter it and redistribute it freely, subject to the following restrictions:

    The origin of this software must not be misrepresented; you must not claim that you wrote the original software. If you use this software in a product, an acknowledgment in the product documentation would be appreciated but is not required.

    Altered source versions must be plainly marked as such, and must not be misrepresented as being the original software.

    This notice may not be removed or altered from any source distribution.
```
