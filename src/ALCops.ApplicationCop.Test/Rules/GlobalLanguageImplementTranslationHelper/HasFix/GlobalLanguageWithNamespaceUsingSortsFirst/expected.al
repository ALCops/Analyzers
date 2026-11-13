namespace MyPublisher.MyExtension.MyAppDomain;

using System.IO;
using System.Utilities;

codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        TranslationHelper: Codeunit "Translation Helper";
    begin
        TranslationHelper.SetGlobalLanguageById(1);
    end;
}
/*EOD*/
namespace System.IO;

codeunit 53 "Translation Helper" { }