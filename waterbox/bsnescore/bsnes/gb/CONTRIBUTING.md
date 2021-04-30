# SameBoy Coding and Contribution Guidelines

## Issues

GitHub Issues are the most effective way to report a bug or request a feature in SameBoy. When reporting a bug, make sure you use the latest stable release, and make sure you mention the SameBoy frontend (Cocoa, SDL, Libretro) and operating system you're using. If you're using Linux/BSD/etc, or you build your own copy of SameBoy for another reason, give as much details as possible on your environment.

If your bug involves a crash, please attach a crash log or a core dump. If you're using Linux/BSD/etc, or if you're using the Libretro core, please attach the `sameboy` binary (or `libretro_sameboy` library) in that case.

If your bug is a regression, it'd be extremely helpful if you can report the the first affected version. You get extra credits if you use `git bisect` to point the exact breaking commit.

If your bug is an emulation bug (Such as a failing test ROM), and you have access to a Game Boy you can test on, please confirm SameBoy is indeed behaving differently from hardware, and report both the emulated model and revision in SameBoy, and the hardware revision you're testing on.

If your issue is a feature request, demonstrating use cases can help me better prioritize it.

## Pull Requests

To allow quicker integration into SameBoy's master branch, contributors are asked to follow SameBoy's style and coding guidelines. Keep in mind that despite the seemingly strict guidelines, all pull requests are welcome – not following the guidelines does not mean your pull request will not be accepted, but it will require manual tweaks from my side for integrating.

### Languages and Compilers

SameBoy's core, SDL frontend, Libretro frontend, and automatic tester (Folders `Core`, `SDL` & `OpenDialog`, `libretro`, and `Tester`; respectively) are all written in C11. The Cocoa frontend, SameBoy's fork of Hex Fiend, JoyKit and the Quick Look previewer (Folders `Cocoa`, `HexFiend`, `JoyKit` and `QuickLook`; respectively) are all written in ARC-enabled Objective-C. The SameBoot ROMs (Under `BootROMs`) are written in rgbds-flavor SM83 assembly, with build tools in C11. The shaders (inside `Shaders`) are written in a polyglot GLSL and Metal style, with a few GLSL- and Metal-specific sources. The build system uses standalone Make, in the GNU flavor. Avoid adding new languages (C++, Swift, Python, CMake...) to any of the existing sub-projects.

SameBoy's main target compiler is Clang, but GCC is also supported when targeting Linux and Libretro. Other compilers (e.g. MSVC) are not supported, and unless there's a good reason, there's no need to go out of your way to add specific support for them. Extensions that are supported by both compilers (Such as `typeof`) may be used if it makes sense. It's OK if you can't test one of these compilers yourself; once you push a commit, the CI bot will let you know if you broke something.

### Third Party Libraries and Tools

Avoid adding new required dependencies; run-time and compile-time dependencies alike. Most importantly, avoid linking against GPL licensed libraries (LGPL libraries are fine), so SameBoy can retain its MIT license.

### Spacing, Indentation and Formatting

In all files and languages (Other than Makefiles when required), 4 spaces are used for indentation. Unix line endings (`\n`) are used exclusively, even in Windows-specific source files. (`\r` and `\t` shouldn't appear in any source file). Opening braces belong on the same line as their control flow directive, and on their own line when following a function prototype. The `else` keyword always starts on its own line. The `case` keyword is indented relative to its `switch` block, and the code inside a `case` is indented relative to its label. A control flow keyword should have a space between it and the following `(`, commas should follow a space, and operator (except `.` and `->`) should be surrounded by spaces.

Control flow statements must use `{}`, with the exception of `if` statements that only contain a single `break`, `continue`, or trivial `return` statements. If `{}`s are omitted, the statement must be on the same line as the `if` condition. Functions that do not have any argument must be specified as `(void)`, as mandated by the C standard. The `sizeof` and `typeof` operators should be used as if they're functions (With `()`). `*`, when used to declare pointer types (including functions that return pointers), and when used to dereference a pointer, is attached to the right side (The variable name) – not to the left, and not with spaces on both sides.

No strict limitations on a line's maximum width, but use your best judgement if you think a statement would benefit from an additional line break.

Well formatted code example:

```
static void my_function(void)
{
    GB_something_t *thing = GB_function(&gb, GB_FLAG_ONE | GB_FLAG_TWO, sizeof(thing));
    if (GB_is_thing(thing)) return;
    
    switch (*thing) {
        case GB_QUACK:
            // Something
        case GB_DUCK:
            // Something else
    }
}
```

Badly formatted code example:
```
static void my_function(){
        GB_something_t* thing=GB_function(&gb , GB_FLAG_ONE|GB_FLAG_TWO , sizeof thing);
        if( GB_is_thing ( thing ) )
                return;

        switch(* thing)
        {
        case GB_QUACK:
                // Something
        case GB_DUCK:
                // Something else
        }
}
```

### Other Coding Conventions

The primitive types to be used in SameBoy are `unsigned` and `signed` (Without the `int` keyword), the `(u)int*_t` types, `char *` for UTF-8 strings, `double` for non-integer numbers, and `bool` for booleans (Including in Objective-C code, avoid `BOOL`). As long as it's not mandated by a 3rd-party API (e.g. `int` when using file descriptors), avoid using other primitive types. Use `const` whenever possible. 

Most C names should be `lower_case_snake_case`. Constants and macros use `UPPER_CASE_SNAKE_CASE`. Type definitions use a `_t` suffix. Type definitions, as well as non-static (exported) core symbols, should be prefixed with `GB_` (SameBoy's core is intended to be used as a library, so it shouldn't contaminate the global namespace without prefixes). Exported symbols that are only meant to be used by other parts of the core should still get the `GB_` prefix, but their header definition should be inside `#ifdef GB_INTERNAL`.

For Objective-C naming conventions, use Apple's conventions (Some old Objective-C code mixes these with the C naming convention; new code should use Apple's convention exclusively). The name prefix for SameBoy classes and constants is `GB`. JoyKit's prefix is `JOY`, and Hex Fiend's prefix is `HF`.

In all languages, prefer long, unambiguous names over short ambiguous ones.
