codeunit 50100 BuiltInInvocationMayReenter
{
    var
        [|MyValue|]: Integer;

    local procedure ShowValue()
    var
        CallbackQuery: Query CallbackQuery;
    begin
        MyValue := 42;
        CallbackQuery.Open();
        Message('%1', MyValue);
    end;
}

query 50101 CallbackQuery
{
    elements
    {
        dataitem(Customer; Customer)
        {
            column(Id; Id) { }
        }
    }

    trigger OnBeforeOpen()
    begin
    end;
}

table 50102 Customer
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
