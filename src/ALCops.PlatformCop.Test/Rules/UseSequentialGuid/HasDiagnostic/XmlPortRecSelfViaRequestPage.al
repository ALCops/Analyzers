xmlport 50100 MyXmlPort
{
    requestpage
    {
        SourceTable = MyTable;
    }

    trigger OnPreXmlPort()
    begin
        Rec."Primary Key" := [|CreateGuid()|];
    end;
}

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
}
