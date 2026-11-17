page 50106 MyPage
{
    SourceTable = MyTable;

    layout
    {
        area(Content)
        {
            field(MyField; [|Rec|].MyField) { }
        }
            area(FactBoxes)
            {
                systempart(Links; Links) { }
                systempart(Notes; Notes) { }
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
                    Message(Format([|Rec|].SystemId));
                end;
            }
        }
    }

    trigger OnOpenPage()
    begin
        [|Rec|].SetRange(MyField, 1);
    end;
}

table 50106 MyTable
{
    fields
    {
        field(1; MyField; Integer) { }
    }
}
