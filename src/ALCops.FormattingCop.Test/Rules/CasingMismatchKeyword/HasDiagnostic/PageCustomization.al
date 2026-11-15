table 50100 "My Table"
{
    fields
    {
        field(1; "My Field"; Integer) { }
        field(2; "My Other Field"; Integer) { }
    }
}

page 50100 "My Page"
{
    PageType = Card;
    SourceTable = "My Table";

    layout
    {
        area(Content)
        {
            field("My Field"; Rec."My Field") { }
            field("My Other Field"; Rec."My Other Field") { }
        }
    }

    actions
    {
        area(Processing)
        {
            action(MyAction) { }
            action(MyOtherAction) { }
        }
    }
}

[|PAGECUSTOMIZATION|] "My Page Customization" [|CUSTOMIZES|] "My Page"
{
    [|LAYOUT|]
    {
        [|MODIFY|]("My Field")
        {
            Visible = [|FALSE|];
        }
        [|MOVEAFTER|]("My Other Field"; "My Field")
    }

    [|ACTIONS|]
    {
        [|MODIFY|](MyAction)
        {
            Visible = [|TRUE|];
        }
    }
}
