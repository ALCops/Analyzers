page 50106 MyPage
{
    SourceTable = MyTable;

    layout
    {
        area(Content)
        {
            field(MyField; [|rec|].MyField) { }
        }
    }

    actions
    {
        area(Processing)
        {
            action(MyAction)
            {
                trigger OnAction()
                begin
                    Message(Format([|rec|].SystemId));
                end;
            }
        }
    }

    trigger OnOpenPage()
    begin
        [|REC|].SetRange(MyField, 1);
    end;
}

table 50106 MyTable
{
    fields
    {
        field(1; MyField; Integer) { }
    }
}
