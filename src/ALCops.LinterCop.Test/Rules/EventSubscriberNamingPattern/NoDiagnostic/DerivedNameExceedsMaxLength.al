// AL304 guard: when the derived name would exceed 120 characters, the analyzer stays
// silent so it never suggests a rename that would itself violate the AL identifier length
// limit. The default template renders {Event Source}_{EventName}, i.e.
//   "Sales Line Archive Line Fields"_OnAfterCalculateVerySpecificValueUsingComplexBusinessLogicWithLotsOfSubsystemInvolvementDone1234
// That's 30 + 1 + 96 = 127 characters — over the 120 budget. The wrongly-named subscriber
// below must not be flagged since there is no valid rename to suggest.
codeunit 50101 "Sales Line Archive Line Fields"
{
    [IntegrationEvent(false, false)]
    procedure OnAfterCalculateVerySpecificValueUsingComplexBusinessLogicWithLotsOfSubsystemInvolvementDone1234()
    begin
    end;
}

codeunit 50100 MySubscriber
{
    [EventSubscriber(ObjectType::Codeunit, Codeunit::"Sales Line Archive Line Fields", OnAfterCalculateVerySpecificValueUsingComplexBusinessLogicWithLotsOfSubsystemInvolvementDone1234, '', false, false)]
    local procedure [|WronglyNamedButUnfixable|]()
    begin
    end;
}
