#include <QuickLook/QuickLook.h>
#include <Cocoa/Cocoa.h>
#include "get_image_for_rom.h"

static OSStatus render(CGContextRef cgContext, CFURLRef url, bool showBorder)
{
    /* Load the template NSImages when generating the first thumbnail */
    static NSImage *template = nil;
    static NSImage *templateUniversal = nil;
    static NSImage *templateColor = nil;
    static NSBundle *bundle = nil;
    static dispatch_once_t onceToken;
    if (showBorder) {
        dispatch_once(&onceToken, ^{
            bundle = [NSBundle bundleWithIdentifier:@"com.github.liji32.sameboy.previewer"];
            template = [[NSImage alloc] initWithContentsOfFile:[bundle pathForResource:@"CartridgeTemplate" ofType:@"png"]];
            templateUniversal = [[NSImage alloc] initWithContentsOfFile:[bundle pathForResource:@"UniversalCartridgeTemplate" ofType:@"png"]];
            templateColor = [[NSImage alloc] initWithContentsOfFile:[bundle pathForResource:@"ColorCartridgeTemplate" ofType:@"png"]];
        });
    }
    uint32_t bitmap[160*144];
    uint8_t cgbFlag = 0;
    
    /* The cgb_boot_fast boot ROM skips the boot animation */
    if (get_image_for_rom([[(__bridge NSURL *)url path] UTF8String],
                          [[bundle pathForResource:@"cgb_boot_fast" ofType:@"bin"] UTF8String],
                          bitmap, &cgbFlag)) {
        return -1;
    }
    
    /* Convert the screenshot to a CGImageRef */
    CGDataProviderRef provider = CGDataProviderCreateWithData(NULL, bitmap, sizeof(bitmap), NULL);
    CGColorSpaceRef colorSpaceRef = CGColorSpaceCreateDeviceRGB();
    CGBitmapInfo bitmapInfo = kCGBitmapByteOrderDefault;
    CGColorRenderingIntent renderingIntent = kCGRenderingIntentDefault;
    
    CGImageRef iref = CGImageCreate(160,
                                    144,
                                    8,
                                    32,
                                    4 * 160,
                                    colorSpaceRef,
                                    bitmapInfo,
                                    provider,
                                    NULL,
                                    YES,
                                    renderingIntent);
    CGContextSetInterpolationQuality(cgContext, kCGInterpolationNone);
    NSGraphicsContext *context = [NSGraphicsContext graphicsContextWithGraphicsPort:(void *)cgContext flipped:NO];
    [NSGraphicsContext setCurrentContext:context];
    
    
    /* Convert the screenshot to a magnified NSImage */
    NSImage *screenshot = [[NSImage alloc] initWithCGImage:iref size:NSMakeSize(160, 144)];
    /* Draw the screenshot */
    if (showBorder) {
        [screenshot drawInRect:NSMakeRect(192, 150, 640, 576)];
    }
    else {
        [screenshot drawInRect:NSMakeRect(0, 0, 640, 576)];
    }
    
    if (showBorder) {
        /* Use the CGB flag to determine the cartridge "look":
         - DMG cartridges are grey
         - CGB cartridges are transparent
         - CGB cartridges that support DMG systems are black
         */
        NSImage *effectiveTemplate = nil;
        switch (cgbFlag) {
            case 0xC0:
                effectiveTemplate = templateColor;
                break;
            case 0x80:
                effectiveTemplate = templateUniversal;
                break;
            default:
                effectiveTemplate = template;
        }
        
        /* Mask it with the template (The middle part of the template image is transparent) */
        [effectiveTemplate drawInRect:(NSRect){{0, 0}, template.size}];
    }
    
    CGColorSpaceRelease(colorSpaceRef);
    CGDataProviderRelease(provider);
    CGImageRelease(iref);
    
    return noErr;
}

OSStatus GeneratePreviewForURL(void *thisInterface, QLPreviewRequestRef preview, CFURLRef url, CFStringRef contentTypeUTI, CFDictionaryRef options)
{
    @autoreleasepool {
        CGContextRef cgContext = QLPreviewRequestCreateContext(preview, ((NSSize){640, 576}), true, nil);
        if (render(cgContext, url, false) == noErr) {
            QLPreviewRequestFlushContext(preview, cgContext);
            CGContextRelease(cgContext);
            return noErr;
        }
        CGContextRelease(cgContext);
        return -1;
    }
}

OSStatus GenerateThumbnailForURL(void *thisInterface, QLThumbnailRequestRef thumbnail, CFURLRef url, CFStringRef contentTypeUTI, CFDictionaryRef options, CGSize maxSize)
{
    extern NSString *kQLThumbnailPropertyIconFlavorKey;
    @autoreleasepool {
        CGContextRef cgContext = QLThumbnailRequestCreateContext(thumbnail, ((NSSize){1024, 1024}), true, (__bridge CFDictionaryRef)(@{kQLThumbnailPropertyIconFlavorKey : @(0)}));
        if (render(cgContext, url, true) == noErr) {
            QLThumbnailRequestFlushContext(thumbnail, cgContext);
            CGContextRelease(cgContext);
            return noErr;
        }
        CGContextRelease(cgContext);
        return -1;
    }
}
