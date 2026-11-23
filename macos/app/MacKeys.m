#import "MacKeys.h"

NSInteger KCArtworkKey(unsigned short code) {
    static const unsigned short map[128] = {
        [0]='A',[1]='S',[2]='D',[3]='F',[4]='H',[5]='G',[6]='Z',[7]='X',[8]='C',[9]='V',[10]=226,
        [11]='B',[12]='Q',[13]='W',[14]='E',[15]='R',[16]='Y',[17]='T',[18]='1',[19]='2',[20]='3',[21]='4',[22]='6',[23]='5',
        [24]=187,[25]='9',[26]='7',[27]=189,[28]='8',[29]='0',[30]=221,[31]='O',[32]='U',[33]=219,[34]='I',[35]='P',
        [36]=13,[37]='L',[38]='J',[39]=222,[40]='K',[41]=186,[42]=220,[43]=188,[44]=191,[45]='N',[46]='M',[47]=190,
        [48]=9,[49]=32,[50]=192,[51]=8,[53]=27,[54]=92,[55]=91,[56]=160,[57]=20,[58]=164,[59]=162,[60]=161,[61]=165,[62]=163,[63]=256,
        [64]=128,[65]=110,[67]=106,[69]=107,[71]=12,[75]=111,[76]=13,[78]=109,[79]=129,[80]=130,[81]=187,
        [82]=96,[83]=97,[84]=98,[85]=99,[86]=100,[87]=101,[88]=102,[89]=103,[90]=131,[91]=104,[92]=105,
        [93]=257,[94]=258,[95]=188,[96]=116,[97]=117,[98]=118,[99]=114,[100]=119,[101]=120,[102]=259,[103]=122,[104]=260,
        [105]=124,[106]=127,[107]=125,[109]=121,[111]=123,[113]=126,[114]=45,[115]=36,[116]=33,[117]=46,[118]=115,
        [119]=35,[120]=113,[121]=34,[122]=112,[123]=37,[124]=39,[125]=40,[126]=38
    };
    return code<128 ? map[code] : 0;
}
NSString *KCKeyLabel(NSInteger vk) {
    if ((vk>=48&&vk<=57)||(vk>=65&&vk<=90)) return [NSString stringWithFormat:@"%c",(int)vk];
    if (vk>=96&&vk<=105) return [NSString stringWithFormat:@"%ld",(long)(vk-96)];
    if (vk>=112&&vk<=131) return [NSString stringWithFormat:@"F%ld",(long)(vk-111)];
    switch (vk) {
        case 8:return @"⌫"; case 9:return @"⇥"; case 12:return @"Clear"; case 13:return @"↩"; case 20:return @"⇪";
        case 27:return @"Esc"; case 32:return @"␣"; case 33:return @"PgUp"; case 34:return @"PgDn"; case 35:return @"End";
        case 36:return @"Home"; case 37:return @"←"; case 38:return @"↑"; case 39:return @"→"; case 40:return @"↓";
        case 45:return @"Help"; case 46:return @"⌦"; case 91:case 92:return @"⌘"; case 106:return @"×"; case 107:return @"+";
        case 109:return @"−"; case 110:case 190:return @"."; case 111:case 191:return @"/";
        case 160:case 161:return @"⇧"; case 162:case 163:return @"⌃"; case 164:case 165:return @"⌥";
        case 186:return @";"; case 187:return @"="; case 188:return @","; case 189:return @"−"; case 192:return @"`";
        case 219:return @"["; case 220:return @"\\"; case 221:return @"]"; case 222:return @"'"; case 226:return @"§";
        case 256:return @"fn"; case 257:return @"¥"; case 258:return @"ろ"; case 259:return @"英数"; case 260:return @"かな";
        default:return nil;
    }
}
