table 50104 MyTable
{
    fields
    {
        field(1; "Type"; Integer) { }
        field(2; "Code"; Code[20]) { }
    }

    trigger OnInsert()
    begin
        Rec.[|type|] := 1;
        Rec.[|CODE|] := 'X';
    end;
}

codeunit 50104 MyCodeunit
{
    procedure Code(): Code[20]
    begin
    end;

    procedure MyProcedure()
    begin
        [|code|]();
        if [|CODE|]() = '' then;
    end;
}
