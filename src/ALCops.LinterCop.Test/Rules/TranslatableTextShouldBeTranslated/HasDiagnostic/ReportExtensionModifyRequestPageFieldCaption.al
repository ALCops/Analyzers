reportextension 50100 MyReportExt extends MyReport
{
    requestpage
    {
        layout
        {
            modify(MyRequestField)
            {
                [|Caption = 'My Modified Request Page Field'|];
            }
        }
    }
}

report 50100 MyReport
{
    ProcessingOnly = true;

    dataset
    {
        dataitem(MyDataItem; MyTable) { }
    }

    requestpage
    {
        layout
        {
            area(Content)
            {
                field(MyRequestField; MyGlobal) { }
                field(MyRequestOption; MyOptionGlobal) { }
            }
        }
    }

    var
        MyGlobal: Text[100];
        MyOptionGlobal: Option First,Second;
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
