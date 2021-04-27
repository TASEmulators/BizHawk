#import "GBCompleteByteSlice.h"

@implementation GBCompleteByteSlice
{
    HFByteArray *_array;
}

- (instancetype) initWithByteArray:(HFByteArray *)array
{
    if ((self = [super init])) {
        _array = array;
    }
    return self;
}

- (unsigned long long)length
{
    return [_array length];
}

- (void)copyBytes:(unsigned char *)dst range:(HFRange)range
{
    [_array copyBytes:dst range:range];
}

@end
