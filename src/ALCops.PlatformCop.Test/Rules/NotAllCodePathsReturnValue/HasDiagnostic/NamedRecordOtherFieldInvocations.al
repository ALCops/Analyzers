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
    procedure [|Build|](Txt: Text) Result: Record MyTable
    var
        Other: Record MyTable;
    begin
        Evaluate(Other."Entry No.", Txt);
    end;

    procedure [|BuildWithStream|]() Result: Record MyTable
    var
        Other: Record MyTable;
        Output: OutStream;
    begin
        Other.Content.CreateOutStream(Output);
    end;
}
