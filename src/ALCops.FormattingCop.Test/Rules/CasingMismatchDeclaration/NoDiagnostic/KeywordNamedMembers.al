table 50104 MyTable
{
    fields
    {
        field(1; "Type"; Integer) { }
        field(2; "Code"; Code[20]) { }
    }

    trigger OnInsert()
    begin
        Rec.[|Type|] := 1;
        Rec.[|Code|] := 'X';
    end;
}

codeunit 50104 MyCodeunit
{
    procedure Code(): Code[20]
    begin
    end;

    procedure MyProcedure()
    begin
        [|Code|]();
        if [|Code|]() = '' then;
    end;
}
