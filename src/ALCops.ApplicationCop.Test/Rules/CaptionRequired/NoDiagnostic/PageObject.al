page 50100 [|MyPage|]
{
    Caption = 'My Page';
    SourceTable = MyTable;

    layout
    {
        area(Content)
        {
            field([|MyField|];Rec.MyField)
            {
                // No Caption property here as it is inherited from the table
            }
            group([|MyGroup|])
            {
                Caption = 'My Group';
                field([|MyVarField|]; MyVarField)
                {
                    Caption = 'My Var Field';
                }
            }
        }
    }
    
    actions
    {
        area(Processing)
        {
            group([|MyActionGroup|])
            {
                Caption = 'My Action Group';
                action([|MyAction|])
                {
                    Caption = 'My Action';
                }
            }
        }
    }

    var
        MyVarField: Text;
}

table 50100 MyTable { fields { field(1; MyField; Code[20]) { Caption = 'My Field'; } } }