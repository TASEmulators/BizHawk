#import <Cocoa/Cocoa.h>

@interface GBOptionalVisualEffectView : NSView

@end

@implementation GBOptionalVisualEffectView

+ (instancetype)allocWithZone:(struct _NSZone *)zone
{
    Class NSVisualEffectView = NSClassFromString(@"NSVisualEffectView");
    if (NSVisualEffectView) {
        return (id)[NSVisualEffectView alloc];
    }
    return [super allocWithZone:zone];
}

@end
