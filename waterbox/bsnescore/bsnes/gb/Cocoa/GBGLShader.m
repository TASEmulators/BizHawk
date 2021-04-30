#import "GBGLShader.h"
#import <OpenGL/gl3.h>

/*
 Loosely based of https://www.raywenderlich.com/70208/opengl-es-pixel-shaders-tutorial
 
 This code probably makes no sense after I upgraded it to OpenGL 3, since OpenGL makes aboslute no sense and has zero
 helpful documentation.
 */

static NSString * const vertex_shader = @"\n\
#version 150 \n\
in vec4 aPosition;\n\
void main(void) {\n\
    gl_Position = aPosition;\n\
}\n\
";

@implementation GBGLShader
{
    GLuint resolution_uniform;
    GLuint texture_uniform;
    GLuint previous_texture_uniform;
    GLuint frame_blending_mode_uniform;

    GLuint position_attribute;
    GLuint texture;
    GLuint previous_texture;
    GLuint program;
}

+ (NSString *) shaderSourceForName:(NSString *) name
{
    return [NSString stringWithContentsOfFile:[[NSBundle mainBundle] pathForResource:name
                                                                              ofType:@"fsh"
                                                                         inDirectory:@"Shaders"]
                                     encoding:NSUTF8StringEncoding
                                        error:nil];
}

- (instancetype)initWithName:(NSString *) shaderName
{
    self = [super init];
    if (self) {
        // Program
        NSString *fragment_shader = [[self class] shaderSourceForName:@"MasterShader"];
        fragment_shader = [fragment_shader stringByReplacingOccurrencesOfString:@"{filter}"
                                                                     withString:[[self class] shaderSourceForName:shaderName]];
        program = [[self class] programWithVertexShader:vertex_shader fragmentShader:fragment_shader];
        // Attributes
        position_attribute = glGetAttribLocation(program, "aPosition");
        // Uniforms
        resolution_uniform = glGetUniformLocation(program, "output_resolution");

        glGenTextures(1, &texture);
        glBindTexture(GL_TEXTURE_2D, texture);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_NEAREST);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_NEAREST);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);
        glBindTexture(GL_TEXTURE_2D, 0);
        texture_uniform = glGetUniformLocation(program, "image");

        glGenTextures(1, &previous_texture);
        glBindTexture(GL_TEXTURE_2D, previous_texture);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_NEAREST);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_NEAREST);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);
        glBindTexture(GL_TEXTURE_2D, 0);
        previous_texture_uniform = glGetUniformLocation(program, "previous_image");

        frame_blending_mode_uniform = glGetUniformLocation(program, "frame_blending_mode");

        // Configure OpenGL
        [self configureOpenGL];

    }
    return self;
}

- (void) renderBitmap: (void *)bitmap previous:(void*) previous sized:(NSSize)srcSize inSize:(NSSize)dstSize scale: (double) scale withBlendingMode:(GB_frame_blending_mode_t)blendingMode
{
    glUseProgram(program);
    glUniform2f(resolution_uniform, dstSize.width * scale, dstSize.height * scale);
    glActiveTexture(GL_TEXTURE0);
    glBindTexture(GL_TEXTURE_2D, texture);
    glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA, srcSize.width, srcSize.height, 0, GL_RGBA, GL_UNSIGNED_BYTE, bitmap);
    glUniform1i(texture_uniform, 0);
    glUniform1i(frame_blending_mode_uniform, blendingMode);
    if (blendingMode) {
        glActiveTexture(GL_TEXTURE1);
        glBindTexture(GL_TEXTURE_2D, previous_texture);
        glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA, srcSize.width, srcSize.height, 0, GL_RGBA, GL_UNSIGNED_BYTE, previous);
        glUniform1i(previous_texture_uniform, 1);
    }
    glBindFragDataLocation(program, 0, "frag_color");
    glDrawArrays(GL_TRIANGLE_STRIP, 0, 4);
}

- (void)configureOpenGL
{
    // Program

    glUseProgram(program);

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
    glEnableVertexAttribArray(position_attribute);
    glVertexAttribPointer(position_attribute, 4, GL_FLOAT, GL_FALSE, 0, 0);
}

+ (GLuint)programWithVertexShader:(NSString*)vsh fragmentShader:(NSString*)fsh
{
    // Build shaders
    GLuint vertex_shader = [self shaderWithContents:vsh type:GL_VERTEX_SHADER];
    GLuint fragment_shader = [self shaderWithContents:fsh type:GL_FRAGMENT_SHADER];
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
        NSLog(@"%@:- GLSL Program Error: %s", self, messages);
    }
    // Delete shaders
    glDeleteShader(vertex_shader);
    glDeleteShader(fragment_shader);

    return program;
}

- (void)dealloc
{
    glDeleteProgram(program);
    glDeleteTextures(1, &texture);
    glDeleteTextures(1, &previous_texture);

    /* OpenGL is black magic. Closing one view causes others to be completely black unless we reload their shaders */
    /* We're probably not freeing thing in the right place. */
    [[NSNotificationCenter defaultCenter] postNotificationName:@"GBFilterChanged" object:nil];
}

+ (GLuint)shaderWithContents:(NSString*)contents type:(GLenum)type
{

    const GLchar *source = [contents UTF8String];
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
        NSLog(@"%@:- GLSL Shader Error: %s", self, messages);
    }
    return shader;
}

@end
