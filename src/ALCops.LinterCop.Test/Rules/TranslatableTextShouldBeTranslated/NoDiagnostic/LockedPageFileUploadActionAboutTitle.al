page 50100 MyPage
{
    actions
    {
        area(Processing)
        {
            fileuploadaction(MyFileUploadAction)
            {
                [|AboutTitle = 'About my file upload action', Locked = true|];

                trigger OnAction(Files: List of [FileUpload])
                begin
                end;
            }
        }
    }
}
