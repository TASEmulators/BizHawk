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

uniform float uIntensity;
uniform sampler2D s_p;

in vec2 vTex;

out vec4 oColor;

void main()
{
	vec4 temp = texture2D(s_p,vTex);
	vec2 wpos = gl_FragCoord.xy;
	if(floor(wpos.y/2.0) != floor(wpos.y)/2.0) temp.rgb *= uIntensity;
	oColor = temp; 
}

#endif