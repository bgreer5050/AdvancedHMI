Imports System.Windows.Forms.Design
Imports System.Drawing.Design

Public Class FormChangeButton
    Inherits Button
    Implements IDisposable

    Dim ht As Windows.Forms.Timer

    Public Sub New()
        ht = New Windows.Forms.Timer
        ht.Interval = 100
        AddHandler ht.Tick, AddressOf HideForm
    End Sub

#Region "Properties"
    Private m_Passcode As String
    Public Property Passcode As String
        Get
            Return m_Passcode
        End Get
        Set(value As String)
            m_Passcode = value
        End Set
    End Property

    Private m_FormToOpen As Type
    <System.ComponentModel.EditorAttribute(GetType(FormListEditor), GetType(System.Drawing.Design.UITypeEditor))> _
    Public Property FormToOpen() As Type
        Get
            Return m_FormToOpen
        End Get
        Set(ByVal value As Type)
            m_FormToOpen = value
        End Set
    End Property
#End Region

#Region "PLC Related Properties"
    '*****************************************************
    '* Property - Component to communicate to PLC through
    '*****************************************************
    Private m_CommComponent As AdvancedHMIDrivers.IComComponent
    <System.ComponentModel.Category("PLC Properties")> _
    Public Property CommComponent() As AdvancedHMIDrivers.IComComponent
        Get
            Return m_CommComponent
        End Get
        Set(ByVal value As AdvancedHMIDrivers.IComComponent)
            If m_CommComponent IsNot value Then
                If SubScriptions IsNot Nothing Then
                    SubScriptions.UnsubscribeAll()
                End If

                m_CommComponent = value

                SubscribeToCommDriver()
            End If
        End Set
    End Property

    '*****************************************
    '* Property - Address in PLC to Link to
    '*****************************************
    Private InvertVisible As Boolean
    Private _PLCAddressVisible As String = ""
    <System.ComponentModel.Category("PLC Properties")> _
    Public Property PLCAddressVisible() As String
        Get
            Return _PLCAddressVisible
        End Get
        Set(ByVal value As String)
            If _PLCAddressVisible <> value Then
                _PLCAddressVisible = value

                '* When address is changed, re-subscribe to new address
                SubscribeToCommDriver()
            End If
        End Set
    End Property
#End Region

#Region "Constructor/Destructor"
    '****************************************************************
    '* UserControl overrides dispose to clean up the component list.
    '****************************************************************
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing Then
                If SubScriptions IsNot Nothing Then
                    SubScriptions.dispose()
                End If
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub
#End Region

#Region "Events"
    Protected Overrides Sub OnClick(e As System.EventArgs)
        If m_FormToOpen IsNot Nothing Then
            If m_Passcode <> "" Then
                '* Create a keypad to prompt for passcode
                Dim kpd As New MfgControl.AdvancedHMI.Controls.Keypad

                kpd.Text = "Enter pass code to continue"

                kpd.StartPosition = FormStartPosition.CenterParent

                If kpd.ShowDialog() = Windows.Forms.DialogResult.OK And kpd.Value = m_Passcode Then
                    MyBase.OnClick(e)

                    OpenForm()
                End If
            Else
                OpenForm()
            End If
        End If
    End Sub

    Private Sub OpenForm()
        If FormToOpen IsNot Nothing Then
            Dim index As Integer
            Dim f = My.Application.OpenForms
            While index < f.Count AndAlso f(index).GetType.Name <> m_FormToOpen.Name
                index += 1
            End While

            If index >= f.Count Then
                Dim NewForm As Form = System.Activator.CreateInstance(m_FormToOpen)
                NewForm.Show()
            Else
                f(index).Show()
                If f(index).WindowState = FormWindowState.Minimized Then
                    f(index).WindowState = FormWindowState.Normal
                End If
                f(index).BringToFront()
            End If

            'System.Threading.Thread.Sleep(500)

            '* Keep going up the tree until we find the top level parent
            pf = Parent
            While (pf IsNot Nothing) AndAlso (Not (TypeOf (pf) Is Form))
                If pf.Parent IsNot Nothing Then
                    pf = pf.Parent
                Else
                    Exit While
                End If
            End While

            '* Hide the form this event came from
            '* It may be embedded in other containers
            If pf IsNot Nothing Then
                '*If it is the same form, then do not hide 04-aug-14
                If index < f.Count AndAlso (f(index) IsNot pf) Then
                    '* Delay hiding the previous form so that it transitions busy forms smoother
                    ht.Start()
                End If
            End If
        End If
    End Sub

    Dim pf As Object
    Private Sub HideForm(ByVal e As Object, ByVal ef As EventArgs)
        e.stop()
        If pf IsNot Nothing Then pf.hide()
    End Sub
#End Region


#Region "Subscribing and PLC data receiving"
    Private SubScriptions As AdvancedHMIControls.SubscriptionHandler
    '*******************************************************************************
    '* Subscribe to addresses in the Comm(PLC) Driver
    '* This code will look at properties to find the "PLCAddress" + property name
    '*
    '*******************************************************************************
    Private Sub SubscribeToCommDriver()
        If Not DesignMode And IsHandleCreated Then
            '* Create a subscription handler object
            If SubScriptions Is Nothing Then
                SubScriptions = New AdvancedHMIControls.SubscriptionHandler
                SubScriptions.CommComponent = m_CommComponent
                AddHandler SubScriptions.DisplayError, AddressOf DisplaySubscribeError
            End If

            '* Check through the properties looking for PLCAddress***, then see if the suffix matches an existing property
            Dim p() As Reflection.PropertyInfo = Me.GetType().GetProperties

            For i As Integer = 0 To p.Length - 1
                '* Does this property start with "PLCAddress"?
                If p(i).Name.IndexOf("PLCAddress", StringComparison.CurrentCultureIgnoreCase) = 0 Then
                    '* Get the property value
                    Dim PLCAddress As String = p(i).GetValue(Me, Nothing)
                    If PLCAddress <> "" Then
                        '* Get the text in the name after PLCAddress
                        Dim PropertyToWrite As String = p(i).Name.Substring(10)
                        Dim j As Integer = 0
                        '* See if there is a corresponding property with the extracted name
                        While j < p.Length AndAlso p(j).Name <> PropertyToWrite
                            j += 1
                        End While

                        '* If the proprty was found, then subscribe to the PLC Address
                        If j < p.Length Then
                            SubScriptions.SubscribeTo(PLCAddress, AddressOf PolledDataReturned, PropertyToWrite)
                        End If
                    End If
                End If
            Next
        End If
    End Sub

    '***************************************
    '* Call backs for returned data
    '***************************************
    Private OriginalText As String
    Private Sub PolledDataReturned(ByVal sender As Object, ByVal e As AdvancedHMIControls.SubscriptionHandlerEventArgs)
        If e.PLCComEventArgs.ErrorId = 0 Then
            Try
                '* Write the value to the property that came from the end of the PLCAddress... property name
                Me.GetType().GetProperty(e.SubscriptionDetail.PropertyNameToSet). _
                            SetValue(Me, CTypeDynamic(e.PLCComEventArgs.Values(0), Me.GetType().GetProperty(e.SubscriptionDetail.PropertyNameToSet).PropertyType), Nothing)
            Catch ex As Exception
                DisplayError("INVALID VALUE RETURNED!" & e.PLCComEventArgs.Values(0))
            End Try
        Else
            DisplayError("Com Error. " & e.PLCComEventArgs.ErrorMessage)
        End If
    End Sub

    Private Sub DisplaySubscribeError(ByVal sender As Object, ByVal e As MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs)
        DisplayError(e.ErrorMessage)
    End Sub
#End Region

#Region "Error Display"
    '********************************************************
    '* Show an error via the text property for a short time
    '********************************************************
    Private WithEvents ErrorDisplayTime As System.Windows.Forms.Timer
    Private Sub DisplayError(ByVal ErrorMessage As String)
        If ErrorDisplayTime Is Nothing Then
            ErrorDisplayTime = New System.Windows.Forms.Timer
            AddHandler ErrorDisplayTime.Tick, AddressOf ErrorDisplay_Tick
            ErrorDisplayTime.Interval = 5000
        End If

        '* Save the text to return to
        If Not ErrorDisplayTime.Enabled Then
            OriginalText = Me.Text
        End If

        ErrorDisplayTime.Enabled = True

        MyBase.Text = ErrorMessage
    End Sub


    '**************************************************************************************
    '* Return the text back to its original after displaying the error for a few seconds.
    '**************************************************************************************
    Private Sub ErrorDisplay_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ErrorDisplayTime.Tick
        Text = OriginalText

        If ErrorDisplayTime IsNot Nothing Then
            ErrorDisplayTime.Enabled = False
            ErrorDisplayTime.Dispose()
            ErrorDisplayTime = Nothing
        End If
    End Sub
#End Region

End Class

<System.Security.Permissions.PermissionSetAttribute(System.Security.Permissions.SecurityAction.Demand, Name:="FullTrust")> _
Public Class FormListEditor
    Inherits System.Drawing.Design.UITypeEditor

    Public Sub New()
    End Sub

    ' Indicates whether the UITypeEditor provides a form-based (modal) dialog, 
    ' drop down dialog, or no UI outside of the properties window.
    Public Overloads Overrides Function GetEditStyle(ByVal context As System.ComponentModel.ITypeDescriptorContext) As System.Drawing.Design.UITypeEditorEditStyle
        Return UITypeEditorEditStyle.DropDown
    End Function

    ' Displays the UI for value selection.
    Dim edSvc As IWindowsFormsEditorService
    Private WithEvents lb As ListBox
    Private ListOfForms As New List(Of Type)

    Public Overloads Overrides Function EditValue(ByVal context As System.ComponentModel.ITypeDescriptorContext, ByVal provider As System.IServiceProvider, ByVal value As Object) As Object
        ' Uses the IWindowsFormsEditorService to display a 
        ' drop-down UI in the Properties window.
        edSvc = CType(provider.GetService(GetType(IWindowsFormsEditorService)), IWindowsFormsEditorService)
        lb = New ListBox

        'Dim x = My.Forms
        'For Each mm As Form In My.Forms

        'Next


        If (edSvc IsNot Nothing) Then
            'Reflection.Assembly.GetEntryAssembly()
            Dim MyTypes As Type() = Reflection.Assembly.GetExecutingAssembly.GetTypes
            For Each mType As Type In MyTypes
                If mType.BaseType Is GetType(Form) Then
                    ListOfForms.Add(mType.UnderlyingSystemType)
                    lb.Items.Add(mType.UnderlyingSystemType.Name)
                End If
            Next

            AddHandler lb.SelectedIndexChanged, AddressOf ListItemSelected

            lb.Height = lb.ItemHeight * (lb.Items.Count + 1)

            edSvc.DropDownControl(lb)
        End If

        If lb.SelectedIndex >= 0 And lb.SelectedIndex < ListOfForms.Count Then
            Return ListOfForms(lb.SelectedIndex)
        Else
            Return Nothing
        End If
    End Function

    Private Sub ListItemSelected(ByVal sender As Object, ByVal e As System.EventArgs)
        edSvc.CloseDropDown()
    End Sub



    ' Indicates whether the UITypeEditor supports painting a 
    ' representation of a property's value.
    Public Overloads Overrides Function GetPaintValueSupported(ByVal context As System.ComponentModel.ITypeDescriptorContext) As Boolean
        Return False
    End Function



End Class

