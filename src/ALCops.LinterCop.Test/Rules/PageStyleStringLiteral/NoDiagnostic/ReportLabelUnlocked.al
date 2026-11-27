report 50100 MyReport
{
    ProcessingOnly = true;

    labels
    {
        StandardType = [|'Standard'|];
        NoneType = [|'None'|], Comment = 'Type of the row';
    }

    trigger OnPreReport()
    begin
    end;
}
