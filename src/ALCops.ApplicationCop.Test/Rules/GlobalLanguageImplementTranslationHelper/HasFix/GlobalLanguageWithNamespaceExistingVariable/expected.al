namespace MyPublisher.MyExtension.MyAppDomain;

using System.IO;

codeunit 50100 MyCodeunit
{
    var
        TranslationHelper: Codeunit "Translation Helper";

    procedure MyProcedure()
    begin
        TranslationHelper.SetGlobalLanguageById(1);
    end;
}
/*EOD*/
namespace System.IO;

codeunit 53 "Translation Helper" { }