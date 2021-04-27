#import <Carbon/Carbon.h>
#import "GBTerminalTextFieldCell.h"

@interface GBTerminalTextView : NSTextView
@property GB_gameboy_t *gb;
@end

@implementation GBTerminalTextFieldCell
{
    GBTerminalTextView *field_editor;
}

- (NSTextView *)fieldEditorForView:(NSView *)controlView
{
    if (field_editor) {
        field_editor.gb = self.gb;
        return field_editor;
    }
    field_editor = [[GBTerminalTextView alloc] init];
    [field_editor setFieldEditor:YES];
    field_editor.gb = self.gb;
    return field_editor;
}

@end

@implementation GBTerminalTextView
{
    NSMutableOrderedSet *lines;
    NSUInteger current_line;
    bool reverse_search_mode;
    NSRange auto_complete_range;
    uintptr_t auto_complete_context;
}

- (instancetype)init
{
    self = [super init];
    if (!self) {
        return NULL;
    }
    lines = [[NSMutableOrderedSet alloc] init];
    return self;
}

- (void)moveUp:(id)sender
{
    reverse_search_mode = false;
    if (current_line != 0) {
        current_line--;
        [self setString:[lines objectAtIndex:current_line]];
    }
    else {
        [self setSelectedRange:NSMakeRange(0, 0)];
        NSBeep();
    }
}

- (void)moveDown:(id)sender
{
    reverse_search_mode = false;
    if (current_line == [lines count]) {
        [self setString:@""];
        NSBeep();
        return;
    }
    current_line++;
    if (current_line == [lines count]) {
        [self setString:@""];
    }
    else {
        [self setString:[lines objectAtIndex:current_line]];
    }
}

-(void)insertNewline:(id)sender
{
    if ([self.string length]) {
        NSString *string = [self.string copy];
        [lines removeObject:string];
        [lines addObject:string];
    }
    [super insertNewline:sender];
    current_line = [lines count];
    reverse_search_mode = false;
}

- (void)keyDown:(NSEvent *)event
{
    if (event.keyCode == kVK_ANSI_R && (event.modifierFlags & NSEventModifierFlagDeviceIndependentFlagsMask) == NSEventModifierFlagControl) {
        if ([lines count] == 0) {
            NSBeep();
            return;
        }
        if (!reverse_search_mode) {
            [self selectAll:self];
            current_line = [lines count] - 1;
        }
        else {
            if (current_line != 0) {
                current_line--;
            }
            else {
                NSBeep();
            }
        }
        
        if (self.string.length) {
            [self updateReverseSearch];
        }
        else {
            [self setNeedsDisplay:YES];
            reverse_search_mode = true;
        }

    }
    else {
        [super keyDown:event];
    }
}

- (void) updateReverseSearch
{
    NSUInteger old_line = current_line;
    reverse_search_mode = false;
    NSString *substring = [self.string substringWithRange:self.selectedRange];
    do {
        NSString *line = [lines objectAtIndex:current_line];
        NSRange range = [line rangeOfString:substring];
        if (range.location != NSNotFound) {
            self.string = line;
            [self setSelectedRange:range];
            reverse_search_mode = true;
            return;
        }
    } while (current_line--);
    current_line = old_line;
    reverse_search_mode = true;
    NSBeep();
}

- (void) insertText:(NSString *)string replacementRange:(NSRange)range
{
    if (reverse_search_mode) {
        range = self.selectedRange;
        self.string = [[self.string substringWithRange:range] stringByAppendingString:string];
        [self selectAll:nil];
        [self updateReverseSearch];
    }
    else {
        [super insertText:string replacementRange:range];
    }
}

-(void)deleteBackward:(id)sender
{
    if (reverse_search_mode && self.string.length) {
        NSRange range = self.selectedRange;
        range.length--;
        self.string = [self.string substringWithRange:range];
        if (range.length) {
            [self selectAll:nil];
            [self updateReverseSearch];
        }
        else {
            reverse_search_mode = true;
            current_line = [lines count] - 1;
        }
    }
    else {
        [super deleteBackward:sender];
    }
}

-(void)setSelectedRanges:(NSArray<NSValue *> *)ranges affinity:(NSSelectionAffinity)affinity stillSelecting:(BOOL)stillSelectingFlag
{
    reverse_search_mode = false;
    auto_complete_context = 0;
    [super setSelectedRanges:ranges affinity:affinity stillSelecting:stillSelectingFlag];
}

- (BOOL)resignFirstResponder 
{
    reverse_search_mode = false;
    return [super resignFirstResponder];
}

-(void)drawRect:(NSRect)dirtyRect
{
    [super drawRect:dirtyRect];
    if (reverse_search_mode && [super string].length == 0) {
        NSMutableDictionary *attributes = [self.typingAttributes mutableCopy];
        NSColor *color = [attributes[NSForegroundColorAttributeName] colorWithAlphaComponent:0.5];
        [attributes setObject:color forKey:NSForegroundColorAttributeName];
        [[[NSAttributedString alloc] initWithString:@"Reverse search..." attributes:attributes] drawAtPoint:NSMakePoint(2, 0)];
    }
}

/* Todo: lazy design, make it use a delegate instead of having a gb reference*/

- (void)insertTab:(id)sender
{
    if (auto_complete_context == 0) {
        NSRange selection = self.selectedRange;
        if (selection.length) {
            [self delete:nil];
        }
        auto_complete_range = NSMakeRange(selection.location, 0);
    }
    char *substring = strdup([self.string substringToIndex:auto_complete_range.location].UTF8String);
    uintptr_t context = auto_complete_context;
    char *completion = GB_debugger_complete_substring(self.gb, substring, &context);
    free(substring);
    if (completion) {
        NSString *ns_completion = @(completion);
        free(completion);
        if (!ns_completion) {
            goto error;
        }
        self.selectedRange = auto_complete_range;
        auto_complete_range.length = ns_completion.length;
        [self replaceCharactersInRange:self.selectedRange withString:ns_completion];
        auto_complete_context = context;
        return;
    }
error:
    auto_complete_context = context;
    NSBeep();
}

@end
