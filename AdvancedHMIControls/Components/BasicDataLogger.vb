Imports System.ComponentModel
'*****************************************************************************
'* Simple Data Logger
'*
'* Archie Jacobs
'* Manufacturing Automation, LLC
'* 03-MAR-13
'* http://www.advancedhmi.com
'*
'* This component subscribes to a value in the PLC through a comm driver
'* and log it to a text file. It can log either by time interval or
'* data change.
'*
'* 03-MAR-13 Created
'*****************************************************************************
Public Class BasicDataLogger
    Inherits DataSubscriber

    Private sw As System.IO.StreamWriter

#Region "Properties"
    Private m_FileFolder As String = "C:"
    <BrowsableAttribute(True), EditorAttribute(GetType(FileFolderEditor), GetType(System.Drawing.Design.UITypeEditor))> _
    Public Property FileFolder As String
        Get
            Return m_FileFolder
        End Get
        Set(value As String)
            If value.Length > 0 Then
                '* Remove the last back slash if it is there
                If value.Substring(value.Length - 1, 1) = "\" Then value = value.Substring(0, value.Length - 1)
                m_FileFolder = value
            End If
        End Set
    End Property

    Private m_FileName As String = "PLCDataLog.log"
    Public Property FileName As String
        Get
            Return m_FileName
        End Get
        Set(value As String)
            m_FileName = value
        End Set
    End Property

    Public Enum TriggerType
        TimeInterval
        DataChange
        WriteOnBitTrue
    End Enum
    Private m_LogTriggerType As TriggerType
    Public Property LogTriggerType As TriggerType
        Get
            Return m_LogTriggerType
        End Get
        Set(value As TriggerType)
            m_LogTriggerType = value
        End Set
    End Property

    Private LogTimer As Timer
    Private m_LogInterval As Integer = 1000
    Public Property LogInterval As Integer
        Get
            Return m_LogInterval
        End Get
        Set(value As Integer)
            m_LogInterval = value
        End Set
    End Property

    Private m_Prefix As String
    Public Property Prefix As String
        Get
            Return m_Prefix
        End Get
        Set(value As String)
            m_Prefix = value
        End Set
    End Property

    Private m_TimeStampFormat As String = "dd-MMM-yy HH:mm:ss"
    Public Property TimeStampFormat As String
        Get
            Return m_TimeStampFormat
        End Get
        Set(value As String)
            Try
                Dim TestString As String = Now.ToString("value")
                m_TimeStampFormat = value
            Catch ex As Exception
                MsgBox("Invalid DateTime format of " & value)
            End Try
        End Set
    End Property
#End Region

#Region "Constructor/Destructor"
    Protected Overrides Sub Dispose(disposing As Boolean)
        If LogTimer IsNot Nothing Then
            LogTimer.Enabled = False
        End If

        MyBase.Dispose(disposing)
    End Sub
#End Region

#Region "Events"
    Protected Overrides Sub onDataChanged(ByVal e As MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs)
        MyBase.OnDataChanged(e)

        If m_LogTriggerType = TriggerType.DataChange Then
            StoreValue()
        End If
    End Sub

    '* When the subscription with the PLC succeeded, setup for logging
    Protected Overrides Sub OnSuccessfulSubscription(e As MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs)
        MyBase.OnSuccessfulSubscription(e)

        '* create the timer to log the data
        If m_LogTriggerType = TriggerType.TimeInterval Then
            If LogTimer Is Nothing Then
                LogTimer = New Timer
                If m_LogInterval > 0 Then
                    LogTimer.Interval = m_LogInterval
                Else
                    LogTimer.Interval = 1000
                End If
                AddHandler LogTimer.Tick, AddressOf LogInterval_Tick

                LogTimer.Enabled = True
            End If
        End If

        '* Create the file for logging data
        Try
            sw = New System.IO.StreamWriter(m_FileFolder & "\" & m_FileName, True)
        Catch ex As Exception
            OnComError(New MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs(-101, ex.Message))
        End Try
    End Sub

    '* Timer tick interval used to store data at a periodic rate
    Private Sub LogInterval_Tick(sender As System.Object, e As System.EventArgs)
        StoreValue()
    End Sub

    Private Sub StoreValue()
        Try
            Dim StringToWrite As String = m_Prefix
            If m_TimeStampFormat <> "" Then StringToWrite &= Now.ToString(m_TimeStampFormat)
            StringToWrite &= "," & m_Value
            sw.WriteLine(StringToWrite)
            sw.Flush()
        Catch ex As Exception
        End Try
    End Sub
#End Region
End Class


Public Class FileFolderEditor
    Inherits System.Drawing.Design.UITypeEditor

    Public Sub New()
    End Sub

    ' Indicates whether the UITypeEditor provides a form-based (modal) dialog,  
    ' drop down dialog, or no UI outside of the properties window. 
    Public Overloads Overrides Function GetEditStyle(ByVal context As System.ComponentModel.ITypeDescriptorContext) As System.Drawing.Design.UITypeEditorEditStyle
        Return System.Drawing.Design.UITypeEditorEditStyle.Modal
    End Function

    ' Displays the UI for value selection. 
    Public Overloads Overrides Function EditValue(ByVal context As System.ComponentModel.ITypeDescriptorContext, ByVal provider As System.IServiceProvider, ByVal value As Object) As Object
        Dim fb As New FolderBrowserDialog
        fb.ShowDialog()

        Return fb.SelectedPath
    End Function

    Public Overrides Function GetPaintValueSupported(ByVal context As System.ComponentModel.ITypeDescriptorContext) As Boolean
        Return False
    End Function
End Class

