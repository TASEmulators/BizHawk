//https://raw.githubusercontent.com/Themaister/Emulator-Shader-Pack/master/Cg/TV/gamma.cg

/*
   Author: Themaister
   License: Public domain
*/

// Shader that replicates gamma-ramp of bSNES/Higan.

#ifdef VERTEX
uniform mat4 modelViewProj;

in vec4 position;
in vec2 tex;

out vec2 vTex;

void main()
{
	gl_Position = modelViewProj * position;
	vTex = tex;
}

#endif

#ifdef FRAGMENT

uniform sampler2D s_p;

in vec2 vTex;

out vec4 oColor;

// Tweakables.
#define saturation 1.0
#define gamma 1.5
#define luminance 1.0

vec3 grayscale(vec3 col)
{
	// Non-conventional way to do grayscale,
	// but bSNES uses this as grayscale value.
	float v = dot(col, vec3(0.3333,0.3333,0.3333));
	return vec3(v,v,v);
}


void main()
{
	vec3 res = texture2D(s_p,vTex).xyz;
	res = mix(grayscale(res), res, saturation); // Apply saturation
	res = pow(res, vec3(gamma,gamma,gamma)); // Apply gamma
	oColor = vec4(clamp(res * luminance,0.0,1.0), 1.0);
}

#endif
