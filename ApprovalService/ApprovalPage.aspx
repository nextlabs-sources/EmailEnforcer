<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="ApprovalPage.aspx.cs" Inherits="ApprovalService.ApprovalPage" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
    <script type="text/javascript">
        var second = 10;
        var timer;
        function change() {
            second--;

            if (second > -1) {
                document.getElementById("spSecond").innerHTML = second;
                timer = setTimeout('change()', 1000);
            }
            else {
                window.close();
            }
        }
        function ClosePage()
        {
            timer = setTimeout('change()', 1000);
        }
</script>
</head>
<body>
    <form id="form1" runat="server">
        
    <div>
        
        <div id="divAllow" runat="server" visible="false">
            <p>Thank for you Review you are Allow this mail send!</p>
        </div>

        <div id="divDeny" runat="server" visible="false">
             <p>Thank for you Review you are Deny this mail send!</p>
        </div>

        <div id="divUnknow" runat="server" visible="false">
            <p>Thank for you Review !</p>
            <p>please click flower button</p>
            <div>
                <asp:Button ID="btAllow" runat="server" Text="Approval" OnClick="btAllow_Click" />
                &nbsp;
                &nbsp;
                &nbsp;
                &nbsp;
                <asp:Button ID="btDeny" runat="server" Text="NOT Approval" OnClick="btDeny_Click" />
            </div>
        </div>

        <div id="ErrorDiv" runat="server" visible="false">
            <p>Error:<span id="spError" runat="server"></span> </p>
        </div>

        <div id="divAutoClose" runat="server" visible="false">
            this page will auto close at <span id="spSecond"></span> Second
        </div>
    </div>
    </form>
</body>
</html>
