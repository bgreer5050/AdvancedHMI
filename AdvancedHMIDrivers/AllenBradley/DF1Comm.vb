'**********************************************************************************************
'* AdvancedHMI Driver
'* http://www.advancedhmi.com
'* DF1 Data Link Layer & Application Layer
'*
'* Archie Jacobs
'* Manufacturing Automation, LLC
'* support@advancedhmi.com
'* 22-NOV-06
'*
'* Copyright 2006, 2010 Archie Jacobs
'*
'* This class implements the two layers of the Allen Bradley DF1 protocol.
'* In terms of the AB documentation, the data link layer acts as the transmitter and receiver.
'* Communication commands in the format described in chapter 7, are passed to
'* the data link layer using the SendData method.
'*
'* Reference : Allen Bradley Publication 1770-6.5.16
'*
'* Distributed under the GNU General Public License (www.gnu.org)
'*
'* This program is free software; you can redistribute it and/or
'* as published by the Free Software Foundation; either version 2
'* of the License, or (at your option) any later version.
'*
'* This program is distributed in the hope that it will be useful,
'* but WITHOUT ANY WARRANTY; without even the implied warranty of
'* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
'* GNU General Public License for more details.

'* You should have received a copy of the GNU General Public License
'* along with this program; if not, write to the Free Software
'* Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
'*
'*
'* 09-JUL-11  Split up in order to make a single class common for both DF1 and EthernetIP
'*******************************************************************************************************
Imports System.ComponentModel.Design
Imports System.ComponentModel

'<Assembly: system.Security.Permissions.SecurityPermissionAttribute(system.Security.Permissions.SecurityAction.RequestMinimum)> 
Public Class DF1Comm
    Inherits AllenBradleyPCCC
    Implements AdvancedHMIDrivers.IComComponent

    '* Create a common instance to share so multiple DF1Comms can be used in a project
    'Private Shared DLL(10) As MfgControl.AdvancedHMI.Drivers.DF1DataLinkLayer
    Private Shared DLL As New List(Of MfgControl.AdvancedHMI.Drivers.DF1DataLinkLayer)
    Private MyDLLInstance As Integer
    Protected Shared InstanceCount As Integer
    Protected Friend EventHandlerDLLInstance As Integer

    Public Event AutoDetectTry As EventHandler


#Region "Constructor"
    Public Sub New(ByVal container As System.ComponentModel.IContainer)
        MyBase.New()

        If DLL Is Nothing Then
            DLL = New List(Of MfgControl.AdvancedHMI.Drivers.DF1DataLinkLayer)
        End If

        InstanceCount += 1


        'Required for Windows.Forms Class Composition Designer support
        container.Add(Me)
    End Sub

    Public Sub New()
        If DLL Is Nothing Then
            DLL = New List(Of MfgControl.AdvancedHMI.Drivers.DF1DataLinkLayer)
        End If

        'ObjectIDs += 1
        'MyObjectID = ObjectIDs

        InstanceCount += 1
    End Sub

    '***************************************************************
    '* Create the Data Link Layer Instances
    '* if the IP Address is the same, then resuse a common instance
    '***************************************************************
    Friend Overrides Sub CreateDLLInstance()
        If DLL IsNot Nothing AndAlso DLL.Count > 0 Then
            '* At least one DLL instance already exists,
            '* so check to see if it has the same IP address
            '* if so, reuse the instance, otherwise create a new one
            Dim i As Integer
            While i < DLL.Count AndAlso DLL(i) IsNot Nothing AndAlso DLL(i).ComPort <> m_ComPort
                i += 1
            End While
            MyDLLInstance = i
        End If

        If MyDLLInstance >= DLL.Count Then
            Dim NewDLL As New MfgControl.AdvancedHMI.Drivers.DF1DataLinkLayer
            If m_BaudRate <> "AUTO" Then
                NewDLL.BaudRate = CInt(m_BaudRate)
            End If
            NewDLL.Parity = m_Parity
            NewDLL.ChecksumType = m_CheckSumType
            NewDLL.ComPort = m_ComPort
            DLL.Add(NewDLL)
        End If

        '* Have we already attached event handler to this data link layer?
        If EventHandlerDLLInstance <> (MyDLLInstance + 1) Then
            '* If event handler to another layer has been created, remove them
            If EventHandlerDLLInstance > 0 Then
                RemoveHandler DLL(EventHandlerDLLInstance).DataReceived, AddressOf DataLinkLayer_DataReceived
                'RemoveHandler DLL(EventHandlerDLLInstance).ComError, AddressOf DataLinkLayerComError
            End If

            AddHandler DLL(MyDLLInstance).DataReceived, AddressOf DataLinkLayer_DataReceived
            'AddHandler DLL(MyDLLInstance).ComError, AddressOf DataLinkLayerComError
            EventHandlerDLLInstance = MyDLLInstance + 1
        End If
    End Sub


    'Component overrides dispose to clean up the component list.
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        '* The handle linked to the DataLink Layer has to be removed, otherwise it causes a problem when a form is closed
        If DLL IsNot Nothing AndAlso DLL.Count > MyDLLInstance AndAlso DLL(MyDLLInstance) IsNot Nothing Then
            RemoveHandler DLL(MyDLLInstance).DataReceived, AddressOf DataLinkLayer_DataReceived
            InstanceCount -= 1

            '* 14-DEC-11 - Added the Remove from collection to fix problem where new DLL was not created
            '* if it the port were previously closed
            If InstanceCount <= 0 Then
                DLL(MyDLLInstance).Dispose(disposing)
                DLL.Remove(DLL(MyDLLInstance))
            End If

        End If

        MyBase.Dispose(disposing)
    End Sub
#End Region

#Region "Properties"
    Private m_BaudRate As String = "AUTO"
    <EditorAttribute(GetType(BaudRateEditor), GetType(System.Drawing.Design.UITypeEditor))> _
Public Property BaudRate() As String
        Get
            Return m_BaudRate
        End Get
        Set(ByVal value As String)
            If value <> m_BaudRate Then
                If Not Me.DesignMode Then
                    '* If a new instance needs to be created, such as a different Com Port
                    CreateDLLInstance()

                    If DLL IsNot Nothing Then
                        If DLL.Count >= MyDLLInstance AndAlso DLL(MyDLLInstance) IsNot Nothing Then
                            DLL(MyDLLInstance).CloseCom()
                            Try
                                DLL(MyDLLInstance).BaudRate = CInt(value)
                            Catch ex As Exception
                                '* 0 means AUTO to the data link layer
                                DLL(MyDLLInstance).BaudRate = 0
                            End Try
                        End If
                    End If
                End If
                m_BaudRate = value
            End If
        End Set
    End Property

    '* This is need so the current value of Auto detect can be viewed
    Public ReadOnly Property ActualBaudRate() As Integer
        Get
            If DLL.Count <= 0 OrElse DLL(MyDLLInstance) Is Nothing Then
                Return 0
            Else
                Return DLL(MyDLLInstance).BaudRate
            End If
        End Get
    End Property

    Private m_ComPort As String = "COM1"
    Public Property ComPort() As String
        Get
            'Return DLL(MyDLLInstance).ComPort
            Return m_ComPort
        End Get
        Set(ByVal value As String)
            'If value <> DLL(MyDLLInstance).ComPort Then DLL(MyDLLInstance).CloseComms()
            'DLL(MyDLLInstance).ComPort = value
            m_ComPort = value

            '* If a new instance needs to be created, such as a different Com Port
            'CreateDLLInstance()


            If (DLL IsNot Nothing) AndAlso (DLL.Count > MyDLLInstance) AndAlso DLL(MyDLLInstance) IsNot Nothing Then
                'Else
                DLL(MyDLLInstance).ComPort = value
            End If
        End Set
    End Property

    Private m_Parity As System.IO.Ports.Parity = IO.Ports.Parity.None
    Public Property Parity() As System.IO.Ports.Parity
        Get
            Return m_Parity
        End Get
        Set(ByVal value As System.IO.Ports.Parity)
            m_Parity = value
        End Set
    End Property


    'Public Enum CheckSumOptions
    '    Crc = 0
    '    Bcc = 1
    'End Enum

    Private m_CheckSumType As MfgControl.AdvancedHMI.Drivers.DF1DataLinkLayer.ChecksumOptions
    Public Property CheckSumType() As MfgControl.AdvancedHMI.Drivers.DF1DataLinkLayer.ChecksumOptions
        Get
            Return m_CheckSumType
        End Get
        Set(ByVal value As MfgControl.AdvancedHMI.Drivers.DF1DataLinkLayer.ChecksumOptions)
            m_CheckSumType = value
            If DLL.Count > 0 AndAlso DLL(MyDLLInstance) IsNot Nothing Then   'AndAlso Not DLL(MyDLLInstance).IsPortOpen Then
                DLL(MyDLLInstance).ChecksumType = m_CheckSumType
            End If

        End Set
    End Property

    '**************************************************
    '* Its purpose is to fetch
    '* the main form in order to synchronize the
    '* notification thread/event
    '**************************************************
    Protected m_SynchronizingObject As System.ComponentModel.ISynchronizeInvoke
    '* do not let this property show up in the property window
    ' <System.ComponentModel.Browsable(False)> _
    Public Overrides Property SynchronizingObject() As System.ComponentModel.ISynchronizeInvoke
        Get
            'If Me.Site.DesignMode Then

            Dim host1 As IDesignerHost
            Dim obj1 As Object
            If (m_SynchronizingObject Is Nothing) AndAlso Me.DesignMode Then
                host1 = CType(Me.GetService(GetType(IDesignerHost)), IDesignerHost)
                If host1 IsNot Nothing Then
                    obj1 = host1.RootComponent
                    m_SynchronizingObject = CType(obj1, System.ComponentModel.ISynchronizeInvoke)
                End If
            End If
            'End If
            Return m_SynchronizingObject


        End Get

        Set(ByVal Value As System.ComponentModel.ISynchronizeInvoke)
            If Not Value Is Nothing Then
                m_SynchronizingObject = Value
            End If
        End Set
    End Property
#End Region

#Region "Public Methods"

    '***************************************************************
    '* This method is intended to make it easy to configure the
    '* comm port settings. It is similar to the auto configure
    '* in RSLinx.
    '* It uses the echo command and sends the character "A", then
    '* checks if it received a response.
    '**************************************************************
    ''' <summary>
    ''' This method is intended to make it easy to configure the
    ''' comm port settings. It is similar to the auto configure
    ''' in RSLinx. A successful configuration returns a 0 and sets the
    ''' properties to the discovered values.
    ''' It will fire the event "AutoDetectTry" for each setting attempt
    ''' It uses the echo command and sends the character "A", then
    ''' checks if it received a response.
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function DetectCommSettings() As Integer
        'Dim rTNS As Integer

        Dim data() As Byte = {65}
        Dim BaudRates() As Integer = {38400, 19200, 9600, 1200}
        Dim BRIndex As Integer = 0
        Dim Parities() As System.IO.Ports.Parity = {System.IO.Ports.Parity.None, System.IO.Ports.Parity.Even}
        Dim PIndex As Integer
        Dim Checksums() As MfgControl.AdvancedHMI.Drivers.DF1DataLinkLayer.ChecksumOptions = {MfgControl.AdvancedHMI.Drivers.DF1DataLinkLayer.ChecksumOptions.Crc, MfgControl.AdvancedHMI.Drivers.DF1DataLinkLayer.ChecksumOptions.Bcc}
        Dim CSIndex As Integer
        Dim reply As Integer = -1

        DisableEvent = True
        '* We are sending a small amount of data, so speed up the response
        MaxTicks = 3
        While BRIndex < BaudRates.Length And reply <> 0
            PIndex = 0
            While PIndex < Parities.Length And reply <> 0
                CSIndex = 0
                While CSIndex < Checksums.Length And reply <> 0
                    DLL(MyDLLInstance).CloseCom()
                    'm_BaudRate = BaudRates(BRIndex)
                    DLL(MyDLLInstance).BaudRate = BaudRates(BRIndex)
                    DLL(MyDLLInstance).Parity = Parities(PIndex)
                    DLL(MyDLLInstance).ChecksumType = Checksums(CSIndex)

                    RaiseEvent AutoDetectTry(Me, System.EventArgs.Empty)


                    '* Send an ENQ sequence until we get a reply
                    reply = DLL(MyDLLInstance).SendENQ()

                    '* If we pass the ENQ test, then test an echo
                    '* send an "A" and look for echo back
                    If reply = 0 Then
                        Dim rTNS As Integer
                        reply = PrefixAndSend(&H6, &H0, data, True, rTNS)
                    End If

                    '* If port cannot be opened, do not retry
                    If reply = -6 Then Return reply

                    MaxTicks += 1
                    CSIndex += 1
                End While
                PIndex += 1
            End While
            BRIndex += 1
        End While

        DisableEvent = False
        MaxTicks = 85
        Return reply
    End Function


    'End of Public Methods
#End Region

#Region "Helper"
    '**************************************************************
    '* This method implements the common application routine
    '* as discussed in the Software Layer section of the AB manual
    '**************************************************************
    Friend Overrides Function PrefixAndSend(ByVal Command As Byte, ByVal Func As Byte, ByVal data() As Byte, ByVal Wait As Boolean, ByVal TNS As Integer) As Integer
        Dim PacketSize As Integer
        'PacketSize = data.Length + 6
        PacketSize = data.Length + 4 '* make this more generic for CIP Ethernet/IP encap


        Dim CommandPacket(PacketSize) As Byte
        Dim BytePos As Integer

        'CommandPacke(0) = TargetNode
        'CommandPacke(1) = MyNode
        'BytePos = 2
        BytePos = 0

        CommandPacket(BytePos) = Command
        CommandPacket(BytePos + 1) = 0       '* STS (status, always 0)

        'Dim dTNS As Integer
        'dTNS = DLL(MyDLLInstance).GetNextTransactionNumber(255)

        CommandPacket(BytePos + 2) = CByte(TNS And 255)
        CommandPacket(BytePos + 3) = CByte(TNS >> 8)

        '*Mark whether this was requested by a subscription or not
        '* FIX
        PLCAddressByTNS(TNS And 255).InternallyRequested = InternalRequest


        CommandPacket(BytePos + 4) = Func

        If data.Length > 0 Then
            data.CopyTo(CommandPacket, BytePos + 5)
        End If

        Dim rTNS As Integer = TNS And &HFF
        'Responded(rTNS) = False
        PLCAddressByTNS(rTNS).Responded = False
        Dim resultTNS As Integer
        resultTNS = DLL(MyDLLInstance).SendData(CommandPacket, m_MyNode, m_TargetNode, MyObjectID)


        Dim Result As Integer
        If resultTNS >= 0 And Wait Then
            Result = WaitForResponse(rTNS)

            '* Return status byte that came from controller
            If Result = 0 Then
                If DataPackets(rTNS) IsNot Nothing Then
                    If (DataPackets(rTNS).Count > 3) Then
                        Result = DataPackets(rTNS)(3)  '* STS position in DF1 message
                        '* If its and EXT STS, page 8-4
                        If Result = &HF0 Then
                            '* The EXT STS is the last byte in the packet
                            'result = DataPackets(rTNS)(DataPackets(rTNS).Count - 2) + &H100
                            Result = DataPackets(rTNS)(DataPackets(rTNS).Count - 1) + &H100
                        End If
                    End If
                Else
                    Result = -8 '* no response came back from PLC
                End If
            Else
                Dim DebugCheck As Integer = 0
            End If
        Else
            Dim DebugCheck As Integer = 0
        End If

        Return result
    End Function

    '**************************************************************
    '* This method Sends a response from an unsolicited msg
    '**************************************************************
    Private Function SendResponse(ByVal Command As Byte, ByVal rTNS As Integer) As Integer
        Dim PacketSize As Integer
        'PacketSize = Data.Length + 5
        PacketSize = 5
        PacketSize = 3    'Ethernet/IP Preparation


        Dim CommandPacke(PacketSize) As Byte
        Dim BytePos As Integer

        CommandPacke(1) = CByte(TargetNode And 255)
        CommandPacke(0) = CByte(MyNode And 255)
        BytePos = 2
        BytePos = 0

        CommandPacke(BytePos) = Command
        CommandPacke(BytePos + 1) = 0       '* STS (status, always 0)

        CommandPacke(BytePos + 2) = CByte(rTNS And 255)
        CommandPacke(BytePos + 3) = CByte(rTNS >> 8)


        Dim result As Integer
        result = DLL(MyDLLInstance).SendData(CommandPacke, m_MyNode, m_TargetNode, MyObjectID)
    End Function


    '****************************************************
    '* Wait for a response from PLC before returning
    '****************************************************
    Private MaxTicks As Integer = 150  '* 50 ticks per second
    Friend Overrides Function WaitForResponse(ByVal rTNS As Integer) As Integer
        Dim Loops As Integer = 0
        rTNS = rTNS And 255
        While Not PLCAddressByTNS(rTNS).Responded And Loops < MaxTicks
            System.Threading.Thread.Sleep(10)
            Loops += 1
        End While


        If Loops >= MaxTicks Then
            Return -20
        ElseIf DLL(MyDLLInstance).LastResponseWasNAK Then
            Return -21
        End If

        Return 0
    End Function


    Friend Overrides Function GetNextTNSNumber(ByVal max As Integer, OwnerObjectID As Int64) As Integer
        If DLL IsNot Nothing AndAlso DLL.Count > MyDLLInstance Then
        Else
            CreateDLLInstance()
        End If
        Return DLL(MyDLLInstance).GetNextTransactionNumber(max)
    End Function
#End Region
End Class

