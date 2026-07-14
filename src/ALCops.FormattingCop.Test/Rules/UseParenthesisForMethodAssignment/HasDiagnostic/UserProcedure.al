codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        MyOther: Codeunit MyOther;
    begin
        [|MyOther.SetValue := 5;|]
    end;
}

codeunit 50101 MyOther
{
    procedure SetValue(NewValue: Integer)
    begin
    end;
}
