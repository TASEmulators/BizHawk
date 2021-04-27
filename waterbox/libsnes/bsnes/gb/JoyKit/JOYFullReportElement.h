#import <Foundation/Foundation.h>
#include <IOKit/hid/IOHIDLib.h>
#include "JOYElement.h"

@interface JOYFullReportElement : JOYElement<NSCopying>
- (instancetype)initWithDevice:(IOHIDDeviceRef) device reportID:(unsigned)reportID;
- (void)updateValue:(NSData *)value;
@end


