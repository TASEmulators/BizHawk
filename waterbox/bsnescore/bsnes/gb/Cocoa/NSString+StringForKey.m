#import "NSString+StringForKey.h"
#import "KeyboardShortcutPrivateAPIs.h"
#import <Carbon/Carbon.h>

@implementation NSString (StringForKey)

+ (NSString *) displayStringForKeyString: (NSString *)key_string
{
    return [[NSKeyboardShortcut shortcutWithKeyEquivalent:key_string modifierMask:0] localizedDisplayName];
}

+ (NSString *) displayStringForKeyCode:(unsigned short) keyCode
{
    /* These cases are not handled by stringForVirtualKey */
    switch (keyCode) {
            
        case kVK_Home: return @"↖";
        case kVK_End: return @"↘";
        case kVK_PageUp: return @"⇞";
        case kVK_PageDown: return @"⇟";
        case kVK_Delete: return @"⌫";
        case kVK_ForwardDelete: return @"⌦";
        case kVK_ANSI_KeypadEnter: return @"⌤";
        case kVK_CapsLock: return @"⇪";
        case kVK_Shift: return @"Left ⇧";
        case kVK_Control: return @"Left ⌃";
        case kVK_Option: return @"Left ⌥";
        case kVK_Command: return @"Left ⌘";
        case kVK_RightShift: return @"Right ⇧";
        case kVK_RightControl: return @"Right ⌃";
        case kVK_RightOption: return @"Right ⌥";
        case kVK_RightCommand: return @"Right ⌘";
        case kVK_Function: return @"fn";
            
        /* Label Keypad buttons accordingly */
        default:
            if ((keyCode < kVK_ANSI_Keypad0 || keyCode > kVK_ANSI_Keypad9)) {
                return [NSPrefPaneUtils stringForVirtualKey:keyCode modifiers:0];
            }
            
        case kVK_ANSI_KeypadDecimal: case kVK_ANSI_KeypadMultiply: case kVK_ANSI_KeypadPlus: case kVK_ANSI_KeypadDivide: case kVK_ANSI_KeypadMinus: case kVK_ANSI_KeypadEquals:
            return [@"Keypad " stringByAppendingString:[NSPrefPaneUtils stringForVirtualKey:keyCode modifiers:0]];
    }
}

@end
