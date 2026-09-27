table 50100 MyTable
{
    fields
    {
        field(1; "No."; Code[20]) { }
        field(2; MyField; Text[100])
        {
            [|Caption = 'My Field'|];
            [|ToolTip = 'Specifies my field.'|];
            ObsoleteState = Pending;
            ObsoleteReason = 'Replaced by MyNewField.';
        }
    }
}
