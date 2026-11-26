xmlport 50100 MyXmlPort
{
    schema
    {
        textelement(Root)
        {
            tableelement(MyTableElement; MyTable)
            {
                [|RequestFilterHeading = 'My Table Element Filters'|];
            }
        }
    }
}

table 50100 MyTable
{
    fields
    {
        field(1; MyField; Text[100]) { }
        field(2; MyOption; Option)
        {
            OptionMembers = First,Second;
        }
    }
}
