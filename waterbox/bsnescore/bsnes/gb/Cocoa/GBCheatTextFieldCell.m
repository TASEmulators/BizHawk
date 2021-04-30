#import "GBCheatTextFieldCell.h"

@interface GBCheatTextView : NSTextView
@property bool usesAddressFormat;
@end

@implementation GBCheatTextView

- (bool)_insertText:(NSString *)string replacementRange:(NSRange)range
{
    if (range.location == NSNotFound) {
        range = self.selectedRange;
    }
    
    NSString *new = [self.string stringByReplacingCharactersInRange:range withString:string];
    if (!self.usesAddressFormat) {
        NSRegularExpression *regex = [NSRegularExpression regularExpressionWithPattern:@"^(\\$[0-9A-Fa-f]{1,2}|[0-9]{1,3})$" options:0 error:NULL];
        if ([regex numberOfMatchesInString:new options:0 range:NSMakeRange(0, new.length)]) {
            [super insertText:string replacementRange:range];
            return true;
        }
        if ([regex numberOfMatchesInString:[@"$" stringByAppendingString:new] options:0 range:NSMakeRange(0, new.length + 1)]) {
            [super insertText:string replacementRange:range];
            [super insertText:@"$" replacementRange:NSMakeRange(0, 0)];
            return true;
        }
        if ([new isEqualToString:@"$"] || [string length] == 0) {
            self.string = @"$00";
            self.selectedRange = NSMakeRange(1, 2);
            return true;
        }
    }
    else {
        NSRegularExpression *regex = [NSRegularExpression regularExpressionWithPattern:@"^(\\$[0-9A-Fa-f]{1,3}:)?\\$[0-9a-fA-F]{1,4}$" options:0 error:NULL];
        if ([regex numberOfMatchesInString:new options:0 range:NSMakeRange(0, new.length)]) {
            [super insertText:string replacementRange:range];
            return true;
        }
        if ([string length] == 0) {
            NSUInteger index = [new rangeOfString:@":"].location;
            if (index != NSNotFound) {
                if (range.location > index) {
                    self.string = [[new componentsSeparatedByString:@":"] firstObject];
                    self.selectedRange = NSMakeRange(self.string.length, 0);
                    return true;
                }
                self.string = [[new componentsSeparatedByString:@":"] lastObject];
                self.selectedRange = NSMakeRange(0, 0);
                return true;
            }
            else if ([[self.string substringWithRange:range] isEqualToString:@":"]) {
                self.string = [[self.string componentsSeparatedByString:@":"] lastObject];
                self.selectedRange = NSMakeRange(0, 0);
                return true;
            }
        }
        if ([new isEqualToString:@"$"] || [string length] == 0) {
            self.string = @"$0000";
            self.selectedRange = NSMakeRange(1, 4);
            return true;
        }
        if (([string isEqualToString:@"$"] || [string isEqualToString:@":"]) && range.length == 0 && range.location == 0) {
            if ([self _insertText:@"$00:" replacementRange:range]) {
                self.selectedRange = NSMakeRange(1, 2);
                return true;
            }
        }
        if ([string isEqualToString:@":"] && range.length + range.location == self.string.length) {
            if ([self _insertText:@":$0" replacementRange:range]) {
                self.selectedRange = NSMakeRange(self.string.length - 2, 2);
                return true;
            }
        }
        if ([string isEqualToString:@"$"]) {
            if ([self _insertText:@"$0" replacementRange:range]) {
                self.selectedRange = NSMakeRange(range.location + 1, 1);
                return true;
            }
        }
    }
    return false;
}

- (NSUndoManager *)undoManager
{
    return nil;
}

- (void)insertText:(id)string replacementRange:(NSRange)replacementRange
{
    if (![self _insertText:string replacementRange:replacementRange]) {
        NSBeep();
    }
}

/* Private API, don't tell the police! */
- (void)_userReplaceRange:(NSRange)range withString:(NSString *)string
{
    [self insertText:string replacementRange:range];
}

@end

@implementation GBCheatTextFieldCell
{
    bool _drawing, _editing;
    GBCheatTextView *_fieldEditor;
}

- (NSTextView *)fieldEditorForView:(NSView *)controlView
{
    if (_fieldEditor) {
        _fieldEditor.usesAddressFormat = self.usesAddressFormat;
        return _fieldEditor;
    }
    _fieldEditor = [[GBCheatTextView alloc] initWithFrame:controlView.frame];
    _fieldEditor.fieldEditor = YES;
    _fieldEditor.usesAddressFormat = self.usesAddressFormat;
    return _fieldEditor;
}
@end
