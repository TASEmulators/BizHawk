#import "GBViewMetal.h"
#pragma clang diagnostic ignored "-Wpartial-availability"


static const vector_float2 rect[] =
{
    {-1, -1},
    { 1, -1},
    {-1,  1},
    { 1,  1},
};

@implementation GBViewMetal
{
    id<MTLDevice> device;
    id<MTLTexture> texture, previous_texture;
    id<MTLBuffer> vertices;
    id<MTLRenderPipelineState> pipeline_state;
    id<MTLCommandQueue> command_queue;
    id<MTLBuffer> frame_blending_mode_buffer;
    id<MTLBuffer> output_resolution_buffer;
    vector_float2 output_resolution;
}

+ (bool)isSupported
{
    if (MTLCopyAllDevices) {
        return [MTLCopyAllDevices() count];
    }
    return false;
}

- (void) allocateTextures
{
    if (!device) return;
    
    MTLTextureDescriptor *texture_descriptor = [[MTLTextureDescriptor alloc] init];
    
    texture_descriptor.pixelFormat = MTLPixelFormatRGBA8Unorm;
    
    texture_descriptor.width = GB_get_screen_width(self.gb);
    texture_descriptor.height = GB_get_screen_height(self.gb);
    
    texture = [device newTextureWithDescriptor:texture_descriptor];
    previous_texture = [device newTextureWithDescriptor:texture_descriptor];

}

- (void)createInternalView
{
    MTKView *view = [[MTKView alloc] initWithFrame:self.frame device:(device = MTLCreateSystemDefaultDevice())];
    view.delegate = self;
    self.internalView = view;
    view.paused = YES;
    view.enableSetNeedsDisplay = YES;
    
    vertices = [device newBufferWithBytes:rect
                                   length:sizeof(rect)
                                  options:MTLResourceStorageModeShared];
    
    static const GB_frame_blending_mode_t default_blending_mode = GB_FRAME_BLENDING_MODE_DISABLED;
    frame_blending_mode_buffer = [device newBufferWithBytes:&default_blending_mode
                                          length:sizeof(default_blending_mode)
                                         options:MTLResourceStorageModeShared];
    
    output_resolution_buffer = [device newBufferWithBytes:&output_resolution
                                                   length:sizeof(output_resolution)
                                                  options:MTLResourceStorageModeShared];
    
    output_resolution = (simd_float2){view.drawableSize.width, view.drawableSize.height};
    [[NSNotificationCenter defaultCenter] addObserver:self selector:@selector(loadShader) name:@"GBFilterChanged" object:nil];
    [self loadShader];
}

- (void) loadShader
{
    NSError *error = nil;
    NSString *shader_source = [NSString stringWithContentsOfFile:[[NSBundle mainBundle] pathForResource:@"MasterShader"
                                                                                                 ofType:@"metal"
                                                                                            inDirectory:@"Shaders"]
                                                        encoding:NSUTF8StringEncoding
                                                           error:nil];
    
    NSString *shader_name = [[NSUserDefaults standardUserDefaults] objectForKey:@"GBFilter"];
    NSString *scaler_source = [NSString stringWithContentsOfFile:[[NSBundle mainBundle] pathForResource:shader_name
                                                                                                 ofType:@"fsh"
                                                                                            inDirectory:@"Shaders"]
                                                        encoding:NSUTF8StringEncoding
                                                           error:nil];
    
    shader_source = [shader_source stringByReplacingOccurrencesOfString:@"{filter}"
                                                             withString:scaler_source];

    MTLCompileOptions *options = [[MTLCompileOptions alloc] init];
    options.fastMathEnabled = YES;
    id<MTLLibrary> library = [device newLibraryWithSource:shader_source
                                                   options:options
                                                     error:&error];
    if (error) {
        NSLog(@"Error: %@", error);
        if (!library) {
            return;
        }
    }
    
    id<MTLFunction> vertex_function = [library newFunctionWithName:@"vertex_shader"];
    id<MTLFunction> fragment_function = [library newFunctionWithName:@"fragment_shader"];
    
    // Set up a descriptor for creating a pipeline state object
    MTLRenderPipelineDescriptor *pipeline_state_descriptor = [[MTLRenderPipelineDescriptor alloc] init];
    pipeline_state_descriptor.vertexFunction = vertex_function;
    pipeline_state_descriptor.fragmentFunction = fragment_function;
    pipeline_state_descriptor.colorAttachments[0].pixelFormat = ((MTKView *)self.internalView).colorPixelFormat;
    
    error = nil;
    pipeline_state = [device newRenderPipelineStateWithDescriptor:pipeline_state_descriptor
                                                             error:&error];
    if (error)  {
        NSLog(@"Failed to created pipeline state, error %@", error);
        return;
    }
    
    command_queue = [device newCommandQueue];
}

- (void)mtkView:(nonnull MTKView *)view drawableSizeWillChange:(CGSize)size
{
    output_resolution = (vector_float2){size.width, size.height};
    dispatch_async(dispatch_get_main_queue(), ^{
        [(MTKView *)self.internalView draw];
    });
}

- (void)drawInMTKView:(nonnull MTKView *)view
{
    if (!(view.window.occlusionState & NSWindowOcclusionStateVisible)) return;
    if (!self.gb) return;
    if (texture.width  != GB_get_screen_width(self.gb) ||
        texture.height != GB_get_screen_height(self.gb)) {
        [self allocateTextures];
    }
    
    MTLRegion region = {
        {0, 0, 0},         // MTLOrigin
        {texture.width, texture.height, 1} // MTLSize
    };

    [texture replaceRegion:region
               mipmapLevel:0
                 withBytes:[self currentBuffer]
               bytesPerRow:texture.width * 4];
    if ([self frameBlendingMode]) {
        [previous_texture replaceRegion:region
                            mipmapLevel:0
                              withBytes:[self previousBuffer]
                            bytesPerRow:texture.width * 4];
    }
    
    MTLRenderPassDescriptor *render_pass_descriptor = view.currentRenderPassDescriptor;
    id<MTLCommandBuffer> command_buffer = [command_queue commandBuffer];

    if (render_pass_descriptor != nil) { 
        *(GB_frame_blending_mode_t *)[frame_blending_mode_buffer contents] = [self frameBlendingMode];
        *(vector_float2 *)[output_resolution_buffer contents] = output_resolution;

        id<MTLRenderCommandEncoder> render_encoder =
            [command_buffer renderCommandEncoderWithDescriptor:render_pass_descriptor];
        
        [render_encoder setViewport:(MTLViewport){0.0, 0.0,
            output_resolution.x,
            output_resolution.y,
            -1.0, 1.0}];
        
        [render_encoder setRenderPipelineState:pipeline_state];
        
        [render_encoder setVertexBuffer:vertices
                                 offset:0
                                atIndex:0];
        
        [render_encoder setFragmentBuffer:frame_blending_mode_buffer
                                   offset:0
                                  atIndex:0];
        
        [render_encoder setFragmentBuffer:output_resolution_buffer
                                   offset:0
                                  atIndex:1];
        
        [render_encoder setFragmentTexture:texture
                                  atIndex:0];
        
        [render_encoder setFragmentTexture:previous_texture
                                   atIndex:1];
        
        [render_encoder drawPrimitives:MTLPrimitiveTypeTriangleStrip
                          vertexStart:0
                          vertexCount:4];
        
        [render_encoder endEncoding];
        
        [command_buffer presentDrawable:view.currentDrawable];
    }
    
    
    [command_buffer commit];
}

- (void)flip
{
    [super flip];
    dispatch_async(dispatch_get_main_queue(), ^{
        [(MTKView *)self.internalView setNeedsDisplay:YES];
    });
}

@end
