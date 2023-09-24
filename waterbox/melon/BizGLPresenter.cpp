#include "GPU.h"
#include "OpenGLSupport.h"
#include "frontend/FrontendUtil.h"

#include "BizGLPresenter.h"

#include <emulibc.h>

// half of this is taken from melonDS/src/frontend/qt_sdl/main.cpp

namespace GLPresenter
{

static const char* VertexShader = R"(#version 140

uniform vec2 uSize;
uniform mat2x3 uTransform;

in vec2 vPosition;
in vec2 vTexcoord;

smooth out vec2 fTexcoord;

void main()
{
	vec4 fpos;

	fpos.xy = vec3(vPosition, 1.0) * uTransform;

	fpos.xy = ((fpos.xy * 2.0) / uSize) - 1.0;
	// fpos.y *= -1;
	fpos.z = 0.0;
	fpos.w = 1.0;

	gl_Position = fpos;
	fTexcoord = vTexcoord;
}
)";

static const char* FragmentShader = R"(#version 140

uniform sampler2D Tex;

smooth in vec2 fTexcoord;

out vec4 oColor;

void main()
{
	vec4 pixel = texture(Tex, fTexcoord);

	oColor = vec4(pixel.bgr, 1.0);
}
)";


ECL_INVISIBLE static GLuint ShaderProgram[3];
ECL_INVISIBLE static GLuint ShaderTransformULoc, ShaderSizeULoc;

ECL_INVISIBLE static GLuint VertexBuffer, VertexArray;

ECL_INVISIBLE static float TransformMatrix[2 * 6];

ECL_INVISIBLE static u32 Width, Height;

ECL_INVISIBLE static GLuint TextureID;
ECL_INVISIBLE static GLuint FboID;
ECL_INVISIBLE static GLuint PboID;

void Init(u32 scale)
{
	Width = 256 * scale;
	Height = 384 * scale;

	OpenGL::BuildShaderProgram(VertexShader, FragmentShader, ShaderProgram, "GLPresenterShader");

	GLuint pid = ShaderProgram[2];
	glBindAttribLocation(pid, 0, "vPosition");
	glBindAttribLocation(pid, 1, "vTexcoord");
	glBindFragDataLocation(pid, 0, "oColor");

	OpenGL::LinkShaderProgram(ShaderProgram);

	glUseProgram(pid);
	glUniform1i(glGetUniformLocation(pid, "Tex"), 0);

	ShaderSizeULoc = glGetUniformLocation(pid, "uSize");
	ShaderTransformULoc = glGetUniformLocation(pid, "uTransform");

	constexpr int paddedHeight = 192 * 2 + 2;
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

	// TODO: Could we use this instead of all the screen transforming code in the frontend?
	// see https://github.com/TASEmulators/BizHawk/issues/3772
	Frontend::SetupScreenLayout(Width, Height, Frontend::screenLayout_Natural, Frontend::screenRot_0Deg, Frontend::screenSizing_Even, 0, true, false, 1, 1);
	int discard[2];
	Frontend::GetScreenTransforms(TransformMatrix, discard);

	glGenTextures(1, &TextureID);
	glActiveTexture(GL_TEXTURE0);
	glBindTexture(GL_TEXTURE_2D, TextureID);
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_NEAREST);
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_NEAREST);
	glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA, Width, Height, 0, GL_RGBA, GL_UNSIGNED_BYTE, nullptr);
	glBindTexture(GL_TEXTURE_2D, 0);

	glGenFramebuffers(1, &FboID);
	glBindFramebuffer(GL_FRAMEBUFFER, FboID);
	glFramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_2D, TextureID, 0);
	glDrawBuffer(GL_COLOR_ATTACHMENT0);

	glGenBuffers(1, &PboID);
}

std::pair<u32, u32> Present(bool filter)
{
	glBindFramebuffer(GL_FRAMEBUFFER, FboID);
	glDisable(GL_DEPTH_TEST);
	glDepthMask(false);
	glDisable(GL_BLEND);
	glDisable(GL_SCISSOR_TEST);
	glDisable(GL_STENCIL_TEST);
	glClear(GL_COLOR_BUFFER_BIT);

	glViewport(0, 0, Width, Height);

	glUseProgram(ShaderProgram[2]);
	glUniform2f(ShaderSizeULoc, Width, Height);

	glActiveTexture(GL_TEXTURE0);
	GPU::CurGLCompositor->BindOutputTexture(GPU::FrontBuffer);

	GLint texFilter = filter ? GL_LINEAR : GL_NEAREST;
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, texFilter);
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, texFilter);

	glBindBuffer(GL_ARRAY_BUFFER, VertexBuffer);
	glBindVertexArray(VertexArray);

	glUniformMatrix2x3fv(ShaderTransformULoc, 1, GL_TRUE, &TransformMatrix[0]);
	glDrawArrays(GL_TRIANGLES, 0, 6);

	glUniformMatrix2x3fv(ShaderTransformULoc, 1, GL_TRUE, &TransformMatrix[6]);
	glDrawArrays(GL_TRIANGLES, 6, 6);

	glFlush();

	glBindBuffer(GL_PIXEL_PACK_BUFFER, PboID);
	glBufferData(GL_PIXEL_PACK_BUFFER, Width * Height * sizeof(u32), nullptr, GL_STREAM_READ);
	glReadPixels(0, 0, Width, Height, GL_BGRA, GL_UNSIGNED_INT_8_8_8_8_REV, static_cast<void*>(0));

	return std::make_pair(Width, Height);
}

ECL_EXPORT u32 GetGLTexture()
{
	return TextureID;
}

ECL_EXPORT void ReadFrameBuffer(u32* buffer)
{
	glBindBuffer(GL_PIXEL_PACK_BUFFER, PboID);
	const auto p = static_cast<const u32*>(glMapBuffer(GL_PIXEL_PACK_BUFFER, GL_READ_ONLY));
	if (p) {
		// FBOs render upside down, so flip vertically to counteract that
		buffer += Width * (Height - 1);
		const int w = Width;
		const int h = Height;
		for (int i = 0; i < h; i++) {
			std::memcpy(&buffer[-i * w], &p[i * w], Width * sizeof(u32));
		}

		glUnmapBuffer(GL_PIXEL_PACK_BUFFER);
	}
}

}
