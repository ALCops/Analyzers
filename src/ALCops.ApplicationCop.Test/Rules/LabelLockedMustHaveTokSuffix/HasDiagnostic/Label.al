codeunit 50100 MyCodeunit
{
    var
    [|MyGlobalLbl|]: Label 'MyText', Locked = true;

    procedure MyProcedure()
    var
        [|MyLabelLbl|]: Label 'MyText', Locked = true;
    begin
    end;
}