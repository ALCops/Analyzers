table 50100 TryFunctionImplicitRec
{
    fields
    {
        field(1; MyField; Integer)
        {
        }
    }

    [TryFunction]
    procedure TryWork()
    begin
        [|Insert()|];
        [|Modify|];
    end;
}
