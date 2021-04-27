#import "JOYFullReportElement.h"
#include <IOKit/hid/IOHIDLib.h>

@implementation JOYFullReportElement
{
    IOHIDDeviceRef _device;
    NSData *_data;
    unsigned _reportID;
    size_t _capacity;
}

- (uint32_t)uniqueID
{
    return _reportID ^ 0xFFFF;
}

- (instancetype)initWithDevice:(IOHIDDeviceRef) device reportID:(unsigned)reportID
{
    if ((self = [super init])) {
        _data = [[NSMutableData alloc] initWithLength:[(__bridge NSNumber *)IOHIDDeviceGetProperty(device, CFSTR(kIOHIDMaxOutputReportSizeKey)) unsignedIntValue]];
        *(uint8_t *)(((NSMutableData *)_data).mutableBytes) = reportID;
        _reportID = reportID;
        _device = device;
    }
    return self;
}

- (int32_t)value
{
    [self doesNotRecognizeSelector:_cmd];
    return 0;
}

- (NSData *)dataValue
{
    return _data;
}

- (IOReturn)setValue:(uint32_t)value
{
    [self doesNotRecognizeSelector:_cmd];
    return -1;
}

- (IOReturn)setDataValue:(NSData *)value
{

    [self updateValue:value];
    return IOHIDDeviceSetReport(_device, kIOHIDReportTypeOutput, _reportID, [_data bytes], [_data length]);;
}

- (void)updateValue:(NSData *)value
{
    _data = [value copy];
}

/* For use as a dictionary key */

- (NSUInteger)hash
{
    return self.uniqueID;
}

- (BOOL)isEqual:(id)object
{
    return self.uniqueID == self.uniqueID;
}

- (id)copyWithZone:(nullable NSZone *)zone;
{
    return self;
}
@end
