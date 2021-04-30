#import "JOYSubElement.h"

@interface JOYElement ()
{
    @public uint16_t _usage;
    @public uint16_t _usagePage;
    @public uint32_t _uniqueID;
    @public int32_t _min;
    @public int32_t _max;
    @public int32_t _reportID;
    @public int32_t _parentID;
}
@end

@implementation JOYSubElement
{
    JOYElement *_parent;
    size_t _size; // in bits
    size_t _offset; // in bits
}

- (instancetype)initWithRealElement:(JOYElement *)element
                               size:(size_t) size // in bits
                             offset:(size_t) offset // in bits
                          usagePage:(uint16_t)usagePage
                              usage:(uint16_t)usage
                                min:(int32_t)min
                                max:(int32_t)max
{
    if ((self = [super init])) {
        _parent = element;
        _size = size;
        _offset = offset;
        _usage = usage;
        _usagePage = usagePage;
        _uniqueID = (uint32_t)((_parent.uniqueID << 16) | offset);
        _min = min;
        _max = max;
        _reportID = _parent.reportID;
        _parentID = _parent.parentID;
    }
    return self;
}

- (int32_t)value
{
    NSData *parentValue = [_parent dataValue];
    if (!parentValue) return 0;
    if (_size > 32) return 0;
    if (_size + (_offset % 8) > 32) return 0;
    size_t parentLength = parentValue.length;
    if (_size > parentLength * 8) return 0;
    if (_size + _offset >= parentLength * 8) return 0;
    const uint8_t *bytes = parentValue.bytes;
    
    uint8_t temp[4] = {0,};
    memcpy(temp, bytes + _offset / 8, (_offset + _size - 1) / 8 - _offset / 8 + 1);
    uint32_t ret = (*(uint32_t *)temp) >> (_offset % 8);
    ret &= (1 << _size) - 1;
    
    if (_max < _min) {
        return _max + _min - ret;
    }

    return ret;
}

- (IOReturn)setValue: (uint32_t) value
{
    NSMutableData *dataValue = [[_parent dataValue] mutableCopy];
    if (!dataValue) return -1;
    if (_size > 32) return -1;
    if (_size + (_offset % 8) > 32) return -1;
    size_t parentLength = dataValue.length;
    if (_size > parentLength * 8) return -1;
    if (_size + _offset >= parentLength * 8) return -1;
    uint8_t *bytes = dataValue.mutableBytes;
    
    uint8_t temp[4] = {0,};
    memcpy(temp, bytes + _offset / 8, (_offset + _size - 1) / 8 - _offset / 8 + 1);
    (*(uint32_t *)temp) &= ~((1 << (_size - 1)) << (_offset % 8));
    (*(uint32_t *)temp) |= (value) << (_offset % 8);
    memcpy(bytes + _offset / 8, temp, (_offset + _size - 1) / 8 - _offset / 8 + 1);
    return [_parent setDataValue:dataValue];
}

- (NSData *)dataValue
{
    [self doesNotRecognizeSelector:_cmd];
    return nil;
}

- (IOReturn)setDataValue:(NSData *)data
{
    [self doesNotRecognizeSelector:_cmd];
    return -1;
}


@end
