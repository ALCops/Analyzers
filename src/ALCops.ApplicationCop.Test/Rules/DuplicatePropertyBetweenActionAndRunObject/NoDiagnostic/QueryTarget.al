page 50100 MyPage
{
    actions
    {
        area(Processing)
        {
            action(RunCustomerQuery)
            {
                [|Caption = 'Customers';|]
                RunObject = query MyQuery;
            }
        }
    }
}

table 50100 MyTable
{
    fields
    {
        field(1; MyField; Integer) { }
    }
}

query 50100 MyQuery
{
    Caption = 'Customers';

    elements
    {
        dataitem(MyTable; MyTable)
        {
            column(MyField; MyField) { }
        }
    }
}
