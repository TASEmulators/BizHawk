#import <Cocoa/Cocoa.h>
#import <MetalKit/MetalKit.h>
#import "GBView.h"

@interface GBViewMetal : GBView<MTKViewDelegate>
+ (bool) isSupported;
@end
