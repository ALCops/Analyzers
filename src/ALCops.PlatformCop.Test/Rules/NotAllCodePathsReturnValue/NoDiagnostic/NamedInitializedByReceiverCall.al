table 50100 MyTable
{
    fields
    {
        field(1; "No."; Code[20]) { }
    }

    keys
    {
        key(PK; "No.") { Clustered = true; }
    }
}

codeunit 50101 MyCodeunit
{
    procedure [|GetRecord|](No: Code[20]) MyRec: Record MyTable
    begin
        MyRec.Get(No);
    end;
}
