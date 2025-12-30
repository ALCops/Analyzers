codeunit 50100 MyCodeunit
{
    var
    [|MyGlobalLabelTok|]: Label 'MyText', Locked = true;

    procedure MyProcedure()
    var
        [|MyLabelTok|]: Label 'MyText', Locked = true;
    begin
    end;
}