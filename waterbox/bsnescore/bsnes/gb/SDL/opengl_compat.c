#define GL_GLEXT_PROTOTYPES
#include <SDL_opengl.h>

#ifndef __APPLE__
#define GL_COMPAT_NAME(func) gl_compat_##func
#define GL_COMPAT_VAR(func) typeof(func) *GL_COMPAT_NAME(func)

GL_COMPAT_VAR(glCreateShader);
GL_COMPAT_VAR(glGetAttribLocation);
GL_COMPAT_VAR(glGetUniformLocation);
GL_COMPAT_VAR(glUseProgram);
GL_COMPAT_VAR(glGenVertexArrays);
GL_COMPAT_VAR(glBindVertexArray);
GL_COMPAT_VAR(glGenBuffers);
GL_COMPAT_VAR(glBindBuffer);
GL_COMPAT_VAR(glBufferData);
GL_COMPAT_VAR(glEnableVertexAttribArray);
GL_COMPAT_VAR(glVertexAttribPointer);
GL_COMPAT_VAR(glCreateProgram);
GL_COMPAT_VAR(glAttachShader);
GL_COMPAT_VAR(glLinkProgram);
GL_COMPAT_VAR(glGetProgramiv);
GL_COMPAT_VAR(glGetProgramInfoLog);
GL_COMPAT_VAR(glDeleteShader);
GL_COMPAT_VAR(glUniform2f);
GL_COMPAT_VAR(glActiveTexture);
GL_COMPAT_VAR(glUniform1i);
GL_COMPAT_VAR(glBindFragDataLocation);
GL_COMPAT_VAR(glDeleteProgram);
GL_COMPAT_VAR(glShaderSource);
GL_COMPAT_VAR(glCompileShader);
GL_COMPAT_VAR(glGetShaderiv);
GL_COMPAT_VAR(glGetShaderInfoLog);
#endif
