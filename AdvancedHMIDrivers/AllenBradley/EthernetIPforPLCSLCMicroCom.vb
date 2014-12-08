'**********************************************************************************************
'* AdvancedHMI Driver
'* http://www.advancedhmi.com
'* PCCC over Ethernet/IP
'*
'* Archie Jacobs
'* Manufacturing Automation, LLC
'* support@advancedhmi.com
'* 01-DEC-09
'*
'* Copyright 2009, 2010 Archie Jacobs
'*
'* NOTICE : If you received this code without a complete AdvancedHMI solution
'* please report to sales@advancedhmi.com
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
'* 01-DEC-09  Adapted to use Ethernet/IP as the data layer 
'* 02-JUN-14 Fixed where an IP address setting was closing the DLL
'*******************************************************************************************************
Imports System.ComponentModel.Design


Public Class EthernetIPforPLCSLCMicroCom
    Inherits AllenBradleyPCCC
    Implements AdvancedHMIDrivers.IComComponent

    '* Create a common instance to share so multiple DF1Comms can be used in a project
    Private Shared DLL(100) As MfgControl.AdvancedHMI.Drivers.CIP
    Private MyDLLInstance As Integer

#Region "Constructor"
    Private components As System.ComponentModel.IContainer
    Public Sub New(ByVal container As System.ComponentModel.IContainer)
        'Exit Sub
        MyClass.New()

        'Required for Windows.Forms Class Composition Designer support
        container.Add(Me)
    End Sub

    Public Sub New()
        MyBase.New()
    End Sub

    'Component overrides dispose to clean up the component list.
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        '* The handle linked to the DataLink Layer has to be removed, otherwise it causes a problem when a form is closed
        If DLL(MyDLLInstance) IsNot Nothing Then
            'DLL(MyDLLInstance).ForwardClose()
            RemoveHandler DLL(MyDLLInstance).DataReceived, AddressOf DataLinkLayer_DataReceived
            RemoveHandler DLL(MyDLLInstance).ConnectionEstablished, AddressOf CIPConnectionEstablished
            'RemoveHandler DLL(MyDLLInstance).ConnectionClosed, AddressOf CIPConnect

            If DLL(MyDLLInstance).ConnectionCount <= 1 Then
                CloseConnection()
                DLL(MyDLLInstance).dispose()
            Else
                DLL(MyDLLInstance).ConnectionCount -= 1
            End If
        End If

        If disposing AndAlso components IsNot Nothing Then
            components.Dispose()
        End If

        MyBase.Dispose(disposing)
    End Sub

    '***************************************************************
    '* Create the Data Link Layer Instances
    '* if the IP Address is the same, then resuse a common instance
    '***************************************************************
    Friend Overrides Sub CreateDLLInstance()
        If Me.DesignMode Then Exit Sub

        If DLL(0) IsNot Nothing Then
            '* At least one DLL instance already exists,
            '* so check to see if it has the same IP address
            '* if so, reuse the instance, otherwise create a new one
            Dim i As Integer
            While DLL(i) IsNot Nothing AndAlso DLL(i).EIPEncap.IPAddress <> m_IPAddress AndAlso i < 11
                i += 1
            End While
            MyDLLInstance = i
        End If

        If DLL(MyDLLInstance) Is Nothing Then
            DLL(MyDLLInstance) = New MfgControl.AdvancedHMI.Drivers.CIP
            DLL(MyDLLInstance).EIPEncap.IPAddress = m_IPAddress
            DLL(MyDLLInstance).EIPEncap.Port = m_Port
        End If

        If DLL(MyDLLInstance) IsNot Nothing Then
            AddHandler DLL(MyDLLInstance).DataReceived, AddressOf DataLinkLayer_DataReceived
            AddHandler DLL(MyDLLInstance).ConnectionEstablished, AddressOf CIPConnectionEstablished
            DLL(MyDLLInstance).ConnectionCount += 1
        End If
    End Sub
#End Region

#Region "Properties"
    Private m_IPAddress As String = "192.168.0.10"
    <System.ComponentModel.Category("Communication Settings")> _
    Public Property IPAddress() As String
        Get
            'Return DLL(MyDLLInstance).EIPEncap.IPAddress
            Return m_IPAddress
        End Get
        Set(ByVal value As String)
            If m_IPAddress <> value Then
                If DLL(MyDLLInstance) IsNot Nothing AndAlso DLL(MyDLLInstance).EIPEncap.IPAddress <> value Then
                    DLL(MyDLLInstance).ForwardClose()
                End If

                m_IPAddress = value

                '* If a new instance needs to be created, such as a different IP Address
                CreateDLLInstance()


                If DLL(MyDLLInstance) Is Nothing Then
                Else
                    DLL(MyDLLInstance).EIPEncap.IPAddress = value
                End If
            End If
        End Set
    End Property

    Private m_Port As UShort = &HAF12
    Public Property Port As Integer
        Get
            Return m_Port
        End Get
        Set(value As Integer)
            '* Limit the value to 0-65535
            m_Port = CUShort(Math.Max(0, Math.Min(value, 65535)))
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
            If (m_SynchronizingObject Is Nothing) AndAlso MyBase.DesignMode Then
                host1 = CType(Me.GetService(GetType(IDesignerHost)), IDesignerHost)
                If host1 IsNot Nothing Then
                    obj1 = host1.RootComponent
                    m_SynchronizingObject = CType(obj1, System.ComponentModel.ISynchronizeInvoke)
                End If
            End If
            'End If
            Return m_SynchronizingObject



            'Dim dh As IDesignerHost = DirectCast(Me.GetService(GetType(IDesignerHost)), IDesignerHost)
            'If dh IsNot Nothing Then
            '    Dim obj As Object = dh.RootComponent
            '    If obj IsNot Nothing Then
            '        m_ParentForm = DirectCast(obj, Form)
            '    End If
            'End If

            'Dim instance As IDesignerHost = Me.GetService(GetType(IDesignerHost))
            'm_SynchronizingObject = instance.RootComponent
            ''End If
            'Return m_SynchronizingObject
        End Get

        Set(ByVal Value As System.ComponentModel.ISynchronizeInvoke)
            If Not Value Is Nothing Then
                m_SynchronizingObject = Value
            End If
        End Set
    End Property
#End Region

#Region "Helper"
    '****************************************************
    '* Wait for a response from PLC before returning
    '****************************************************
    Dim MaxTicks As Integer = 500  '* 50 ticks per second
    Friend Overrides Function WaitForResponse(ByVal rTNS As Integer) As Integer
        'Responded = False

        Dim Loops As Integer = 0
        While Not PLCAddressByTNS(rTNS And 255).Responded And Loops < MaxTicks
            'Application.DoEvents()
            System.Threading.Thread.Sleep(1)
            Loops += 1
        End While

        If Loops >= MaxTicks Then
            Return -20
        Else
            Return 0
        End If
    End Function

    '**************************************************************
    '* This method implements the common application routine
    '* as discussed in the Software Layer section of the AB manual
    '**************************************************************
    Friend Overrides Function PrefixAndSend(ByVal Command As Byte, ByVal Func As Byte, ByVal data() As Byte, ByVal Wait As Boolean, ByVal TNS As Integer) As Integer
        '14-OCT-12, 16-OCT-12 Return a negative value, so it knows nothing was sent
        If m_IPAddress = "0.0.0.0" Then
            Return -10000
        End If

        Dim PacketSize As Integer
        'PacketSize = data.Length + 6
        PacketSize = data.Length + 4 '* make this more generic for CIP Ethernet/IP encap


        Dim CommandPacket(PacketSize) As Byte

        Dim TNSLowerByte As Byte = CByte(TNS And &HFF)

        CommandPacket(0) = Command
        CommandPacket(1) = 0       '* STS (status, always 0)


        CommandPacket(2) = TNSLowerByte
        CommandPacket(3) = CByte(TNS >> 8)

        '*Mark whether this was requested by a subscription or not
        '* FIX
        PLCAddressByTNS(TNSLowerByte).InternallyRequested = InternalRequest


        CommandPacket(4) = Func

        If data.Length > 0 Then
            data.CopyTo(CommandPacket, 5)
        End If

        PLCAddressByTNS(TNSLowerByte).Responded = False
        Dim result As Integer
        result = SendData(CommandPacket, TNS)


        If result = 0 And Wait Then
            result = WaitForResponse(TNSLowerByte)

            '* Return status byte that came from controller
            If result = 0 Then
                If DataPackets(TNSLowerByte) IsNot Nothing Then
                    If (DataPackets(TNSLowerByte).Count > 3) Then
                        result = DataPackets(TNSLowerByte)(3)  '* STS position in DF1 message
                        '* If its and EXT STS, page 8-4
                        If result = &HF0 Then
                            '* The EXT STS is the last byte in the packet
                            'result = DataPackets(rTNS)(DataPackets(rTNS).Count - 2) + &H100
                            result = DataPackets(TNSLowerByte)(DataPackets(TNSLowerByte).Count - 1) + &H100
                        End If
                    End If
                Else
                    result = -8 '* no response came back from PLC
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
        'PacketSize = 5
        PacketSize = 3    'Ethernet/IP Preparation


        Dim CommandPacket(PacketSize) As Byte
        Dim BytePos As Integer

        'CommandPacket(1) = m_TargetNode
        'CommandPacket(0) = m_MyNode
        'BytePos = 2
        BytePos = 0

        CommandPacket(BytePos) = Command
        CommandPacket(BytePos + 1) = 0       '* STS (status, always 0)

        CommandPacket(BytePos + 2) = CByte(rTNS And 255)
        CommandPacket(BytePos + 3) = CByte(rTNS >> 8)


        Dim result As Integer
        result = SendData(CommandPacket, rTNS)
    End Function

    '* This is needed so the handler can be removed
    'Private Dr As EventHandler = AddressOf DataLinkLayer_DataReceived
    'Private Function SendData(ByVal data() As Byte, ByVal MyNode As Byte, ByVal TargetNode As Byte) As Integer
    Private Function SendData(ByVal data() As Byte, ByVal TNS As Integer) As Integer
        If DLL Is Nothing OrElse DLL(MyDLLInstance) Is Nothing Then
            CreateDLLInstance()
        End If

        Return DLL(MyDLLInstance).ExecutePCCC(data, TNS, MyObjectID)
    End Function


    Friend Overrides Function GetNextTNSNumber(ByVal max As Integer, OwnerObjectID As Int64) As Integer
        If DLL(MyDLLInstance) IsNot Nothing Then
            Return DLL(MyDLLInstance).GetNextTransactionNumber(max)
        Else
            Return 0
        End If
    End Function


#End Region

#Region "Public Methods"
    Public Sub CloseConnection()
        DLL(MyDLLInstance).ForwardClose()
    End Sub
#End Region

End Class