namespace MyPublisher.MyExtension.MyAppDomain;

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
codeunit 53 "Translation Helper" { }