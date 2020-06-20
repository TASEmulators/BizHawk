//Yeah, I'm sorry this uses really old non-generic attributes
//that's just how old this code is; support on ancient graphics cards is helpful

#ifdef VERTEX
uniform mat4 modelViewProj;

void main()
{
	gl_Position = modelViewProj * gl_Vertex;
	gl_TexCoord[0] = gl_MultiTexCoord0;
}

#endif

#ifdef FRAGMENT

uniform float uIntensity;
uniform sampler2D s_p;

uniform vec2 output_size;

void main()
{
	vec2 vTex = gl_TexCoord[0].xy;
	vec4 temp = texture2D(s_p,vTex);
	vec2 wpos = gl_FragCoord.xy;
	if(floor(wpos.y/2.0) != floor(wpos.y)/2.0) temp.rgb *= uIntensity;
	gl_FragColor = temp; 
}

#endif