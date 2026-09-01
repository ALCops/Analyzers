page 50110 TrigBoolMissRetPg
{
    PageType = Card;
    SourceTable = MyBuffer;
    ApplicationArea = All;

    trigger [|OnQueryClosePage|](CloseAction: Action): Boolean
    begin
        if CloseAction = Action::LookupOK then
            exit(true);
    end;
}

page 50111 TrigBoolCaseNoElsePg
{
    PageType = Card;
    SourceTable = MyBuffer;
    ApplicationArea = All;

    trigger [|OnQueryClosePage|](CloseAction: Action): Boolean
    begin
        case CloseAction of
            Action::LookupOK:
                exit(true);

            Action::OK:
                exit(false);
        end;
    end;
}

page 50112 TrigBoolNestMissingPg
{
    PageType = Card;
    SourceTable = MyBuffer;
    ApplicationArea = All;

    trigger [|OnQueryClosePage|](CloseAction: Action): Boolean
    begin
        if CloseAction = Action::LookupOK then
            if true then
                exit(true)
            else if false then
                exit(false);
    end;
}

page 50113 TrigBoolAllPathsPg
{
    PageType = Card;
    SourceTable = MyBuffer;
    ApplicationArea = All;

    trigger [|OnQueryClosePage|](CloseAction: Action): Boolean
    begin
        if CloseAction = Action::LookupOK then
            exit(true)
        else
            exit(false);
    end;
}

page 50114 TrigBoolCaseAllPg
{
    PageType = Card;
    SourceTable = MyBuffer;
    ApplicationArea = All;

    trigger [|OnQueryClosePage|](CloseAction: Action): Boolean
    begin
        case CloseAction of
            Action::LookupOK:
                exit(true);

            Action::OK:
                exit(false);

            else
                exit(false);
        end;
    end;
}

page 50115 TrigBoolNestAllPg
{
    PageType = Card;
    SourceTable = MyBuffer;
    ApplicationArea = All;

    trigger [|OnQueryClosePage|](CloseAction: Action): Boolean
    begin
        if CloseAction = Action::LookupOK then
            if true then
                exit(true)
            else if false then
                exit(false)
            else
                exit(false)
        else
            exit(false);
    end;
}

table 50150 MyBuffer
{
    fields
    {
        field(1; "No."; Integer)
        {
        }
    }
}
