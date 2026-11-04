table 50100 MyTable
{
    fields
    {
        field(1; "No."; Code[20]) { }
        field(2; Description; Text[100]) { }
    }

    keys
    {
        key(PK; "No.") { Clustered = true; }
    }
}
codeunit 50101 MyCodeunit
{
    procedure [|BuildBuffer|]() TempMyRec: Record MyTable temporary
    begin
        FillBuffer(TempMyRec);
    end;

    local procedure FillBuffer(var TempBuffer: Record MyTable temporary)
    begin
        TempBuffer."No." := 'A';
        TempBuffer.Insert();
    end;
}
