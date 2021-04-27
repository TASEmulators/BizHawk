#import <AppKit/AppKit.h>
#import <objc/runtime.h>

@interface NSImageRep(PrivateAPI)
@property(setter=_setAppearanceName:) NSString *_appearanceName;
@end

static NSImage * (*imageNamed)(Class self, SEL _cmd, NSString *name);

@implementation NSImage(DarkHooks)

+ (NSImage *)imageNamedWithDark:(NSImageName)name
{
    NSImage *light = imageNamed(self, _cmd, name);
    if (@available(macOS 10.14, *)) {
        NSImage *dark = imageNamed(self, _cmd, [name stringByAppendingString:@"~dark"]);
        if (!dark) {
            return light;
        }
        NSImage *ret = [[NSImage alloc] initWithSize:light.size];
        for (NSImageRep *rep in light.representations) {
            [rep _setAppearanceName:NSAppearanceNameAqua];
            [ret addRepresentation:rep];
        }
        for (NSImageRep *rep in dark.representations) {
            [rep _setAppearanceName:NSAppearanceNameDarkAqua];
            [ret addRepresentation:rep];
        }
        return ret;
    }
    return light;
}

+(void)load
{
    if (@available(macOS 10.14, *)) {
        imageNamed = (void *)[self methodForSelector:@selector(imageNamed:)];
        method_setImplementation(class_getClassMethod(self, @selector(imageNamed:)),
                                 [self methodForSelector:@selector(imageNamedWithDark:)]);
    }
}
@end
