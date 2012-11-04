#ifndef __DRIVERS_SHADER_H
#define __DRIVERS_SHADER_H

typedef enum {
	SHADER_NONE = 0,
	SHADER_SCALE2X,
        SHADER_AUTOIP,
	SHADER_AUTOIPSHARPER,
	SHADER_IPSHARPER,
        SHADER_IPXNOTY,
        SHADER_IPXNOTYSHARPER,
        SHADER_IPYNOTX,
        SHADER_IPYNOTXSHARPER,
} ShaderType;


bool InitShader(ShaderType pixshader);
bool ShaderBegin(const MDFN_Rect *rect, const MDFN_Rect *dest_rect, int tw, int th);
bool ShaderEnd(void);
bool KillShader(void);

#endif
