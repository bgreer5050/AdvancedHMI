Option Strict On
'***********************************************************************
'* Omron Host Link Com
'*
'* Copyright 2011 Archie Jacobs
'*
'* Reference : Omron W342-E1-15 (W342-E1-15+CS-CJ-CP-NSJ+RefManual.pdf)
'* Revision February 2010
'*
'***********************************************************************
'Imports OmronDriver.Common
Namespace Omron
    Public Class OmronEthernetFINSCom
        Inherits Omron.FINSBaseCom
        Implements IComComponent

        Private Shared DLL As List(Of MfgControl.AdvancedHMI.Drivers.Omron.FinsTcpDataLinkLayer)
        Protected Shared InstanceCount As Integer


#Region "Properties"
        Private m_IPAddress As String = "192.168.0.1"
        <System.ComponentModel.Category("Communication Settings")> _
        Public Property IPAddress() As String
            Get
                Return m_IPAddress
            End Get
            Set(ByVal value As String)
                m_IPAddress = value

                CreateDLLInstance()

                If DLL.Count > 0 AndAlso DLL(MyDLLInstance) IsNot Nothing Then
                    DLL(MyDLLInstance).IPAddress = value
                End If
            End Set
        End Property
#End Region

#Region "Constructor"
        Public Sub New()
            MyBase.new()

            If DLL Is Nothing Then
                DLL = New List(Of MfgControl.AdvancedHMI.Drivers.Omron.FinsTcpDataLinkLayer)
            End If

            TargetAddress = New MfgControl.AdvancedHMI.Drivers.Omron.DeviceAddress
            '* default port 1 (&HFC)
            SourceAddress = New MfgControl.AdvancedHMI.Drivers.Omron.DeviceAddress(0, &HFB, 0)

            InstanceCount += 1
        End Sub

        Public Sub New(ByVal container As System.ComponentModel.IContainer)
            MyClass.New()

            'Required for Windows.Forms Class Composition Designer support
            container.Add(Me)
        End Sub


        Protected Overrides Sub Dispose(ByVal disposing As Boolean)
            '* The handle linked to the DataLink Layer has to be removed, otherwise it causes a problem when a form is closed
            If DLL.Count > 0 AndAlso DLL(MyDLLInstance) IsNot Nothing Then
                RemoveHandler DLL(MyDLLInstance).DataReceived, AddressOf DataLinkLayerDataReceived
                RemoveHandler DLL(MyDLLInstance).ComError, AddressOf DataLinkLayerComError

                InstanceCount -= 1
                If InstanceCount <= 0 Then
                    DLL(MyDLLInstance).dispose(disposing)
                    DLL.Remove(DLL(MyDLLInstance))
                End If
            End If

            MyBase.Dispose(disposing)
        End Sub

#End Region


#Region "Private Methods"
        '***************************************************************
        '* Create the Data Link Layer Instances
        '* if the IP Address is the same, then resuse a common instance
        '***************************************************************
        Protected Overrides Sub CreateDLLInstance()
            'If Me.DesignMode Then Exit Sub
            '*** For Windows CE port, this checks designmode and works in full .NET also***
            If AppDomain.CurrentDomain.FriendlyName.IndexOf("DefaultDomain", System.StringComparison.CurrentCultureIgnoreCase) >= 0 Then
                Exit Sub
            End If

            If DLL.Count > 0 Then
                '* At least one DLL instance already exists,
                '* so check to see if it has the same IP address
                '* if so, reuse the instance, otherwise create a new one
                Dim i As Integer
                While i < DLL.Count AndAlso DLL(i) IsNot Nothing AndAlso DLL(i).IPAddress <> m_IPAddress
                    i += 1
                End While
                MyDLLInstance = i
            End If

            If MyDLLInstance >= DLL.Count Then
                Dim NewDLL As New MfgControl.AdvancedHMI.Drivers.Omron.FinsTcpDataLinkLayer(m_IPAddress)
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
        End Sub


        Friend Overrides Function SendData(ByVal FinsF As MfgControl.AdvancedHMI.Drivers.Omron.FINSFrame, ByVal InternalRequest As Boolean) As Boolean
            '* If a Subscription (Internal Request) begin to overflow the que, ignore some
            '* This can occur from too fast polling
            If Not InternalRequest Or DLL(MyDLLInstance).SendQueDepth < 10 Then
                DLL(MyDLLInstance).SendFinsFrame(FinsF)
                Return True
            Else
                Return False
            End If
        End Function
#End Region
    End Class
End Namespace
