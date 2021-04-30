#ifndef opengl_compat_h
#define opengl_compat_h

#define GL_GLEXT_PROTOTYPES
#include <SDL_opengl.h>
#include <SDL_video.h>

#ifndef __APPLE__
#define GL_COMPAT_NAME(func) gl_compat_##func

#define GL_COMPAT_WRAPPER(func) \
({  extern typeof(func) *GL_COMPAT_NAME(func); \
if (!GL_COMPAT_NAME(func)) GL_COMPAT_NAME(func) = SDL_GL_GetProcAddress(#func); \
    GL_COMPAT_NAME(func); \
})

#define glCreateShader GL_COMPAT_WRAPPER(glCreateShader)
#define glGetAttribLocation GL_COMPAT_WRAPPER(glGetAttribLocation)
#define glGetUniformLocation GL_COMPAT_WRAPPER(glGetUniformLocation)
#define glUseProgram GL_COMPAT_WRAPPER(glUseProgram)
#define glGenVertexArrays GL_COMPAT_WRAPPER(glGenVertexArrays)
#define glBindVertexArray GL_COMPAT_WRAPPER(glBindVertexArray)
#define glGenBuffers GL_COMPAT_WRAPPER(glGenBuffers)
#define glBindBuffer GL_COMPAT_WRAPPER(glBindBuffer)
#define glBufferData GL_COMPAT_WRAPPER(glBufferData)
#define glEnableVertexAttribArray GL_COMPAT_WRAPPER(glEnableVertexAttribArray)
#define glVertexAttribPointer GL_COMPAT_WRAPPER(glVertexAttribPointer)
#define glCreateProgram GL_COMPAT_WRAPPER(glCreateProgram)
#define glAttachShader GL_COMPAT_WRAPPER(glAttachShader)
#define glLinkProgram GL_COMPAT_WRAPPER(glLinkProgram)
#define glGetProgramiv GL_COMPAT_WRAPPER(glGetProgramiv)
#define glGetProgramInfoLog GL_COMPAT_WRAPPER(glGetProgramInfoLog)
#define glDeleteShader GL_COMPAT_WRAPPER(glDeleteShader)
#define glUniform2f GL_COMPAT_WRAPPER(glUniform2f)
#define glActiveTexture GL_COMPAT_WRAPPER(glActiveTexture)
#define glUniform1i GL_COMPAT_WRAPPER(glUniform1i)
#define glBindFragDataLocation GL_COMPAT_WRAPPER(glBindFragDataLocation)
#define glDeleteProgram GL_COMPAT_WRAPPER(glDeleteProgram)
#define glShaderSource GL_COMPAT_WRAPPER(glShaderSource)
#define glCompileShader GL_COMPAT_WRAPPER(glCompileShader)
#define glGetShaderiv GL_COMPAT_WRAPPER(glGetShaderiv)
#define glGetShaderInfoLog GL_COMPAT_WRAPPER(glGetShaderInfoLog)
#endif

#endif /* opengl_compat_h */
