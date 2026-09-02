page 50100 PageObjectIsExcluded
{
    PageType = Card;
    ApplicationArea = All;
    SourceTable = Customer;

    layout
    {
        area(Content)
        {
            field(Id; Rec.Id)
            {
                ApplicationArea = All;
            }
        }
    }

    var
        [|MyValue|]: Integer;

    local procedure ShowValue()
    begin
        MyValue := 42;
        Message('%1', MyValue);
    end;
}

table 50101 Customer
{
    fields
    {
        field(1; Id; Integer) { }
    }

    keys
    {
        key(PK; Id) { Clustered = true; }
    }
}
