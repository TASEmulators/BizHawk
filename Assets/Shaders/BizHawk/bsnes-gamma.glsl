//https://raw.githubusercontent.com/Themaister/Emulator-Shader-Pack/master/Cg/TV/gamma.cg

/*
   Author: Themaister
   License: Public domain
*/

// Shader that replicates gamma-ramp of bSNES/Higan.

#ifdef VERTEX
uniform mat4 modelViewProj;

void main()
{
	gl_Position = modelViewProj * gl_Vertex;
	gl_TexCoord[0] = gl_MultiTexCoord0;
}

#endif

#ifdef FRAGMENT

uniform sampler2D s_p;

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
	vec3 res = texture2D(s_p,gl_TexCoord[0].xy).xyz;
	res = mix(grayscale(res), res, saturation); // Apply saturation
	res = pow(res, vec3(gamma,gamma,gamma)); // Apply gamma
	gl_FragColor = vec4(clamp(res * luminance,0.0,1.0), 1.0);
}

#endif
