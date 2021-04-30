#import "JOYElement.h"

@interface JOYSubElement : JOYElement
- (instancetype)initWithRealElement:(JOYElement *)element
                               size:(size_t) size // in bits
                             offset:(size_t) offset // in bits
                          usagePage:(uint16_t)usagePage
                              usage:(uint16_t)usage
                                min:(int32_t)min
                                max:(int32_t)max;

@end


