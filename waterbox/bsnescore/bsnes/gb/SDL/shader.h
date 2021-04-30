#ifndef shader_h
#define shader_h
#include "opengl_compat.h"
#include <stdbool.h>

typedef struct shader_s {
    GLuint resolution_uniform;
    GLuint origin_uniform;
    GLuint texture_uniform;
    GLuint previous_texture_uniform;
    GLuint blending_mode_uniform;
    
    GLuint position_attribute;
    GLuint texture;
    GLuint previous_texture;
    GLuint program;
} shader_t;

typedef enum {
    GB_FRAME_BLENDING_MODE_DISABLED,
    GB_FRAME_BLENDING_MODE_SIMPLE,
    GB_FRAME_BLENDING_MODE_ACCURATE,
    GB_FRAME_BLENDING_MODE_ACCURATE_EVEN = GB_FRAME_BLENDING_MODE_ACCURATE,
    GB_FRAME_BLENDING_MODE_ACCURATE_ODD,
} GB_frame_blending_mode_t;

bool init_shader_with_name(shader_t *shader, const char *name);
void render_bitmap_with_shader(shader_t *shader, void *bitmap, void *previous,
                               unsigned source_width, unsigned source_height,
                               unsigned x, unsigned y, unsigned w, unsigned h,
                               GB_frame_blending_mode_t blending_mode);
void free_shader(struct shader_s *shader);

#endif /* shader_h */
