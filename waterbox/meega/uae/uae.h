#pragma once

#define AMIGA_MAX_LINES 2048

typedef struct _RenderData {
    unsigned char* pixels;
    int width;
    int height;
    int limit_x;
    int limit_y;
    int limit_w;
    int limit_h;
    //int updated;
    char line[AMIGA_MAX_LINES];
    int flags;
    void *(*grow)(int width, int height);
    double refresh_rate;
    int bpp;
} RenderData;

typedef void (*event_function)(int);

typedef void (*init_function)(void);

typedef void (*render_function)(RenderData *rd);

typedef void (*display_function)();

typedef void (*log_function)(const char *msg);

