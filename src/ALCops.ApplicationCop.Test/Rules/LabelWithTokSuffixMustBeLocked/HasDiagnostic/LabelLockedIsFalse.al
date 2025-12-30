codeunit 50100 MyCodeunit
{
    var
    [|MyGlobalLabelTok|]: Label 'MyText', Locked = false;

    procedure MyProcedure()
    var
        [|MyLabelTok|]: Label 'MyText', Locked = false;
    begin
    end;
}