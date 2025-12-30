codeunit 50100 MyCodeunit
{
    var
    [|MyGlobalLabelTok|]: Label 'MyText';

    procedure MyProcedure()
    var
        [|MyLabelTok|]: Label 'MyText';
    begin
    end;
}