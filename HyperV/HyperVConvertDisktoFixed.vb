Imports Ayehu.Sdk.ActivityCreation.Interfaces
Imports Ayehu.Sdk.ActivityCreation.Extension
Imports System.Text
Imports System
Imports System.Xml
Imports System.Data
Imports System.IO
Imports System.Management
Imports System.Collections.Generic
Imports Microsoft.VisualBasic
Imports System.Management.Automation
Imports System.Management.Automation.Runspaces

Namespace Ayehu.Sdk.ActivityCreation
    Public Class ActivityClass
        Implements IActivity


        Public HostName As String
        Public UserName As String
        Public Password As String
        Public VMName As String
        Public SCVMM As String
        Public VDD As String

        Public Function Execute() As ICustomActivityResult Implements IActivity.Execute


            Dim sw As StringWriter = New StringWriter()
            Dim dt As DataTable = New DataTable("resultSet")
            dt.Columns.Add("Result", GetType(String))

            Dim serverUri As System.Uri = New Uri("http://" + HostName + ":5985/wsman")
            Dim shellUri As String = "http://schemas.microsoft.com/powershell/Microsoft.PowerShell"

            Dim securePassword As System.Security.SecureString = New System.Security.SecureString()
            For Each c As Char In Password.ToCharArray()
                securePassword.AppendChar(c)
            Next

            Dim creds As PSCredential = New PSCredential(UserName, securePassword)

            Dim rc As RunspaceConfiguration = RunspaceConfiguration.Create()

            Dim wsManInfo As WSManConnectionInfo = New WSManConnectionInfo(serverUri, shellUri, creds)
            wsManInfo.SkipCNCheck = True
            wsManInfo.SkipCACheck = True

            Dim runspace As Object = RunspaceFactory.CreateRunspace(wsManInfo)
            runspace.Open()
            Dim powershell As PowerShell = powershell.Create()
            powershell.Runspace = runspace

            powershell.AddScript(GetScript(VMName, VDD))
            Dim results As Object = powershell.Invoke()

            If powershell.Streams.Error.Count > 0 Then
                Dim sw3 As New StringWriter
                For Each errorRecord As Object In powershell.Streams.Error
                    sw3.WriteLine(errorRecord.ToString())
                Next
                runspace.close()
                powershell.Dispose()
                Throw New Exception(sw3.ToString)
            End If

            runspace.close()
            powershell.Dispose()
            dt.Rows.Add("Success")

            Return Me.GenerateActivityResult(dt)


        End Function

        Function GetScript(VMName As String, VDD As String) As String

            Dim sw As New StringWriter

            sw.WriteLine("$VirtDiskDrive = Get-SCVirtualDiskDrive -VM (Get-SCVirtualMachine -Name " + Chr(34) + VMName + Chr(34) + " | where {$_.VirtualHardDisks -match " + Chr(34) + VDD + Chr(34) + "} )")

            sw.WriteLine("Convert-SCVirtualDiskDrive -VirtualDiskDrive $VirtDiskDrive -Fixed")

            Dim result As String = sw.ToString
            Return result
        End Function

    End Class
End Namespace

