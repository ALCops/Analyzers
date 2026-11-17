table 50107 MyTable
{
    fields
    {
        field(1; "No."; Code[20]) { }
    }

    trigger OnInsert()
    begin
        [|REC|]."No." := 'X';
        [|rec|].TestField("No.");
    end;

    procedure MyProcedure()
    begin
        if [|rec|]."No." = '' then;
    end;
}
