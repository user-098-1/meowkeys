#import <Cocoa/Cocoa.h>
// Physical US/ISO key positions drive artwork. No event is converted into text.
NSInteger KCArtworkKey(unsigned short hardwareCode);
NSString *KCKeyLabel(NSInteger vk);
