codeunit 50100 ConditionalParameterNoFix
{
    procedure RemoveParameter(
#if not ACTIVE
        [|MyText: Text|];
#else
        InactiveParameter: Date;
#endif
        MyInteger: Integer)
    begin
        MyInteger := 1;
    end;
}