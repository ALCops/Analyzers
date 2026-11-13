namespace MyPublisher.MyExtension.MyAppDomain;

using System.IO;
#if not CLEAN25
using Microsoft.Utilities;
#endif

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