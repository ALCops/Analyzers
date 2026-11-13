namespace MyPublisher.MyExtension.MyAppDomain;

using System.Utilities;

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