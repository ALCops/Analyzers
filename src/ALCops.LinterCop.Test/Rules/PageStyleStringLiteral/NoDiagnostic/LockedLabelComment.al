codeunit 50100 MyCodeunit
{
    var
        HelloTok: Label 'Hello', Locked = true, Comment = [|'Standard'|];
}

report 50100 MyReport
{
    ProcessingOnly = true;

    labels
    {
        HelloLbl = 'Hello', Locked = true, Comment = [|'Strong'|];
    }

    trigger OnPreReport()
    begin
    end;
}
