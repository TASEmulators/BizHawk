//Yeah, I'm sorry this uses really old non-generic attributes
//that's just how old this code is; support on ancient graphics cards is helpful

uniform struct
{
	vec2 video_size;
	vec2 texture_size;
	vec2 output_size;
} IN;

#ifdef VERTEX
uniform mat4 modelViewProj;

void main()
{
	gl_Position = modelViewProj * gl_Vertex;

	vec2 texsize = IN.texture_size;
	vec2 delta = 0.5 / texsize;
	float dx = delta.x;
	float dy = delta.y;

	gl_TexCoord[0].xy = gl_MultiTexCoord0.xy + vec2(-dx, -dy);
	gl_TexCoord[1].xy = gl_MultiTexCoord0.xy + vec2(-dx, 0.0);
	gl_TexCoord[2].xy = gl_MultiTexCoord0.xy + vec2(-dx, dy);
	gl_TexCoord[3].xy = gl_MultiTexCoord0.xy + vec2(0.0, -dy);
	gl_TexCoord[4].xy = gl_MultiTexCoord0.xy + vec2(0.0, 0.0);
	gl_TexCoord[5].xy = gl_MultiTexCoord0.xy + vec2(0.0, dy);
	gl_TexCoord[6].xy = gl_MultiTexCoord0.xy + vec2(dx, -dy);
	gl_TexCoord[7].xy = gl_MultiTexCoord0.xy + vec2(dx, 0);
	gl_TexCoord[7].zw = gl_MultiTexCoord0.xy + vec2(dx, dy);
}

#endif

#ifdef FRAGMENT

uniform sampler2D s_p;

const float mx = 0.325;      // start smoothing wt.
const float k = -0.250;      // wt. decrease factor
const float max_w = 0.25;    // max filter weigth
const float min_w = -0.05;    // min filter weigth
const float lum_add = 0.25;  // effects smoothing

void main()
{
	vec3 c00 = texture2D(s_p, gl_TexCoord[0].xy).xyz;
	vec3 c01 = texture2D(s_p, gl_TexCoord[1].xy).xyz;
	vec3 c02 = texture2D(s_p, gl_TexCoord[2].xy).xyz;
	vec3 c10 = texture2D(s_p, gl_TexCoord[3].xy).xyz;
	vec3 c11 = texture2D(s_p, gl_TexCoord[4].xy).xyz;
	vec3 c12 = texture2D(s_p, gl_TexCoord[5].xy).xyz;
	vec3 c20 = texture2D(s_p, gl_TexCoord[6].xy).xyz;
	vec3 c21 = texture2D(s_p, gl_TexCoord[7].xy).xyz;
	vec3 c22 = texture2D(s_p, gl_TexCoord[7].zw).xyz;
	vec3 dt = vec3(1.0,1.0,1.0);

	float md1 = dot(abs(c00 - c22), dt);
	float md2 = dot(abs(c02 - c20), dt);

	float w1 = dot(abs(c22 - c11), dt) * md2;
	float w2 = dot(abs(c02 - c11), dt) * md1;
	float w3 = dot(abs(c00 - c11), dt) * md2;
	float w4 = dot(abs(c20 - c11), dt) * md1;

	float t1 = w1 + w3;
	float t2 = w2 + w4;
	float ww = max(t1, t2) + 0.0001;

	c11 = (w1 * c00 + w2 * c20 + w3 * c22 + w4 * c02 + ww * c11) / (t1 + t2 + ww);

	float lc1 = k / (0.12 * dot(c10 + c12 + c11, dt) + lum_add);
	float lc2 = k / (0.12 * dot(c01 + c21 + c11, dt) + lum_add);

	w1 = clamp(lc1 * dot(abs(c11 - c10), dt) + mx, min_w, max_w);
	w2 = clamp(lc2 * dot(abs(c11 - c21), dt) + mx, min_w, max_w);
	w3 = clamp(lc1 * dot(abs(c11 - c12), dt) + mx, min_w, max_w);
	w4 = clamp(lc2 * dot(abs(c11 - c01), dt) + mx, min_w, max_w);

	gl_FragColor = vec4(w1 * c10 + w2 * c21 + w3 * c12 + w4 * c01 + (1.0 - w1 - w2 - w3 - w4) * c11, 1.0);
}

#endif