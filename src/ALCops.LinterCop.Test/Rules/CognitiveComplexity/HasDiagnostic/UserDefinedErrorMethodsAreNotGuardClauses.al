codeunit 50100 "Error Method Caller"
{
    procedure [|CheckValues|](Handler: Codeunit "Error Method Handler")
    begin
        if true then
            Handler.Error();
        if true then
            Handler.FieldError();
        if true then
            Handler.Error();
        if true then
            Handler.FieldError();
        if true then
            Handler.Error();
        if true then
            Handler.FieldError();
        if true then
            Handler.Error();
        if true then
            Handler.FieldError();
        if true then
            Handler.Error();
        if true then
            Handler.FieldError();
        if true then
            Handler.Error();
        if true then
            Handler.FieldError();
        if true then
            Handler.Error();
        if true then
            Handler.FieldError();
        if true then
            Handler.Error();
    end;
}

codeunit 50101 "Error Method Handler"
{
    procedure Error()
    begin
    end;

    procedure FieldError()
    begin
    end;
}