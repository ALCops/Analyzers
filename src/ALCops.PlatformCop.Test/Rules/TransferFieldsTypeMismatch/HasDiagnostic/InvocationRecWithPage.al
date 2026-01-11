page 50100 MyPage
{
    SourceTable = MyTableA;

    actions
    {
        area(Processing)
        {
            action(MyAction)
            {
                ApplicationArea = All;

                trigger OnAction()
                var
                    FromRec: Record MyTableB;
                begin
                    [|Rec.TransferFields(FromRec)|];
                end;
            }
        }
    }

    var
        GlobalFromRec: Record MyTableB;

    trigger OnAfterGetRecord()
    begin
        [|Rec.TransferFields(GlobalFromRec)|];
    end;
}

table 50100 MyTableA
{
    fields
    {
        field(1; "Primary Key"; Code[20]) { }
        [|field(2; MyField; Integer) { }|]
    }
}

table 50101 MyTableB
{
    fields
    {
        field(1; "Primary Key"; Code[20]) { }
        [|field(2; MyField; Boolean) { }|] // Same ID (2) as in MyTableA, different type
    }
}