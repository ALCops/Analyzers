page 50100 [|MyPage|]
{
    SourceTable = MyTable;

    layout
    {
        area(Content)
        {
            field([|MyField|];Rec.MyField)
            {
            }
            group([|MyGroup|])
            {
                field([|MyVarField|]; MyVarField)
                {
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
                action([|MyAction|])
                {
                }
            }
        }
    }

    var
        MyVarField: Text;
}


table 50100 MyTable { fields { field(1; MyField; Code[20]) { } } }