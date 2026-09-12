table 50100 MyTable
{
    fields
    {
        field(1; "Primary Key"; Guid) { }
    }

    keys
    {
        key(PK; "Primary Key") { }
    }

    var
        GlobalId: Guid;

    trigger OnInsert()
    begin
        GlobalId := [|CreateGuid()|];
    end;

    procedure UseGuid()
    begin
        "Primary Key" := GlobalId;
    end;
}
