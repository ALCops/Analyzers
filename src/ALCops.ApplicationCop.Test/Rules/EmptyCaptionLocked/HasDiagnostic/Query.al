query 50100 MyQuery
{
    [|Caption|] = '';

    elements
    {
        dataitem(MyTable; MyTable)
        {
            column(MyField; MyField)
            {
                [|Caption|] = '';
            }
            filter(MyFilter; MyField)
            {
                [|Caption|] = '';
            }
        }
    }
}

table 50100 MyTable { fields { field(1; MyField; Integer) { } } }