table 50100 MyTable
{
    fields
    {
        field(1; Name; Text[100])
        {
            trigger OnValidate()
            begin
                DoSomething([|xRec|]);
            end;
        }
    }

    procedure DoSomething(Other: Record MyTable)
    begin
    end;
}
