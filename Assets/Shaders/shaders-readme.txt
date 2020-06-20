Despite using the "cgp" derived "retro shader" approach, we're doing things a bit different than retroarch.

A single .cgp file is used for any graphics backend. The shader references inside it can be written extensionless, or with .cg (or with .hlsl or .glsl for reasons, read on)

In case you have shaders that work only GLSL or HLSL -- well, they simply won't work in the wrong mode. 
In that case, try something like bizhawkdir/Shaders/myshaders/glsl/mybadshader.cgp
In this case, the .cgp can reference .glsl shaders internally.

An important point, which you will perceive by checking out the bizhawk shaders, is that a .cgp is now portable due to the extension-ignorant behaviour AND the separate .glsl and .hlsl extensions.
(For instance, BizScanlines.cgp+BizScanlines.hlsl+BizScanlines.glsl).
The separate extensions let there be separate shaders for each backend, each referenced by the same cgp which references .cg (arbitrarily; it could be blank)
However if the .cgp referenced a .glsl, it wouldn't work on D3D.

In case you haven't pieced it together yet, this means there is no automatic mechanism for transpiling shaders. It isn't reliable and created an unmaintainable, slow, mess.

Porting shaders from retroarch will require touching them extensively, probably.