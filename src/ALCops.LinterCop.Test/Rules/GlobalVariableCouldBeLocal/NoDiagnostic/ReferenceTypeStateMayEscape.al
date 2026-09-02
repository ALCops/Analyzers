codeunit 50100 ReferenceTypeStateMayEscape
{
    var
        [|MyValues|]: List of [Integer];

    local procedure RebuildValues()
    begin
        Clear(MyValues);
        MyValues.Add(1);
        Message('%1', MyValues.Count());
    end;
}
