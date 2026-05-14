codeunit 50100 MyCodeunit
{
    procedure ScheduleJobAction()
    var
        JobQueueEntry: Record MyTable;
        JobQueueEntryExists: Boolean;
    begin
        [|JobQueueEntry.SetLoadFields(JobQueueEntry."No.")|];
        JobQueueEntry.SetRange("No.", '001');
        JobQueueEntryExists := JobQueueEntry.FindFirst();
        if not JobQueueEntryExists then begin
            JobQueueEntry.Init();
            JobQueueEntry.Insert(true);
        end;
    end;
}

table 50100 MyTable
{
    fields
    {
        field(1; "No."; Code[20]) { }
        field(2; Description; Text[100]) { }
    }

    keys
    {
        key(PK; "No.") { Clustered = true; }
    }
}
