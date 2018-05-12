<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="AblAsaExportPage.aspx.cs" Inherits="AblAsaBillingExportUtility.AblAsaExportPage" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Able to ASA Export</title>
    <link rel="stylesheet" href="http://code.jquery.com/ui/1.10.0/themes/base/jquery-ui.css" />
     <script type="text/javascript" src="http://code.jquery.com/jquery-1.8.3.js"></script>
     <script type="text/javascript" src="http://code.jquery.com/ui/1.10.0/jquery-ui.js"></script>
    <script type="text/javascript">

        $(function () {

            $("#BeginDateTextBox").datepicker();
            $("#EndDateTextBox").datepicker();
        });  

    </script>
</head>
<body>
    
    <form id="form1" runat="server">
    <!--<div>
        <asp:Label ID="BeginDateLabel" runat="server" Text="Begin Date: "></asp:Label><asp:TextBox ID="BeginDateTextBox" runat="server"></asp:TextBox><br /><br />
        <asp:Label ID="EndDateLabel" runat="server" Text="End Date: "></asp:Label><asp:TextBox ID="EndDateTextBox" runat="server"></asp:TextBox><br /><br />
    </div>-->
    <div>
        <asp:GridView ID="gvAbleAsa" runat="server" ShowHeaderWhenEmpty="true" AutoGenerateColumns="False">
            <headerstyle backcolor="LightGray"/>
            <Columns>
                <asp:BoundField HeaderText="Type" DataField="RecordType" />
                <asp:BoundField HeaderText="Order Number" DataField="OrderNumber" />
                <asp:BoundField DataField="CustomerNumber" HeaderText="Customer No" />
                <asp:BoundField DataField="ProductCode" HeaderText="Product Code" />
                <asp:BoundField DataField="Quantity" HeaderText="Quantity" />
                <asp:BoundField DataField="UnitPrice" HeaderText="Unit Price" />
            </Columns>
        </asp:GridView><br /><br />
    </div>
    <div>
        <asp:Button ID="ExportButton" runat="server" Text="Export" 
            onclick="ExportButton_Click" />
        <asp:Button ID="clearBtn" runat="server" onclick="clearBtn_Click" 
            Text="Clear" />
    </div>
    </form>
</body>
</html>
