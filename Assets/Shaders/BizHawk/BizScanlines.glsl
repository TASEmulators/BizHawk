#ifdef VERTEX
uniform mat4 MVPMatrix;

in vec4 VertexCoord;
in vec2 TexCoord;

out vec2 vTex;

void main()
{
	gl_Position = MVPMatrix * VertexCoord;
	vTex = TexCoord;
}

#endif

#ifdef FRAGMENT

uniform float uIntensity;
uniform sampler2D s_p;

in vec2 vTex;

out vec4 FragColor;

void main()
{
	vec4 temp = texture(s_p,vTex);
	vec2 wpos = gl_FragCoord.xy;
	if(floor(wpos.y/2.0) != floor(wpos.y)/2.0) temp.rgb *= uIntensity;
	FragColor = temp; 
}

#endif