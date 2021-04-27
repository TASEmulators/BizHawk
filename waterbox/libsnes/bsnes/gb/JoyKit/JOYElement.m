#import "JOYElement.h"
#include <IOKit/hid/IOHIDLib.h>
#include <objc/runtime.h>

@implementation JOYElement
{
    id _element;
    IOHIDDeviceRef _device;
    int32_t _min, _max;
}

- (int32_t)min
{
    return MIN(_min, _max);
}

- (int32_t)max
{
    return MAX(_max, _min);
}

-(void)setMin:(int32_t)min
{
    _min = min;
}

- (void)setMax:(int32_t)max
{
    _max = max;
}

/* Ugly hack because IOHIDDeviceCopyMatchingElements is slow */
+ (NSArray *) cookiesToSkipForDevice:(IOHIDDeviceRef)device
{
    id _device = (__bridge id)device;
    NSMutableArray *ret = objc_getAssociatedObject(_device, _cmd);
    if (ret) return ret;
    
    ret = [NSMutableArray array];
    NSArray *nones = CFBridgingRelease(IOHIDDeviceCopyMatchingElements(device,
                                                                       (__bridge CFDictionaryRef)@{@(kIOHIDElementTypeKey): @(kIOHIDElementTypeInput_NULL)},
                                                                       0));
    for (id none in nones) {
        [ret addObject:@(IOHIDElementGetCookie((__bridge IOHIDElementRef)none))];
    }
    objc_setAssociatedObject(_device, _cmd, ret, OBJC_ASSOCIATION_RETAIN);
    return ret;
}

- (instancetype)initWithElement:(IOHIDElementRef)element
{
    if ((self = [super init])) {
        _element = (__bridge id)element;
        _usage = IOHIDElementGetUsage(element);
        _usagePage = IOHIDElementGetUsagePage(element);
        _uniqueID = (uint32_t)IOHIDElementGetCookie(element);
        _min = (int32_t) IOHIDElementGetLogicalMin(element);
        _max = (int32_t) IOHIDElementGetLogicalMax(element);
        _reportID = IOHIDElementGetReportID(element);
        IOHIDElementRef parent = IOHIDElementGetParent(element);
        _parentID = parent? (uint32_t)IOHIDElementGetCookie(parent) : -1;
        _device = IOHIDElementGetDevice(element);
        
        /* Catalina added a new input type in a way that breaks cookie consistency across macOS versions,
           we shall adjust our cookies to to compensate */
        unsigned cookieShift = 0, parentCookieShift = 0;

        for (NSNumber *none in [JOYElement cookiesToSkipForDevice:_device]) {
            if (none.unsignedIntValue < _uniqueID) {
                cookieShift++;
            }
            if (none.unsignedIntValue < (int32_t)_parentID) {
                parentCookieShift++;
            }
        }
        
        _uniqueID -= cookieShift;
        _parentID -= parentCookieShift;
    }
    return self;
}

- (int32_t)value
{
    IOHIDValueRef value = NULL;
    IOHIDDeviceGetValue(_device, (__bridge IOHIDElementRef)_element, &value);
    if (!value) return 0;
    CFRelease(CFRetain(value)); // For some reason, this is required to prevent leaks.
    return (int32_t)IOHIDValueGetIntegerValue(value);
}

- (NSData *)dataValue
{
    IOHIDValueRef value = NULL;
    IOHIDDeviceGetValue(_device, (__bridge IOHIDElementRef)_element, &value);
    if (!value) return 0;
    CFRelease(CFRetain(value)); // For some reason, this is required to prevent leaks.
    return [NSData dataWithBytes:IOHIDValueGetBytePtr(value) length:IOHIDValueGetLength(value)];
}

- (IOReturn)setValue:(uint32_t)value
{
    IOHIDValueRef ivalue = IOHIDValueCreateWithIntegerValue(NULL, (__bridge IOHIDElementRef)_element, 0, value);
    IOReturn ret = IOHIDDeviceSetValue(_device, (__bridge IOHIDElementRef)_element, ivalue);
    CFRelease(ivalue);
    return ret;
}

- (IOReturn)setDataValue:(NSData *)value
{
    IOHIDValueRef ivalue = IOHIDValueCreateWithBytes(NULL, (__bridge IOHIDElementRef)_element, 0, value.bytes, value.length);
    IOReturn ret = IOHIDDeviceSetValue(_device, (__bridge IOHIDElementRef)_element, ivalue);
    CFRelease(ivalue);
    return ret;
}

/* For use as a dictionary key */

- (NSUInteger)hash
{
    return self.uniqueID;
}

- (BOOL)isEqual:(id)object
{
    return self->_element == object;
}

- (id)copyWithZone:(nullable NSZone *)zone;
{
    return self;
}
@end
