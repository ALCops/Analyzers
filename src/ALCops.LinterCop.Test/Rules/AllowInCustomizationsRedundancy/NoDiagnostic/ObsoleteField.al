table 50100 MyTable
{
    AllowInCustomizations = Never;
    ObsoleteState = Pending;
    ObsoleteReason = 'Obsolete';

    fields
    {
        field(1; [|MyField|]; Integer)
        {
            AllowInCustomizations = Never;
            ObsoleteState = Pending;
            ObsoleteReason = 'Obsolete';
        }
    }
}
