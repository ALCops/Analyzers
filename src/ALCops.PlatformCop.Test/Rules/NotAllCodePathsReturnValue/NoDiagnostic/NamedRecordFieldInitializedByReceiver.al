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
    procedure [|Build|]() Result: Record MyTable
    var
        Output: OutStream;
    begin
        Result.Content.CreateOutStream(Output);
    end;
}
