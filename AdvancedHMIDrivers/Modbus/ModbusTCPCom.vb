Imports MfgControl.AdvancedHMI.Drivers.Common
Imports MfgControl.AdvancedHMI.Drivers.Modbus

'******************************************************************************
'* Modbus TCP Protocol Implementation
'*
'* Archie Jacobs
'* Manufacturing Automation, LLC
'* ajacobs@mfgcontrol.com
'* 13-OCT-11
'*
'* Copyright 2011 Archie Jacobs
'*
'* Implements driver for communication to ModbusTCP devices
'* 5-MAR-12 Fixed a bug where ReadAny would call itself instead of the overload
'* 9-JAN-13 When TNS was over 255 it would go out of bounds in Transactions array
'* 27-JAN-13 Add the second byte that some require for writing to bits
'*******************************************************************************
Public Class ModbusTCPCom
    Inherits ModbusBase
    '* Use a shared Data Link Layer so multiple instances will not create multiple connections
    Private Shared DLL As List(Of MfgControl.AdvancedHMI.Drivers.ModbusTCP.ModbusTcpDataLinkLayer)
    Private MyDLLInstance As Integer
    Protected Friend EventHandlerDLLInstance As Integer


#Region "Properties"
    Private m_IPAddress As String = "0.0.0.0"   '* this is a default value
    <System.ComponentModel.Category("Communication Settings")> _
    Public Property IPAddress() As String
        Get
            Return m_IPAddress.ToString
        End Get
        Set(ByVal value As String)
            m_IPAddress = value

            If Not Me.DesignMode Then
                '* If a new instance needs to be created, such as a different AMS Address
                CreateDLLInstance()


                If DLL.Count > MyDLLInstance AndAlso DLL(MyDLLInstance) IsNot Nothing Then
                    DLL(MyDLLInstance).IPAddress = value
                End If
            End If
        End Set
    End Property

    Private m_TcpipPort As UInt16 = 502
    <System.ComponentModel.Category("Communication Settings")> _
    Public Property TcpipPort() As UInt16
        Get
            Return m_TcpipPort
        End Get
        Set(ByVal value As UInt16)
            m_TcpipPort = value

            If Not Me.DesignMode Then
                '* If a new instance needs to be created, such as a different AMS Address
                CreateDLLInstance()


                If DLL.Count > MyDLLInstance AndAlso DLL(MyDLLInstance) IsNot Nothing Then
                    DLL(MyDLLInstance).Port = value
                End If
            End If
        End Set
    End Property

    Private m_UnitId As Byte
    <System.ComponentModel.Category("Communication Settings")> _
    Public Property UnitId() As Byte
        Get
            Return m_UnitId
        End Get
        Set(ByVal value As Byte)
            m_UnitId = value
        End Set
    End Property

#End Region

#Region "ConstructorDestructor"
    Public Sub New(ByVal container As System.ComponentModel.IContainer)
        MyClass.New()

        '* Default UnitID
        m_UnitId = 1

        'Required for Windows.Forms Class Composition Designer support
        container.Add(Me)
    End Sub

    Public Sub New()
        MyBase.New()

        '* Default UnitID
        m_UnitId = 1

        If DLL Is Nothing Then
            DLL = New List(Of MfgControl.AdvancedHMI.Drivers.ModbusTCP.ModbusTcpDataLinkLayer)
        End If
    End Sub

    'Component overrides dispose to clean up the component list.
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        '* The handle linked to the DataLink Layer has to be removed, otherwise it causes a problem when a form is closed
        If DLL.Count > MyDLLInstance AndAlso DLL(MyDLLInstance) IsNot Nothing Then
            RemoveHandler DLL(MyDLLInstance).DataReceived, AddressOf DataLinkLayerDataReceived
            RemoveHandler DLL(MyDLLInstance).ComError, AddressOf DataLinkLayerComError

            DLL(MyDLLInstance).Dispose()
        End If

        MyBase.Dispose(disposing)
    End Sub

    '***************************************************************
    '* Create the Data Link Layer Instances
    '* if the IP Address is the same, then resuse a common instance
    '***************************************************************
    Protected Overrides Sub CreateDLLInstance()
        '* Still default, so ignore
        If m_IPAddress = "0.0.0.0" Then Exit Sub

        'If DLL.Count <= 0 OrElse (DLL(MyDLLInstance).IPAddress <> m_IPAddress Or DLL(MyDLLInstance).Port <> m_TcpipPort) Then

        If DLL.Count > 0 Then
            '* At least one DLL instance already exists,
            '* so check to see if it has the same IP address
            '* if so, reuse the instance, otherwise create a new one
            Dim i As Integer
            While i < DLL.Count AndAlso DLL(i) IsNot Nothing AndAlso (DLL(i).IPAddress <> m_IPAddress Or DLL(i).Port <> m_TcpipPort)
                i += 1
            End While
            MyDLLInstance = i
        End If

        If MyDLLInstance >= DLL.Count Then
            Dim NewDLL As New MfgControl.AdvancedHMI.Drivers.ModbusTCP.ModbusTcpDataLinkLayer(m_IPAddress, m_TcpipPort)
            DLL.Add(NewDLL)
        End If

        '* Have we already attached event handler to this data link layer?
        If EventHandlerDLLInstance <> (MyDLLInstance + 1) Then
            '* If event handler to another layer has been created, remove them
            If EventHandlerDLLInstance > 0 Then
                RemoveHandler DLL(EventHandlerDLLInstance).DataReceived, AddressOf DataLinkLayerDataReceived
                RemoveHandler DLL(EventHandlerDLLInstance).ComError, AddressOf DataLinkLayerComError
            End If

            AddHandler DLL(MyDLLInstance).DataReceived, AddressOf DataLinkLayerDataReceived
            AddHandler DLL(MyDLLInstance).ComError, AddressOf DataLinkLayerComError
            EventHandlerDLLInstance = MyDLLInstance + 1
        End If
        'End If
    End Sub
#End Region

#Region "Private Methods"
    Friend Overrides Function SendRequest(ByVal PDU As ModbusPDUFrame) As Integer
        '* If a Subscription (Internal Request) begin to overflow the que, ignore some
        '* This can occur from too fast polling
        'If DLL.Count <= 0 Then
        '    CreateDLLInstance()
        'End If

        Dim TCPFrame As MfgControl.AdvancedHMI.Drivers.ModbusTCP.ModbusTCPFrame
        TCPFrame = New MfgControl.AdvancedHMI.Drivers.ModbusTCP.ModbusTCPFrame(PDU, MyObjectID)

        'If (DLL(MyDLLInstance).SendQueDepth < 50) Then
        Return DLL(MyDLLInstance).SendData(TCPFrame)
        ' Return True
        'Else
        'Throw New PLCDriverException("Send Que Full")
        'End If
    End Function

    Protected Overrides Function GetNextTransactionID(ByVal maxValue As Integer) As Integer
        'If DLL.Count <= 0 OrElse (DLL(MyDLLInstance).Port <> m_TcpipPort) Then
        'CreateDLLInstance()
        'End If

        If DLL.Count > MyDLLInstance Then
            Return DLL(MyDLLInstance).GetNextTransactionNumber(maxValue)
        Else
            Return 0
        End If
    End Function

    Protected Overrides Function IsInQue(transactionNumber As Integer, ownerObjectID As Long) As Boolean
        Return False
        'Return DLL(MyDLLInstance).IsInQue(transactionNumber, ownerObjectID)
    End Function
#End Region

#Region "Events"
    '************************************************
    '* Process data recieved from controller
    '************************************************
    Private Sub DataLinkLayerDataReceived(ByVal sender As Object, ByVal e As MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs)

        Dim TCP As New MfgControl.AdvancedHMI.Drivers.ModbusTCP.ModbusTCPFrame(New List(Of Byte)(e.RawData).ToArray, e.RawData.Length)

        ProcessDataReceived(TCP.PDU, e)
    End Sub

#End Region

End Class