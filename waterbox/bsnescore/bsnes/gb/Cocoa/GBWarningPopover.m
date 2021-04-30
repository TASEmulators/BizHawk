#import "GBWarningPopover.h"

static GBWarningPopover *lastPopover;

@implementation GBWarningPopover

+ (GBWarningPopover *) popoverWithContents:(NSString *)contents onView:(NSView *)view
{
    [lastPopover close];
    lastPopover = [[self alloc] init];
    
    [lastPopover setBehavior:NSPopoverBehaviorApplicationDefined];
    [lastPopover setAnimates:YES];
    lastPopover.contentViewController = [[NSViewController alloc] initWithNibName:@"PopoverView" bundle:nil];
    NSTextField *field = (NSTextField *)lastPopover.contentViewController.view;
    [field setStringValue:contents];
    NSSize textSize = [field.cell cellSizeForBounds:[field.cell drawingRectForBounds:NSMakeRect(0, 0, 240, CGFLOAT_MAX)]];
    textSize.width = ceil(textSize.width) + 16;
    textSize.height = ceil(textSize.height) + 12;
    [lastPopover setContentSize:textSize];
    
    if (!view.window.isVisible) {
        [view.window setIsVisible:YES];
    }
    
    [lastPopover showRelativeToRect:view.bounds
                             ofView:view
                      preferredEdge:NSMinYEdge];
    
    NSRect frame = field.frame;
    frame.origin.x += 8;
    frame.origin.y -= 6;
    field.frame = frame;
    

    [lastPopover performSelector:@selector(close) withObject:nil afterDelay:3.0];
    
    return lastPopover;
}

+ (GBWarningPopover *)popoverWithContents:(NSString *)contents onWindow:(NSWindow *)window
{
    return [self popoverWithContents:contents onView:window.contentView.superview.subviews.lastObject];
}

@end
