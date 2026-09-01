table 50100 ProbeBuffer
{
    fields
    {
        field(1; PK; Integer) { }
        field(2; Result; Integer) { }
    }

    keys
    {
        key(PK; PK) { Clustered = true; }
    }
}

codeunit 50101 MyCodeunit
{
    procedure [|Compute|](var Buf: Record ProbeBuffer) Result: Integer
    begin
        Buf.Result := 5;
        exit(1);
    end;
}
