codeunit 50100 ParameterCommentCases
{
    procedure LineComments(
        // before the previous parameter
        MyInteger: Integer; // on the parameter being removed
        // after the parameter being removed
        // before the next parameter
        MyDate: Date) // on the next parameter
        // after the next parameter
    begin
        MyInteger := 1;
        MyDate := Today();
    end;

    procedure BlockComments(
        /* before the previous parameter */ MyInteger: Integer /* on the previous parameter */;
        /* before the next parameter */ MyDate: Date /* on the next parameter */ /* after the next parameter */)
    begin
        MyInteger := 1;
        MyDate := Today();
    end;
}