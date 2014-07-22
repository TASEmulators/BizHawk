/* version info */
#define PLUGIN_NAME    "Jabo Direct3D8 wrapper for Mupen64Plus"
#define PLUGIN_VERSION           0x020000
#define VIDEO_PLUGIN_API_VERSION 0x020200
#define CONFIG_API_VERSION       0x020000
#define VIDEXT_API_VERSION       0x030000
#define CONFIG_PARAM_VERSION     1.00

#define VERSION_PRINTF_SPLIT(x) (((x) >> 16) & 0xffff), (((x) >> 8) & 0xff), ((x) & 0xff)

typedef struct {
  int anisotropic_level;
  int brightness;
  int antialiasing_level;
  BOOL super2xsal;
  BOOL texture_filter;
  BOOL adjust_aspect_ratio;
  BOOL legacy_pixel_pipeline;
  BOOL alpha_blending;
//  BOOL wireframe;
  BOOL direct3d_transformation_pipeline;
  BOOL z_compare;
  BOOL copy_framebuffer;
  int resolution_width;
  int resolution_height;
  int clear_mode;
} SETTINGS;