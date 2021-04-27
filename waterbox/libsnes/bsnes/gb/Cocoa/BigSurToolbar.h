#import <Cocoa/Cocoa.h>
#ifndef BigSurToolbar_h
#define BigSurToolbar_h

/* Backport the toolbarStyle property to allow compilation with older SDKs*/
#ifndef __MAC_10_16
typedef NS_ENUM(NSInteger, NSWindowToolbarStyle) {
    // The default value. The style will be determined by the window's given configuration
    NSWindowToolbarStyleAutomatic,
    // The toolbar will appear below the window title
    NSWindowToolbarStyleExpanded,
    // The toolbar will appear below the window title and the items in the toolbar will attempt to have equal widths when possible
    NSWindowToolbarStylePreference,
    // The window title will appear inline with the toolbar when visible
    NSWindowToolbarStyleUnified,
    // Same as NSWindowToolbarStyleUnified, but with reduced margins in the toolbar allowing more focus to be on the contents of the window
    NSWindowToolbarStyleUnifiedCompact
} API_AVAILABLE(macos(11.0));

@interface NSWindow (toolbarStyle)
@property NSWindowToolbarStyle toolbarStyle API_AVAILABLE(macos(11.0));
@end

@interface NSImage (SFSymbols)
+ (instancetype)imageWithSystemSymbolName:(NSString *)symbolName accessibilityDescription:(NSString *)description API_AVAILABLE(macos(11.0));
@end

#endif

#endif
