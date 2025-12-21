codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        TranslationHelper: Codeunit "Translation Helper";
    begin
        TranslationHelper.SetGlobalLanguageToDefault();
    end;
}