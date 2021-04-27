#import <AppKit/AppKit.h>
#include "open_dialog.h"


char *do_open_rom_dialog(void)
{
    @autoreleasepool {
        NSWindow *key = [NSApp keyWindow];
        NSOpenPanel *dialog = [NSOpenPanel openPanel];
        dialog.title = @"Open ROM";
        dialog.allowedFileTypes = @[@"gb", @"gbc", @"sgb", @"isx"];
        [dialog runModal];
        [key makeKeyAndOrderFront:nil];
        NSString *ret = [[[dialog URLs] firstObject] path];
        if (ret) {
            return strdup(ret.UTF8String);
        }
        return NULL;
    }
}
