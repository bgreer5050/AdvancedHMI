Option Strict On
'******************************************************************************
'* Base Host Link
'*
'* Copyright 2012 Archie Jacobs
'*
'* Reference : Omron W342-E1-15 (W342-E1-15+CS-CJ-CP-NSJ+RefManual.pdf)
'* Revision February 2010
'*
'* This class must be inherited by a class that implements the
'* data link layer (e.g RS232 (Host Link))
'*
'* 29-DEC-12 Created based on HostBaseCom
'***************************************************************************************************
'Imports OmronDriver.Common
Namespace Omron
    Public MustInherit Class HostLinkBaseCom
        Inherits System.ComponentModel.Component
        Implements IComComponent
        Implements IDisposable

        Public Event DataReceived As EventHandler(Of MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs)
        Public Event ComError As EventHandler(Of MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs)

        Protected Friend TargetAddress As MfgControl.AdvancedHMI.Drivers.Omron.DeviceAddress
        Protected Friend SourceAddress As MfgControl.AdvancedHMI.Drivers.Omron.DeviceAddress

        Private TNS As MfgControl.AdvancedHMI.Drivers.Common.TransactionNumber
        Private SavedRequests(255) As MfgControl.AdvancedHMI.Drivers.Omron.OmronPlcAddress
        Private SavedResponse(255) As MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs
        'Private SavedErrorEventArgs(255) As MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs


        Protected Friend MyDLLInstance As Integer
        Protected Friend EventHandlerDLLInstance As Integer


        Private tmrPoll As MfgControl.AdvancedHMI.Drivers.Common.RestartableTimer
        Private HighestPollRateDivisor As Integer
        Private PollCounts As Integer
        Private PollRateDivisorList As New List(Of Integer)

        Private PolledAddressList As New List(Of MfgControl.AdvancedHMI.Drivers.Omron.PolledAddressInfo)
        Private NewSubscriptionsAdded As Boolean

        Private IsDisposed As Boolean '* Without this, it can dispose the DLL completely

        Private Shared ObjectIDs As Int64
        Private MyObjectID As Int64


#Region "Properties"
        <System.ComponentModel.Category("Communication Settings")> _
        Public Property TargetNetworkAddress() As Byte
            Get
                Return TargetAddress.NetworkAddress
            End Get
            Set(ByVal value As Byte)
                TargetAddress.NetworkAddress = value
            End Set
        End Property

        '************************************************************
        '* If this is false, then wait for response before returning
        '* from read and writes
        '************************************************************
        Private m_AsyncMode As Boolean
        Public Property AsyncMode() As Boolean
            Get
                Return m_AsyncMode
            End Get
            Set(ByVal value As Boolean)
                m_AsyncMode = value
            End Set
        End Property

        Private m_TreatDataAsHex As Boolean
        <System.ComponentModel.Category("Communication Settings")> _
        Public Property TreatDataAsHex() As Boolean
            Get
                Return m_TreatDataAsHex
            End Get
            Set(ByVal value As Boolean)
                m_TreatDataAsHex = value
            End Set
        End Property

        Private _PollRateOverride As Integer
        <System.ComponentModel.Category("Communication Settings")> _
        Public Property PollRateOverride() As Integer
            Get
                Return _PollRateOverride
            End Get
            Set(ByVal value As Integer)
                _PollRateOverride = value
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

                If tmrPoll IsNot Nothing Then
                    If value Then
                        '* Stop the poll timers
                        tmrPoll.Pause()
                    Else
                        '* Start the poll timers
                        tmrPoll.Start()
                    End If
                End If
            End Set
        End Property

        Private m_Tag As String
        Public Property Tag() As String
            Get
                Return m_Tag
            End Get
            Set(ByVal value As String)
                m_Tag = value
            End Set
        End Property
#End Region

#Region "Constructor"
        Protected Sub New()
            'If TNS Is Nothing Then
            TNS = New MfgControl.AdvancedHMI.Drivers.Common.TransactionNumber(0, SavedRequests.Length - 1)
            'End If

            ObjectIDs += 1
            MyObjectID = ObjectIDs
            'InstanceCount += 1
        End Sub

        'Public Sub New(ByVal container As System.ComponentModel.IContainer)
        '    MyClass.New()

        '    'Required for Windows.Forms Class Composition Designer support
        '    container.Add(Me)
        'End Sub


        Protected Overrides Sub Dispose(ByVal disposing As Boolean)
            DisableSubscriptions = True
            '* Stop all of the polling timers
            If tmrPoll IsNot Nothing Then tmrPoll.Pause()
            '* remove all subscriptions
            For i As Integer = 0 To PolledAddressList.Count - 1
                PolledAddressList(i).MarkForDeletion = True
            Next

            '* The handle linked to the DataLink Layer has to be removed, otherwise it causes a problem when a form is closed
            'If DLL.Count > 0 AndAlso DLL(MyDLLInstance) IsNot Nothing Then
            '    RemoveHandler DLL(MyDLLInstance).DataReceived, AddressOf DataLinkLayerDataReceived
            '    RemoveHandler DLL(MyDLLInstance).ComError, AddressOf DataLinkLayerComError

            '    InstanceCount -= 1
            '    If InstanceCount <= 0 Then DLL(MyDLLInstance).Dispose()
            'End If

            MyBase.Dispose(disposing)
        End Sub
#End Region

#Region "Subscription"
        '*******************************************************************
        '*******************************************************************
        Private CurrentID As Integer
        Public Function Subscribe(ByVal plcAddress As String, ByVal numberOfElements As Int16, ByVal pollRate As Integer, ByVal callback As EventHandler(Of MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs)) As Integer Implements IComComponent.Subscribe
            '* If PollRateOverride is other than 0, use that poll rate for this subscription
            If _PollRateOverride > 0 Then
                pollRate = _PollRateOverride
            End If

            '* Avoid a 0 poll rate
            If pollRate <= 0 Then
                pollRate = 500
            End If

            '***********************************************
            '* Create an Address object address information
            '***********************************************
            Dim address As New MfgControl.AdvancedHMI.Drivers.Omron.OmronPlcAddress(plcAddress)

            '***********************************************************
            '* Check if there was already a subscription made for this
            '***********************************************************
            Dim index As Integer

            While index < PolledAddressList.Count AndAlso _
                (PolledAddressList(index).Address.Address <> plcAddress Or PolledAddressList(index).dlgCallBack <> callback)
                index += 1
            End While


            '* If a subscription was already found, then returns it's ID
            If (index < PolledAddressList.Count) Then
                '* Return the subscription that already exists
                Return PolledAddressList(index).ID
            Else
                '* The ID is used as a reference for removing polled addresses
                CurrentID += 1

                Dim tmpPA As New MfgControl.AdvancedHMI.Drivers.Omron.PolledAddressInfo

                tmpPA.PollRate = pollRate

                tmpPA.PollRateDivisor = CInt(pollRate / 100)
                If tmpPA.PollRateDivisor <= 0 Then tmpPA.PollRateDivisor = 1

                '* Keep a running list of PollRate divisors for calculating to common divisor
                Dim m As Integer
                While m < PollRateDivisorList.Count AndAlso PollRateDivisorList(m) <> tmpPA.PollRateDivisor
                    m += 1
                End While
                If m >= PollRateDivisorList.Count Then PollRateDivisorList.Add(tmpPA.PollRateDivisor)

                HighestPollRateDivisor = 1
                m = 0
                For m = 0 To PollRateDivisorList.Count - 1
                    HighestPollRateDivisor *= PollRateDivisorList(m)
                Next


                tmpPA.dlgCallBack = callback
                tmpPA.ID = CurrentID
                tmpPA.Address = address
                tmpPA.Address.Tag = CurrentID
                tmpPA.Address.NumberOfElements = numberOfElements

                '* Add this subscription to the collection and sort
                PolledAddressList.Add(tmpPA)
                NewSubscriptionsAdded = True
                '* Move the sort to PollUpdate
                'PolledAddressList.Sort(AddressOf SortPolledAddresses)

                '********************************************************************
                '* Check to see if there already exists a timer for this poll rate
                '********************************************************************
                'Dim j As Integer = 0
                'While j < tmrPollList.Count AndAlso tmrPollList(j) IsNot Nothing AndAlso tmrPollList(j).Interval <> pollRate
                '    j += 1
                'End While

                If tmrPoll Is Nothing Then
                    tmrPoll = New MfgControl.AdvancedHMI.Drivers.Common.RestartableTimer(AddressOf PollUpdate, 100)

                    If m_DisableSubscriptions Then
                        tmrPoll.Pause()
                    Else
                        tmrPoll.Start()
                    End If

                    'tmrPollList.Add(tmrTemp)
                End If

                'If j >= tmrPollList.Count Then
                '    '* Add new timer
                '    Dim Interval As Integer
                '    If pollRate > 20 Then
                '        Interval = pollRate
                '    Else
                '        Interval = 250
                '    End If
                '    Dim tmrTemp As New MfgControl.AdvancedHMI.Drivers.Common.RestartableTimer(AddressOf PollUpdate, Interval)

                '    If m_DisableSubscriptions Then
                '        tmrTemp.Pause()
                '    Else
                '        tmrTemp.Start()
                '    End If

                '    tmrPollList.Add(tmrTemp)
                '    'AddHandler tmrPollList(j).Elapse, AddressOf PollUpdate

                '    'tmrTemp.Enabled = True
                'End If

                Return tmpPA.ID
            End If
        End Function

        '***************************************************************
        '* Used to sort polled addresses by File Type and element
        '* This helps in optimizing reading
        '**************************************************************
        Private Function SortPolledAddresses(ByVal A1 As MfgControl.AdvancedHMI.Drivers.Omron.PolledAddressInfo, ByVal A2 As MfgControl.AdvancedHMI.Drivers.Omron.PolledAddressInfo) As Integer
            If (A1.Address.MemoryAreaCode > A2.Address.MemoryAreaCode) Or (A1.Address.MemoryAreaCode = A2.Address.MemoryAreaCode And A1.Address.ElementNumber > A2.Address.ElementNumber) Then
                Return 1
            ElseIf A1.Address.MemoryAreaCode = A2.Address.MemoryAreaCode And A1.Address.ElementNumber = A2.Address.ElementNumber Then
                Return 0
            Else
                Return -1
            End If
        End Function

        '**************************************************************
        '* Perform the reads for the variables added for notification
        '* Attempt to optimize by grouping reads
        '**************************************************************
        Private PollUpdating As Boolean
        Private Sub PollUpdate(ByVal so As Object)
            If PollUpdating Then
                Exit Sub
            Else
                PollUpdating = True
            End If

            '* Point a timer variable to the sender object for early binding
            Dim SourceTimer As MfgControl.AdvancedHMI.Drivers.Common.RestartableTimer = DirectCast(so, MfgControl.AdvancedHMI.Drivers.Common.RestartableTimer)

            '* Stop the poll timer
            SourceTimer.Pause()

            If IsDisposed Then Exit Sub

            'Dim PollList = PolledAddressList.clone

            Dim i, j, HighestElement, ElementSpan As Integer
            Dim CurrentTNS As Integer

            '*
            PollCounts += 1
            If PollCounts > HighestPollRateDivisor Then PollCounts = 1


            '* 10-MAR-12 Check if anything is marked for removal and remove
            '* to prevent subscription reordering during poll update
            Dim k As Integer = 0
            While k < PolledAddressList.Count
                If PolledAddressList(k).MarkForDeletion Then
                    PolledAddressList.RemoveAt(k)
                Else
                    k += 1
                End If
            End While

            Dim ItemCountToUpdate As Integer = PolledAddressList.Count

            '* 10-MAR-12 Sort if anything new was added. Moved from Subscribe function
            '* to prevent subscription reordering during poll update
            If NewSubscriptionsAdded Then
                PolledAddressList.Sort(AddressOf SortPolledAddresses)
                NewSubscriptionsAdded = False
            End If


            While i < PolledAddressList.Count And i < ItemCountToUpdate
                '* Is this firing timer at the requested poll rate
                If i < ItemCountToUpdate AndAlso (PollCounts / PolledAddressList(i).PollRateDivisor) = CInt(PollCounts / PolledAddressList(i).PollRateDivisor) Then
                    Dim SavedAsync As Boolean = m_AsyncMode
                    Try
                        m_AsyncMode = True
                        PolledAddressList(i).Address.InternalRequest = True
                        'PolledAddressList(i).Address.Tag = PolledAddressList(i).ID

                        j = 0
                        HighestElement = PolledAddressList(i).Address.ElementNumber + PolledAddressList(i).Address.NumberOfElements - 1
                        ElementSpan = HighestElement - PolledAddressList(i).Address.ElementNumber
                        While (i + j + 1) < ItemCountToUpdate AndAlso _
                            PolledAddressList(i + j).Address.MemoryAreaCode = PolledAddressList(i + j + 1).Address.MemoryAreaCode AndAlso _
                            ((PolledAddressList(i + j + 1).Address.ElementNumber + PolledAddressList(i + j + 1).Address.NumberOfElements) - PolledAddressList(i).Address.ElementNumber) < 25

                            If (PolledAddressList(i + j + 1).Address.ElementNumber + PolledAddressList(i + j + 1).Address.NumberOfElements - 1) > HighestElement Then
                                HighestElement = PolledAddressList(i + j + 1).Address.ElementNumber + PolledAddressList(i + j + 1).Address.NumberOfElements - 1
                            End If

                            ElementSpan = HighestElement - PolledAddressList(i).Address.ElementNumber
                            j += 1
                        End While
                    Catch ex1 As Exception
                        Dim fw As New System.IO.StreamWriter("DriverErrorLog.log", True)
                        fw.WriteLine("1,PollUpdate-" & ex1.Message)
                        fw.Close()
                        Dim debug = 0
                    End Try

                    Try
                        Dim ReadAddress As New MfgControl.AdvancedHMI.Drivers.Omron.OmronPlcAddress(PolledAddressList(i).Address.Address, ElementSpan + 1)
                        ReadAddress.Tag = PolledAddressList(i).ID
                        ReadAddress.InternalRequest = True

                        Try
                            CurrentTNS = CInt(Read(ReadAddress))
                        Catch ex1 As Exception
                            Dim fw As New System.IO.StreamWriter("DriverErrorLog.log", True)
                            fw.WriteLine(Format(Now, "MM/dd/yyyy hh:mm:ss") & " -2,PollUpdate-" & ex1.Message)
                            fw.Close()
                        End Try

                        m_AsyncMode = SavedAsync
                    Catch ex As Exception
                        OnComError(New MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs(-1, ex.Message))

                        '* changed to handle new delegate format 31-OCT-11 ARJ
                        If i < ItemCountToUpdate Then
                            Try
                                Dim values() As String = {""}
                                Dim p As New MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs(values, "", CUShort(CurrentTNS))
                                p.Values.Add(ex.Message)
                                Dim Parameters() As Object = {Me, p}
                                m_SynchronizingObject.BeginInvoke(PolledAddressList(i).dlgCallBack, Parameters)
                            Catch ex1 As Exception
                                Dim fw As New System.IO.StreamWriter("DriverErrorLog.log", True)
                                fw.WriteLine("3,PollUpdate-" & ex.Message)
                                fw.Close()
                                Dim debug = 0
                            End Try
                        End If
                    End Try
                End If

                i += 1 + j
            End While



            Try
                PollUpdating = False
                '* Start the poll timer
                If Not m_DisableSubscriptions Then SourceTimer.Start()
            Catch ex As Exception
                Dim fw As New System.IO.StreamWriter("DriverErrorLog.log", True)
                fw.WriteLine("2,PollUpdate-" & ex.Message)
                fw.Close()
            End Try

        End Sub

        Private SubScribedObjectBeingRemoved As Boolean
        Public Function Unsubscribe(ByVal id As Integer) As Integer Implements IComComponent.Unsubscribe
            Dim i As Integer = 0
            While i < PolledAddressList.Count AndAlso PolledAddressList(i).ID <> id
                i += 1
            End While

            If i < PolledAddressList.Count Then
                Dim PollRate As Integer = PolledAddressList(i).PollRate
                SubScribedObjectBeingRemoved = True
                'PolledAddressList.RemoveAt(i)
                PolledAddressList(i).MarkForDeletion = True

                If PolledAddressList.Count = 0 Then
                    '* No more items to be polled, so remove all polling timers 28-NOV-10
                    'While tmrPollList.Count > 0
                    '    tmrPollList(0).Pause()
                    '    tmrPollList(0).Dispose()
                    '    tmrPollList.Remove(tmrPollList(0))
                    'End While
                Else
                    '* Check if no more subscriptions to this poll rate
                    'Dim j As Integer
                    'Dim StillUsed As Boolean
                    'While j < PolledAddressList.Count
                    '    If PolledAddressList(j).PollRate = PollRate Then
                    '        StillUsed = True
                    '    End If
                    '    j += 1
                    'End While

                    'If Not StillUsed Then
                    '    '* Find the timer with this poll rate
                    '    j = 0
                    '    While j < tmrPollList.Count AndAlso tmrPollList(j).Interval <> PollRate
                    '        j += 1
                    '    End While

                    '    If j < tmrPollList.Count Then
                    '        tmrPollList(j).Pause()
                    '        tmrPollList(j).Dispose()
                    '        tmrPollList.Remove(tmrPollList(j))
                    '    End If
                    'End If
                End If
            End If
        End Function

        Public Function UnsubscribeAll() As Integer
            Dim i As Integer
            While i < PolledAddressList.Count
                PolledAddressList(i).MarkForDeletion = True
            End While
        End Function

        '* 31-JAN-12
        Public Function IsSubscriptionActive(ByVal id As Integer) As Boolean
            Dim i As Integer = 0
            While i < PolledAddressList.Count AndAlso PolledAddressList(i).ID <> id
                i += 1
            End While

            Return (i < PolledAddressList.Count)
        End Function

        '* 31-JAN-12
        Public Function GetSubscriptionAddress(ByVal id As Integer) As String
            Dim i As Integer = 0
            While i < PolledAddressList.Count AndAlso PolledAddressList(i).ID <> id
                i += 1
            End While

            If i < PolledAddressList.Count Then
                Return PolledAddressList(i).Address.Address
            Else
                Return ""
            End If
        End Function
#End Region

#Region "Public Methods"
        Public Function BeginRead(ByVal startAddress As String, ByVal numberOfElements As Integer) As Integer Implements IComComponent.BeginRead
            Dim address As New MfgControl.AdvancedHMI.Drivers.Omron.OmronPlcAddress(startAddress, numberOfElements)
            Return Read(address)
        End Function

        '* Memory Area Read
        '* Reference : Section 4-3
        Public Function Read(ByVal address As MfgControl.AdvancedHMI.Drivers.Omron.OmronPlcAddress) As Integer
            If IsDisposed Then
                Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("ReadAny. Object is disposed")
            End If

            Dim CurrentTNS As Byte
            '* Save this 
            CurrentTNS = CByte(TNS.GetNextNumber(Tag, MyObjectID))

            SavedRequests(CurrentTNS) = address


            Dim b(7) As Byte

            MfgControl.AdvancedHMI.Drivers.Common.CalculationsAndConversions.ValueToBcdASCII(address.ElementNumber).CopyTo(b, 0)
            MfgControl.AdvancedHMI.Drivers.Common.CalculationsAndConversions.ValueToBcdASCII(address.NumberOfElements).CopyTo(b, 4)

            Dim HostLinkPacket As New MfgControl.AdvancedHMI.Drivers.Omron.HostLinkFrame(0, address.HostLinkReadHeaderCode, b)
            HostLinkPacket.TransactionNumber = CurrentTNS

            Try
                If SendData(HostLinkPacket, SavedRequests(CurrentTNS).InternalRequest) Then
                    '* Save this TNS to check if data received was requested by this instance
                    'ActiveTNSs.Add(CurrentTNS)
                Else
                    '* Buffer is full, so release
                    TNS.ReleaseNumber(CurrentTNS)
                    '* Throw an exception if not an internal request
                    If Not SavedRequests(CurrentTNS).InternalRequest Then
                        Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("Send Buffer is full, may have lost communication.")
                    End If
                End If
            Catch ex1 As Exception
                Dim fw As New System.IO.StreamWriter("DriverErrorLog.log", True)
                fw.WriteLine("1,ReadAny-" & ex1.Message)
                fw.Close()
                'Throw New MfgControl.AdvancedHMI.Drivers.Common.PlcDriverException("ReadAny-Driver Instance Has been disposed")
                Throw
            End Try



            If m_AsyncMode Then
                Return CurrentTNS
            Else
            End If
            'End SyncLock
        End Function

        Public Function BeginRead(ByVal startAddress As String) As Integer  'Implements IComComponent.Read 
            Return BeginRead(startAddress, 1)
        End Function

        Public Function Read(ByVal startAddress As String, ByVal numberOfElements As Integer) As String() Implements IComComponent.Read
            Dim index As Integer = BeginRead(startAddress)

            If WaitForResponse(CUShort(index), 50) = 0 Then
                If SavedResponse(index) IsNot Nothing Then
                    Dim tmp(SavedResponse(index).Values.Count - 1) As String
                    For i As Integer = 0 To tmp.Length - 1
                        tmp(i) = SavedResponse(index).Values(i)
                    Next
                    Return tmp
                Else
                    Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("No Reponse from PLC. Ensure baud rate is correct.")
                End If
            Else
                If SavedRequests(index).ErrorReturned Then
                    Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("Error Returned " & SavedResponse(index).ErrorId)
                Else
                    Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("No Reponse from PLC. Ensure baud rate is correct.")
                End If
            End If
        End Function

        '*******************************************************
        Public Function Write(ByVal startAddress As String, ByVal dataToWrite As String) As String Implements IComComponent.Write
            Dim DataAsArray() As String = {dataToWrite}
            Dim address As New MfgControl.AdvancedHMI.Drivers.Omron.OmronPlcAddress(startAddress, 1)

            WriteData(address, DataAsArray)
            Return "0"
        End Function

        Public Function WriteData(ByVal address As String, ByVal dataToWrite() As String) As String()
            Return WriteData(New MfgControl.AdvancedHMI.Drivers.Omron.OmronPlcAddress(address), dataToWrite)
        End Function


        '* Memory Area Read
        '* Reference : Section 4-3
        Public Function WriteData(ByVal address As MfgControl.AdvancedHMI.Drivers.Omron.OmronPlcAddress, ByVal dataToWrite() As String) As String()
            '* No data was sent, so exit
            If dataToWrite.Length <= 0 Then Return New String() {"0"}

            Dim CurrentTNS As Byte
            '* Save this 
            CurrentTNS = CByte(TNS.GetNextNumber(Tag, MyObjectID))

            'Dim Header As New MfgControl.AdvancedHMI.Drivers.Omron.FINSHeaderFrame(GatewayCountOption.Three, TargetAddress, SourceAddress, CByte(CurrentTNS))

            '* Save this TNS to check if data received was requested by this instance
            'ActiveTNSs.Add(CurrentTNS)

            '* Mark as a write and Save
            address.IsWrite = True
            SavedRequests(CurrentTNS) = address

            '* Attach the instruction data
            Dim dataPacket As New List(Of Byte)
            dataPacket.Add(address.MemoryAreaCode)
            dataPacket.Add(CByte((address.ElementNumber >> 8) And 255))
            dataPacket.Add(CByte((address.ElementNumber) And 255))
            dataPacket.Add(address.BitNumber)
            dataPacket.Add(CByte((address.NumberOfElements >> 8) And 255))
            dataPacket.Add(CByte((address.NumberOfElements) And 255))

            If address.BitsPerElement = 16 Then
                Dim x(1) As Byte
                For i As Integer = 0 To dataToWrite.Length - 1
                    If m_TreatDataAsHex Then
                        Dim data As Integer
                        Try
                            data = Convert.ToUInt16(dataToWrite(i), 16)
                        Catch ex As Exception
                            Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("Invalid hexadecimal value " & dataToWrite(i))
                        End Try
                        x(0) = CByte(data And 255)
                        x(1) = CByte(data >> 8)
                    Else
                        x = BitConverter.GetBytes(CUShort(dataToWrite(i)))
                        If address.IsBCD Then
                            '* Convert to BCD
                            x(1) = CByte(CUShort(Math.Floor(CDbl(dataToWrite(i)) / 100)))
                            x(0) = MfgControl.AdvancedHMI.Drivers.Common.CalculationsAndConversions.HexToByte(CStr(CUShort(dataToWrite(i)) - (x(1) * 100)))
                            x(1) = MfgControl.AdvancedHMI.Drivers.Common.CalculationsAndConversions.HexToByte(CStr(x(1)))
                        End If
                    End If
                    '* Bitconverter uses LittleEndian
                    '* Omron uses BigEndian, so reverse
                    dataPacket.Add(x(1))
                    dataPacket.Add(x(0))
                Next
            Else
                '* Bit level
                For i As Integer = 0 To dataToWrite.Length - 1
                    If CInt(dataToWrite(i)) > 0 Then
                        dataPacket.Add(1)
                    Else
                        dataPacket.Add(0)
                    End If
                Next
            End If

            Dim b(3 + dataToWrite.Length * 4) As Byte

            '* The element number is in BCD
            MfgControl.AdvancedHMI.Drivers.Common.CalculationsAndConversions.ValueToBcdASCII(address.ElementNumber).CopyTo(b, 0)


            '* The data is in HEX
            '* 10-JUN-13 Removed the * 4 at the end
            For i As Integer = 0 To (dataToWrite.Length - 1)
                Dim HexBytes() As Byte
                If m_TreatDataAsHex Then
                    '* add the leading 0's
                    While dataToWrite(i).Length < 4
                        dataToWrite(i) = "0" & dataToWrite(i)
                    End While
                    HexBytes = System.Text.Encoding.ASCII.GetBytes(dataToWrite(i))
                Else
                    HexBytes = System.Text.Encoding.ASCII.GetBytes(MfgControl.AdvancedHMI.Drivers.Common.CalculationsAndConversions.IntToHex(CInt(dataToWrite(i))))
                End If
                HexBytes.CopyTo(b, 4 + i * 4)
            Next

            Dim HostLinkPacket As New MfgControl.AdvancedHMI.Drivers.Omron.HostLinkFrame(0, address.HostLinkWriteHeaderCode, b)
            HostLinkPacket.TransactionNumber = CurrentTNS

            'Dim HostPacketStream() As Byte = HostLinkPacket.GetByteStream

            If SendData(HostLinkPacket, address.InternalRequest) Then
                Return New String() {"0"}
            Else
                Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("Send Buffer is full, may have lost communication.")
            End If
        End Function

        Public Function ReadProgram() As Byte()
            If IsDisposed Then
                Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("ReadProgram. Object is disposed")
            End If

            '***************************************************
            '* First initialize to cancel any previous commands
            '***************************************************
            InitializeComs()

            '*************************************************************
            '* READ THE PROGRAM
            '*************************************************************
            '* Save this 
            Dim CurrentTNS As UInt16 = CByte(TNS.GetNextNumber(Tag, MyObjectID))

            Dim DummyAddress As MfgControl.AdvancedHMI.Drivers.Omron.OmronPlcAddress = New MfgControl.AdvancedHMI.Drivers.Omron.OmronPlcAddress("RP")
            SavedRequests(CurrentTNS) = DummyAddress

            Dim HostLinkPacket As MfgControl.AdvancedHMI.Drivers.Omron.HostLinkFrame = New MfgControl.AdvancedHMI.Drivers.Omron.HostLinkFrame(0, DummyAddress.HostLinkReadHeaderCode, Nothing)
            HostLinkPacket.TransactionNumber = CurrentTNS

            Try
                If SendData(HostLinkPacket, SavedRequests(CurrentTNS).InternalRequest) Then
                    '* Save this TNS to check if data received was requested by this instance
                    'ActiveTNSs.Add(CurrentTNS)
                Else
                    '* Buffer is full, so release
                    TNS.ReleaseNumber(CurrentTNS)
                    '* Throw an exception if not an internal request
                    If Not SavedRequests(CurrentTNS).InternalRequest Then
                        Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("Send Buffer is full, may have lost communication.")
                    End If
                End If
            Catch ex1 As Exception
                Dim fw As New System.IO.StreamWriter("DriverErrorLog.log", True)
                fw.WriteLine("1,ReadProgram2-" & ex1.Message)
                fw.Close()
                'Throw New MfgControl.AdvancedHMI.Drivers.Common.PlcDriverException("ReadAny-Driver Instance Has been disposed")
                Throw
            End Try



            If Not m_AsyncMode Then
                Dim index As Integer = CInt(CurrentTNS)
                If WaitForResponse(CUShort(index), 10000) = 0 Then
                    If SavedResponse(index) IsNot Nothing Then
                        '* Remove the @00RP.. from the rawdata and the FCS, CR, and *
                        Dim tmp(SavedResponse(index).RawData.Length - 11) As Byte
                        For i As Integer = 0 To tmp.Length - 1
                            tmp(i) = SavedResponse(index).RawData(i + 7)
                        Next
                        Return tmp
                    Else
                        Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("No Reponse from PLC. Ensure baud rate is correct.")
                    End If
                Else
                    If SavedRequests(index).ErrorReturned Then
                        Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("Error Returned " & SavedResponse(index).ErrorId)
                    Else
                        Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("No Reponse from PLC. Ensure baud rate is correct.")
                    End If
                End If
            End If

            Dim result() As Byte = {CByte(CurrentTNS And 255)}
            Return result
        End Function


        Public Function WriteProgram(ByVal data() As Byte) As Integer
            InitializeComs()

            '*************************************************************
            '* Write THE PROGRAM
            '*************************************************************
            '* Save this 
            Dim CurrentTNS As Byte = CByte(TNS.GetNextNumber(Tag, MyObjectID))

            Dim DummyAddress As New MfgControl.AdvancedHMI.Drivers.Omron.OmronPlcAddress("WP")
            SavedRequests(CurrentTNS) = DummyAddress

            Dim HostLinkPacket As New MfgControl.AdvancedHMI.Drivers.Omron.HostLinkFrame(0, DummyAddress.HostLinkWriteHeaderCode, data)
            HostLinkPacket.TransactionNumber = CurrentTNS

            Try
                If SendData(HostLinkPacket, SavedRequests(CurrentTNS).InternalRequest) Then
                    '* Save this TNS to check if data received was requested by this instance
                    'ActiveTNSs.Add(CurrentTNS)
                Else
                    '* Buffer is full, so release
                    TNS.ReleaseNumber(CurrentTNS)
                    '* Throw an exception if not an internal request
                    If Not SavedRequests(CurrentTNS).InternalRequest Then
                        Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("Send Buffer is full, may have lost communication.")
                    End If
                End If
            Catch ex1 As Exception
                Dim fw As New System.IO.StreamWriter("DriverErrorLog.log", True)
                fw.WriteLine("1,ReadProgram2-" & ex1.Message)
                fw.Close()
                'Throw New MfgControl.AdvancedHMI.Drivers.Common.PlcDriverException("ReadAny-Driver Instance Has been disposed")
                Throw
            End Try



            If Not m_AsyncMode Then
                Dim index As Integer = CInt(CurrentTNS)
                If WaitForResponse(CUShort(index), 10000) = 0 Then
                    If SavedResponse(index) IsNot Nothing Then
                        Return 0
                    Else
                        Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("No Reponse from PLC. Ensure baud rate is correct.")
                    End If
                Else
                    If SavedRequests(index).ErrorReturned Then
                        Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("Error Returned " & SavedResponse(index).ErrorId)
                    Else
                        Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("No Reponse from PLC. Ensure baud rate is correct.")
                    End If
                End If
            End If
        End Function

        Public Sub InitializeComs()
            Dim CurrentTNS As Byte = CByte(TNS.GetNextNumber(Tag, MyObjectID))

            Dim DummyAddress As New MfgControl.AdvancedHMI.Drivers.Omron.OmronPlcAddress("**")
            SavedRequests(CurrentTNS) = DummyAddress

            Dim HostLinkPacket As New MfgControl.AdvancedHMI.Drivers.Omron.HostLinkFrame(0, DummyAddress.HostLinkReadHeaderCode, Nothing)
            HostLinkPacket.TransactionNumber = CurrentTNS

            Try
                If SendData(HostLinkPacket, SavedRequests(CurrentTNS).InternalRequest) Then
                    '* Save this TNS to check if data received was requested by this instance
                    'ActiveTNSs.Add(CurrentTNS)
                Else
                    '* Buffer is full, so release
                    TNS.ReleaseNumber(CurrentTNS)
                    '* Throw an exception if not an internal request
                    If Not SavedRequests(CurrentTNS).InternalRequest Then
                        Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("Send Buffer is full, may have lost communication.")
                    End If
                End If
            Catch ex1 As Exception
                Dim fw As New System.IO.StreamWriter("DriverErrorLog.log", True)
                fw.WriteLine("1,ReadProgram1-" & ex1.Message)
                fw.Close()
                'Throw New MfgControl.AdvancedHMI.Drivers.Common.PlcDriverException("ReadAny-Driver Instance Has been disposed")
                Throw
            End Try
        End Sub

        Public Function SetProgramMode() As Integer
            Return ChangeStatus(0)
        End Function

        Public Function SetMonitorMode() As Integer
            Return ChangeStatus(2)
        End Function

        Public Function SetRunMode() As Integer
            Return ChangeStatus(3)
        End Function

        Public Function ChangeStatus(ByVal Mode As Byte) As Integer
            '*************************************************************
            '* ChangeProcessor mode to PROGRAM
            '*************************************************************
            '* Save this 
            Dim CurrentTNS As Byte = CByte(TNS.GetNextNumber(Tag, MyObjectID))

            Dim DummyAddress As New MfgControl.AdvancedHMI.Drivers.Omron.OmronPlcAddress("SC")
            SavedRequests(CurrentTNS) = DummyAddress

            'Send a mode of "00"
            Dim ModeRequestinHex As String = MfgControl.AdvancedHMI.Drivers.Common.CalculationsAndConversions.ByteToHex(Mode)
            Dim s() As Byte = {CByte(Asc(ModeRequestinHex.Substring(0, 1))), CByte(Asc(ModeRequestinHex.Substring(1, 1)))}
            Dim HostLinkPacket As New MfgControl.AdvancedHMI.Drivers.Omron.HostLinkFrame(0, DummyAddress.HostLinkWriteHeaderCode, s)
            HostLinkPacket.TransactionNumber = CurrentTNS

            Try
                If SendData(HostLinkPacket, SavedRequests(CurrentTNS).InternalRequest) Then
                    '* Save this TNS to check if data received was requested by this instance
                    'ActiveTNSs.Add(CurrentTNS)
                Else
                    '* Buffer is full, so release
                    TNS.ReleaseNumber(CurrentTNS)
                    '* Throw an exception if not an internal request
                    If Not SavedRequests(CurrentTNS).InternalRequest Then
                        Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("Send Buffer is full, may have lost communication.")
                    End If
                End If
            Catch ex1 As Exception
                Dim fw As New System.IO.StreamWriter("DriverErrorLog.log", True)
                fw.WriteLine("1,SetProgram2-" & ex1.Message)
                fw.Close()
                'Throw New MfgControl.AdvancedHMI.Drivers.Common.PlcDriverException("ReadAny-Driver Instance Has been disposed")
                Throw
            End Try



            '****************************************
            '* This is always a Synchronous Command
            '****************************************
            Dim index As Integer = CInt(CurrentTNS)
            If WaitForResponse(CUShort(index), 10000) = 0 Then
                If SavedResponse(index) IsNot Nothing Then
                    Return 0
                Else
                    Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("No Reponse from PLC. Ensure baud rate is correct.")
                End If
            Else
                If SavedRequests(index).ErrorReturned Then
                    Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("Error Returned " & SavedResponse(index).ErrorId)
                Else
                    Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("No Reponse from PLC. Ensure baud rate is correct.")
                End If
            End If
        End Function

        '**********************************************************************
        '* This sends an undocumented MailBox command that unlocks the program
        '**********************************************************************
        Public Sub UnlockPLC(ByVal PassCode As String)
            Dim data(11) As Byte
            data = System.Text.Encoding.ASCII.GetBytes("31060000" & PassCode)
            SendMailBoxData(data)
        End Sub

        Public Sub SendMailBoxData(ByVal data() As Byte)
            '    @00MB31060000

            '* Save this 
            Dim CurrentTNS As Byte = CByte(TNS.GetNextNumber(Tag, MyObjectID))

            Dim DummyAddress As New MfgControl.AdvancedHMI.Drivers.Omron.OmronPlcAddress("MB")
            SavedRequests(CurrentTNS) = DummyAddress

            Dim HostLinkPacket As New MfgControl.AdvancedHMI.Drivers.Omron.HostLinkFrame(0, DummyAddress.HostLinkWriteHeaderCode, data)
            HostLinkPacket.TransactionNumber = CurrentTNS

            Try
                If SendData(HostLinkPacket, SavedRequests(CurrentTNS).InternalRequest) Then
                    '* Save this TNS to check if data received was requested by this instance
                    'ActiveTNSs.Add(CurrentTNS)
                Else
                    '* Buffer is full, so release
                    TNS.ReleaseNumber(CurrentTNS)
                    '* Throw an exception if not an internal request
                    If Not SavedRequests(CurrentTNS).InternalRequest Then
                        Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("Send Buffer is full, may have lost communication.")
                    End If
                End If
            Catch ex1 As Exception
                Dim fw As New System.IO.StreamWriter("DriverErrorLog.log", True)
                fw.WriteLine("1,SetProgram2-" & ex1.Message)
                fw.Close()
                'Throw New MfgControl.AdvancedHMI.Drivers.Common.PlcDriverException("ReadAny-Driver Instance Has been disposed")
                Throw
            End Try


        End Sub

        '* Write Clock (FINS 7,2)
        '* Reference : Section 5-3-20
        'Public Function WriteClock(ByVal dataToWrite() As String) As String()
        '    '* No data was sent, so exit
        '    If dataToWrite.Length <= 0 Then Return New String() {"0"}

        '    Dim CurrentTNS As Byte
        '    '* Save this 
        '    CurrentTNS = CByte(TNS.GetNextNumber(Tag))

        '    Dim Header As New MfgControl.AdvancedHMI.Drivers.Omron.FINSHeaderFrame(GatewayCountOption.Three, TargetAddress, SourceAddress, CByte(CurrentTNS))

        '    Dim address As New MfgControl.AdvancedHMI.Drivers.Omron.OmronPlcAddress

        '    '* Mark as a write and Save
        '    address.IsWrite = True
        '    address.BitsPerElement = 8
        '    address.NumberOfElements = dataToWrite.Length

        '    SavedRequests(CurrentTNS) = address

        '    '* Attach the instruction data
        '    Dim dataPacket As New List(Of Byte)

        '    Dim x(1) As Byte
        '    For i As Integer = 0 To dataToWrite.Length - 1
        '        If m_TreatDataAsHex Then
        '            Dim data As Integer
        '            Try
        '                data = Convert.ToByte(dataToWrite(i), 16)
        '            Catch ex As Exception
        '                Throw New MfgControl.AdvancedHMI.Drivers.Common.PlcDriverException("Invalid hexadecimal value " & dataToWrite(i))
        '            End Try
        '            x(0) = CByte(data And 255)
        '            x(1) = CByte(data >> 8)

        '        Else
        '            x = BitConverter.GetBytes(CUShort(dataToWrite(i)))
        '            If address.IsBCD Then
        '                '* Convert to BCD
        '                x(1) = CByte(CUShort(Math.Floor(CDbl(dataToWrite(i)) / 100)))
        '                x(0) =MfgControl.AdvancedHMI.Drivers.Common.CalculationsAndConversions.HexToByte(CStr(CUShort(dataToWrite(i)) - (x(1) * 100)))
        '                x(1) =MfgControl.AdvancedHMI.Drivers.Common.CalculationsAndConversions.HexToByte(CStr(x(1)))
        '            End If
        '        End If
        '        '* Bitconverter uses LittleEndian
        '        '* Omron uses BigEndian, so reverse
        '        'dataPacket.Add(x(1))

        '        '* TODO:
        '        '* This command only accepts BCD, so when it converts to hex by host link frame
        '        Dim tmp As Byte = CByte(Math.Floor(x(0) / 10) * 16)
        '        Dim tmp2 As Byte = CByte(x(0) - Math.Floor(x(0) / 10) * 10)
        '        x(0) = tmp + tmp2

        '        dataPacket.Add(x(0))
        '    Next


        '    '* MR=7, SR=2
        '    Dim FINSPacket As New MfgControl.AdvancedHMI.Drivers.Omron.FINSFrame(Header, 7, 2, dataPacket.ToArray)

        '    Dim FINSPacketStream() As Byte = FINSPacket.GetByteStream


        '    If SendData(FINSPacket, address.InternalRequest) Then
        '        Return New String() {"0"}
        '    Else
        '        Throw New MfgControl.AdvancedHMI.Drivers.Common.PlcDriverException("Send Buffer is full, may have lost communication.")
        '    End If
        'End Function


        '****************************************************
        '* Wait for a response from PLC before returning
        '* Used for Synchronous communications
        '****************************************************
        'Private MaxTicks As Integer = 100  '* 50 ticks per second
        Private Function WaitForResponse(ByVal ID As UInt16, ByVal maxTicks As Integer) As Integer
            SyncLock (Me)
                Dim Loops As Integer = 0
                While Not SavedRequests(ID).Responded And Not SavedRequests(ID).ErrorReturned And Loops < maxTicks
                    System.Threading.Thread.Sleep(25)
                    Loops += 1
                End While

                If Loops >= maxTicks Then
                    Return -20
                Else
                    '* Only let the 1st time be a long delay
                    maxTicks = 75
                    Return 0
                End If
            End SyncLock
        End Function
#End Region

#Region "Private Methods"
        '***************************************************************
        '* Create the Data Link Layer Instances
        '* if the IP Address is the same, then resuse a common instance
        '***************************************************************
        Protected MustOverride Sub CreateDLLInstance()

        Friend MustOverride Function SendData(ByVal HostLinkF As MfgControl.AdvancedHMI.Drivers.Omron.HostLinkFrame, ByVal InternalRequest As Boolean) As Boolean



        'Private dr As New DataReceivedEventHandler(AddressOf DataLinkLayerDataReceived)
        '************************************************
        '* Process data recieved from controller
        '************************************************
        Protected Sub DataLinkLayerDataReceived(ByVal sender As Object, ByVal e As MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs)
            '* Not enough data to make up a FINS packet
            '* 3-MAR-13 Changed from 12 to 10 for HostLinkCom
            If e.RawData Is Nothing OrElse e.RawData.Length < 10 Then
                Exit Sub
            End If


            '* Does this request belong to this driver instance?
            If Not MfgControl.AdvancedHMI.Drivers.Common.TransactionNumber.IsMyTNS(e.TransactionNumber, MyObjectID) Then
                Exit Sub
            End If

            TNS.ReleaseNumber(e.TransactionNumber)

            SavedResponse(e.TransactionNumber) = e
            Dim HostFrame As New MfgControl.AdvancedHMI.Drivers.Omron.HostLinkFrame(e.RawData)

            If HostFrame.EndCode <> 0 Then
                OnComError(New MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs(HostFrame.EndCode, "Error Returned From PLC=" & DecodeEndCode(HostFrame.EndCode)))
                Exit Sub
            End If

            '* 9-MAR-12 V1.15
            If SavedRequests(e.TransactionNumber) Is Nothing OrElse SavedResponse(e.TransactionNumber) Is Nothing _
                OrElse (e.TransactionNumber < 0 Or e.TransactionNumber > (SavedRequests.Length - 1)) Then
                Exit Sub
            End If

            e.PlcAddress = SavedRequests(e.TransactionNumber).Address

            If Not SavedRequests(e.TransactionNumber).IsWrite Then
                '* Extract the data
                Dim values() As String = ExtractData(HostFrame.EncapsulatedData, 0, SavedRequests(e.TransactionNumber))
                For i As Integer = 0 To values.Length - 1
                    e.Values.Add(values(i))
                Next

                '* Verify that enough elements were returned before continuing V1.11 6-MAR-12
                If e.Values.Count < SavedRequests(e.TransactionNumber).NumberOfElements Then
                    Exit Sub
                End If

                '* Is this from a subscription?
                If Not SavedRequests(e.TransactionNumber).InternalRequest Then
                    OnDataReceived(e)
                Else
                    SendToSubscriptions(e)
                End If
            End If

            SavedRequests(e.TransactionNumber).Responded = True
        End Sub

        Private Sub SendToSubscriptions(ByVal e As MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs)
            Dim i As Integer
            While i < PolledAddressList.Count
                '* trap and ignore because subscription may change in the middle of processing
                Try
                    '* 11-MAR-12 V1.20 If a subscription was deleted, then ignore
                    If Not PolledAddressList(i).MarkForDeletion Then
                        '* 06-MAR-12 V1.11 Make sure there are enough values returned (4th condition)
                        If SavedRequests(e.TransactionNumber).MemoryAreaCode = PolledAddressList(i).Address.MemoryAreaCode AndAlso _
                                                            SavedRequests(e.TransactionNumber).ElementNumber <= PolledAddressList(i).Address.ElementNumber AndAlso _
                                                           (SavedRequests(e.TransactionNumber).ElementNumber + SavedRequests(e.TransactionNumber).NumberOfElements) >= (PolledAddressList(i).Address.ElementNumber + PolledAddressList(i).Address.NumberOfElements) AndAlso _
                                                           (PolledAddressList(i).Address.ElementNumber - SavedRequests(e.TransactionNumber).ElementNumber + PolledAddressList(i).Address.NumberOfElements) <= SavedResponse(e.TransactionNumber).Values.Count Then
                            Dim f As New MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs(New Byte() {0}, PolledAddressList(i).Address.Address, e.TransactionNumber)
                            Dim index As Integer = 0
                            While index < PolledAddressList(i).Address.NumberOfElements
                                f.Values.Add(SavedResponse(e.TransactionNumber).Values(PolledAddressList(i).Address.ElementNumber - SavedRequests(e.TransactionNumber).ElementNumber + index))
                                index += 1
                            End While

                            Try
                                '* 11-MAR-12 V1.20
                                If Not PolledAddressList(i).MarkForDeletion Then
                                    If m_SynchronizingObject IsNot Nothing Then
                                        m_SynchronizingObject.BeginInvoke(PolledAddressList(i).dlgCallBack, New Object() {Me, f})
                                    Else
                                        PolledAddressList(i).dlgCallBack(Me, f)
                                    End If
                                End If
                            Catch ex As Exception
                                Dim fw As New System.IO.StreamWriter("DriverErrorLog.log", True)
                                fw.WriteLine("1,FinsBaseCom.DataLinkLayerDataReceived-" & ex.Message)
                                fw.Close()
                                'Dim debug = 0
                                '* V1.16 - mark so it can continue
                                SavedRequests(e.TransactionNumber).ErrorReturned = True
                            End Try
                        End If
                    End If
                Catch ex As Exception
                    Dim fw As New System.IO.StreamWriter("DriverErrorLog.log", True)
                    fw.WriteLine("2,FinsBaseCom.DataLinkLayerDataReceived-" & ex.Message)
                    fw.Close()
                    '* V1.16 - mark so it can continue
                    SavedRequests(e.TransactionNumber).ErrorReturned = True
                End Try
                i += 1
            End While
        End Sub


        Protected Friend Sub DataLinkLayerComError(ByVal sender As Object, ByVal e As MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs)
            If MfgControl.AdvancedHMI.Drivers.Common.TransactionNumber.IsMyTNS(e.TransactionNumber, MyObjectID) Then
                If e.TransactionNumber >= 0 Then
                    '* Save this for other uses
                    SavedResponse(e.TransactionNumber) = e

                    SavedRequests(e.TransactionNumber).ErrorReturned = True
                End If
            End If

            OnComError(e)

            SendToSubscriptions(e)
        End Sub


        '***************************************
        '* Extract the returned data
        '***************************************
        Private Function ExtractData(ByVal RawData As List(Of Byte), ByVal startByte As Integer, ByVal address As MfgControl.AdvancedHMI.Drivers.Omron.OmronPlcAddress) As String()
            If address.Address = "RP" Then
                'Dim index As Integer
                Dim values(RawData.Count - 11) As String
                For i As Integer = 0 To values.Length - 1
                    If Not address.IsBCD And Not m_TreatDataAsHex Then
                        values(i) = CStr(RawData(i))
                    Else
                        values(i) = MfgControl.AdvancedHMI.Drivers.Common.CalculationsAndConversions.ByteToHex(RawData(i))
                    End If
                    'tmp(i) = RawData(i + 7)
                Next
                Return values
            Else
                Dim values(address.NumberOfElements - 1) As String

                Dim NumberOfBytes As Integer = CInt(Math.Ceiling(address.BitsPerElement / 8))


                Dim i As Integer
                While i < address.NumberOfElements And (startByte + i) < Math.Floor(RawData.Count / NumberOfBytes)
                    'Dim HexByte1 As String = Chr(RawData(startByte + i * NumberOfBytes)) & Chr(RawData(startByte + (i * NumberOfBytes) + 1))
                    If NumberOfBytes > 1 Then
                        'Dim HexByte2 As String = Chr(RawData(startByte + (i * NumberOfBytes) + 2)) & Chr(RawData(startByte + (i * NumberOfBytes) + 3))
                        If Not address.IsBCD And Not m_TreatDataAsHex Then
                            values(i) = CStr(RawData(startByte + i * NumberOfBytes) * 256 + RawData(startByte + i * NumberOfBytes + 1))
                        Else
                            values(i) = MfgControl.AdvancedHMI.Drivers.Common.CalculationsAndConversions.ByteToHex(RawData(startByte + i * NumberOfBytes)) & MfgControl.AdvancedHMI.Drivers.Common.CalculationsAndConversions.ByteToHex(RawData(startByte + i * NumberOfBytes + 1))
                        End If
                    Else
                        If Not m_TreatDataAsHex Then
                            values(i) = CStr(RawData(startByte + i * NumberOfBytes + 1))
                        Else
                            values(i) = MfgControl.AdvancedHMI.Drivers.Common.CalculationsAndConversions.ByteToHex(RawData(startByte + i * NumberOfBytes))
                        End If
                    End If

                    i += 1
                End While

                Return values
            End If
        End Function

        '**********************************************
        '* Translate from End code to message
        '* Ref Section 4-2
        '***********************************************
        Private Function DecodeEndCode(ByVal endCode As UShort) As String
            Select Case endCode
                Case 0
                    Return "Normal Completion"
                Case 1
                    Return "Not executable in RUN mode"
                Case 2
                    Return "Not executable in MONITOR mode"
                Case 3
                    Return "UM write-protected"
                Case 4
                    Return "Address over"
                Case &HB
                    Return "Not executable in PROGRAM mode"
                Case &H13
                    Return "FCS error"
                Case &H14
                    Return "Format error"
                Case &H15
                    Return "Entry number data error"
                Case &H16
                    Return "Command not supported"
                Case &H18
                    Return "Frame length error"
                Case &H19
                    Return "Not executable"
                Case &H20
                    Return "Could not create I/O table"
                Case &H21
                    Return "Not executable due to CPU Unit CPU error"
                Case &H23
                    Return "User Memory Protected"
                Case &HA3
                    Return "Aborted due to FCS error in transmission data"
                Case &HA4
                    Return "Aborted due to format error in transmission data"
                Case &HA5
                    Return "Aborted due to entry number data error in transmission data"
                Case &HA6
                    Return "Aborted due to frame length error in transmission data"
                Case Else
                    Return "Unknown End Code : " & endCode
            End Select
        End Function
#End Region

#Region "Events"
        Protected Overridable Sub OnDataReceived(ByVal e As MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs)
            If m_SynchronizingObject IsNot Nothing Then
                Dim Parameters() As Object = {Me, e}
                m_SynchronizingObject.BeginInvoke(drsd, Parameters)
            Else
                RaiseEvent DataReceived(Me, e)
            End If
        End Sub

        Protected Overridable Sub OnComError(ByVal e As MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs)
            If m_SynchronizingObject IsNot Nothing Then
                m_SynchronizingObject.BeginInvoke(errorsd, New Object() {Me, e})
            Else
                RaiseEvent ComError(Me, e)
            End If
        End Sub

        '***********************************************************
        '* Used to synchronize the event back to the calling thread
        '***********************************************************
        Delegate Sub DataReceive(ByVal e As MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs)
        Private drsd As DataReceive = (AddressOf DataReceivedSync)
        Private Sub DataReceivedSync(ByVal e As MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs)
            RaiseEvent DataReceived(Me, e)
        End Sub

        Private errorsd As New EventHandler(Of MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs)(AddressOf ErrorReceivedSync)
        Private Sub ErrorReceivedSync(ByVal sender As Object, ByVal e As MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs)
            RaiseEvent ComError(Me, e)
        End Sub

#End Region
    End Class
End Namespace
