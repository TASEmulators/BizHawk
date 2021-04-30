#import <Cocoa/Cocoa.h>

@interface GBSplitView : NSSplitView

-(void) setDividerColor:(NSColor *)color;
- (NSArray<NSView *> *)arrangedSubviews;
@end
