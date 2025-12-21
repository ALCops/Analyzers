codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        TranslationHelper: Codeunit "Translation Helper";
    begin
        // My comment
        TranslationHelper.SetGlobalLanguageById(1);
    end;
}