#if PROFILE_D3D9
	#define SAMPLE2D(tex, samp, uv) tex2D(samp, uv)
	#define VP_APPLY_VIEWPORT(hpos) hpos.xy = hpos.xy * vd_viewport.xy + vd_viewport.zw * hpos.w
#else
	#define SAMPLE2D(tex, samp, uv) tex.Sample(samp, uv)
	#define VP_APPLY_VIEWPORT(hpos) (0)
#endif

// system variables
extern float4 vd_viewport : register(vs, c0);
