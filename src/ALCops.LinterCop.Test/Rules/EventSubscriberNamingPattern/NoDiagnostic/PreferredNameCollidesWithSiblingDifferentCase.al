// Collision guard (case-insensitive): the subscriber below currently carries a wrong name,
// and a sibling procedure "mypublisher_onsomething" already exists in the same codeunit —
// this differs only in casing from the canonical name "MyPublisher_OnSomething" the CodeFix
// would suggest. AL treats duplicate method identifiers case-insensitively, so applying the
// fix would produce a duplicate-identifier compile error. The analyzer must stay silent.
codeunit 50100 MySubscriber
{
    [EventSubscriber(ObjectType::Codeunit, Codeunit::MyPublisher, OnSomething, '', false, false)]
    local procedure [|WronglyNamedSubscriber|]()
    begin
    end;

    // Pre-existing plain procedure whose name differs from the canonical suggestion only
    // in casing. The case-insensitive collision guard must suppress the diagnostic.
    local procedure "mypublisher_onsomething"()
    begin
    end;
}

codeunit 50101 MyPublisher
{
    [IntegrationEvent(false, false)]
    procedure OnSomething()
    begin
    end;
}
