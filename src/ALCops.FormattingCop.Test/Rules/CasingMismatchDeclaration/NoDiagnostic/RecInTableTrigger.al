table 50107 MyTable
{
    fields
    {
        field(1; "No."; Code[20]) { }
    }

    trigger OnInsert()
    begin
        [|Rec|]."No." := 'X';
        [|Rec|].TestField("No.");
    end;

    procedure MyProcedure()
    begin
        if [|Rec|]."No." = '' then;
    end;
}
