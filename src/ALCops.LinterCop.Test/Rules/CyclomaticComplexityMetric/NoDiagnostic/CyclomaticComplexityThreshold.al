codeunit 50200 MyCodeunit
{
    procedure [|MyProcedure()|]
    begin
        if true then;
        if true then;
        if true then;
        if true then;
        if true then;
        if true then;
        // if true then; // One less if statement should be just below the default threshold of 8
    end;
}