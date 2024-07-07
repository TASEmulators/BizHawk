#include "NDS.h"
#include "GPU.h"
#include "OpenGLSupport.h"
#include "frontend/FrontendUtil.h"

#include "BizGLPresenter.h"

#include <emulibc.h>

// half of this is taken from melonDS/src/frontend/qt_sdl/main.cpp

namespace Frontend
{
	extern float TouchMtx[6];
	extern float HybTouchMtx[6];
	extern bool BotEnable;
	extern bool HybEnable;
	extern int HybScreen;
	extern void M23_Transform(float* m, float& x, float& y);
}

namespace GLPresenter
{

constexpr u32 NDS_WIDTH = 256;
constexpr u32 NDS_HEIGHT = 384;

static const char* ScreenVS = R"(#version 140

uniform vec2 uScreenSize;
uniform mat2x3 uScreenTransform;

in vec2 vPosition;
in vec2 vTexcoord;

smooth out vec2 fTexcoord;

void main()
{
	vec4 fpos;

	fpos.xy = vec3(vPosition, 1.0) * uScreenTransform;

	fpos.xy = ((fpos.xy * 2.0) / uScreenSize) - 1.0;
	fpos.y *= -1;
	fpos.z = 0.0;
	fpos.w = 1.0;

	gl_Position = fpos;
	fTexcoord = vTexcoord;
}
)";

static const char* ScreenFS = R"(#version 140

uniform sampler2D ScreenTex;

smooth in vec2 fTexcoord;

out vec4 oColor;

void main()
{
	vec4 pixel = texture(ScreenTex, fTexcoord);

	oColor = vec4(pixel.bgr, 1.0);
}
)";

ECL_INVISIBLE static GLuint ScreenShaderProgram;
ECL_INVISIBLE static GLuint ScreenShaderTransformULoc, ScreenShaderSizeULoc;

ECL_INVISIBLE static GLuint VertexBuffer, VertexArray;

ECL_INVISIBLE static float ScreenMatrix[3 * 6];
ECL_INVISIBLE static int ScreenKinds[3];
ECL_INVISIBLE static int NumScreens;

ECL_INVISIBLE static u32 Width, Height;
ECL_INVISIBLE static u32 GLScale;

ECL_INVISIBLE static GLuint InputTextureID;
ECL_INVISIBLE static GLuint OutputTextureID;
ECL_INVISIBLE static GLuint OutputFboID;
ECL_INVISIBLE static GLuint OutputPboID;

void Init(u32 scale)
{
	Frontend::OpenGL::CompileVertexFragmentProgram(
		ScreenShaderProgram,
		ScreenVS, ScreenFS,
		"GLPresenterShader",
		{{"vPosition", 0}, {"vTexcoord", 1}},
		{{"oColor", 0}});

	glUseProgram(ScreenShaderProgram);
	glUniform1i(glGetUniformLocation(ScreenShaderProgram, "ScreenTex"), 0);

	ScreenShaderSizeULoc = glGetUniformLocation(ScreenShaderProgram, "uScreenSize");
	ScreenShaderTransformULoc = glGetUniformLocation(ScreenShaderProgram, "uScreenTransform");

	constexpr int paddedHeight = NDS_HEIGHT + 2;
	constexpr float padPixels = 1.f / paddedHeight;

	static const float vertices[] =
	{
		0.f,   0.f,    0.f, 0.f,
		0.f,   192.f,  0.f, 0.5f - padPixels,
		256.f, 192.f,  1.f, 0.5f - padPixels,
		0.f,   0.f,    0.f, 0.f,
		256.f, 192.f,  1.f, 0.5f - padPixels,
		256.f, 0.f,    1.f, 0.f,

		0.f,   0.f,    0.f, 0.5f + padPixels,
		0.f,   192.f,  0.f, 1.f,
		256.f, 192.f,  1.f, 1.f,
		0.f,   0.f,    0.f, 0.5f + padPixels,
		256.f, 192.f,  1.f, 1.f,
		256.f, 0.f,    1.f, 0.5f + padPixels
	};

	glGenBuffers(1, &VertexBuffer);
	glBindBuffer(GL_ARRAY_BUFFER, VertexBuffer);
	glBufferData(GL_ARRAY_BUFFER, sizeof(vertices), vertices, GL_STATIC_DRAW);

	glGenVertexArrays(1, &VertexArray);
	glBindVertexArray(VertexArray);
	glEnableVertexAttribArray(0); // position
	glVertexAttribPointer(0, 2, GL_FLOAT, GL_FALSE, 4 * 4, (void*)(0));
	glEnableVertexAttribArray(1); // texcoord
	glVertexAttribPointer(1, 2, GL_FLOAT, GL_FALSE, 4 * 4, (void*)(2 * 4));

	glGenTextures(1, &InputTextureID);
	glActiveTexture(GL_TEXTURE0);
	glBindTexture(GL_TEXTURE_2D, InputTextureID);
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_NEAREST);
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_NEAREST);
	glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA8, NDS_WIDTH, paddedHeight, 0, GL_RGBA, GL_UNSIGNED_INT_8_8_8_8_REV, nullptr);
	static u32 zeroData[NDS_WIDTH * 2]{};
	glTexSubImage2D(GL_TEXTURE_2D, 0, 0, NDS_HEIGHT / 2, NDS_WIDTH, 2, GL_RGBA, GL_UNSIGNED_INT_8_8_8_8_REV, zeroData);

	glGenBuffers(1, &OutputPboID);

	GLScale = scale;
}

std::pair<u32, u32> Present(melonDS::GPU& gpu)
{
	glBindFramebuffer(GL_FRAMEBUFFER, OutputFboID);
	glDisable(GL_DEPTH_TEST);
	glDepthMask(false);
	glDisable(GL_BLEND);
	glDisable(GL_SCISSOR_TEST);
	glDisable(GL_STENCIL_TEST);
	glClear(GL_COLOR_BUFFER_BIT);

	glViewport(0, 0, Width, Height);

	glUseProgram(ScreenShaderProgram);
	glUniform2f(ScreenShaderSizeULoc, Width, Height);

	glActiveTexture(GL_TEXTURE0);

	auto& renderer3d = gpu.GetRenderer3D();
	if (renderer3d.Accelerated)
	{
		renderer3d.BindOutputTexture(gpu.FrontBuffer);
	}
	else
	{
		glBindTexture(GL_TEXTURE_2D, InputTextureID);
		glTexSubImage2D(GL_TEXTURE_2D, 0, 0, 0, NDS_WIDTH, NDS_HEIGHT / 2, GL_RGBA, GL_UNSIGNED_INT_8_8_8_8_REV, gpu.Framebuffer[gpu.FrontBuffer][0].get());
		glTexSubImage2D(GL_TEXTURE_2D, 0, 0, NDS_HEIGHT / 2 + 2, NDS_WIDTH, NDS_HEIGHT / 2, GL_RGBA, GL_UNSIGNED_INT_8_8_8_8_REV, gpu.Framebuffer[gpu.FrontBuffer][1].get());
	}

	glBindBuffer(GL_ARRAY_BUFFER, VertexBuffer);
	glBindVertexArray(VertexArray);

	for (int i = 0; i < NumScreens; i++)
	{
		glUniformMatrix2x3fv(ScreenShaderTransformULoc, 1, GL_TRUE, &ScreenMatrix[i * 6]);
		glDrawArrays(GL_TRIANGLES, ScreenKinds[i] == 0 ? 0 : 6, 6);
	}

	glFlush();

	glBindBuffer(GL_PIXEL_PACK_BUFFER, OutputPboID);
	glBufferData(GL_PIXEL_PACK_BUFFER, Width * Height * sizeof(u32), nullptr, GL_STREAM_READ);
	glReadPixels(0, 0, Width, Height, GL_BGRA, GL_UNSIGNED_INT_8_8_8_8_REV, (void*)(0));

	return std::make_pair(Width, Height);
}

ECL_EXPORT u32 GetGLTexture()
{
	return OutputTextureID;
}

ECL_EXPORT void ReadFrameBuffer(u32* buffer)
{
	glBindBuffer(GL_PIXEL_PACK_BUFFER, OutputPboID);
	const auto p = static_cast<const u32*>(glMapBuffer(GL_PIXEL_PACK_BUFFER, GL_READ_ONLY));
	if (p)
	{
		// FBOs render upside down, so flip vertically to counteract that
		buffer += Width * (Height - 1);
		const int w = Width;
		const int h = Height;
		for (int i = 0; i < h; i++)
		{
			std::memcpy(&buffer[-i * w], &p[i * w], Width * sizeof(u32));
		}

		glUnmapBuffer(GL_PIXEL_PACK_BUFFER);
	}
}

struct ScreenSettings
{
	Frontend::ScreenLayout ScreenLayout;
	Frontend::ScreenRotation ScreenRotation;
	Frontend::ScreenSizing ScreenSizing;
	int ScreenGap;
	bool ScreenSwap;
};

static std::pair<u32, u32> GetScreenSize(const ScreenSettings* screenSettings, u32 scale)
{
	bool isHori = screenSettings->ScreenRotation == Frontend::screenRot_90Deg
		|| screenSettings->ScreenRotation == Frontend::screenRot_270Deg;
	int gap = screenSettings->ScreenGap * scale;

	int w = NDS_WIDTH * scale;
	int h = (NDS_HEIGHT / 2) * scale;

	if (screenSettings->ScreenSizing == Frontend::screenSizing_TopOnly
		|| screenSettings->ScreenSizing == Frontend::screenSizing_BotOnly)
	{
		return isHori
			? std::make_pair(h, w)
			: std::make_pair(w, h);
	}

	switch (screenSettings->ScreenLayout)
	{
		case Frontend::screenLayout_Natural:
			return isHori
				? std::make_pair(h * 2 + gap, w)
				: std::make_pair(w, h * 2 + gap);
		case Frontend::screenLayout_Vertical:
			return isHori
				? std::make_pair(h, w * 2 + gap)
				: std::make_pair(w, h * 2 + gap);
		case Frontend::screenLayout_Horizontal:
			return isHori
				? std::make_pair(h * 2 + gap, w)
				: std::make_pair(w * 2 + gap, h);
		case Frontend::screenLayout_Hybrid:
			return isHori
				? std::make_pair(h * 2 + gap, w * 3 + (int)ceil(gap * 4 / 3.0))
				: std::make_pair(w * 3 + (int)ceil(gap * 4 / 3.0), h * 2 + gap);
		default:
			__builtin_unreachable();
	}
}

ECL_EXPORT void SetScreenSettings(melonDS::NDS* nds, const ScreenSettings* screenSettings, u32* width, u32* height, u32* vwidth, u32* vheight)
{
	auto [w, h] = GetScreenSize(screenSettings, GLScale);
	if (w != Width || h != Height)
	{
		Width = w;
		Height = h;

		glDeleteTextures(1, &OutputTextureID);
		glGenTextures(1, &OutputTextureID);
		glActiveTexture(GL_TEXTURE0);
		glBindTexture(GL_TEXTURE_2D, OutputTextureID);
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_NEAREST);
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_NEAREST);
		glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA8, Width, Height, 0, GL_RGBA, GL_UNSIGNED_INT_8_8_8_8_REV, nullptr);

		glDeleteFramebuffers(1, &OutputFboID);
		glGenFramebuffers(1, &OutputFboID);
		glBindFramebuffer(GL_FRAMEBUFFER, OutputFboID);
		glFramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_2D, OutputTextureID, 0);
		glDrawBuffer(GL_COLOR_ATTACHMENT0);
	}

	Frontend::SetupScreenLayout(w, h,
		screenSettings->ScreenLayout,
		screenSettings->ScreenRotation,
		screenSettings->ScreenSizing,
		screenSettings->ScreenGap,
		true, // Integer Scaling
		screenSettings->ScreenSwap,
		1, 1); // Aspect Ratio

	NumScreens = Frontend::GetScreenTransforms(ScreenMatrix, ScreenKinds);

	Present(nds->GPU);

	*width = w;
	*height = h;

	if (GLScale > 1)
	{
		auto [vw, vh] = GetScreenSize(screenSettings, 1);
		*vwidth = vw;
		*vheight = vh;
	}
	else
	{
		*vwidth = w;
		*vheight = h;
	}
}

ECL_EXPORT void GetTouchCoords(int* x, int* y)
{
	float vx = *x;
	float vy = *y;

	if (Frontend::HybEnable && Frontend::HybScreen == 1)
	{
		Frontend::M23_Transform(Frontend::HybTouchMtx, vx, vy);
	}
	else
	{
		Frontend::M23_Transform(Frontend::TouchMtx, vx, vy);
	}

	*x = vx;
	*y = vy;

	if (!Frontend::BotEnable)
	{
		// top screen only, offset y to account for that
		*y -= 192;
	}
}

ECL_EXPORT void GetScreenCoords(float* x, float* y)
{
	for (int i = NumScreens - 1; i >= 0; i--)
	{
		// bottom screen
		if (ScreenKinds[i] == 1)
		{
			Frontend::M23_Transform(&ScreenMatrix[i * 6], *x, *y);
			return;
		}
	}

	// top screen only, offset y to account for that
	*y += 192;

	for (int i = 0; i < NumScreens; i++)
	{
		// top screen
		if (ScreenKinds[i] == 0)
		{
			Frontend::M23_Transform(&ScreenMatrix[i * 6], *x, *y);
			return;
		}
	}
}

}
