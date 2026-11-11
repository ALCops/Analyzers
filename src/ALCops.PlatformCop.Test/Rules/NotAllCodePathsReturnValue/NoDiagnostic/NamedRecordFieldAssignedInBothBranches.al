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
    procedure [|BuildRecord|](UseA: Boolean) MyRec: Record MyTable
    begin
        if UseA then
            MyRec."No." := 'A'
        else
            MyRec."No." := 'B';
    end;
}
