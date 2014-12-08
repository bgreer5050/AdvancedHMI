Option Strict On
'******************************************************************************
'* Modbus Implementation
'*
'* Archie Jacobs
'* Manufacturing Automation, LLC
'* ajacobs@mfgcontrol.com
'* 06-DEC-14
'*
'* Copyright 2014 Archie Jacobs
'******************************************************************************
Public MustInherit Class ModbusBase
    Inherits System.ComponentModel.Component
    Implements IComComponent

    Public Event DataReceived As EventHandler(Of MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs)
    Public Event ComError As EventHandler(Of MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs)

    Private Requests(255) As MfgControl.AdvancedHMI.Drivers.Modbus.ModbusAddress
    Private Responses(255) As MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs

    Private Shared ObjectIDs As Int64
    Protected MyObjectID As Int64


    Protected MustOverride Sub CreateDLLInstance()
    Protected MustOverride Function GetNextTransactionID(ByVal maxValue As Integer) As Integer
    Friend MustOverride Function SendRequest(ByVal PDU As MfgControl.AdvancedHMI.Drivers.Modbus.ModbusPDUFrame) As Integer
    Protected MustOverride Function IsInQue(transactionNumber As Integer, ownerObjectID As Int64) As Boolean

#Region "Properties"
    Private m_PollRateOverride As Integer = 500
    <System.ComponentModel.Category("Communication Settings")> _
    <System.ComponentModel.DefaultValue(0)> _
    Public Property PollRateOverride() As Integer
        Get
            Return m_PollRateOverride
        End Get
        Set(ByVal value As Integer)
            '* Poll rate are in increments of 100
            m_PollRateOverride = CInt(Math.Ceiling(value / 10)) * 10
        End Set
    End Property

    '**************************************************
    '* Its purpose is to fetch
    '* the main form in order to synchronize the
    '* notification thread/event
    '**************************************************
    Private m_SynchronizingObject As System.ComponentModel.ISynchronizeInvoke
    Public Property SynchronizingObject() As System.ComponentModel.ISynchronizeInvoke
        Get
            'If (m_SynchronizingObject Is Nothing) AndAlso Me.DesignMode Then
            If (m_SynchronizingObject Is Nothing) AndAlso AppDomain.CurrentDomain.FriendlyName.IndexOf("DefaultDomain", System.StringComparison.CurrentCultureIgnoreCase) >= 0 Then
                Dim host1 As System.ComponentModel.Design.IDesignerHost
                host1 = CType(Me.GetService(GetType(System.ComponentModel.Design.IDesignerHost)), System.ComponentModel.Design.IDesignerHost)
                If host1 IsNot Nothing Then
                    m_SynchronizingObject = CType(host1.RootComponent, System.ComponentModel.ISynchronizeInvoke)
                End If
                '* Windows CE, comment above 5 lines
            End If
            Return m_SynchronizingObject
        End Get

        Set(ByVal Value As System.ComponentModel.ISynchronizeInvoke)
            If Value IsNot Nothing Then
                m_SynchronizingObject = Value
            End If
        End Set
    End Property

    '*********************************************************************************
    '* Used to stop subscription updates when not needed to reduce communication load
    '*********************************************************************************
    Private m_DisableSubscriptions As Boolean
    Public Property DisableSubscriptions() As Boolean Implements IComComponent.DisableSubscriptions
        Get
            Return m_DisableSubscriptions
        End Get
        Set(ByVal value As Boolean)
            m_DisableSubscriptions = value

        End Set
    End Property
#End Region

#Region "Constructor/Destructor"
    Public Sub New()
        ObjectIDs += 1
        MyObjectID = ObjectIDs
    End Sub

    Public Sub New(ByVal container As System.ComponentModel.IContainer)
        MyClass.New()

        'Required for Windows.Forms Class Composition Designer support
        container.Add(Me)
    End Sub

    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        '* Stop the subscription thread
        StopSubscriptions = True
    End Sub
#End Region

#Region "Public Methods"
    '**********************************************************************************************
    '* Read methods
    '**********************************************************************************************
    Public Function Read(ByVal startAddress As String) As String
        Return Read(startAddress, 1)(0)
    End Function

    Public Function Read(ByVal startAddress As String, ByVal numberOfElements As Integer) As String() Implements IComComponent.Read
        Dim TransactionID As Integer = BeginRead(startAddress, numberOfElements)

        If WaitForResponse(CUShort(TransactionID)) = 0 Then
            If Responses(CUShort(TransactionID)) IsNot Nothing AndAlso Responses(CUShort(TransactionID)).ErrorId = 0 Then
                Dim tmp(Responses(TransactionID).Values.Count - 1) As String
                For i As Integer = 0 To tmp.Length - 1
                    tmp(i) = Responses(TransactionID).Values(i)
                Next
                Return tmp
            Else
                Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException(MfgControl.AdvancedHMI.Drivers.ModbusUtilities.DecodeMessage(Responses(TransactionID).ErrorId))
            End If
        Else
            Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("No Reponse from PLC. Ensure baud rate is correct.")
        End If
    End Function

    Public Function BeginRead(ByVal startAddress As String, ByVal numberOfElements As Integer) As Integer Implements IComComponent.BeginRead
        Dim address As New MfgControl.AdvancedHMI.Drivers.Modbus.ModbusAddress(startAddress, numberOfElements)
        Return BeginRead(address)
    End Function

    Public Function BeginRead(ByVal startAddress As String) As Integer
        Return BeginRead(startAddress, 1)
    End Function

    Public Function BeginRead(ByVal address As MfgControl.AdvancedHMI.Drivers.Modbus.ModbusAddress) As Integer
        Dim TransactionID As Integer
        TransactionID = GetNextTransactionID(32767)

        Requests(TransactionID And 255) = address

        Dim PDU As New MfgControl.AdvancedHMI.Drivers.Modbus.ModbusPDUFrame(address.ReadFunctionCode, address, TransactionID)

        SendRequest(PDU)

        Return TransactionID
    End Function

    '**********************************************************************************************
    '* Write methods
    '**********************************************************************************************
    Public Function Write(ByVal startAddress As String, ByVal dataToWrite As String) As String Implements IComComponent.Write
        Dim DataAsArray() As String = {dataToWrite}
        Dim address As New MfgControl.AdvancedHMI.Drivers.Modbus.ModbusAddress(startAddress, 1)

        Return CStr(Write(address, DataAsArray))
    End Function

    '* Write
    '* Reference : 
    Public Function Write(ByVal address As MfgControl.AdvancedHMI.Drivers.Modbus.ModbusAddress, ByVal dataToWrite() As String) As Integer
        '* No data was sent, so exit
        If dataToWrite.Length <= 0 Then Return 0

        ''* Attach the instruction data
        Dim dataPacket As New List(Of Byte)
        dataPacket = address.GetWriteBytes(dataToWrite)


        If address.WriteFunctionCode > 0 Then
            Dim TransactionID As Integer = GetNextTransactionID(32767)
            Dim PDU As New MfgControl.AdvancedHMI.Drivers.Modbus.ModbusPDUFrame(address.WriteFunctionCode, address, TransactionID, dataPacket.ToArray)

            Requests(TransactionID And 255) = address
            SendRequest(PDU)

            Return TransactionID
        Else
            Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException(-200, "Invalid address to write to")
        End If
    End Function


#End Region

#Region "Subscription"
    Private SubscriptionList As New List(Of PolledAddressInfo)

    Private Structure PolledAddressInfo
        Dim Address As MfgControl.AdvancedHMI.Drivers.Modbus.ModbusAddress
        Dim dlgCallBack As EventHandler(Of MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs)
        Dim PollRate As Integer
        Dim PollRateDivisor As Integer
        Dim ID As Integer
    End Structure

    Private SubscriptionThread As System.ComponentModel.BackgroundWorker

    '* This is used to optimize the reads of the subscriptions
    Private Class SubscriptionRead
        Friend Address As String
        Friend NumberToRead As Integer
    End Class

    Private GroupedSubscriptionReads As New List(Of SubscriptionRead)
    Private SubscriptionListChanged As Boolean


    '*******************************************************************
    '*******************************************************************
    Private CurrentID As Integer
    Public Function Subscribe(ByVal plcAddress As String, ByVal numberOfElements As Int16, ByVal pollRate As Integer, ByVal callback As EventHandler(Of MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs)) As Integer Implements IComComponent.Subscribe
        If plcAddress = "4001" Then
            Dim dbg = 0
        End If

        If m_PollRateOverride <> 0 Then
            pollRate = m_PollRateOverride
        Else
            '* Poll rate is in 50ms increments
            pollRate = CInt(Math.Ceiling(pollRate / 50) * 50)
            '* Avoid a 0 poll rate
            If pollRate <= 0 Then
                pollRate = 500
            End If
        End If

        '***********************************************
        '* Create an Address object address information
        '***********************************************
        Dim address As New MfgControl.AdvancedHMI.Drivers.Modbus.ModbusAddress(plcAddress)

        '***********************************************************
        '* Check if there was already a subscription made for this
        '***********************************************************
        Dim index As Integer

        While index < SubscriptionList.Count AndAlso _
            (SubscriptionList(index).Address.Address <> plcAddress Or SubscriptionList(index).dlgCallBack <> callback)
            index += 1
        End While


        '* If a subscription was already found, then returns it's ID
        If (index < SubscriptionList.Count) Then
            '* Return the subscription that already exists
            Return SubscriptionList(index).ID
        Else
            '* The ID is used as a reference for removing polled addresses
            CurrentID += 1

            Dim tmpPA As PolledAddressInfo

            tmpPA.PollRate = pollRate

            '* Poll rate is only in increments of 50ms
            tmpPA.PollRateDivisor = CInt(pollRate / 50)
            If tmpPA.PollRateDivisor <= 0 Then tmpPA.PollRateDivisor = 1

            tmpPA.dlgCallBack = callback
            tmpPA.ID = CurrentID
            tmpPA.Address = address
            tmpPA.Address.Tag = CurrentID
            tmpPA.Address.NumberOfElements = numberOfElements

            '* Add this subscription to the collection and sort
            SubscriptionList.Add(tmpPA)
            SubscriptionList.Sort(AddressOf SortPolledAddresses)

            '* Flag this so it will run the optimizer after the first read
            SubscriptionListChanged = True

            '* Put it in the read list. Later it will get grouped for optimizing
            Dim x As New SubscriptionRead
            x.Address = tmpPA.Address.Address
            x.NumberToRead = tmpPA.Address.NumberOfElements
            GroupedSubscriptionReads.Add(x)

            '* Start the subscription updater if not already running
            If SubscriptionThread Is Nothing Then
                SubscriptionThread = New System.ComponentModel.BackgroundWorker
                AddHandler SubscriptionThread.DoWork, AddressOf SubscriptionUpdate
                SubscriptionThread.RunWorkerAsync()
            End If


            Return tmpPA.ID
        End If
    End Function

    '***************************************************************
    '* Used to sort polled addresses by File Type and element
    '* This helps in optimizing reading
    '**************************************************************
    Private Function SortPolledAddresses(ByVal A1 As PolledAddressInfo, ByVal A2 As PolledAddressInfo) As Integer
        If (A1.Address.ReadFunctionCode > A2.Address.ReadFunctionCode) Or (A1.Address.ReadFunctionCode = A2.Address.ReadFunctionCode And A1.Address.Element > A2.Address.Element) Or _
             (A1.Address.ReadFunctionCode = A2.Address.ReadFunctionCode And A1.Address.Element = A2.Address.Element And A1.Address.BitNumber > A2.Address.BitNumber) Then
            Return 1
        ElseIf A1.Address.ReadFunctionCode = A2.Address.ReadFunctionCode And A1.Address.Element = A2.Address.Element And A1.Address.BitNumber = A2.Address.BitNumber Then
            Return 0
        Else
            Return -1
        End If
    End Function

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
                            '**************************************************************
                            '* See if a request is already queued so we don't que it again
                            '* Added Version 3.68
                            '**************************************************************
                            Dim index2 As Integer = 0
                            Dim AlreadyQueued As Boolean = False
                            Try
                                While index2 < 127 And Not AlreadyQueued
                                    If Requests(index2) IsNot Nothing Then
                                        If Requests(index2).Address = GroupedSubscriptionReads(index).Address AndAlso _
                                            Requests(index2).NumberOfElements = GroupedSubscriptionReads(index).NumberToRead Then
                                            Try
                                                '* See if it has a response and is still alive
                                                If Not Requests(index2).Responded And IsInQue(index2, MyObjectID) Then
                                                    AlreadyQueued = True
                                                End If
                                            Catch ex As Exception
                                                Dim dbg = 0
                                                Throw
                                            End Try
                                        End If
                                    End If
                                    index2 += 1
                                End While
                            Catch ex As Exception
                                Dim dbg = 0
                                Throw
                            End Try

                            If Not m_DisableSubscriptions And Not AlreadyQueued Then
                                Dim TransactionNumber As Integer
                                TransactionNumber = Me.BeginRead(GroupedSubscriptionReads(index).Address, GroupedSubscriptionReads(index).NumberToRead)

                                Dim response As Integer = WaitForResponse(TransactionNumber And 255)
                                If response = 0 Then
                                    SendToSubscriptions(Responses(TransactionNumber And 255))
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

    '****************************************************************************
    '* Group reads together to optimize communications
    '****************************************************************************
    Private Sub CreateGroupedReadList()
        GroupedSubscriptionReads.Clear()
        SubscriptionListChanged = False

        Dim index, ItemCountToGroup, HighestElement, ElementSpan As Integer


        While index < SubscriptionList.Count
            Try
                '* optimize in as few reads as possible - try group reading
                '* and perform in Async Mode
                ItemCountToGroup = 0
                'HighestElement = SubscriptionList(index).Address.Element + SubscriptionList(index).Address.NumberOfElements - 1
                ElementSpan = HighestElement - SubscriptionList(index).Address.Element
                While (index + ItemCountToGroup + 1) < SubscriptionList.Count AndAlso _
                    SubscriptionList(index + ItemCountToGroup).Address.ReadFunctionCode = SubscriptionList(index + ItemCountToGroup + 1).Address.ReadFunctionCode AndAlso _
                    SubscriptionList(index + ItemCountToGroup).Address.BitsPerElement = SubscriptionList(index + ItemCountToGroup + 1).Address.BitsPerElement AndAlso _
                    ((SubscriptionList(index + ItemCountToGroup + 1).Address.Element + SubscriptionList(index + ItemCountToGroup + 1).Address.NumberOfElements) - SubscriptionList(index).Address.Element) < 25

                    'If (SubscriptionList(index + ItemCountToGroup + 1).Address.Element + SubscriptionList(index + ItemCountToGroup + 1).Address.NumberOfElements - 1) > HighestElement Then
                    '    HighestElement = SubscriptionList(index + ItemCountToGroup + 1).Address.Element + (SubscriptionList(index + ItemCountToGroup + 1).Address.NumberOfElements) - 1
                    'End If

                    ItemCountToGroup += 1
                End While
            Catch ex As Exception
                Dim dbg = 0
                Throw
            End Try

            ElementSpan = (SubscriptionList(index + ItemCountToGroup).Address.Element - SubscriptionList(index).Address.Element)
            Dim sr As New SubscriptionRead
            sr.Address = SubscriptionList(index).Address.Address
            sr.NumberToRead = (ElementSpan + 1)
            If SubscriptionList(index).Address.BitsPerElement = 32 Then
                sr.NumberToRead *= 2
                'ElseIf SubscriptionList(index).Address.BitsPerElement = 1 Then
                '   sr.NumberToRead = CInt(Math.Ceiling(sr.NumberToRead / 8))
            End If

            GroupedSubscriptionReads.Add(sr)
            index += (1 + ItemCountToGroup)
        End While
    End Sub



    Public Function Unsubscribe(ByVal id As Integer) As Integer Implements IComComponent.Unsubscribe
        Dim i As Integer = 0
        While i < SubscriptionList.Count AndAlso SubscriptionList(i).ID <> id
            i += 1
        End While

        If i < SubscriptionList.Count Then
            Dim PollRate As Integer = SubscriptionList(i).PollRate
            SubscriptionList.RemoveAt(i)
            If SubscriptionList.Count = 0 Then
            Else
                '* Check if no more subscriptions to this poll rate
                Dim j As Integer
                Dim StillUsed As Boolean
                While j < SubscriptionList.Count
                    If SubscriptionList(j).PollRate = PollRate Then
                        StillUsed = True
                    End If
                    j += 1
                End While
            End If
        End If
    End Function

    '* 31-JAN-12
    Public Function IsSubscriptionActive(ByVal id As Integer) As Boolean
        Dim i As Integer = 0
        While i < SubscriptionList.Count AndAlso SubscriptionList(i).ID <> id
            i += 1
        End While

        Return (i < SubscriptionList.Count)
    End Function

    '* 31-JAN-12
    Public Function GetSubscriptionAddress(ByVal id As Integer) As String
        Dim i As Integer = 0
        While i < SubscriptionList.Count AndAlso SubscriptionList(i).ID <> id
            i += 1
        End While

        If i < SubscriptionList.Count Then
            Return SubscriptionList(i).Address.Address
        Else
            Return ""
        End If
    End Function

    Private Sub SendToSubscriptions(ByVal e As MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs)
        Dim i As Integer
        '* 07-MAR-12 V1.12 If a subscription was deleted, then ignore
        Dim SavedCount As Integer = SubscriptionList.Count
        While i < SubscriptionList.Count
            Dim TNSByte As Integer = e.TransactionNumber And 255
            '* 06-MAR-12 V1.11 Make sure there are enough values returned (4th condition)
            '* trap and ignore because subscription may change in the middle of processin
            Try
                If SubscriptionList(i).Address.Address = "4001" Then
                    Dim dbg = 0
                End If

                If Requests(TNSByte).ReadFunctionCode = SubscriptionList(i).Address.ReadFunctionCode AndAlso _
                             Requests(TNSByte).Element <= SubscriptionList(i).Address.Element AndAlso _
                            (Requests(TNSByte).Element + Requests(TNSByte).NumberOfElements) >= (SubscriptionList(i).Address.Element + SubscriptionList(i).Address.NumberOfElements) AndAlso _
                             Requests(TNSByte).BitsPerElement = SubscriptionList(i).Address.BitsPerElement Then
                    ' AndAlso (PolledAddressList(i).Address.Element - SavedRequests(TNSByte).Element + PolledAddressList(i).Address.NumberOfElements) <= SavedResponse(TNSByte).Values.Count Then
                    Dim f As New MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs(e.ErrorId, e.ErrorMessage, CUShort(TNSByte), MyObjectID)
                    f.PlcAddress = SubscriptionList(i).Address.Address

                    Dim index As Integer = 0
                    While index < SubscriptionList(i).Address.NumberOfElements
                        Dim ValueIndex As Integer
                        ValueIndex = CInt(SubscriptionList(i).Address.Element - Requests(TNSByte).Element) + index
                        If SubscriptionList(i).Address.BitsPerElement = 32 Then
                            '* INT32 values in Modbus skip every other address, so compensate
                            ValueIndex = CInt((SubscriptionList(i).Address.Element - Requests(TNSByte).Element) / 2 + index)
                        ElseIf SubscriptionList(i).Address.BitsPerElement = 1 Then
                            '* Bit level
                            ValueIndex = CInt((SubscriptionList(i).Address.Element - Requests(TNSByte).Element))
                        End If

                        If Responses(TNSByte).Values.Count > ValueIndex Then
                            If SubscriptionList(i).Address.BitNumber < 0 And Requests(TNSByte).BitNumber = SubscriptionList(i).Address.BitNumber Then
                                f.Values.Add(Responses(TNSByte).Values(ValueIndex))
                            ElseIf SubscriptionList(i).Address.BitNumber >= 0 AndAlso Requests(TNSByte).BitNumber < 0 Then
                                '* Bit designated in address
                                f.Values.Add(CStr(CBool((CInt(2 ^ SubscriptionList(i).Address.BitNumber) And CInt(Responses(TNSByte).Values(ValueIndex))) > 0)))
                            End If
                        End If
                        index += 1
                    End While

                    Try
                        If SavedCount = SubscriptionList.Count Then
                            If m_SynchronizingObject IsNot Nothing Then
                                Dim x As Object() = {Me, f}
                                m_SynchronizingObject.BeginInvoke(SubscriptionList(i).dlgCallBack, x)
                            Else
                                SubscriptionList(i).dlgCallBack(Me, f)
                            End If
                        End If
                    Catch ex As Exception
                        Dim debug = 0
                    End Try
                End If
            Catch ex As Exception
                Dim debug = 0
            End Try
            i += 1
        End While
    End Sub
#End Region

#Region "Private Methods"
    '****************************************************
    '* Wait for a response from PLC before returning
    '* Used for Synchronous communications
    '****************************************************
    Private MaxTicks As Integer = 250  '* 50 ticks per second
    Private Function WaitForResponse(ByVal ID As Integer) As Integer
        ID = ID And 255
        SyncLock (Me)
            Dim Loops As Integer = 0
            While Not Requests(ID).Responded And Not Requests(ID).ErrorReturned And Loops < MaxTicks
                System.Threading.Thread.Sleep(25)
                Loops += 1
            End While

            If Loops >= MaxTicks Then
                Return -20
            Else
                '* Only let the 1st time be a long delay
                MaxTicks = 75
                Return 0
            End If
        End SyncLock
    End Function
#End Region

#Region "Events"
    '************************************************
    '* Process data recieved from controller
    '************************************************
    Protected Sub ProcessDataReceived(ByVal PDU As MfgControl.AdvancedHMI.Drivers.Modbus.ModbusPDUFrame, ByVal e As MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs)
        '* Not enough data to make up a packet
        If e.RawData Is Nothing OrElse e.RawData.Length < 4 Then
            Exit Sub
        End If

        Dim TNSByte As Integer = e.TransactionNumber And 255

        If Requests(TNSByte) IsNot Nothing AndAlso Requests(TNSByte).Address = "4001" Then
            Dim dbg = 0
        End If


        If e.OwnerObjectID = MyObjectID Then
            Responses(TNSByte) = e

            e.PlcAddress = Requests(TNSByte).Address
            Requests(TNSByte).Responded = True

            If PDU.ExceptionCode = 0 Then
                If Not Requests(TNSByte).IsWrite Then
                    '* Extract the data
                    Dim values() As String = MfgControl.AdvancedHMI.Drivers.ModbusUtilities.ExtractData(PDU.EncapsulatedData, 0, Requests(TNSByte))
                    For i As Integer = 0 To values.Length - 1
                        e.Values.Add(values(i))
                    Next

                    '* Verify that enough elements were returned before continuing V1.11 6-MAR-12
                    If e.Values.Count < Requests(TNSByte).NumberOfElements Then
                        Exit Sub
                    End If

                    '********************************************************************
                    '* Send the information back to DataReceived evemts or subscriptions
                    '********************************************************************
                    '* Is this from a subscription?
                    'If Not Requests(TNSByte).InternallyRequested Then
                    OnDataReceived(e)
                    'Else
                    'SendToSubscriptions(e)
                    'End If
                End If
            Else
                OnComError(New MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs(PDU.ExceptionCode, MfgControl.AdvancedHMI.Drivers.ModbusUtilities.DecodeMessage(PDU.ExceptionCode)))

                Requests(TNSByte).ErrorReturned = True
            End If
        End If
    End Sub

    Protected Overridable Sub OnDataReceived(ByVal e As MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs)
        If m_SynchronizingObject IsNot Nothing Then
            Dim Parameters() As Object = {Me, e}
            m_SynchronizingObject.BeginInvoke(drsd, Parameters)
        Else
            DataReceivedSync(Me, e)
        End If
    End Sub

    Protected Overridable Sub OnComError(ByVal e As MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs)
        If m_SynchronizingObject IsNot Nothing Then
            Dim Parameters() As Object = {Me, e}
            m_SynchronizingObject.BeginInvoke(errorsd, Parameters)
        Else
            ErrorReceivedSync(Me, e)
        End If

        'e.ErrorMessage = modbusutilities.DecodeMEssage(e.ErrorId)
        SendToSubscriptions(e)
    End Sub


    '***********************************************************
    '* Used to synchronize the event back to the calling thread
    '***********************************************************
    Private drsd As New EventHandler(Of MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs)(AddressOf DataReceivedSync)
    Private Sub DataReceivedSync(ByVal sender As Object, ByVal e As EventArgs)
        RaiseEvent DataReceived(sender, DirectCast(e, MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs))
    End Sub

    Private errorsd As New EventHandler(Of MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs)(AddressOf ErrorReceivedSync)
    Private Sub ErrorReceivedSync(ByVal sender As Object, ByVal e As EventArgs)
        RaiseEvent ComError(sender, DirectCast(e, MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs))
    End Sub


    Protected Friend Sub DataLinkLayerComError(ByVal sender As Object, ByVal e As MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs)
        If e.OwnerObjectID = MyObjectID Then
            If e.TransactionNumber >= 0 Then

                '* Add the message to the value because subscriptions get the message form there
                'e.Values.Add(e.ErrorMessage)

                '* Save this for other uses
                Responses(e.TransactionNumber) = e

                '* This is kind of a patch because the response can occur too fast
                If Requests(e.TransactionNumber) Is Nothing Then
                    System.Threading.Thread.Sleep(250)
                End If

                If Requests(e.TransactionNumber) IsNot Nothing Then
                    Requests(e.TransactionNumber).ErrorReturned = True
                    Requests(e.TransactionNumber).Responded = True
                End If

                OnComError(e)

                SendToSubscriptions(e)
            End If
        End If
    End Sub
#End Region
End Class
