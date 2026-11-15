codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    begin
        this.[|setvalue|] := 42;
    end;

    procedure SetValue(NewValue: Integer)
    begin
    end;
}
