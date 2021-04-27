#import <Cocoa/Cocoa.h>

@interface GBWarningPopover : NSPopover

+ (GBWarningPopover *) popoverWithContents:(NSString *)contents onView:(NSView *)view;
+ (GBWarningPopover *) popoverWithContents:(NSString *)contents onWindow:(NSWindow *)window;

@end
