////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////
////
////    VERTEX SHADER : STOLEN FROM NEIGHBOURING SHADERS
////
////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////

//Yeah, I'm sorry this uses really old non-generic attributes
//that's just how old this code is; support on ancient graphics cards is helpful

#ifdef VERTEX

uniform mat4 modelViewProj;

void main()
{
	gl_Position = modelViewProj * gl_Vertex;
	gl_TexCoord[0] = gl_MultiTexCoord0;
}

#endif //VERTEX

#ifdef FRAGMENT

uniform struct
{
	vec2 video_size;
	vec2 texture_size;
	vec2 output_size;
} IN;

uniform float Time;

uniform sampler2D s_p;

float saturate(float x)
{
  return max(0, min(1, x));
}

vec2 saturate(vec2 x)
{
  return max(vec2(0.0,0.0), min(vec2(1.0,1.0), x));
}

////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////
////    EFFECT CONSTANTS : TWEAK THEM!
////

		// Size of the border effect
		const vec2 OverscanMaskHardness = vec2(12.0f ,12.0f );
		// Attenuation of the border effect
		const float OverscanMaskPower = 4.0f;
		// Intensity of the border effect
		const float OverscanIntensity = 0.96f;
		
		// Intensity of the TV Corners (round-ness) deformation 
		const float TVDeformInstensity = 0.02f;
		
		
		// How much R, G and B are offset : default is -0.333 pixels in fake-pixel-space
		const float ColorFringeIntensity = -0.666;
		// How much luminosity is output by a fake-pixel
		const float FakePixelMaskGain = 0.75f;
		// How much luminosity is output between fake-pixels (adds to the fake-pixel value)
		const float FakePixelMaskOffset = 0.55f;
		// How sharp will appear the pixels (Horizontal Sharpness, Vertical Sharpness A.K.A Scanlines)
		const vec2 FakePixelMaskPower = vec2(0.150f ,2.0f );
		// Scanline Off Sync (Slides one line out of two)
		const float ScanlineOffSync = 0.25;
		// Base Brightness
		const float BaseBrightness = 0.55f;
		
		// How much the Fake-Pixel effect is Active (0.0 = normal image, 1.0 = full FakePixel Effect)
		const float FakePixelEffectBlend = 0.95f;		

		// Ghost Sampling : enable define to activate
		#define GHOST_SAMPLING;

			const float GhostLatencyIntensity = 0.03f;
			// Number of samples (higer is slower)
			const int GhostNumSamples = 32;
			// Latency of the RGB Signal (per-signal, in screen width percentage)
			const vec3 SignalLatencyRGB = vec3(0.184f,0.08f,0.0624f);
			// Attenuation of the ghosting latency
			const float SignalLatencyAttenuation = 1.0f;

		// Bloom : enable define to activate
		#define BLOOM;
			const float BloomIntensity = 0.75f;
			const float BloomExponent = 1.00f;
			const float BloomWeights[25] = float[] (
				0.003765,	0.015019,	0.023792,	0.015019,	0.003765,
				0.015019,	0.059912,	0.094907,	0.059912,	0.015019,
				0.023792,	0.094907,	0.150342,	0.094907,	0.023792,
				0.015019,	0.059912,	0.094907,	0.059912,	0.015019,
				0.003765,	0.015019,	0.023792,	0.015019,	0.003765
			);

			const float BloomPositions[5] = float[] ( -2, -1, 0 , 1 , 2 );

////
////
////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////

float expow(float value, float exponent) {
	return mix(1.0f,pow(value,max(exponent,1.0f)),saturate(exponent));
}

//the code that calls expow() carefully builds vec2 for some reason and calls this only to have it implicitly thrown away (which is a warning)
//so this was added to get rid of the warning
float expow(vec2 value, vec2 exponent) {
	return mix(1.0f,pow(value.x,max(exponent.x,1.0f)),saturate(exponent.x));
}
float expow(vec2 value, float exponent) {
	return mix(1.0f,pow(value.x,max(exponent,1.0f)),saturate(exponent));
}

// MultiSampling for ghosting effect
vec3 GhostSample(vec2 t, float latency) {

	vec3 Out = texture2D(s_p,t).rgb;
	float Weight = 1.0f;
	vec2 Direction = vec2(-latency,0.0f);
	for(int i=1; i < GhostNumSamples; i++) {
		float curweight = pow(1.0f-(float(i)/GhostNumSamples),1.0f/SignalLatencyAttenuation);

		Out += GhostLatencyIntensity * curweight * texture2D(s_p,saturate(t+(1.0f-curweight)*Direction)).xyz;
		Weight += GhostLatencyIntensity * curweight;
	}
	return Out/Weight;
}

// MultiSampling for ghosting effect
vec3 Bloom(vec2 t, vec2 r) {

	vec3 Out = vec3(0,0,0);
	for(int j = 0; j < 5; j++)
		for(int i = 0; i < 5; i++)
		{
			vec2 offset = vec2(BloomPositions[i],BloomPositions[j]) / r;
			Out += texture2D(s_p, t + offset).rgb * BloomWeights[i*5+j];
		}
	return pow(Out, vec3(BloomExponent,BloomExponent,BloomExponent)) * BloomIntensity;
}

// Compositing of the TV Emulation
vec3 TVEffect(vec2 in_Position, vec2 FakeResolution, float Time) {

	// TV Deformation
	vec2 ScreenPos = in_Position + dot(in_Position-0.5f,in_Position-0.5f)*(in_Position-0.5f)* TVDeformInstensity;

	// Apply Off-Sync
	ScreenPos += (ScanlineOffSync/FakeResolution.x) * vec2(sin((Time*30*3.1415926)+(ScreenPos.y*3.1415926*FakeResolution.y)),0);

	// Sampling 3 Images biased to simulate TV RGB Offset
	#ifdef GHOST_SAMPLING 
		vec3 latencyweight = vec3(0.0f,0.0f,0.0f);
		for(int i=1; i < GhostNumSamples; i++) {
			latencyweight += texture2D(s_p, ScreenPos + vec2(1.0f/FakeResolution.x,0.0f)).xyz;
		}	
		vec3 LatencyRGB = SignalLatencyRGB * (1.0-(latencyweight/GhostNumSamples));

		vec3 SMP_Red = GhostSample((ScreenPos),LatencyRGB.x).xyz;
		vec3 SMP_Green = GhostSample((ScreenPos) + ((vec2(ColorFringeIntensity,0.0f))/FakeResolution),LatencyRGB.y).xyz;
		vec3 SMP_Blue = GhostSample((ScreenPos) + ((vec2(ColorFringeIntensity*2.0f,0.0f))/FakeResolution),LatencyRGB.z).xyz;
	#else
		vec3 SMP_Red = texture2D(s_p, (ScreenPos)).xyz;
		vec3 SMP_Green = texture2D(s_p, (ScreenPos) + ((vec2(ColorFringeIntensity,0.0f))/FakeResolution)).xyz;
		vec3 SMP_Blue = texture2D(s_p, (ScreenPos) + ((vec2(ColorFringeIntensity*2.0f,0.0f))/FakeResolution)).xyz;
	#endif

	#ifdef BLOOM
		vec3 bloom = Bloom(ScreenPos, FakeResolution);
		SMP_Red += bloom.r;
		SMP_Green += bloom.g;
		SMP_Blue += bloom.b;
	#endif

	// Apply base Brightness
	SMP_Red *= BaseBrightness;
	SMP_Green *= BaseBrightness;
	SMP_Blue *= BaseBrightness;

	// Overscan Darkening Mask
	float ScreenMaskPower = 1.0f/OverscanMaskPower;
	vec2 ScreenMask = pow(saturate(ScreenPos*(1.0f-ScreenPos)*OverscanMaskHardness),vec2(ScreenMaskPower,ScreenMaskPower));
	float mask = mix(1.0, ScreenMask.x * ScreenMask.y, OverscanIntensity);
	
	// CRT Cell Masks (HorizontalRGB+Scanline)
	float PixelMaskR = expow(saturate(4*fract(ScreenPos.x*FakeResolution.x)*(1.0f-fract(ScreenPos.x*FakeResolution.x))),FakePixelMaskPower.x);
	float PixelMaskG = expow(saturate(4*fract(ScreenPos.x*FakeResolution.x+vec2(ColorFringeIntensity,0.0f))*(1.0f-fract(ScreenPos.x*FakeResolution.x+vec2(ColorFringeIntensity,0.0f)))),FakePixelMaskPower.x);
	float PixelMaskB = expow(saturate(4*fract(ScreenPos.x*FakeResolution.x+vec2(ColorFringeIntensity*2.0f,0.0f))*(1.0f-fract(ScreenPos.x*FakeResolution.x+vec2(ColorFringeIntensity*2.0f,0.0f)))),FakePixelMaskPower.x);
	float PixelMaskScanline = pow(saturate(4*fract(ScreenPos.y*FakeResolution.y)*(1.0f-fract(ScreenPos.y*FakeResolution.y))),FakePixelMaskPower.y);

	vec3 PixelRGB = vec3 ( 
								((PixelMaskR*PixelMaskScanline * FakePixelMaskGain)+FakePixelMaskOffset)  * SMP_Red.x ,
								((PixelMaskG*PixelMaskScanline * FakePixelMaskGain)+FakePixelMaskOffset)  * SMP_Green.y ,
								((PixelMaskB*PixelMaskScanline * FakePixelMaskGain)+FakePixelMaskOffset)  * SMP_Blue.z
								);

	// Non-Pixelated Image
	vec3 ImageRGB = texture2D(s_p, ScreenPos).xyz;
	return mix(ImageRGB, PixelRGB, FakePixelEffectBlend) * mask;

}

void main()
{
	vec4 color = vec4(1.0f,1.0f,1.0f,1.0f);
	color.xyz = TVEffect(gl_TexCoord[0].xy, IN.texture_size, Time);
	gl_FragColor = color;
}

#endif