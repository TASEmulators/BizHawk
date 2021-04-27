#import <Foundation/Foundation.h>

@interface NSString (StringForKey)
+ (NSString *) displayStringForKeyString: (NSString *)key_string;
+ (NSString *) displayStringForKeyCode:(unsigned short) keyCode;
@end
