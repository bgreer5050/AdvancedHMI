'**********************************************************************************************
'* AdvancedHMI Driver
'* http://www.advancedhmi.com
'* Ethernet/IP for ControlLogix
'*
'* Archie Jacobs
'* Manufacturing Automation, LLC
'* support@advancedhmi.com
'* 14-DEC-10
'*
'*
'* Copyright 2010,2013 Archie Jacobs
'*
'* This class implements the Ethernet/IP protocol.
'*
'* NOTICE : If you received this code without a complete AdvancedHMI solution
'* please report to sales@advancedhmi.com
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
'* 23-MAY-12  Renamed PolledAddress* variables to Subscription* for clarity
'* 23-MAY-12  Created ComError event and check if subscription exists before sending error
'* 24-SEP-12  Added array reading optimization for subscriptions
'* 11-OCT-12  Do not optimize complex types, such as strings
'* 07-NOV-12  Array tags not sorting by element and caused number of elements to read to be wrong
'* 22-JAN-13  Added BasePLCAddress to Subscription info and used to sort arrays properly
'*******************************************************************************************************
Imports System.ComponentModel.Design

'<Assembly: system.Security.Permissions.SecurityPermissionAttribute(system.Security.Permissions.SecurityAction.RequestMinimum)> 
'<Assembly: CLSCompliant(True)> 
Public Class EthernetIPforCLXCom
    Inherits System.ComponentModel.Component
    Implements AdvancedHMIDrivers.IComComponent

    '* Create a common instance to share so multiple driver instances can be used in a project
    Protected Shared DLL(100) As MfgControl.AdvancedHMI.Drivers.CIP
    Protected MyDLLInstance As Integer

    Public Event DataReceived As EventHandler(Of MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs)
    Public Event ComError As EventHandler(Of MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs)
    Public Event ConnectionEstablished As EventHandler
    Public Event ConnectionClosed As EventHandler
    Public Event UnsolictedMessageRcvd As EventHandler

    Private Shared ObjectIDs As Int64
    Private MyObjectID As Int64

    '* keep the original address by ref of low TNS byte so it can be returned to a linked polling address
    '* 2-AUG-13 Removed the Shared
    Private PLCAddressByTNS(255) As CLXAddressRead
    Private ReturnedInfo(255) As MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs


#Region "Constructor"
    Public Sub New(ByVal container As System.ComponentModel.IContainer)
        MyClass.New()

        ObjectIDs += 1
        MyObjectID = ObjectIDs

        'Required for Windows.Forms Class Composition Designer support
        container.Add(Me)
    End Sub

    Public Sub New()
        MyBase.New()

        'CreateDLLInstance()
    End Sub

    '***************************************************************
    '* Create the Data Link Layer Instances
    '* if the IP Address is the same, then resuse a common instance
    '***************************************************************
    Protected Overridable Sub CreateDLLInstance()
        If Me.DesignMode Then Exit Sub

        If DLL(0) IsNot Nothing Then
            '* At least one DLL instance already exists,
            '* so check to see if it has the same IP address
            '* if so, reuse the instance, otherwise create a new one
            Dim i As Integer
            While DLL(i) IsNot Nothing AndAlso (DLL(i).EIPEncap.IPAddress <> m_IPAddress OrElse DLL(i).ProcessorSlot <> m_ProcessorSlot) AndAlso i < 11
                i += 1
            End While
            MyDLLInstance = i
        End If

        If MyDLLInstance > DLL.Length Then
            '* TODO:
            MsgBox("A limit of " & DLL.Length & " driver instances")
            Exit Sub
        End If

        If DLL(MyDLLInstance) Is Nothing Then
            Try
                DLL(MyDLLInstance) = New MfgControl.AdvancedHMI.Drivers.CIP
            Catch ex As Exception
                MsgBox(ex.Message)
            End Try

            DLL(MyDLLInstance).EIPEncap.IPAddress = m_IPAddress
            DLL(MyDLLInstance).EIPEncap.Port = m_Port
            '* Set to 1 for backplane path to processor slot
            DLL(MyDLLInstance).ConnectionPathPort = 1
            DLL(MyDLLInstance).ProcessorSlot = m_ProcessorSlot
        End If

        AddHandler DLL(MyDLLInstance).DataReceived, AddressOf DataLinkLayer_DataReceived
        AddHandler DLL(MyDLLInstance).ComError, AddressOf ComErrorHandler
        AddHandler DLL(MyDLLInstance).ConnectionEstablished, AddressOf CIPConnectionEstablished
        AddHandler DLL(MyDLLInstance).ConnectionClosed, AddressOf CIPConnectionClosed
        DLL(MyDLLInstance).ConnectionCount += 1
    End Sub


    'Component overrides dispose to clean up the component list.
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        '* Stop the subscription thread
        StopSubscriptions = True

        '* The handle linked to the DataLink Layer has to be removed, otherwise it causes a problem when a form is closed
        If DLL(MyDLLInstance) IsNot Nothing Then
            RemoveHandler DLL(MyDLLInstance).DataReceived, AddressOf DataLinkLayer_DataReceived
            RemoveHandler DLL(MyDLLInstance).ComError, AddressOf ComErrorHandler
            RemoveHandler DLL(MyDLLInstance).ConnectionEstablished, AddressOf CIPConnectionEstablished
            RemoveHandler DLL(MyDLLInstance).ConnectionClosed, AddressOf CIPConnectionClosed

            If DLL(MyDLLInstance).ConnectionCount <= 1 Then
                CloseConnection()
                DLL(MyDLLInstance).dispose()
            Else
                DLL(MyDLLInstance).ConnectionCount -= 1
            End If
        End If

        MyBase.Dispose(disposing)
    End Sub
#End Region

#Region "Properties"
    Private m_ProcessorSlot As Integer
    Public Property ProcessorSlot() As Integer
        Get
            Return m_ProcessorSlot
        End Get
        Set(ByVal value As Integer)
            m_ProcessorSlot = value
            If DLL(MyDLLInstance) IsNot Nothing Then
                DLL(MyDLLInstance).ProcessorSlot = value
            End If
        End Set
    End Property

    Private m_IPAddress As String = "192.168.0.10"
    Private m_IPIniFile As String = ""
    Public Property IPAddress() As String
        Get
            'Return DLL(MyDLLInstance).EIPEncap.IPAddress
            If m_IPIniFile <> "" Then
                Return m_IPIniFile
            Else
                Return m_IPAddress
                'Return "abc"
            End If
        End Get

        Set(ByVal value As String)
            If m_IPAddress <> value Then
                If value.IndexOf(".ini", 0, StringComparison.CurrentCultureIgnoreCase) > 0 Then
                    Try
                        If Not Me.DesignMode Then
                            Dim p As New IniParser(value)
                            m_IPAddress = p.GetSetting("IPADDRESS")
                        End If
                        m_IPIniFile = value
                    Catch ex As Exception
                        MsgBox(ex.Message)
                        'Dim dbg = 0
                        Exit Property
                    End Try
                Else
                    m_IPAddress = value
                    m_IPIniFile = ""
                End If
            End If


            '* If a new instance needs to be created, such as a different IP Address
            CreateDLLInstance()


            If DLL(MyDLLInstance) IsNot Nothing Then
                DLL(MyDLLInstance).EIPEncap.IPAddress = m_IPAddress
            End If
            ' End If
        End Set
    End Property

    Private m_Port As Integer = &HAF12
    Public Property Port As Integer
        Get
            Return m_Port
        End Get
        Set(value As Integer)
            If value <> m_Port Then
                '* Limit the value to 0-65535
                m_Port = Math.Max(0, Math.Min(value, 65535))

                '* If a new instance needs to be created, such as a different IP Address
                CreateDLLInstance()


                If DLL(MyDLLInstance) Is Nothing Then
                Else
                    DLL(MyDLLInstance).EIPEncap.Port = value
                End If
            End If
        End Set
    End Property


    Private m_PollRateOverride As Integer = 500
    <System.ComponentModel.Category("Communication Settings")> _
    Public Property PollRateOverride() As Integer
        Get
            Return m_PollRateOverride
        End Get
        Set(ByVal value As Integer)
            If value >= 0 Then
                m_PollRateOverride = value
            End If
        End Set
    End Property

    '**************************************************************
    '* Stop the polling of subscribed data
    '**************************************************************
    Private m_DisableSubscriptions As Boolean
    Public Property DisableSubscriptions() As Boolean Implements IComComponent.DisableSubscriptions
        Get
            Return m_DisableSubscriptions
        End Get
        Set(ByVal value As Boolean)
            m_DisableSubscriptions = value
        End Set
    End Property

    '**************************************************
    '* Its purpose is to fetch
    '* the main form in order to synchronize the
    '* notification thread/event
    '**************************************************
    Private m_SynchronizingObject As System.ComponentModel.ISynchronizeInvoke
    '* do not let this property show up in the property window
    ' <System.ComponentModel.Browsable(False)> _
    '* Copied from the Timer.cs file of the .NET source code
    Public Property SynchronizingObject() As System.ComponentModel.ISynchronizeInvoke
        Get
            If m_SynchronizingObject Is Nothing AndAlso DesignMode Then
                Dim host As IDesignerHost = DirectCast(GetService(GetType(IDesignerHost)), IDesignerHost)
                If host IsNot Nothing Then
                    Dim baseComponent As Object = host.RootComponent
                    If baseComponent IsNot Nothing AndAlso TypeOf baseComponent Is System.ComponentModel.ISynchronizeInvoke Then
                        Me.SynchronizingObject = DirectCast(baseComponent, System.ComponentModel.ISynchronizeInvoke)
                    End If
                End If
            End If

            Return m_SynchronizingObject
        End Get

        Set(value As System.ComponentModel.ISynchronizeInvoke)
            m_SynchronizingObject = value
        End Set
    End Property
#End Region

#Region "Public Methods"
    Public Sub CloseConnection()
        DLL(MyDLLInstance).ForwardClose()
    End Sub

    '**********************************************************************************
    '* Synchronous read operation that will wait for result before returning to caller
    '**********************************************************************************
    Public Function Read(ByVal startAddress As String, ByVal numberOfElements As Integer) As String() Implements IComComponent.Read
        Dim SequenceNumber As Integer = BeginRead(startAddress, numberOfElements)

        Dim result As Integer = WaitForResponse(SequenceNumber, 2500)
        SequenceNumber = SequenceNumber And 255

        If result = 0 Then
            Dim d(ReturnedInfo(SequenceNumber).Values.Count - 1) As String
            If ReturnedInfo(SequenceNumber).ErrorId = 0 Then
                '* a Bit Array will return number of elements rounded up to 32, so return only the amount requested
                If ReturnedInfo(SequenceNumber).Values.Count > numberOfElements Then
                    Dim v(numberOfElements - 1) As String
                    For i As Integer = 0 To v.Length - 1
                        v(i) = ReturnedInfo(SequenceNumber).Values(i)
                    Next i
                    Return v
                Else
                    '* Is the start address a bit number?
                    If PLCAddressByTNS(SequenceNumber).BitNumber >= 0 Then
                        Dim ElementNumber, BitNumber As Integer
                        Dim BitsPerElement As Integer = 32
                        Select Case PLCAddressByTNS(SequenceNumber).AbreviatedDataType
                            Case &HC3 '* INT Value read 
                                BitsPerElement = 16
                            Case &HC4 '* DINT Value read (&H91)
                                BitsPerElement = 32
                            Case &HC2 '* SINT
                                BitsPerElement = 8
                        End Select

                        '* Convert values to bits
                        For i = 0 To d.Length - 1
                            ElementNumber = CInt(Math.Floor((PLCAddressByTNS(SequenceNumber).BitNumber + i) / BitsPerElement))
                            BitNumber = (PLCAddressByTNS(SequenceNumber).BitNumber + i) - ElementNumber * BitsPerElement
                            If ReturnedInfo(SequenceNumber).Values.Count > ElementNumber Then
                                d(i) = CStr((CInt(ReturnedInfo(SequenceNumber).Values(ElementNumber)) And CInt(2 ^ BitNumber)))
                            End If
                        Next
                    Else
                        ReturnedInfo(SequenceNumber).Values.CopyTo(d, 0)
                    End If

                    Return d
                End If
            Else
                Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("Read Failed. " & ReturnedInfo(SequenceNumber).ErrorMessage & ",  Status Code=" & ReturnedInfo(SequenceNumber).ErrorId)
            End If
        Else
            ReturnedInfo(SequenceNumber).ErrorId = result
            Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException(result, "Read Failed - Result=" & result & ". " & DecodeMessage(result))
        End If
    End Function

    Public Function Read(ByVal startAddress As String) As String
        Return Read(startAddress, 1)(0)
    End Function


    '*********************************************************************
    '* Asynchronous Read that returns Transaction Number(Sequence Number)
    '*********************************************************************
    Private ReadLock As New Object
    Public Function BeginRead(ByVal startAddress As String, ByVal numberOfElements As Integer) As Integer Implements IComComponent.BeginRead
        SyncLock (ReadLock)
            Return BeginRead(New CLXAddressRead(startAddress), numberOfElements)
        End SyncLock
    End Function

    '**************************************************************************************
    '* This function is used for subscriptions to make sure InternalRequest doesn't change
    '**************************************************************************************
    Public Function BeginRead(ByVal address As CLXAddressRead, ByVal numberOfElements As Integer) As Integer
        address.TransactionNumber = CUShort(DLL(MyDLLInstance).GetNextTransactionNumber(32767))

        PLCAddressByTNS(address.TransactionNumber And 255) = address

        DLL(MyDLLInstance).ReadTagValue(address, numberOfElements, CUShort(address.TransactionNumber), MyObjectID)

        Return address.TransactionNumber
    End Function

    '*************************************************
    '* Retreive the list of tags in the ControlLogix
    '* This is operation waits until all tags are
    '*  retreived before returning
    '*************************************************
    Public Function GetTagList() As MfgControl.AdvancedHMI.Drivers.CLXTag()
        '* We must get the sequence number from the DLL
        '* and save the read information because it can comeback before this
        '* information gets put in the PLCAddressByTNS array
        Dim SequenceNumber As Integer = DLL(MyDLLInstance).GetNextTransactionNumber(32767) ' TNS1.GetNextNumber("c", MyObjectID)

        'Responded(SequenceNumber And 255) = False

        PLCAddressByTNS(SequenceNumber And 255) = New AdvancedHMIDrivers.CLXAddressRead
        PLCAddressByTNS(SequenceNumber And 255).TransactionNumber = CUShort(SequenceNumber)
        'PLCAddressByTNS(SequenceNumber And 255).InternalRequest = False 'InternalRequest
        PLCAddressByTNS(SequenceNumber And 255).Responded = False

        Dim d() As MfgControl.AdvancedHMI.Drivers.CLXTag = DLL(MyDLLInstance).GetCLXTags(10)

        'TNS1.ReleaseNumber(SequenceNumber)

        Return d
    End Function


    '******************************************************************************************************
    '* Write Section
    '******************************************************************************************************

    '******************************************************
    ' Write a single integer value to a PLC data table
    '******************************************************
    Public Function Write(ByVal startAddress As String, ByVal dataToWrite As Integer) As Integer
        Dim temp(1) As String
        temp(0) = CStr(dataToWrite)
        Return Write(startAddress, 1, temp)
    End Function


    '******************************************************
    ' Write a single floating point value to a data table
    '******************************************************
    Public Function Write(ByVal startAddress As String, ByVal dataToWrite As Single) As Integer
        Dim temp(1) As Single
        temp(0) = dataToWrite
        Return Write(startAddress, 1, temp)
    End Function

    '****************************
    '* Write an array of Singles
    '****************************
    Public Function Write(ByVal startAddress As String, ByVal numberOfElements As Integer, ByVal dataToWrite() As Single) As Integer
        Dim StringVals(numberOfElements) As String
        For i As Integer = 0 To numberOfElements - 1
            StringVals(i) = CStr(dataToWrite(i))
        Next

        Write(startAddress, numberOfElements, StringVals)
    End Function

    '***********************************
    '* Write all single values in array
    '***********************************
    Public Function Write(ByVal startAddress As String, ByVal dataToWrite() As Single) As Integer
        Dim StringVals(dataToWrite.Length - 1) As String
        For i As Integer = 0 To StringVals.Length - 1
            StringVals(i) = CStr(dataToWrite(i))
        Next

        Write(startAddress, dataToWrite.Length, StringVals)
    End Function


    '**********************************************
    '* Write specified number of elements in array
    '**********************************************
    Public Function Write(ByVal startAddress As String, ByVal numberOfElements As Integer, ByVal dataToWrite() As Integer) As Integer
        Dim StringVals(numberOfElements) As String
        For i As Integer = 0 To numberOfElements - 1
            StringVals(i) = CStr(dataToWrite(i))
        Next

        Write(startAddress, numberOfElements, StringVals)
    End Function

    '******************************
    '* Write all integers in array
    '******************************
    Public Function Write(ByVal startAddress As String, ByVal dataToWrite() As Integer) As Integer
        Dim StringVals(dataToWrite.Length - 1) As String
        For i As Integer = 0 To StringVals.Length - 1
            StringVals(i) = CStr(dataToWrite(i))
        Next

        Write(startAddress, dataToWrite.Length, StringVals)
    End Function

    '***********************
    '* Write a single value
    '***********************
    Public Function Write(ByVal startAddress As String, ByVal dataToWrite As String) As String Implements IComComponent.Write
        If dataToWrite Is Nothing Then
            Return "0"
        End If
        Dim v() As String = {dataToWrite}

        Write(startAddress, 1, v)

        Return "0"
    End Function

    '*******************************
    '* Write to DLL
    '*******************************
    Public Function Write(ByVal startAddress As String, ByVal numberOfElements As Integer, ByVal dataToWrite() As String) As Integer
        Dim StringVals(numberOfElements) As String
        For i As Integer = 0 To numberOfElements - 1
            StringVals(i) = CStr(dataToWrite(i))
        Next
        Dim SequenceNumber As Integer = DLL(MyDLLInstance).GetNextTransactionNumber(32767)  ' TNS1.GetNextNumber("WD", MyObjectID)
        Dim tag As New CLXAddressRead
        tag.TagName = startAddress
        SyncLock (ReadLock)
            DLL(MyDLLInstance).WriteTagValue(tag, StringVals, numberOfElements, SequenceNumber, MyObjectID)
        End SyncLock
    End Function
    '******************************************************************************************************


    Public Function GetTagInformation(ByVal TagName As String) As MfgControl.AdvancedHMI.Drivers.CLXTag
        Dim Tag As New CLXAddressRead
        Tag.TagName = TagName

        Return DLL(MyDLLInstance).GetTagInformation(Tag)
    End Function


    '********************************************************************
    '* Extract the data from the byte stream returned
    '* Use the abreviated type byte that is returned with data
    '********************************************************************
    Private Shared Function ExtractData(ByVal startAddress As String, ByVal AbreviatedType As Byte, ByVal returnedData() As Byte, startIndex As Integer) As String()
        '* Get the element size in bytes
        Dim ElementSize As Integer
        Select Case AbreviatedType
            Case &HC1 '* BIT
                ElementSize = 1
            Case &HC2 '* SINT
                ElementSize = 1
            Case &HC3 ' * INT
                ElementSize = 2
            Case &HC4, &HCA '* DINT, REAL Value read (&H91)
                ElementSize = 4
            Case &HC5 ' * LINT
                ElementSize = 8
            Case &HD3 '* Bit Array
                ElementSize = 4
            Case &H82, &H83 ' * Timer, Counter, Control
                ElementSize = 14
            Case &HCE '* String
                'ElementSize = ReturnedData(0) + ReturnedData(1) * 256
                ElementSize = 88
            Case Else
                ElementSize = 2
        End Select

        Dim BitsPerElement As Integer = ElementSize * 8
        '***************************************************
        '* Extract returned data into appropriate data type
        '***************************************************
        Dim ElementsToReturn As Integer = CInt(Math.Floor((returnedData.Length - startIndex) / ElementSize) - 1)
        '* Bit Arrays are return as DINT, so it will have to be extracted
        Dim BitIndex As Integer
        If AbreviatedType = &HD3 Then
            If startAddress.LastIndexOf("[") > 0 Then
                BitIndex = CInt(startAddress.Substring(startAddress.LastIndexOf("[") + 1, startAddress.LastIndexOf("]") - startAddress.LastIndexOf("[") - 1))
            End If
            BitIndex -= CInt(Math.Floor(BitIndex / 32) * 32)
            '* Return all the bits that came back even if less were requested
            ElementsToReturn = (returnedData.Length - startIndex) * 8 - BitIndex - 1
            BitsPerElement = 32
        End If

        '* 18-MAY-12
        '* Check if it is addressing a single bit in a larger data type
        Dim PeriodPos As Integer = startAddress.IndexOf(".")
        If PeriodPos > 0 Then
            Dim SubElement As String = startAddress.Substring(PeriodPos + 1)

            Try
                If Integer.TryParse(SubElement, BitIndex) Then
                    'BitIndex = CInt(SubElement)

                    Select Case AbreviatedType
                        Case &HC3 '* INT Value read 
                            BitsPerElement = 16
                        Case &HC4 '* DINT Value read (&H91)
                            BitsPerElement = 32
                        Case &HC2 '* SINT
                            BitsPerElement = 8
                    End Select

                    If BitIndex > 0 And BitIndex < BitsPerElement Then
                        '* Force it to appear like a bit array
                        AbreviatedType = &HD3
                        BitIndex -= CInt(Math.Floor(BitIndex / BitsPerElement) * BitsPerElement)
                        '* Return all the bits that came back even if less were requested
                        ElementsToReturn = (returnedData.Length - startIndex) * 8 - BitIndex - 1
                    End If
                End If
            Catch ex As Exception
                '* If the value can not be converted, then it is not a valid integer
            End Try
        End If


        Dim result(ElementsToReturn) As String

        '* If requesting 0 elements, then default to 1
        'Dim ArrayElements As Int16 = Math.Max(result.Length - 1 - 1, 0)


        Select Case AbreviatedType
            Case &HC1 '* BIT
                For i As Integer = 0 To result.Length - 1
                    If returnedData(i + startIndex) > 0 Then
                        result(i) = "True"
                    Else
                        result(i) = "False"
                    End If
                Next
            Case &HCA '* REAL read (&H8A)
                For i As Integer = 0 To result.Length - 1
                    result(i) = CStr(BitConverter.ToSingle(returnedData, (i * 4 + startIndex)))
                Next
            Case &HC3 '* INT Value read 
                For i As Integer = 0 To result.Length - 1
                    result(i) = CStr(BitConverter.ToInt16(returnedData, (i * 2) + startIndex))
                Next
            Case &HC4 '* DINT Value read (&H91)
                For i As Integer = 0 To result.Length - 1
                    result(i) = CStr(BitConverter.ToInt32(returnedData, (i * 4) + startIndex))
                Next
            Case &HC5 '* LINT Value read
                For i As Integer = 0 To result.Length - 1
                    result(i) = CStr(BitConverter.ToInt64(returnedData, (i * 8) + startIndex))
                Next
            Case &HC2 '* SINT
                For i As Integer = 0 To result.Length - 1
                    '* Ver 3.59
                    If returnedData(i + startIndex) < 128 Then
                        result(i) = CStr(returnedData(i + startIndex))
                    Else
                        result(i) = CStr(returnedData(i + startIndex) - 256)
                    End If
                Next
            Case &HD3 '* BOOL Array
                Dim i As Integer
                Dim x, l As UInt32
                Dim CurrentBitPos As Integer = BitIndex
                For j = 0 To ((returnedData.Length - startIndex) / 4) - 1
                    x = BitConverter.ToUInt32(returnedData, CInt(j * 4 + startIndex))
                    While CurrentBitPos < BitsPerElement
                        l = CUInt(2 ^ (CurrentBitPos))
                        result(i) = CStr((x And l) > 0)
                        i += 1
                        CurrentBitPos += 1
                    End While
                    CurrentBitPos = 0
                Next
            Case &H82, &H83 '* TODO: Timer, Counter, Control 
                Dim StartByte As Int16 = 2
                '                Dim x = startAddress
                '* Look for which sub element is specificed
                If startAddress.IndexOf(".") >= 0 Then
                    If startAddress.ToUpper.IndexOf("PRE") > 0 Then
                        StartByte = 6
                    ElseIf startAddress.ToUpper.IndexOf("ACC") > 0 Then
                        StartByte = 10
                    End If
                Else
                    '* If no subelement, then use ACC
                End If

                For i As Integer = 0 To result.Length - 1
                    result(i) = CStr(BitConverter.ToInt32(returnedData, (i + StartByte + startIndex)))
                Next
            Case &HCE ' * String
                For i As Integer = 0 To result.Length - 1
                    result(i) = System.Text.Encoding.ASCII.GetString(returnedData, 88 * i + 4 + startIndex, returnedData(88 * i + startIndex) + returnedData(88 * i + 1 + startIndex) * 256)
                Next
            Case Else
                For i As Integer = 0 To result.Length - 1
                    result(i) = CStr(BitConverter.ToInt16(returnedData, (i * 2) + startIndex))
                Next
        End Select


        Return result
    End Function
#End Region

#Region "Subscriptions"
    Private Class SubscriptionInfo
        Public Sub New()
            PLCAddress = New MfgControl.AdvancedHMI.Drivers.CLXAddress
        End Sub

        Public PLCAddress As MfgControl.AdvancedHMI.Drivers.CLXAddress

        Public dlgCallBack As EventHandler(Of MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs)
        Public PollRate As Integer
        Public ID As Integer
        Public ElementsToRead As Integer
        'Public SkipReads As Integer
        Public DataType As Byte
    End Class

    '* This is used to optimize the reads of the subscriptions
    Private Class SubscriptionRead
        Friend TagName As String
        Friend NumberToRead As Integer
    End Class

    Private GroupedSubscriptionReads As New List(Of SubscriptionRead)
    Private SubscriptionList As New List(Of SubscriptionInfo)

    Private CurrentSubscriptionID As Integer = 1
    Private SubscriptionListChanged As Boolean

    Private SubscriptionThread As System.ComponentModel.BackgroundWorker
    Public Function Subscribe(ByVal PLCAddress As String, ByVal numberOfElements As Int16, ByVal PollRate As Integer, ByVal CallBack As EventHandler(Of MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs)) As Integer Implements IComComponent.Subscribe
        If m_DisableSubscriptions Then
            Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("Cannot create new subscription when DisableSubscriptions=True")
        End If

        If m_PollRateOverride > 0 Then
            PollRate = m_PollRateOverride
        End If

        '* Avoid a 0 poll rate
        If PollRate <= 0 Then
            PollRate = 500
        End If


        '* Valid address?
        'If ParsedResult.FileType <> 0 Then
        Dim tmpPA As New SubscriptionInfo
        tmpPA.PLCAddress.TagName = PLCAddress
        tmpPA.PollRate = PollRate
        tmpPA.dlgCallBack = CallBack

        ''* Attempt to read this value in order to get the DataType
        'SyncLock (ReadLock)
        '    Try
        '        Read(PLCAddress)
        '    Catch ex As Exception
        '        Dim x As New MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs(-50, ex.Message)
        '        x.PlcAddress = PLCAddress
        '        'OnComError(x)
        '        ComErrorHandler(Me, x)
        '    End Try
        'End SyncLock
 

        tmpPA.ID = CurrentSubscriptionID
        '* The ID is used as a reference for removing polled addresses
        CurrentSubscriptionID += 1

        tmpPA.ElementsToRead = numberOfElements

        SubscriptionList.Add(tmpPA)

        '* Sort the list to allow easier grouping
        SubscriptionList.Sort(AddressOf SortPolledAddresses)

        '* Flag this so it will run the optimizer after the first read
        SubscriptionListChanged = True

        '* Put it in the read list. Later it will get grouped for optimizing
        Dim x As New SubscriptionRead
        x.TagName = tmpPA.PLCAddress.TagName
        x.NumberToRead = tmpPA.ElementsToRead
        GroupedSubscriptionReads.Add(x)


        '* Create an list of optimized reads
        'CreateGroupedReadList()

        '* Start the subscription updater if not already running
        If SubscriptionThread Is Nothing Then
            SubscriptionThread = New System.ComponentModel.BackgroundWorker
            AddHandler SubscriptionThread.DoWork, AddressOf SubscriptionUpdate
            SubscriptionThread.RunWorkerAsync()
        End If

        Return tmpPA.ID
    End Function

    '***************************************************************
    '* Used to sort polled addresses by File Number and element
    '* This helps in optimizing reading
    '**************************************************************
    Private Function SortPolledAddresses(ByVal A1 As SubscriptionInfo, ByVal A2 As SubscriptionInfo) As Integer
        '* Sort Tags

        '* 22-JAN-13
        '* Are they in the same array?
        If A1.PLCAddress.BaseArrayTag <> A2.PLCAddress.BaseArrayTag Then
            If A1.PLCAddress.BaseArrayTag > A2.PLCAddress.BaseArrayTag Then
                Return 1
            ElseIf A1.PLCAddress.BaseArrayTag < A2.PLCAddress.BaseArrayTag Then
                Return -1
            End If
        Else
            '* TODO : sort multidimensional array
            If A1.PLCAddress.ArrayIndex1 > A2.PLCAddress.ArrayIndex1 Then
                Return 1
            ElseIf A1.PLCAddress.ArrayIndex1 < A2.PLCAddress.ArrayIndex1 Then
                Return -1
            ElseIf A1.PLCAddress.BitNumber >= 0 And A2.PLCAddress.BitNumber >= 0 Then
                If A1.PLCAddress.BitNumber > A2.PLCAddress.BitNumber Then
                    Return 1
                ElseIf A1.PLCAddress.BitNumber < A2.PLCAddress.BitNumber Then
                    Return -1
                End If
            End If
        End If

        Return 0
    End Function

    Public Function UnSubscribe(ByVal ID As Integer) As Integer Implements IComComponent.Unsubscribe
        Dim i As Integer = 0
        While i < SubscriptionList.Count AndAlso SubscriptionList(i).ID <> ID
            i += 1
        End While

        If i < SubscriptionList.Count Then
            SubscriptionList.RemoveAt(i)
        End If

        CreateGroupedReadList()

        Return 0
    End Function

    '******************************************************************************************
    '* Group together subscriptions for fewer reads to increase speed and efficiency
    '******************************************************************************************
    Private Sub CreateGroupedReadList()
        GroupedSubscriptionReads.Clear()

        SubscriptionListChanged = False

        Dim i, NumberToRead, FirstElement As Integer
        While i < SubscriptionList.Count
            NumberToRead = SubscriptionList(i).ElementsToRead
            FirstElement = i
            Dim GroupedCount As Integer

            GroupedCount = 1
            '* Optimize by reading array elements together - only single dimension array and basic data types. Do not group
            If SubscriptionList(FirstElement).PLCAddress.ArrayIndex1 >= 0 And SubscriptionList(FirstElement).PLCAddress.ArrayIndex2 < 0 And _
                SubscriptionList(FirstElement).DataType >= &HC2 And SubscriptionList(FirstElement).DataType <= &HD3 And
                (SubscriptionList(FirstElement).PLCAddress.SubElement = "") Then
                Try
                    While FirstElement + GroupedCount < SubscriptionList.Count AndAlso _
                      SubscriptionList(FirstElement).PLCAddress.BaseArrayTag = SubscriptionList(FirstElement + GroupedCount).PLCAddress.BaseArrayTag

                        '* Add the number of span between the array
                        '* 07-NOV-12 - Sorting is Alphanumeric, so array do not sort properly
                        If NumberToRead < (SubscriptionList(FirstElement + GroupedCount).PLCAddress.ArrayIndex1 - SubscriptionList(FirstElement).PLCAddress.ArrayIndex1 + 1) Then
                            NumberToRead = SubscriptionList(FirstElement + GroupedCount).PLCAddress.ArrayIndex1 - SubscriptionList(FirstElement).PLCAddress.ArrayIndex1 + 1
                            '* If it's a BOOL Array, then find number of DINTs spanned
                            If SubscriptionList(FirstElement).DataType = &HD3 Then
                                NumberToRead = CInt(Math.Ceiling(NumberToRead / 32))
                                '* Do we span across words (e.g. start at 16 and end at 48)
                                NumberToRead += CInt((Math.Floor(SubscriptionList(FirstElement + GroupedCount).PLCAddress.ArrayIndex1 / 32) - Math.Floor(SubscriptionList(FirstElement).PLCAddress.ArrayIndex1 / 32)))
                            End If
                        End If
                        GroupedCount += 1
                    End While
                Catch ex As Exception
                    Dim dbg = 0
                End Try
            End If

            '* Strip off the bit because we will read the complete element, then extract the bit after data is returned
            Dim TagNameNoBit As String = SubscriptionList(FirstElement).PLCAddress.TagName
            If SubscriptionList(FirstElement).PLCAddress.BitNumber >= 0 Then
                TagNameNoBit = TagNameNoBit.Substring(0, TagNameNoBit.LastIndexOf("."))
            End If

            '* Check to see if it already in the subscription list
            Dim AlreadyInList As Boolean = False
            For Each g In GroupedSubscriptionReads
                If g.TagName = SubscriptionList(FirstElement).PLCAddress.TagName Then
                    AlreadyInList = True
                End If
            Next

            If Not AlreadyInList Then
                '* Add this read to the list  of grouped reads
                Dim x As New SubscriptionRead
                x.TagName = TagNameNoBit
                '* If it is a BOOL Array round down to the nearest DINT aligned bit
                If SubscriptionList(FirstElement).DataType = &HD3 Then
                    Dim DINTNumber As Integer = CInt(Math.Floor(SubscriptionList(FirstElement).PLCAddress.ArrayIndex1 / 32))
                    x.TagName = TagNameNoBit.Substring(0, TagNameNoBit.LastIndexOf("[") + 1) & DINTNumber * 32 & "]"
                End If

                x.NumberToRead = NumberToRead
                GroupedSubscriptionReads.Add(x)
            End If

            i += GroupedCount
        End While
    End Sub

    '**************************************************************
    '* Perform the reads for the variables added for notification
    '* Attempt to optimize by grouping reads
    '**************************************************************
    'Private InternalRequest As Boolean '* This is used to dinstinquish when to send data back to notification request
    Private HandleCreated As Boolean
    Private StopSubscriptions As Boolean
    Private Sub SubscriptionUpdate(sender As System.Object, e As System.ComponentModel.DoWorkEventArgs)
        Dim ReadTime As New Stopwatch
        'Dim SequenceNumber As Integer
        While Not StopSubscriptions
            Dim index As Integer
            If Not m_DisableSubscriptions And GroupedSubscriptionReads IsNot Nothing Then
                '* 3-JUN-13 Do not read data until handles are created to avoid exceptions
                If Not HandleCreated AndAlso m_SynchronizingObject IsNot Nothing Then
                    If TypeOf (m_SynchronizingObject) Is System.Windows.Forms.Control Then
                        If DirectCast(m_SynchronizingObject, System.Windows.Forms.Control).IsHandleCreated Then
                            HandleCreated = True
                        End If
                    End If
                Else
                    Dim DelayBetweenPackets As Integer
                    index = 0
                    While index < GroupedSubscriptionReads.Count And Not StopSubscriptions
                        '* Evenly space out read requests to avoid Send Que Full
                        DelayBetweenPackets = CInt(Math.Max(1, Math.Floor(m_PollRateOverride / GroupedSubscriptionReads.Count)))
                        ReadTime.Start()
                        Try
                            If Not m_DisableSubscriptions Then
                                Dim TransactionNumber As Integer
                                TransactionNumber = Me.BeginRead(GroupedSubscriptionReads(index).TagName, GroupedSubscriptionReads(index).NumberToRead)
                                
                                Dim response As Integer = WaitForResponse(TransactionNumber, 300)
                                If response = 0 Then
                                    SendToSubscriptions(ReturnedInfo(TransactionNumber And 255))
                                Else
                                    Dim dbg = 0
                                End If
                            End If
                        Catch ex As MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException
                            Dim x As New MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs(ex.ErrorCode, ex.Message)
                            SendToSubscriptions(x)
                        Catch ex As Exception
                            '* Send this message back to the requesting control
                            Dim x As New MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs(-99, ex.Message)
                            SendToSubscriptions(x)
                        End Try

                        ReadTime.Stop()

                        '* Evenly space out the reads to avoid SendQue Full
                        If CInt(ReadTime.ElapsedMilliseconds) < DelayBetweenPackets Then
                            Threading.Thread.Sleep(DelayBetweenPackets - CInt(ReadTime.ElapsedMilliseconds))
                        End If

                        ReadTime.Reset()
                        index += 1
                    End While
                End If
            End If

            If GroupedSubscriptionReads.Count <= 0 Then
                Threading.Thread.Sleep(m_PollRateOverride)
            End If

            If SubscriptionListChanged Then
                CreateGroupedReadList()
            End If
        End While
    End Sub

    Private Sub SendToSubscriptions(ByVal e As MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs)
        '*********************************************************
        '* Check to see if this is from the Polled variable list
        '*********************************************************
        For i As Integer = 0 To SubscriptionList.Count - 1
            '* Is it a complex data type
            If e.RawData IsNot Nothing AndAlso e.RawData.Length > 4 Then
                If e.RawData(4) = &HA0 Then
                    SubscriptionList(i).DataType = e.RawData(6)
                Else
                    SubscriptionList(i).DataType = e.RawData(4)
                End If
            End If

            If e.ErrorId = 0 Then
                Dim MaxElementNeeded As Integer = SubscriptionList(i).PLCAddress.ArrayIndex1 - PLCAddressByTNS(e.TransactionNumber And 255).ArrayIndex1 + 1
                Dim ElementNumber1 As Integer = PLCAddressByTNS(e.TransactionNumber And 255).ArrayIndex1
                Dim ElementNumber2 As Integer = PLCAddressByTNS(e.TransactionNumber And 255).ArrayIndex2
                Dim ElementNumber3 As Integer = PLCAddressByTNS(e.TransactionNumber And 255).ArrayIndex3


                Dim MaxElementRead As Integer = e.Values.Count
                Dim StartElementRead As Integer = PLCAddressByTNS(e.TransactionNumber And 255).ArrayIndex1
                Dim StartElementNeeded As Integer = SubscriptionList(i).PLCAddress.ArrayIndex1

                '*Boolean array are read like DINT
                '* Boolean array
                If PLCAddressByTNS(e.TransactionNumber And 255).AbreviatedDataType = &HD3 Then
                    ElementNumber1 = PLCAddressByTNS(e.TransactionNumber And 255).ArrayIndex1 * 32
                    MaxElementNeeded = SubscriptionList(i).PLCAddress.ArrayIndex1 - PLCAddressByTNS(e.TransactionNumber And 255).ArrayIndex1 * 32 + 1
                    StartElementRead *= 32
                End If

                If ElementNumber1 < 0 Then
                    ElementNumber1 = 0
                End If

                If (SubscriptionList(i).PLCAddress.BaseArrayTag = PLCAddressByTNS(e.TransactionNumber And 255).BaseArrayTag And _
                        ((StartElementRead <= StartElementNeeded And MaxElementNeeded <= MaxElementRead) Or StartElementNeeded < 0)) Then

                    Dim BitResult(SubscriptionList(i).ElementsToRead - 1) As String

                    '* All other data types
                    For k As Integer = 0 To SubscriptionList(i).ElementsToRead - 1
                        '* a -1 in ArrayElement number means it is not an array
                        If SubscriptionList(i).PLCAddress.ArrayIndex1 >= 0 Then
                            BitResult(k) = e.Values(SubscriptionList(i).PLCAddress.ArrayIndex1 - ElementNumber1 + k)
                        Else
                            BitResult(k) = e.Values(k)
                        End If
                    Next


                    '* 23-APR-13 Did we read an array of integers, but the subscribed element was a bit?
                    If PLCAddressByTNS(e.TransactionNumber And 255).BitNumber < 0 And SubscriptionList(i).PLCAddress.BitNumber >= 0 Then
                        BitResult(0) = CStr((CInt(2 ^ SubscriptionList(i).PLCAddress.BitNumber) And CInt(BitResult(0))) > 0)
                    End If

                    '* Do we have a subelement?
                    If SubscriptionList(i).PLCAddress.SubElement = "" OrElse SubscriptionList(i).PLCAddress.SubElement = PLCAddressByTNS(e.TransactionNumber And 255).SubElement Then

                        '* Is it a multi-dimensional array?
                        If PLCAddressByTNS(e.TransactionNumber And 255).ArrayIndex2 = SubscriptionList(i).PLCAddress.ArrayIndex2 And PLCAddressByTNS(e.TransactionNumber And 255).ArrayIndex3 = SubscriptionList(i).PLCAddress.ArrayIndex3 Then
                            '* Make sure the Handle is created on the form
                            '* 27-AUG-13 This prevents a problem caused by the DataSubscriber
                            If m_SynchronizingObject Is Nothing OrElse DirectCast(m_SynchronizingObject, Windows.Forms.Control).IsHandleCreated Then
                                '* Modify the grouped read to be as if it only read this one subscription
                                e.PlcAddress = SubscriptionList(i).PLCAddress.TagName

                                e.Values.Clear()
                                For index = 0 To BitResult.Length - 1
                                    e.Values.Add(BitResult(index))
                                Next

                                e.SubscriptionID = SubscriptionList(i).ID
                                'Dim x As New MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs(BitResult, SubscriptionList(i).PLCAddress.TagName, e.TransactionNumber)
                                'x.SubscriptionID = SubscriptionList(i).ID
                                Dim z() As Object = {Me, e}
                                '* 27-AUG-14 version 3.67
                                If m_SynchronizingObject IsNot Nothing Then
                                    m_SynchronizingObject.BeginInvoke(SubscriptionList(i).dlgCallBack, z)
                                Else
                                    SubscriptionList(i).dlgCallBack(Me, e)
                                End If
                            End If
                        End If
                    End If
                End If
            Else
                '* Error to send
                If (PLCAddressByTNS(e.TransactionNumber And 255) Is Nothing) OrElse (SubscriptionList(i).PLCAddress.BaseArrayTag = PLCAddressByTNS(e.TransactionNumber And 255).BaseArrayTag) Then
                    If m_SynchronizingObject IsNot Nothing Then
                        Dim z() As Object = {Me, e}
                        m_SynchronizingObject.BeginInvoke(SubscriptionList(i).dlgCallBack, z)
                    Else
                        SubscriptionList(i).dlgCallBack(Me, e)
                    End If
                End If
            End If
        Next
    End Sub
#End Region

#Region "Helper"
    '****************************************************
    '* Wait for a response from PLC before returning
    '****************************************************
    Private Function WaitForResponse(ByVal rTNS As Integer, timeout As Integer) As Integer
        Dim Loops As Integer
        While Not PLCAddressByTNS(rTNS And 255).Responded And Loops < timeout
            System.Threading.Thread.Sleep(1)
            Loops += 1
        End While

        If Loops >= timeout Then
            Return -20
        Else
            Return 0
        End If
    End Function


    '************************************************
    '* Convert the message code number into a string
    '* Ref Page 8-3
    '************************************************
    Public Shared Function DecodeMessage(ByVal msgNumber As Integer) As String
        Select Case msgNumber
            Case 0
                DecodeMessage = ""
            Case -1
                Return "Ethernet Socket Error"
            Case -4
                Return "Unknown Message from DataLink Layer"
            Case -5
                Return "Invalid Address"
            Case -7
                Return "No data specified to data link layer"
            Case -8
                Return "No data returned from PLC"
            Case -20
                Return "No Data Returned"

                '*** Errors coming from PLC
            Case 4
                Return "Invalid Tag Address."
            Case 5
                Return "The particular item referenced (usually instance) could not be found"
            Case &HA
                Return "An error has occurred trying to process one of the attributes"
            Case &H13
                Return "Not enough command data / parameters were supplied in the command to execute the service requested"
            Case &H1C
                Return "An insufficient number of attributes were provided compared to the attribute count"
            Case &H26
                Return "The IOI word length did not match the amount of IOI which was processed"
            Case 32
                Return "PLC Has a Problem and Will Not Communicate"

                '* EXT STS Section - 256 is added to code to distinguish EXT codes
            Case 257
                Return "A field has an illegal value"
            Case 258
                Return "Less levels specified in address than minimum for any address"
            Case 270
                Return "Command cannot be executed"
                '* Extended CIP codes - Page 13 Logix5000 Data Access
            Case &H2105
                Return "You have tried to access beyond the end of the data object"
            Case &H2107
                Return "The abbreviated type does not match the data type of the data object."
            Case &H2104
                Return "The beginning offset was beyond the end of the template"
            Case Else
                Return "Unknown Message - " & msgNumber
        End Select
    End Function

#End Region

#Region "Events"
    '***************************************************************************************
    '* If an error comes back from the driver, return the description back to the control
    '***************************************************************************************
    Protected Sub ComErrorHandler(ByVal sender As Object, ByVal e As MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs)
        Dim d() As String = {DecodeMessage(e.ErrorId)}

        If PLCAddressByTNS(e.TransactionNumber And 255) IsNot Nothing Then
            PLCAddressByTNS(e.TransactionNumber And 255).Responded = True
        End If

        OnComError(e)

        SendToSubscriptions(e)
    End Sub


    '********************************************************************************************************************************
    Protected Sub CIPConnectionClosed(ByVal sender As Object, e As EventArgs)
        OnConnectionClosed(e)
    End Sub

    Protected Overridable Sub OnConnectionClosed(ByVal e As EventArgs)
        If m_SynchronizingObject IsNot Nothing Then
            Dim Parameters() As Object = {Me, EventArgs.Empty}
            m_SynchronizingObject.BeginInvoke(occd, Parameters)
        Else
            RaiseEvent ConnectionClosed(Me, e)
        End If
    End Sub

    Dim occd As EventHandler(Of EventArgs) = AddressOf ConnectionClosedSync
    Private Sub ConnectionClosedSync(ByVal sender As Object, ByVal e As EventArgs)
        RaiseEvent ConnectionClosed(Me, e)
    End Sub
    '********************************************************************************************************************************



    Protected Sub CIPConnectionEstablished(ByVal sender As Object, e As EventArgs)
        OnConnectionEstablished(e)
    End Sub

    Protected Overridable Sub OnConnectionEstablished(ByVal e As EventArgs)
        If m_SynchronizingObject IsNot Nothing Then
            Dim Parameters() As Object = {Me, EventArgs.Empty}
            m_SynchronizingObject.BeginInvoke(oced, Parameters)
        Else
            RaiseEvent ConnectionEstablished(Me, e)
        End If
    End Sub

    Dim oced As EventHandler(Of EventArgs) = AddressOf ConnectionEstablishedSync
    Private Sub ConnectionEstablishedSync(ByVal sender As Object, ByVal e As EventArgs)
        RaiseEvent ConnectionEstablished(Me, e)
    End Sub



    Protected Sub DataLinkLayer_DataReceived(ByVal sender As Object, ByVal e As MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs)
        If (e Is Nothing) Then Exit Sub

        '* Was this TNS requested by this instance
        If e.OwnerObjectID <> MyObjectID Then
            Exit Sub
        End If

        '* Check the status byte
        Dim StatusByte As Integer = e.RawData(2)
        '* Extended status code, Page 13 of Logix5000 Data Access
        If StatusByte = &HFF And e.RawData.Length >= 5 Then
            StatusByte = e.RawData(5) * 256 + e.RawData(4)
        End If

        If StatusByte = 0 Then
            '**************************************************************
            '* Only extract and send back if this response contained data
            '**************************************************************
            If e.RawData.Length > 5 Then
                '***************************************************
                '* Extract returned data into appropriate data type
                '* Transfer block of data read to the data table array
                '***************************************************
                '* TODO: Check array bounds
                '* Pass the abreviated data type (page 11 of 1756-RM005A)
                Dim DataType As Byte = e.RawData(4)

 
                Dim DataStartIndex As UInt16 = 6
                '* Is it a complex data type
                If DataType = &HA0 Then
                    DataType = e.RawData(6)
                    DataStartIndex = 8
                End If

                PLCAddressByTNS(e.TransactionNumber And 255).AbreviatedDataType = DataType

                Dim d() As String
                d = ExtractData(PLCAddressByTNS(e.TransactionNumber And 255).TagName, DataType, e.RawData, DataStartIndex)

                ReturnedInfo(e.TransactionNumber And 255) = e
                For Each v In d
                    ReturnedInfo(e.TransactionNumber And 255).Values.Add(v)
                Next

                PLCAddressByTNS(e.TransactionNumber And 255).Responded = True

                OnDataReceived(ReturnedInfo(e.TransactionNumber And 255))
            End If
        Else
            '*****************************
            '* Failed Status was returned
            '*****************************
            e.ErrorId = StatusByte
            ReturnedInfo(e.TransactionNumber And 255) = New MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs(StatusByte, DecodeMessage(StatusByte), e.TransactionNumber)

            PLCAddressByTNS(e.TransactionNumber And 255).Responded = True

            'If Not DisableEvent Then
            'If e.Values.Count <= 0 Then e.Values.Add(e.ErrorMessage)
            OnComError(e)
            'End If
        End If
    End Sub


    '******************************************************************
    '* This is called when a message instruction was sent from the PLC
    '******************************************************************
    Private Sub DataLink1_UnsolictedMessageRcvd()
        If m_SynchronizingObject IsNot Nothing Then
            Dim Parameters() As Object = {Me, EventArgs.Empty}
            m_SynchronizingObject.BeginInvoke(drsd, Parameters)
        Else
            RaiseEvent UnsolictedMessageRcvd(Me, System.EventArgs.Empty)
        End If
    End Sub

    Protected Overridable Sub OnDataReceived(ByVal e As MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs)
        If m_SynchronizingObject IsNot Nothing AndAlso m_SynchronizingObject.InvokeRequired Then
            m_SynchronizingObject.BeginInvoke(drsd, New Object() {Me, e})
        Else
            RaiseEvent DataReceived(Me, e)
        End If
    End Sub

    '****************************************************************************
    '* This is required to sync the event back to the parent form's main thread
    '****************************************************************************
    Dim drsd As EventHandler(Of MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs) = AddressOf DataReceivedSync
    Private Sub DataReceivedSync(ByVal sender As Object, ByVal e As MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs)
        RaiseEvent DataReceived(Me, e)
    End Sub


    '***********************************************************************************************************
    Protected Overridable Sub OnComError(ByVal e As MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs)
        If m_SynchronizingObject IsNot Nothing Then
            Dim Parameters() As Object = {Me, e}
            m_SynchronizingObject.BeginInvoke(errorsd, Parameters)
        Else
            RaiseEvent ComError(Me, e)
        End If
    End Sub

    Private errorsd As New EventHandler(Of MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs)(AddressOf ErrorReceivedSync)
    Private Sub ErrorReceivedSync(ByVal sender As Object, ByVal e As MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs)
        RaiseEvent ComError(sender, e)
    End Sub
    '***********************************************************************************************************

    Private Sub UnsolictedMessageRcvdSync(ByVal sender As Object, ByVal e As EventArgs)
        RaiseEvent UnsolictedMessageRcvd(sender, e)
    End Sub
#End Region

End Class
