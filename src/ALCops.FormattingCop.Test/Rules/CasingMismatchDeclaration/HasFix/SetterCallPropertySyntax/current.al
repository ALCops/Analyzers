codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    begin
        [|setvalue|] := 42;
    end;

    procedure SetValue(NewValue: Integer)
    begin
    end;
}
