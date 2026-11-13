namespace MyPublisher.MyExtension.MyAppDomain;

#if not CLEAN25
using Microsoft.Utilities;
#endif

codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    begin
        [|GlobalLanguage(1)|];
    end;
}
/*EOD*/
namespace System.IO;

codeunit 53 "Translation Helper" { }