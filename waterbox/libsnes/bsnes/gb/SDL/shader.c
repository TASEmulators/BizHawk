#include <stdio.h>
#include <string.h>
#include "shader.h"
#include "utils.h"

static const char *vertex_shader = "\n\
#version 150 \n\
in vec4 aPosition;\n\
void main(void) {\n\
gl_Position = aPosition;\n\
}\n\
";

static GLuint create_shader(const char *source, GLenum type)
{
    // Create the shader object
    GLuint shader = glCreateShader(type);
    // Load the shader source
    glShaderSource(shader, 1, &source, 0);
    // Compile the shader
    glCompileShader(shader);
    // Check for errors
    GLint status = 0;
    glGetShaderiv(shader, GL_COMPILE_STATUS, &status);
    if (status == GL_FALSE) {
        GLchar messages[1024];
        glGetShaderInfoLog(shader, sizeof(messages), 0, &messages[0]);
        fprintf(stderr, "GLSL Shader Error: %s", messages);
    }
    return shader;
}

static GLuint create_program(const char *vsh, const char *fsh)
{
    // Build shaders
    GLuint vertex_shader = create_shader(vsh, GL_VERTEX_SHADER);
    GLuint fragment_shader = create_shader(fsh, GL_FRAGMENT_SHADER);
    
    // Create program
    GLuint program = glCreateProgram();
    
    // Attach shaders
    glAttachShader(program, vertex_shader);
    glAttachShader(program, fragment_shader);
    
    // Link program
    glLinkProgram(program);
    // Check for errors
    GLint status;
    glGetProgramiv(program, GL_LINK_STATUS, &status);
    
    if (status == GL_FALSE) {
        GLchar messages[1024];
        glGetProgramInfoLog(program, sizeof(messages), 0, &messages[0]);
        fprintf(stderr, "GLSL Program Error: %s", messages);
    }
    
    // Delete shaders
    glDeleteShader(vertex_shader);
    glDeleteShader(fragment_shader);
    
    return program;
}

bool init_shader_with_name(shader_t *shader, const char *name)
{
    GLint major = 0, minor = 0;
    glGetIntegerv(GL_MAJOR_VERSION, &major);
    glGetIntegerv(GL_MINOR_VERSION, &minor);
    
    if (major * 0x100 + minor < 0x302) {
        return false;
    }
    
    static char master_shader_code[0x801] = {0,};
    static char shader_code[0x10001] = {0,};
    static char final_shader_code[0x10801] = {0,};
    static ssize_t filter_token_location = 0;
    
    if (!master_shader_code[0]) {
        FILE *master_shader_f = fopen(resource_path("Shaders/MasterShader.fsh"), "r");
        if (!master_shader_f) return false;
        fread(master_shader_code, 1, sizeof(master_shader_code) - 1, master_shader_f);
        fclose(master_shader_f);
        filter_token_location = strstr(master_shader_code, "{filter}") - master_shader_code;
        if (filter_token_location < 0) {
            master_shader_code[0] = 0;
            return false;
        }
    }
    
    char shader_path[1024];
    sprintf(shader_path, "Shaders/%s.fsh", name);
    
    FILE *shader_f = fopen(resource_path(shader_path), "r");
    if (!shader_f) return false;
    memset(shader_code, 0, sizeof(shader_code));
    fread(shader_code, 1, sizeof(shader_code) - 1, shader_f);
    fclose(shader_f);
    
    memset(final_shader_code, 0, sizeof(final_shader_code));
    memcpy(final_shader_code, master_shader_code, filter_token_location);
    strcpy(final_shader_code + filter_token_location, shader_code);
    strcat(final_shader_code + filter_token_location,
           master_shader_code + filter_token_location + sizeof("{filter}") - 1);
    
    shader->program = create_program(vertex_shader, final_shader_code);
    
    // Attributes
    shader->position_attribute = glGetAttribLocation(shader->program, "aPosition");
    // Uniforms
    shader->resolution_uniform = glGetUniformLocation(shader->program, "output_resolution");
    shader->origin_uniform = glGetUniformLocation(shader->program, "origin");

    glGenTextures(1, &shader->texture);
    glBindTexture(GL_TEXTURE_2D, shader->texture);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_NEAREST);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_NEAREST);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);
    glBindTexture(GL_TEXTURE_2D, 0);
    shader->texture_uniform = glGetUniformLocation(shader->program, "image");
    
    glGenTextures(1, &shader->previous_texture);
    glBindTexture(GL_TEXTURE_2D, shader->previous_texture);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_NEAREST);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_NEAREST);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);
    glBindTexture(GL_TEXTURE_2D, 0);
    shader->previous_texture_uniform = glGetUniformLocation(shader->program, "previous_image");
    
    shader->blending_mode_uniform = glGetUniformLocation(shader->program, "frame_blending_mode");
    
    // Program
    
    glUseProgram(shader->program);
    
    GLuint vao;
    glGenVertexArrays(1, &vao);
    glBindVertexArray(vao);
    
    GLuint vbo;
    glGenBuffers(1, &vbo);
    
    // Attributes
    
    
    static GLfloat const quad[16] = {
        -1.f, -1.f, 0, 1,
        -1.f, +1.f, 0, 1,
        +1.f, -1.f, 0, 1,
        +1.f, +1.f, 0, 1,
    };
    
    
    glBindBuffer(GL_ARRAY_BUFFER, vbo);
    glBufferData(GL_ARRAY_BUFFER, sizeof(quad), quad, GL_STATIC_DRAW);
    glEnableVertexAttribArray(shader->position_attribute);
    glVertexAttribPointer(shader->position_attribute, 4, GL_FLOAT, GL_FALSE, 0, 0);
    
    return true;
}

void render_bitmap_with_shader(shader_t *shader, void *bitmap, void *previous,
                               unsigned source_width, unsigned source_height,
                               unsigned x, unsigned y, unsigned w, unsigned h,
                               GB_frame_blending_mode_t blending_mode)
{
    glUseProgram(shader->program);
    glUniform2f(shader->origin_uniform, x, y);
    glUniform2f(shader->resolution_uniform, w, h);
    glActiveTexture(GL_TEXTURE0);
    glBindTexture(GL_TEXTURE_2D, shader->texture);
    glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA, source_width, source_height, 0, GL_RGBA, GL_UNSIGNED_BYTE, bitmap);
    glUniform1i(shader->texture_uniform, 0);
    glUniform1i(shader->blending_mode_uniform, previous? blending_mode : GB_FRAME_BLENDING_MODE_DISABLED);
    if (previous) {
        glActiveTexture(GL_TEXTURE1);
        glBindTexture(GL_TEXTURE_2D, shader->previous_texture);
        glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA, source_width, source_height, 0, GL_RGBA, GL_UNSIGNED_BYTE, previous);
        glUniform1i(shader->previous_texture_uniform, 1);
    }
    glBindFragDataLocation(shader->program, 0, "frag_color");
    glDrawArrays(GL_TRIANGLE_STRIP, 0, 4);
}

void free_shader(shader_t *shader)
{
    GLint major = 0, minor = 0;
    glGetIntegerv(GL_MAJOR_VERSION, &major);
    glGetIntegerv(GL_MINOR_VERSION, &minor);
    
    if (major * 0x100 + minor < 0x302) {
        return;
    }
    
    glDeleteProgram(shader->program);
    glDeleteTextures(1, &shader->texture);
    glDeleteTextures(1, &shader->previous_texture);

}
