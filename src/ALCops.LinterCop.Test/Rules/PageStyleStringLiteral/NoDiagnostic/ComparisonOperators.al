codeunit 50100 MyCodeunit
{
    procedure MyProcedure(MyText: Text)
    begin
        if MyText = [|'None'|] then;
        if MyText <> [|'Standard'|] then;
        if MyText < [|'Strong'|] then;
        if MyText <= [|'StrongAccent'|] then;
        if MyText > [|'Attention'|] then;
        if [|'AttentionAccent'|] >= MyText then;
        if MyText in [[|'Favorable'|], [|'Unfavorable'|]] then;
        if MyText in [[|'Attention'|]..[|'Unfavorable'|]] then;
        if MyText = ([|'None'|]) then;
        if MyText <> (([|'Standard'|])) then;
        if MyText in [([|'Standard'|])] then;
    end;
}
