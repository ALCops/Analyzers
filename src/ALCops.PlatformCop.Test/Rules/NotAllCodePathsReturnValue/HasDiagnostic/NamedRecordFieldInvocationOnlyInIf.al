table 50100 MyTable
{
    fields
    {
        field(1; "Entry No."; Integer) { }
        field(2; Content; Blob) { }
    }
}

codeunit 50101 MyCodeunit
{
    procedure [|Build|](Condition: Boolean; Txt: Text) Result: Record MyTable
    begin
        if Condition then begin
            Evaluate(Result."Entry No.", Txt);
        end;
    end;

    procedure [|BuildWithStream|](Condition: Boolean) Result: Record MyTable
    var
        Output: OutStream;
    begin
        if Condition then begin
            Result.Content.CreateOutStream(Output);
        end;
    end;
}
