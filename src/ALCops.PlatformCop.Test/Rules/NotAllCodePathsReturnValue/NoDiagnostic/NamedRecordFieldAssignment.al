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
    procedure [|BuildRecord|](No: Code[20]) MyRec: Record MyTable
    begin
        MyRec."No." := No;
        MyRec.Description := 'Test';
    end;
}
