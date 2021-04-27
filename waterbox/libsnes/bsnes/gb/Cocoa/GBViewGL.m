#import "GBViewGL.h"
#import "GBOpenGLView.h"

@implementation GBViewGL

- (void)createInternalView
{
    NSOpenGLPixelFormatAttribute attrs[] =
    {
        NSOpenGLPFAOpenGLProfile,
        NSOpenGLProfileVersion3_2Core,
        0
    };
    
    NSOpenGLPixelFormat *pf = [[NSOpenGLPixelFormat alloc] initWithAttributes:attrs];
    
    assert(pf);
    
    NSOpenGLContext *context = [[NSOpenGLContext alloc] initWithFormat:pf shareContext:nil];
 
    self.internalView = [[GBOpenGLView alloc] initWithFrame:self.frame pixelFormat:pf];
    ((GBOpenGLView *)self.internalView).wantsBestResolutionOpenGLSurface = YES;
    ((GBOpenGLView *)self.internalView).openGLContext = context;
}

- (void)flip
{
    [super flip];
    dispatch_async(dispatch_get_main_queue(), ^{
        [self.internalView setNeedsDisplay:YES];
        [self setNeedsDisplay:YES];
    });
}

@end
