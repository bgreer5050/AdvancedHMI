'**********************************************************************************************
'* AdvancedHMI Driver
'* http://www.advancedhmi.com
'* PCCC Data Link Layer & Application Layer
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
'* 22-MAR-07  Added floating point read/write capability
'* 23-MAR-07  Added string file read/write
'*              Handle reading of up to 256 elements in one call
'* 24-MAR-07  Corrected ReadAny to allow an array of strings to be read
'* 26-MAR-07  Changed error handling to throw exceptions to comply more with .NET standards
'* 29-MAR-07  When reading multiple sub elements of timers or counters, read all the same sub-element
'* 30-MAR-07  Added GetDataMemory, GetSlotCount, and GetIoConfig  - all were reverse engineered
'* 04-APR-07  Added GetMicroDataMemory
'* 07-APR-07  Reworked the Responded variable to try to fix a small issue during a lot of rapid reads
'* 12-APR-07  Fixed a problem with reading Timers & Counters  more than 39 at a time
'* 01-MAY-07  Fixed a problem with writing multiple elements using WriteData
'* 06-JUN-07  Add the assumption of file number 2 for S file (ParseAddress) e.g. S:1=S2:1
'* 30-AUG-07  Fixed a problem where the value 16 gets doubled, it would not check the last byte
'* 13-FEB-08  Added more errors codes in DecodeMessage, Added the EXT STS if STS=&hF0
'* 13-FEB-08  Added GetML1500DataMemory to work with the MicroLogix 1500
'* 14-FEB-08  Added Reading/Writing of Long data with help from Tony Cicchino
'* 14-FEB-08  Corrected problem when writing an array of Singles to an Integer table
'* 18-FEB-08  Corrected an error in SendData that would not allow it to retry
'* 23-FEB-08  Corrected a problem when reading elements with extended addressing
'* 26-FEB-08  Reconfigured ReadRawData & WriteRawData
'* 28-FEB-08  Completed Downloading & Uploading functions
'* 11-MAR-08  Added processor code &H6F
'* 05-APR-08  Set BytesToRead equal to SerialPort.Read in case less is read
'* 01-SEP-08  Separated Data Link Layer into its own class to prepare for HMI components
'* 23-SEP-08  Added AddVariableNotification to work with visual components (Gauge, Meter, etc)
'* 26-SEP-08  Bug Fix-Clear ReceivedDataPacketBuilder in the event of ACK, NAK, or ENQ
'* 28-SEP-08  Added AUTO baud rate option to auto detect on start up
'* 28-SEP-08  Bug Fix-Auto detect needed to also do an echo test because ENQ alone doesn't do checksum
'* 11-OCT-08  Bug fix- Various problems in ExtractData
'* 11-OCT-08  Modified Write(..., as string) to convert to data type according to address
'* 22-OCT-08  Fixed problem with variable notification of timers other than element 0
'* 24-OCT-08  Fixed problem with reading multiple timers using ReadAny
'* 27-OCT-08  Fixed a problem with InternalRequest being improperly triggered in received data
'*              This was cause when a routine went to PrefixAndSend instead of ReadRawData
'* 01-NOV-08  Renamed AddVariableNotification to Subscribe
'*              A subscription can now request more than one consectutive value
'* 01-DEC-09  Fixed problem with addressing IO at bit levels above 15
'* 27-APR-10  Decreased DataWithNodes array size by 1, extra byte with ML1400 didn't work
'* 27-APR-10  Changes to allow reading multiple nodes on a network
'* 08-MAY-12  Adapted to work with PLC5
'* 23-MAY-12  Renamed PolledAddress* varibles to Subscription* for clarity
'* 28-MAY-12  Added IsPLC5 to check for all processors listed in DF1 Manual page 10-22
'* 23-SEP-12  Restructured Subscription Structure and changed to class
'* 28-SEP-12  When reading IO, EnoughElements was checkingfor BitNumber=99
'*******************************************************************************************************

'<Assembly: system.Security.Permissions.SecurityPermissionAttribute(system.Security.Permissions.SecurityAction.RequestMinimum)> 
'<Assembly: CLSCompliant(True)> 
Public MustInherit Class AllenBradleyPCCC
    Inherits System.ComponentModel.Component
    Implements AdvancedHMIDrivers.IComComponent

    Friend DisableEvent As Boolean
    'Protected TNS1 As New MfgControl.AdvancedHMI.Drivers.Common.TransactionNumber
    Private Shared ObjectIDs As Int64
    Protected MyObjectID As Int64

    Public DataPackets(255) As List(Of Byte)
    Private Responses(255) As MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs


    'Public Delegate Sub PLCCommEventHandler(ByVal sender As Object, ByVal e As MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs)

    Public Event DataReceived As EventHandler
    Public Event UnsolictedMessageRcvd As EventHandler
    Public Event DownloadProgress As EventHandler
    Public Event UploadProgress As EventHandler
    Public Event ConnectionEstablished As EventHandler

    'Friend Responded(255) As Boolean


    '* keep the original address by ref of low TNS byte so it can be returned to a linked polling address
    Friend PLCAddressByTNS(255) As MfgControl.AdvancedHMI.Drivers.PCCCAddress
    Private SubscriptionList As New List(Of PCCCSubscription)
    Private SubscriptionPollTimer As System.Timers.Timer

    Public Sub New(ByVal container As System.ComponentModel.IContainer)
        MyClass.New()

        'ObjectIDs += 1
        'MyObjectID = ObjectIDs

        'Required for Windows.Forms Class Composition Designer support
        container.Add(Me)

        'CreateDataLinkLayer()
    End Sub

    Public Sub New()
        MyBase.New()

        ObjectIDs += 1
        MyObjectID = ObjectIDs

        SubscriptionPollTimer = New System.Timers.Timer
        SubscriptionPollTimer.Enabled = False
        AddHandler SubscriptionPollTimer.Elapsed, AddressOf PollUpdate
        SubscriptionPollTimer.Interval = 500
    End Sub

    Friend MustOverride Sub CreateDLLInstance()


    ''Component overrides dispose to clean up the component list.
    'Protected Overridable Overloads Sub Dispose(ByVal disposing As Boolean)
    '    '* The handle linked to the DataLink Layer has to be removed, otherwise it causes a problem when a form is closed
    '    If DLL IsNot Nothing Then RemoveHandler DLL(MyDLLInstance).DataReceived, Dr

    '    If disposing AndAlso components IsNot Nothing Then
    '        components.Dispose()
    '    End If
    'End Sub



#Region "Properties"
    Protected m_MyNode As Byte
    Public Property MyNode() As Integer
        Get
            Return m_MyNode
        End Get
        Set(ByVal value As Integer)
            m_MyNode = CByte(value And 255)
        End Set
    End Property

    Protected m_TargetNode As Byte
    Public Property TargetNode() As Integer
        Get
            Return m_TargetNode
        End Get
        Set(ByVal value As Integer)
            m_TargetNode = CByte(value And 255)
        End Set
    End Property

    Private m_ProcessorType As Integer
    Private ReadOnly Property ProcessorType As Integer
        Get
            Return GetProcessorType()
        End Get
    End Property

    '*************************************************************************************************
    '* If set to other than 0, this will be the poll rate irrelevant of the value passed to subscribe
    '*************************************************************************************************
    Private m_PollRateOverride As Integer
    <System.ComponentModel.Category("Communication Settings")> _
    Public Property PollRateOverride() As Integer
        Get
            Return m_PollRateOverride
        End Get
        Set(ByVal value As Integer)
            If value > 0 Then
                m_PollRateOverride = value
                SubscriptionPollTimer.Interval = value
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

            If value Then
                SubscriptionPollTimer.Enabled = False
            Else
                '* Start the poll timer
                If SubscriptionList IsNot Nothing AndAlso SubscriptionList.Count > 0 Then
                    SubscriptionPollTimer.Enabled = True
                End If
            End If
        End Set
    End Property

    '**************************************************
    '* Its purpose is to fetch
    '* the main form in order to synchronize the
    '* notification thread/event
    '**************************************************
    'Private m_SynchronizingObject As System.ComponentModel.ISynchronizeInvoke
    '* do not let this property show up in the property window
    ' <System.ComponentModel.Browsable(False)> _
    Public MustOverride Property SynchronizingObject() As System.ComponentModel.ISynchronizeInvoke
#End Region

#Region "Public Methods"
    Public Function Subscribe(ByVal PLCAddress As String, ByVal numberOfElements As Int16, ByVal PollRate As Integer, ByVal CallBack As EventHandler(Of MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs)) As Integer Implements IComComponent.Subscribe
        Dim ParsedResult As New PCCCSubscription(PLCAddress, 1, ProcessorType)
        '* This may help launch the form faster. Needs to be testes 23-AUG-13
        'Dim ParsedResult As New PCCCSubscription(PLCAddress, 1, 0)

        ParsedResult.PollRate = PollRate
        ParsedResult.dlgCallBack = CallBack
        ParsedResult.TargetNode = m_TargetNode

        SubscriptionList.Add(ParsedResult)

        SubscriptionList.Sort(AddressOf SortPolledAddresses)

        SubscriptionPollTimer.Enabled = True
        '
        Return ParsedResult.ID
    End Function

    '***************************************************************
    '* Used to sort polled addresses by File Number and element
    '* This helps in optimizing reading
    '**************************************************************
    Private Function SortPolledAddresses(ByVal A1 As PCCCSubscription, ByVal A2 As PCCCSubscription) As Integer
        If A1.FileNumber = A2.FileNumber Then
            If A1.Element > A2.Element Then
                Return 1
            ElseIf A1.Element = A2.Element Then
                Return 0
            Else
                Return -1
            End If
        End If

        If A1.FileNumber > A2.FileNumber Then
            Return 1
        Else
            Return -1
        End If
    End Function

    Public Function UnSubscribe(ByVal ID As Integer) As Integer Implements IComComponent.Unsubscribe
        Dim i As Integer = 0
        While i < SubscriptionList.Count AndAlso SubscriptionList(i).ID <> ID
            i += 1
        End While

        If i < SubscriptionList.Count Then
            SubscriptionList.RemoveAt(i)
            '* if no more items to be polled, so remove all polling timers 28-NOV-10
            If SubscriptionList.Count <= 0 Then
                SubscriptionPollTimer.Enabled = False
            End If
        End If
    End Function

    '**************************************************************
    '* Perform the reads for the variables added for notification
    '* Attempt to optimize by grouping reads
    '**************************************************************
    Friend InternalRequest As Boolean '* This is used to dinstinquish when to send data back to notification request
    Private SavedPollRate As Double
    Private HandleCreated As Boolean
    Private Sub PollUpdate(ByVal sender As System.Object, ByVal e As System.Timers.ElapsedEventArgs)
        If m_DisableSubscriptions Then
            Exit Sub
        End If

        '* 7-OCT-12 - Seemed like the timer would fire before the subscription was added
        If SubscriptionList Is Nothing OrElse SubscriptionList.Count <= 0 Then Exit Sub

        '* 3-JUN-13 Do not read data until handles are created to avoid exceptions
        If Not HandleCreated Then
            If SynchronizingObject IsNot Nothing AndAlso TypeOf (SynchronizingObject) Is System.Windows.Forms.Control Then
                If Not DirectCast(SynchronizingObject, System.Windows.Forms.Control).IsHandleCreated Then
                    Exit Sub
                Else
                    HandleCreated = True
                End If
            End If
        End If

        '* Stop the poll timer
        SubscriptionPollTimer.Enabled = False

        Dim i, NumberToRead, FirstElement As Integer
        Dim HighestBit As Integer = SubscriptionList(i).BitNumber
        While i < SubscriptionList.Count
            Dim NumberToReadCalc As Integer
            NumberToRead = SubscriptionList(i).NumberOfElements
            FirstElement = i
            Dim PLCAddress As String = SubscriptionList(FirstElement).PLCAddress

            '* Group into the same read if there is less than a 20 element gap
            '* Do not group IO addresses because they can exceed 16 bits which causes problems
            Dim ElementSpan As Integer = 20
            'If SubscriptionList(i).FileType <> &H8B And SubscriptionList(i).FileType <> &H8C Then
            '* ARJ 2-NOV-11 Changed but not tested to fix a limit of 59 float subscriptions
            'While i < SubscriptionList.Count - 1 And NumberToReadCalc < 59 AndAlso (SubscriptionList(i).FileNumber = SubscriptionList(i + 1).FileNumber And _
            While i < SubscriptionList.Count - 1 AndAlso (SubscriptionList(i).FileNumber = SubscriptionList(i + 1).FileNumber And _
                            ((SubscriptionList(i + 1).Element - SubscriptionList(i).Element < 20 And SubscriptionList(i).FileType <> &H8B And SubscriptionList(i).FileType <> &H8C) Or _
                                SubscriptionList(i + 1).Element = SubscriptionList(i).Element))
                NumberToReadCalc = SubscriptionList(i + 1).Element - SubscriptionList(FirstElement).Element + SubscriptionList(i + 1).NumberOfElements
                If NumberToReadCalc > NumberToRead Then NumberToRead = NumberToReadCalc

                '* This is used for IO addresses wher the bit can be above 15
                If SubscriptionList(i).BitNumber < 99 And SubscriptionList(i).BitNumber > HighestBit Then HighestBit = SubscriptionList(i).BitNumber

                i += 1
            End While

            '*****************************************************
            '* IO addresses can exceed bit 15 on the same element
            '*****************************************************
            If SubscriptionList(FirstElement).FileType = &H8B Or SubscriptionList(FirstElement).FileType = &H8C Then
                If SubscriptionList(FirstElement).FileType = SubscriptionList(i).FileType Then
                    If HighestBit > 15 And HighestBit < 99 Then
                        If ((HighestBit >> 4) + 1) > NumberToRead Then
                            NumberToRead = (HighestBit >> 4) + 1
                        End If
                    End If
                End If
            End If


            '* Get file type designation.
            '* Is it more than one character (e.g. "ST")
            If SubscriptionList(FirstElement).PLCAddress.Substring(1, 1) >= "A" And SubscriptionList(FirstElement).PLCAddress.Substring(1, 1) <= "Z" Then
                PLCAddress = SubscriptionList(FirstElement).PLCAddress.Substring(0, 2) & SubscriptionList(FirstElement).FileNumber & ":" & SubscriptionList(FirstElement).Element
            Else
                PLCAddress = SubscriptionList(FirstElement).PLCAddress.Substring(0, 1) & SubscriptionList(FirstElement).FileNumber & ":" & SubscriptionList(FirstElement).Element
            End If



            '*******************************************
            '* Read 3 bytes for each timer and counter
            '*******************************************
            'If SubscriptionList(FirstElement).FileType = 134 Then
            '    'PLCAddress = "T" & SubscriptionList(FirstElement).FileNumber & ":" & SubscriptionList(FirstElement).ElementNumber
            '    NumberToRead *= 3
            'End If

            'If SubscriptionList(FirstElement).FileType = 135 Then
            '    'PLCAddress = "C" & SubscriptionList(FirstElement).FileNumber & ":" & SubscriptionList(FirstElement).ElementNumber
            '    NumberToRead *= 3
            'End If

            'If SubscriptionList(FirstElement).FileType = 136 Then
            '    'PLCAddress = "R" & SubscriptionList(FirstElement).FileNumber & ":" & SubscriptionList(FirstElement).ElementNumber
            '    NumberToRead *= 3
            'End If





            'If SubscriptionList(i).PollRate = sender.Interval Or SavedPollRate > 0 Then
            If SavedPollRate >= 0 Then
                '* Make sure it does not wait for return value befoe coming back
                'Dim tmp As Boolean = Me.AsyncMode
                'Me.AsyncMode = True
                Try
                    InternalRequest = True
                    Read(PLCAddress, NumberToRead)
                    '* PLC 5/40 doesn't seem to like many transmits back to back
                    If MfgControl.AdvancedHMI.Drivers.PCCCAddress.IsPLC5(ProcessorType) Then System.Threading.Thread.Sleep(50)
                    'Me.ReadAny(SubscriptionList(FirstElement).PLCAddress, 1)
                    If SavedPollRate <> 0 Then
                        SubscriptionPollTimer.Interval = SavedPollRate
                        SavedPollRate = 0
                    End If
                Catch ex As Exception
                    '* Send this message back to the requesting control
                    'Dim TempArray() As String = {ex.Message}
                    Dim x As New MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs(-1, ex.Message)
                    '* this is necessary to keep compatibilty, otherwise it throws an exception
                    x.Values.Add(ex.Message)
                    Dim z() As Object = {Me, x}

                    If SubscriptionList IsNot Nothing AndAlso SubscriptionList.Count >= i + 1 Then
                        SynchronizingObject.BeginInvoke(SubscriptionList(i).dlgCallBack, z)
                        '* Slow down the poll rate to avoid app freezing
                        If SavedPollRate = 0 Then SavedPollRate = SubscriptionPollTimer.Interval
                        SubscriptionPollTimer.Interval = 15000
                        SubscriptionPollTimer.Enabled = True
                        'Me.AsyncMode = tmp
                        Exit Sub
                    End If
                End Try
                'Me.AsyncMode = tmp
            End If
            i += 1
        End While


        '* Re-Start the poll timer
        SubscriptionPollTimer.Enabled = True
    End Sub


    ''' <summary>
    ''' Retreives the processor code by using the get status command
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function GetProcessorType() As Integer
        If m_ProcessorType <> 0 Then
            Return m_ProcessorType
        Else
            '* Get the processor type by using a get status command
            'Dim rnd As New Random
            Dim TNS As Integer = GetNextTNSNumber(255, MyObjectID) '= CInt(rnd.Next * 254)
            Dim TNSLowerByte As Integer = TNS And 255
            PLCAddressByTNS(TNSLowerByte) = New MfgControl.AdvancedHMI.Drivers.PCCCAddress
            PLCAddressByTNS(TNSLowerByte).ProcessorType = m_ProcessorType
            ' PLCAddressByTNS(TNSLowerByte).Responded = False
            Dim Data(-1) As Byte
            Dim x = PLCAddressByTNS
            Dim Result As Integer = PrefixAndSend(6, 3, Data, True, TNS)
            If Result = 0 Then
                '* Returned data psoition 11 is the first character in the ASCII name of the processor
                '* Position 9 is the code for the processor
                '* &H15 = PLC 5/40
                '* &H18 = SLC 5/01
                '* &H1A = Fixed SLC500
                '* &H21 = PLC 5/60
                '* &H22 = PLC 5/10
                '* &H23 = PLC 5/60
                '* &H25 = SLC 5/02
                '* &H28 = PLC 5/40
                '* &H29 = PLC 5/60
                '* &H31 = PLC 5/11
                '* &H33 = PLC 5/30
                '* &H49 = SLC 5/03
                '* &H58 = ML1000
                '* &H5B = SLC 5/04
                '* &H6F = SLC 5/04 (L541)
                '* &H78 = SLC 5/05
                '* &H83 = PLC 5/30E
                '* &H86 = PLC 5/80E
                '* &H88 = ML1200
                '* &H89 = ML1500 LSP
                '* &H8C = ML1500 LRP
                '* &H95 = CompactLogix L35E
                '* &H9C = ML1100
                '* &H4A = L20E
                '* &H4B = L40E
                'Dim x = DataPackets(TNSLowerByte)
                '* SLC 500
                If DataPackets(TNSLowerByte)(7) = &HEE Then
                    m_ProcessorType = DataPackets(TNSLowerByte)(9)
                ElseIf DataPackets(TNSLowerByte)(7) = &HFE Or DataPackets(TNSLowerByte)(7) = &HEB Then
                    m_ProcessorType = DataPackets(TNSLowerByte)(8)
                Else
                    m_ProcessorType = DataPackets(TNSLowerByte)(13)
                End If
                Return ProcessorType
            Else
                Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("Failed to get processor type")
            End If
        End If
    End Function

#Region "PCCCCommands"
    '***************************************
    '* COMMAND IMPLEMENTATION SECTION
    '***************************************
    Public Sub SetRunMode()
        '* Get the processor type by using a get status command
        Dim reply As Integer
        Dim data(0) As Byte
        Dim Func As Byte

        If GetProcessorType() = &H58 Then  '* ML1000
            data(0) = 2
            Func = &H3A
        Else
            Func = &H80
            data(0) = 6
        End If

        Dim TNS As Integer = GetNextTNSNumber(255, MyObjectID)
        PLCAddressByTNS(TNS And 255) = New MfgControl.AdvancedHMI.Drivers.PCCCAddress
        reply = PrefixAndSend(&HF, Func, data, True, TNS)

        If reply <> 0 Then Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("Failed to change to Run mode, Check PLC Key switch - " & DecodeMessage(reply))
    End Sub

    Public Sub SetProgramMode()
        '* Get the processor type by using a get status command
        Dim reply As Integer
        Dim data(0) As Byte
        Dim Func As Byte

        If GetProcessorType() = &H58 Then '* ML1000
            data(0) = 0
            Func = &H3A
        Else
            data(0) = 1
            Func = &H80
        End If

        Dim TNS As Integer = GetNextTNSNumber(255, MyObjectID)
        PLCAddressByTNS(TNS And 255) = New MfgControl.AdvancedHMI.Drivers.PCCCAddress
        reply = PrefixAndSend(&HF, Func, data, True, TNS)
        'reply = PrefixAndSend(&HF, Func, data, True, TNS1.GetNextNumber("SPM"))

        If reply <> 0 Then Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("Failed to change to Program mode, Check PLC Key switch - " & DecodeMessage(reply))
    End Sub

    Public Sub ClearFault()
        Dim reply As Integer
        Dim data() As Byte = {&H2, &H2, &H84, &H5, &H0, &HFF, &HFC, &H0, &H0}

        Dim TNS As Integer = GetNextTNSNumber(255, MyObjectID)
        PLCAddressByTNS(TNS And 255) = New MfgControl.AdvancedHMI.Drivers.PCCCAddress
        reply = PrefixAndSend(&HF, &HAB, data, True, TNS)
        'reply = PrefixAndSend(&HF, &HAB, data, True, TNS1.GetNextNumber("CF"))

        If reply <> 0 Then Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("Failed to clear Fault - " & DecodeMessage(reply))
    End Sub

    'Public Sub DisableForces(ByVal targetNode As Integer)
    '    Dim rTNS As Integer
    '    Dim data() As Byte = {}
    '    Dim reply As Integer = PrefixAndSend(TargetNode, &HF, &H41, data, True, rTNS)
    'End Sub


    '* 28-MAY-12 - Added
    '* TODO : Check ML1400 Series A (&H4A?)
    'Public Function IsPLC5() As Boolean
    '    Dim PrType As Integer = GetProcessorType()
    '    IsPLC5 = (PrType = &H15) Or (PrType = &H22) Or (PrType = &H23) Or (PrType = &H28) Or (PrType = &H29) _
    '        Or (PrType = &H31) Or (PrType = &H32) Or (PrType = &H33) Or (PrType = &H4A) _
    '        Or (PrType = &H4B) Or (PrType = &H55) Or (PrType = &H59)
    'End Function

    Public Structure DataFileDetails
        Dim FileType As String
        Dim FileNumber As Integer
        Dim NumberOfElements As Integer
    End Structure


    '*******************************************************************
    '* This is the start of reverse engineering to retreive data tables
    '*   Read 12 bytes File #0, Type 1, start at Element 21
    '*    Then extract the number of data and program files
    '*******************************************************************
    ''' <summary>
    ''' Retreives the list of data tables and number of elements in each
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function GetDataMemory() As DataFileDetails()
        '**************************************************
        '* Read the File 0 (Program & data file directory
        '**************************************************
        Dim FileZeroData() As Byte = ReadFileDirectory()


        Dim NumberOfDataTables As Integer = FileZeroData(52) + FileZeroData(53) * 256
        Dim NumberOfProgramFiles As Integer = FileZeroData(46) + FileZeroData(47) * 256
        'Dim DataFiles(NumberOfDataTables - 1) As DataFileDetails
        Dim DataFiles As New System.Collections.ObjectModel.Collection(Of DataFileDetails)
        Dim FilePosition As Integer
        Dim BytesPerRow As Integer
        '*****************************************
        '* Process the data from the data table
        '*****************************************
        Select Case ProcessorType
            Case &H25, &H58 '*ML1000, SLC 5/02
                FilePosition = 93
                BytesPerRow = 8
            Case &H88 To &H9C   '* ML1100, ML1200, ML1500
                FilePosition = 103
                BytesPerRow = 10
            Case &H9F
                FilePosition = &H71
                BytesPerRow = 10
            Case Else               '* SLC 5/04, 5/05
                FilePosition = 79
                BytesPerRow = 10
        End Select


        '* Comb through data file 0 looking for data table definitions
        Dim i, k, BytesPerElement As Integer
        i = 0

        Dim DataFile As New DataFileDetails
        While k < NumberOfDataTables And FilePosition < FileZeroData.Length
            Select Case FileZeroData(FilePosition)
                Case &H82, &H8B : DataFile.FileType = "O"
                    BytesPerElement = 2
                Case &H83, &H8C : DataFile.FileType = "I"
                    BytesPerElement = 2
                Case &H84 : DataFile.FileType = "S"
                    BytesPerElement = 2
                Case &H85 : DataFile.FileType = "B"
                    BytesPerElement = 2
                Case &H86 : DataFile.FileType = "T"
                    BytesPerElement = 6
                Case &H87 : DataFile.FileType = "C"
                    BytesPerElement = 6
                Case &H88 : DataFile.FileType = "R"
                    BytesPerElement = 6
                Case &H89 : DataFile.FileType = "N"
                    BytesPerElement = 2
                Case &H8A : DataFile.FileType = "F"
                    BytesPerElement = 4
                Case &H8D : DataFile.FileType = "ST"
                    BytesPerElement = 84
                Case &H8E : DataFile.FileType = "A"
                    BytesPerElement = 2
                Case &H91 : DataFile.FileType = "L"   'Long Integer
                    BytesPerElement = 4
                Case &H92 : DataFile.FileType = "MG"   'Message Command 146
                    BytesPerElement = 50
                Case &H93 : DataFile.FileType = "PD"   'PID
                    BytesPerElement = 46
                Case &H94 : DataFile.FileType = "PLS"   'Programmable Limit Swith
                    BytesPerElement = 12

                Case Else : DataFile.FileType = "Undefined" '* 61h=Program File
                    BytesPerElement = 2
            End Select
            DataFile.NumberOfElements = CInt((FileZeroData(FilePosition + 1) + FileZeroData(FilePosition + 2) * 256) / BytesPerElement)
            DataFile.FileNumber = i

            '* Only return valid user data files
            If FileZeroData(FilePosition) > &H81 And FileZeroData(FilePosition) < &H9F Then
                DataFiles.Add(DataFile)
                'DataFile = New DataFileDetails
                k += 1
            End If

            '* Index file number once in the region of data files
            If k > 0 Then i += 1
            FilePosition += BytesPerRow
        End While

        '* Move to an array with a length of only good data files
        'Dim GoodDataFiles(k - 1) As DataFileDetails
        Dim GoodDataFiles(DataFiles.Count - 1) As DataFileDetails
        'For l As Integer = 0 To k - 1
        '    GoodDataFiles(l) = DataFiles(l)
        'Next

        DataFiles.CopyTo(GoodDataFiles, 0)

        Return GoodDataFiles
    End Function



    '*******************************************************************
    '*   Read the data file directory, File 0, Type 2
    '*    Then extract the number of data and program files
    '*******************************************************************
    'Private Function GetML1500DataMemory() As DataFileDetails()
    '    Dim reply As Integer
    '    Dim PAddress As New MfgControl.AdvancedHMI.Drivers.PCCCAddress("D0:0", 1, ProcessorType)

    '    '* Get the length of File 0, Type 2. This is the program/data file directory
    '    'PAddress.FileNumber = 0
    '    'PAddress.FileType = 2
    '    'PAddress.Element = &H2F
    '    Dim data() As Byte = ReadRawData(PAddress).Values.ToArray


    '    If reply = 0 Then
    '        Dim FileZeroSize As Integer = data(0) + (data(1)) * 256

    '        'PAddress.Element = 0
    '        'PAddress.SubElement = 0
    '        '* Read all of File 0, Type 2
    '        PAddress.NumberOfElements = FileZeroSize / 2
    '        Dim FileZeroData() As Byte = ReadRawData(PAddress, reply)

    '        '* Start Reading the data table configuration
    '        Dim DataFiles(256) As DataFileDetails

    '        Dim FilePosition As Integer
    '        Dim i As Integer


    '        '* Process the data from the data table
    '        If reply = 0 Then
    '            '* Comb through data file 0 looking for data table definitions
    '            Dim k, BytesPerElement As Integer
    '            i = 0
    '            FilePosition = 143
    '            While FilePosition < FileZeroData.Length
    '                Select Case FileZeroData(FilePosition)
    '                    Case &H89 : DataFiles(k).FileType = "N"
    '                        BytesPerElement = 2
    '                    Case &H85 : DataFiles(k).FileType = "B"
    '                        BytesPerElement = 2
    '                    Case &H86 : DataFiles(k).FileType = "T"
    '                        BytesPerElement = 6
    '                    Case &H87 : DataFiles(k).FileType = "C"
    '                        BytesPerElement = 6
    '                    Case &H84 : DataFiles(k).FileType = "S"
    '                        BytesPerElement = 2
    '                    Case &H8A : DataFiles(k).FileType = "F"
    '                        BytesPerElement = 4
    '                    Case &H8D : DataFiles(k).FileType = "ST"
    '                        BytesPerElement = 84
    '                    Case &H8E : DataFiles(k).FileType = "A"
    '                        BytesPerElement = 2
    '                    Case &H88 : DataFiles(k).FileType = "R"
    '                        BytesPerElement = 6
    '                    Case &H82, &H8B : DataFiles(k).FileType = "O"
    '                        BytesPerElement = 2
    '                    Case &H83, &H8C : DataFiles(k).FileType = "I"
    '                        BytesPerElement = 2
    '                    Case &H91 : DataFiles(k).FileType = "L"   'Long Integer
    '                        BytesPerElement = 4
    '                    Case &H92 : DataFiles(k).FileType = "MG"   'Message Command 146
    '                        BytesPerElement = 50
    '                    Case &H93 : DataFiles(k).FileType = "PD"   'PID
    '                        BytesPerElement = 46
    '                    Case &H94 : DataFiles(k).FileType = "PLS"   'Programmable Limit Swith
    '                        BytesPerElement = 12

    '                    Case Else : DataFiles(k).FileType = "Undefined"  '* 61h=Program File
    '                        BytesPerElement = 2
    '                End Select
    '                DataFiles(k).NumberOfElements = (FileZeroData(FilePosition + 1) + FileZeroData(FilePosition + 2) * 256) / BytesPerElement
    '                DataFiles(k).FileNumber = i

    '                '* Only return valid user data files
    '                If FileZeroData(FilePosition) > &H81 And FileZeroData(FilePosition) < &H95 Then k += 1

    '                '* Index file number once in the region of data files
    '                If k > 0 Then i += 1
    '                FilePosition += 10
    '            End While

    '            '* Move to an array with a length of only good data files
    '            Dim GoodDataFiles(k - 1) As DataFileDetails
    '            For l As Integer = 0 To k - 1
    '                GoodDataFiles(l) = DataFiles(l)
    '            Next

    '            Return GoodDataFiles
    '        Else
    '            Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException(DecodeMessage(reply) & " - Failed to get data table list")
    '        End If
    '    Else
    '        Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException(DecodeMessage(reply) & " - Failed to get data table list")
    '    End If
    'End Function

    Private Function ReadFileDirectory() As Byte()
        GetProcessorType()

        '*****************************************************
        '* 1 & 2) Get the size of the File Directory
        '*****************************************************
        Dim PAddress As New MfgControl.AdvancedHMI.Drivers.PCCCAddress
        PAddress.ProcessorType = ProcessorType
        Select Case ProcessorType
            Case &H25, &H58  '* SLC 5/02 or ML1000
                'PAddress.FileType = 0
                'PAddress.Element = &H23
                PAddress.SetSpecial(0, &H23)
            Case &H88 To &H9C  '* ML1100, ML1200, ML1500
                'PAddress.FileType = 2
                'PAddress.Element = &H2F
                PAddress.SetSpecial(2, &H2F)
            Case &H9F           '*ML1400
                'PAddress.FileType = 3
                'PAddress.Element = &H34
                PAddress.SetSpecial(3, &H34)
            Case Else           '* SLC 5/04, SLC 5/05
                'PAddress.FileType = 1
                'PAddress.Element = &H23
                PAddress.SetSpecial(1, &H23)
        End Select

        Dim reply As Integer

        Dim TNS As Integer = ReadRawData(PAddress)
        Dim data() As Byte = Responses(TNS).RawData

        If reply <> 0 Then Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("Failed to Get Program Directory Size- " & DecodeMessage(reply))


        '*****************************************************
        '* 3) Read All of File 0 (File Directory)
        '*****************************************************
        PAddress.Element = 0
        Dim FileZeroSize As Integer = data(0) + data(1) * 256
        '* Deafult of 2 bytes per element in file 0
        PAddress.NumberOfElements = CInt(FileZeroSize / 2)
        TNS = ReadRawData(PAddress)
        Dim FileZeroData() As Byte = Responses(TNS And 255).RawData

        If reply <> 0 Then Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("Failed to Get Program Directory - " & DecodeMessage(reply))

        Return FileZeroData
    End Function
    '********************************************************************
    '* Retreive the ladder files
    '* This was developed from a combination of Chapter 12
    '*  and reverse engineering
    '********************************************************************
    Public Structure PLCFileDetails
        Dim FileType As Integer
        Dim FileNumber As Integer
        Dim NumberOfBytes As Integer
        Dim data() As Byte
    End Structure
    Public Function UploadProgramData() As System.Collections.ObjectModel.Collection(Of PLCFileDetails)
        ''*****************************************************
        ''* 1,2 & 3) Read all of the directory File
        ''*****************************************************
        Dim FileZeroData() As Byte = ReadFileDirectory()

        Dim PAddress As New MfgControl.AdvancedHMI.Drivers.PCCCAddress(ProcessorType)
        Dim reply As Integer

        RaiseEvent UploadProgress(Me, System.EventArgs.Empty)

        '**************************************************
        '* 4) Parse the data from the File Directory data
        '**************************************************
        '*********************************************************************************
        '* Starting at corresponding File Position, each program is defined with 10 bytes
        '* 1st byte=File Type
        '* 2nd & 3rd bytes are program size
        '* 4th & 5th bytes are location with memory
        '*********************************************************************************
        Dim FilePosition As Integer
        Dim ProgramFile As New PLCFileDetails
        Dim ProgramFiles As New System.Collections.ObjectModel.Collection(Of PLCFileDetails)

        '*********************************************
        '* 4a) Add the directory information
        '*********************************************
        ProgramFile.FileNumber = 0
        ProgramFile.data = FileZeroData
        ProgramFile.FileType = PAddress.FileType
        ProgramFile.NumberOfBytes = FileZeroData.Length
        ProgramFiles.Add(ProgramFile)

        '**********************************************
        '* 5) Read the rest of the data tables
        '**********************************************
        Dim DataFileGroup, ForceFileGroup, SystemFileGroup, SystemLadderFileGroup As Integer
        Dim LadderFileGroup, Unknown1FileGroup, Unknown2FileGroup As Integer
        If reply = 0 Then
            Dim NumberOfProgramFiles As Integer = FileZeroData(46) + FileZeroData(47) * 256

            '* Comb through data file 0 and get the program file details
            Dim i As Integer
            '* The start of program file definitions
            Select Case ProcessorType
                Case &H25, &H58
                    FilePosition = 93
                Case &H88 To &H9C
                    FilePosition = 103
                Case &H9F   '* ML1400
                    FilePosition = &H71
                Case Else
                    FilePosition = 79
            End Select

            Do While FilePosition < FileZeroData.Length And reply = 0
                ProgramFile.FileType = FileZeroData(FilePosition)
                ProgramFile.NumberOfBytes = (FileZeroData(FilePosition + 1) + FileZeroData(FilePosition + 2) * 256)

                If ProgramFile.FileType >= &H40 AndAlso ProgramFile.FileType <= &H5F Then
                    ProgramFile.FileNumber = SystemFileGroup
                    SystemFileGroup += 1
                End If
                If (ProgramFile.FileType >= &H20 AndAlso ProgramFile.FileType <= &H3F) Then
                    ProgramFile.FileNumber = LadderFileGroup
                    LadderFileGroup += 1
                End If
                If (ProgramFile.FileType >= &H60 AndAlso ProgramFile.FileType <= &H7F) Then
                    ProgramFile.FileNumber = SystemLadderFileGroup
                    SystemLadderFileGroup += 1
                End If
                If ProgramFile.FileType >= &H80 AndAlso ProgramFile.FileType <= &H9F Then
                    ProgramFile.FileNumber = DataFileGroup
                    DataFileGroup += 1
                End If
                If ProgramFile.FileType >= &HA0 AndAlso ProgramFile.FileType <= &HBF Then
                    ProgramFile.FileNumber = ForceFileGroup
                    ForceFileGroup += 1
                End If
                If ProgramFile.FileType >= &HC0 AndAlso ProgramFile.FileType <= &HDF Then
                    ProgramFile.FileNumber = Unknown1FileGroup
                    Unknown1FileGroup += 1
                End If
                If ProgramFile.FileType >= &HE0 AndAlso ProgramFile.FileType <= &HFF Then
                    ProgramFile.FileNumber = Unknown2FileGroup
                    Unknown2FileGroup += 1
                End If

                'PAddress.FileType = ProgramFile.FileType
                'PAddress.FileNumber = ProgramFile.FileNumber
                'PAddress.BitNumber = 99   '* Do not let the extract data try to interpret bit level
                PAddress.SetSpecial(ProgramFile.FileType, ProgramFile.FileNumber)

                If ProgramFile.NumberOfBytes > 0 Then
                    PAddress.NumberOfElements = CInt(ProgramFile.NumberOfBytes / PAddress.BytesPerElement)
                    Dim TNS As Integer = ReadRawData(PAddress)
                    ProgramFile.data = Responses(TNS And 255).RawData
                    If reply <> 0 Then Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("Failed to Read Program File " & PAddress.FileNumber & ", Type " & PAddress.FileType & " - " & DecodeMessage(reply))
                Else
                    Dim ZeroLengthData(-1) As Byte
                    ProgramFile.data = ZeroLengthData
                End If


                ProgramFiles.Add(ProgramFile)
                RaiseEvent UploadProgress(Me, System.EventArgs.Empty)

                i += 1
                '* 10 elements are used to define each program file
                '* SLC 5/02 or ML1000
                If ProcessorType = &H25 OrElse ProcessorType = &H58 Then
                    FilePosition += 8
                Else
                    FilePosition += 10
                End If
            Loop

        End If

        Return ProgramFiles
    End Function

    '****************************************************************
    '* Download a group of files defined in the PLCFiles Collection
    '****************************************************************
    Public Sub DownloadProgramData(ByVal PLCFiles As System.Collections.ObjectModel.Collection(Of PLCFileDetails))
        '******************************
        '* 1 & 2) Change to program Mode
        '******************************
        SetProgramMode()
        RaiseEvent DownloadProgress(Me, System.EventArgs.Empty)

        '*************************************************************************
        '* 2) Initialize Memory & Put in Download mode using Execute Command List
        '*************************************************************************
        Dim DataLength As Integer
        Select Case ProcessorType
            Case &H5B, &H78, &H6F
                DataLength = 13
            Case &H88 To &H9C
                DataLength = 15
            Case &H9F
                DataLength = 15  '*** TODO
            Case Else
                DataLength = 15
        End Select

        Dim data(DataLength) As Byte
        '* 2 commands
        data(0) = &H2
        '* Number of bytes in 1st command
        data(1) = &HA
        '* Function &HAA
        data(2) = &HAA
        '* Write 4 bytes
        data(3) = 4
        data(4) = 0
        '* File type 63
        data(5) = &H63

        '* Lets go ahead and setup the file type for later use
        Dim PAddress As New MfgControl.AdvancedHMI.Drivers.PCCCAddress(ProcessorType)
        Dim reply As Integer

        '**********************************
        '* 2a) Search for File 0, Type 24
        '**********************************
        Dim i As Integer
        While i < PLCFiles.Count AndAlso (PLCFiles(i).FileNumber <> 0 OrElse PLCFiles(i).FileType <> &H24)
            i += 1
        End While

        '* Write bytes 02-07 from File 0, Type 24 to File 0, Type 63
        If i < PLCFiles.Count Then
            data(8) = PLCFiles(i).data(2)
            data(9) = PLCFiles(i).data(3)
            data(10) = PLCFiles(i).data(4)
            data(11) = PLCFiles(i).data(5)
            If DataLength > 14 Then
                data(12) = PLCFiles(i).data(6)
                data(13) = PLCFiles(i).data(7)
            End If
        End If


        Select Case ProcessorType
            Case &H78, &H5B, &H49, &H6F  '* SLC 5/05, 5/04, 5/03
                '* Read these 4 bytes to write back, File 0, Type 63
                PAddress.FileType = &H63
                PAddress.Element = 0
                PAddress.SubElement = 0
                PAddress.NumberOfElements = CInt(4 / PAddress.BytesPerElement)

                Dim TNS As Integer = ReadRawData(PAddress)
                Dim FourBytes() As Byte = Responses(TNS And 255).RawData
                If reply = 0 Then
                    Array.Copy(FourBytes, 0, data, 8, 4)
                    PAddress.FileType = 1
                    PAddress.Element = &H23
                Else
                    Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("Failed to Read File 0, Type 63h - " & DecodeMessage(reply))
                End If

                '* Number of bytes in 1st command
                data(1) = &HA
                '* Number of bytes to write
                data(3) = 4
            Case &H88 To &H9C   '* ML1200, ML1500, ML1100
                '* Number of bytes in 1st command
                data(1) = &HC
                '* Number of bytes to write
                data(3) = 6
                PAddress.FileType = 2
                PAddress.Element = &H23
            Case &H9F   '* ML1400
                '* Number of bytes in 1st command
                data(1) = &HC       '* TODO
                '* Number of bytes to write
                data(3) = 6
                PAddress.FileType = 3
                PAddress.Element = &H28
            Case Else '* Fill in the gap for an unknown processor
                data(1) = &HA
                data(3) = 4
                PAddress.FileType = 1
                PAddress.Element = &H23
        End Select


        '* 1 byte in 2nd command - Start Download
        data(data.Length - 2) = 1
        data(data.Length - 1) = &H56

        reply = PrefixAndSend(&HF, &H88, data, True, GetNextTNSNumber(255, MyObjectID))
        If reply <> 0 Then Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("Failed to Initialize for Download - " & DecodeMessage(reply))
        RaiseEvent DownloadProgress(Me, System.EventArgs.Empty)


        '*********************************
        '* 4) Secure Sole Access
        '*********************************
        Dim data2(-1) As Byte
        reply = PrefixAndSend(&HF, &H11, data2, True, GetNextTNSNumber(255, MyObjectID))
        If reply <> 0 Then Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("Failed to Secure Sole Access - " & DecodeMessage(reply))
        RaiseEvent DownloadProgress(Me, System.EventArgs.Empty)

        '*********************************
        '* 5) Write the directory length
        '*********************************
        PAddress.BitNumber = 16
        Dim data3(1) As Byte
        data3(0) = CByte(PLCFiles(0).data.Length And &HFF)
        data3(1) = CByte((PLCFiles(0).data.Length - data3(0)) / 256)
        reply = WriteRawData(PAddress, 2, data3)
        If reply <> 0 Then Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("Failed to Write Directory Length - " & DecodeMessage(reply))
        RaiseEvent DownloadProgress(Me, System.EventArgs.Empty)

        '*********************************
        '* 6) Write program directory
        '*********************************
        PAddress.Element = 0
        reply = WriteRawData(PAddress, PLCFiles(0).data.Length, PLCFiles(0).data)
        If reply <> 0 Then Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("Failed to Write New Program Directory - " & DecodeMessage(reply))
        RaiseEvent DownloadProgress(Me, System.EventArgs.Empty)

        '*********************************
        '* 7) Write Program & Data Files
        '*********************************
        For i = 1 To PLCFiles.Count - 1
            PAddress.FileNumber = PLCFiles(i).FileNumber
            PAddress.FileType = PLCFiles(i).FileType
            PAddress.Element = 0
            PAddress.SubElement = 0
            PAddress.BitNumber = 16
            reply = WriteRawData(PAddress, PLCFiles(i).data.Length, PLCFiles(i).data)
            If reply <> 0 Then Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("Failed when writing files to PLC - " & DecodeMessage(reply))
            RaiseEvent DownloadProgress(Me, System.EventArgs.Empty)
        Next

        '*********************************
        '* 8) Complete the Download
        '*********************************
        reply = PrefixAndSend(&HF, &H52, data2, True, GetNextTNSNumber(255, MyObjectID))
        If reply <> 0 Then Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("Failed to Indicate to PLC that Download is complete - " & DecodeMessage(reply))
        RaiseEvent DownloadProgress(Me, System.EventArgs.Empty)

        '*********************************
        '* 9) Release Sole Access
        '*********************************
        reply = PrefixAndSend(&HF, &H12, data2, True, GetNextTNSNumber(255, MyObjectID))
        If reply <> 0 Then Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("Failed to Release Sole Access - " & DecodeMessage(reply))
        RaiseEvent DownloadProgress(Me, System.EventArgs.Empty)
    End Sub


    ''' <summary>
    ''' Get the number of slots in the rack
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function GetSlotCount() As Integer
        '* Get the header of the data table definition file
        Dim data(4) As Byte

        '* Number of bytes to read
        data(0) = 4
        '* Data File Number (0 is the system file)
        data(1) = 0
        '* File Type (&H60 must be a system type), this was pulled from reverse engineering
        data(2) = &H60
        '* Element Number
        data(3) = 0
        '* Sub Element Offset in words
        data(4) = 0


        Dim TNS As Integer = GetNextTNSNumber(255, MyObjectID)
        Dim reply As Integer = PrefixAndSend(&HF, &HA2, data, True, TNS)

        If reply = 0 Then
            If DataPackets(TNS And 255)(6) > 0 Then
                Return DataPackets(TNS And 255)(6) - 1  '* if a rack based system, then subtract processor slot
            Else
                Return 0  '* micrologix reports 0 slots
            End If
        Else
            Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("Failed to Release Sole Access - " & DecodeMessage(reply))
        End If
    End Function

    Public Structure IOConfig
        Dim InputBytes As Integer
        Dim OutputBytes As Integer
        Dim CardCode As Integer
    End Structure
    ''' <summary>
    ''' Get IO card list currently in rack
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function GetIOConfig() As IOConfig()
        Dim ProcessorType As Integer = GetProcessorType()


        If ProcessorType = &H89 Or ProcessorType = &H8C Then  '* Is it a Micrologix 1500?
            Return GetML1500IOConfig()
        Else
            Return GetSLCIOConfig()
        End If
    End Function

    ''' <summary>
    ''' Get IO card list currently in rack of a SLC
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function GetSLCIOConfig() As IOConfig()
        Dim slots As Integer = GetSlotCount()

        If slots > 0 Then
            '* Get the header of the data table definition file
            Dim data(4) As Byte

            '* Number of bytes to read
            data(0) = CByte(4 + (slots + 1) * 6 + 2)
            '* Data File Number (0 is the system file)
            data(1) = 0
            '* File Type (&H60 must be a system type), this was pulled from reverse engineering
            data(2) = &H60
            '* Element Number
            data(3) = 0
            '* Sub Element Offset in words
            data(4) = 0


            Dim TNS As Integer = GetNextTNSNumber(255, MyObjectID)
            Dim TNSLowerByte As Integer = TNS And 255
            Dim reply As Integer = PrefixAndSend(&HF, &HA2, data, True, TNS)

            Dim BytesForConverting(1) As Byte
            Dim IOResult(slots) As IOConfig
            If reply = 0 Then
                '* Extract IO information
                For i As Integer = 0 To slots
                    IOResult(i).InputBytes = DataPackets(TNSLowerByte)(i * 6 + 10)
                    IOResult(i).OutputBytes = DataPackets(TNSLowerByte)(i * 6 + 12)
                    BytesForConverting(0) = DataPackets(TNSLowerByte)(i * 6 + 14)
                    BytesForConverting(1) = DataPackets(TNSLowerByte)(i * 6 + 15)
                    IOResult(i).CardCode = BitConverter.ToInt16(BytesForConverting, 0)
                Next
                Return IOResult
            Else
                Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("Failed to get IO Config - " & DecodeMessage(reply))
            End If
        Else
            Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("Failed to get Slot Count")
        End If
    End Function


    ''' <summary>
    ''' Get IO card list currently in rack of a ML1500
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function GetML1500IOConfig() As IOConfig()
        '*************************************************************************
        '* Read the first 4 bytes of File 0, type 62 to get the total file length
        '**************************************************************************
        Dim data(4) As Byte
        Dim TNS As Integer = GetNextTNSNumber(255, MyObjectID)
        Dim TNSLowerByte As Integer = TNS And 255

        '* Number of bytes to read
        data(0) = 4
        '* Data File Number (0 is the system file)
        data(1) = 0
        '* File Type (&H62 must be a system type), this was pulled from reverse engineering
        data(2) = &H62
        '* Element Number
        data(3) = 0
        '* Sub Element Offset in words
        data(4) = 0

        Dim reply As Integer = PrefixAndSend(&HF, &HA2, data, True, TNS)

        '******************************************
        '* Read all of File Zero, Type 62
        '******************************************
        If reply = 0 Then
            'TODO: Get this corrected
            Dim FileZeroSize As Integer = DataPackets(TNSLowerByte)(6) * 2
            Dim FileZeroData(FileZeroSize) As Byte
            Dim FilePosition As Integer
            Dim Subelement As Integer
            Dim i As Integer

            '* Number of bytes to read
            If FileZeroSize > &H50 Then
                data(0) = &H50
            Else
                data(0) = CByte(FileZeroSize)
            End If

            '* Loop through reading all of file 0 in chunks of 80 bytes
            Do While FilePosition < FileZeroSize And reply = 0

                '* Read the file
                TNS = GetNextTNSNumber(255, MyObjectID)
                TNSLowerByte = TNS And 255
                reply = PrefixAndSend(&HF, &HA2, data, True, TNS)

                '* Transfer block of data read to the data table array
                i = 0
                Do While i < data(0)
                    FileZeroData(FilePosition) = DataPackets(TNSLowerByte)(i + 6)
                    i += 1
                    FilePosition += 1
                Loop


                '* point to the next element, by taking the last Start Element(in words) and adding it to the number of bytes read
                Subelement += CInt(data(0) / 2)
                If Subelement < 255 Then
                    data(3) = CByte(Subelement)
                Else
                    '* Use extended addressing
                    If data.Length < 6 Then ReDim Preserve data(5)
                    data(5) = CByte(Math.Floor(Subelement / 256))  '* 256+data(5)
                    data(4) = CByte(Subelement - (data(5) * 256)) '*  calculate offset
                    data(3) = 255
                End If

                '* Set next length of data to read. Max of 80
                If FileZeroSize - FilePosition < 80 Then
                    data(0) = CByte(FileZeroSize - FilePosition)
                Else
                    data(0) = 80
                End If
            Loop


            '**********************************
            '* Extract the data from the file
            '**********************************
            If reply = 0 Then
                Dim SlotCount As Integer = FileZeroData(2) - 2
                If SlotCount < 0 Then SlotCount = 0
                Dim SlotIndex As Integer = 1
                Dim IOResult(SlotCount) As IOConfig

                '*Start getting slot data
                i = 32 + SlotCount * 4
                Dim BytesForConverting(1) As Byte

                Do While SlotIndex <= SlotCount
                    IOResult(SlotIndex).InputBytes = FileZeroData(i + 2) * 2
                    IOResult(SlotIndex).OutputBytes = FileZeroData(i + 8) * 2
                    BytesForConverting(0) = FileZeroData(i + 18)
                    BytesForConverting(1) = FileZeroData(i + 19)
                    IOResult(SlotIndex).CardCode = BitConverter.ToInt16(BytesForConverting, 0)

                    i += 26
                    SlotIndex += 1
                Loop


                '****************************************
                '* Get the Slot 0(base unit) information
                '****************************************
                data(0) = 8
                '* Data File Number (0 is the system file)
                data(1) = 0
                '* File Type (&H60 must be a system type), this was pulled from reverse engineering
                data(2) = &H60
                '* Element Number
                data(3) = 0
                '* Sub Element Offset in words
                data(4) = 0


                '* Read File 0 to get the IO count on the base unit
                TNS = GetNextTNSNumber(255, MyObjectID)
                TNSLowerByte = TNS And 255
                reply = PrefixAndSend(&HF, &HA2, data, True, TNS)

                If reply = 0 Then
                    IOResult(0).InputBytes = DataPackets(TNSLowerByte)(10)
                    IOResult(0).OutputBytes = DataPackets(TNSLowerByte)(12)
                Else
                    Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("Failed to get Base IO Config for Micrologix 1500- " & DecodeMessage(reply))
                End If


                Return IOResult
            End If
        End If

        Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("Failed to get IO Config for Micrologix 1500- " & DecodeMessage(reply))
    End Function
#End Region

#Region "Data Reading"
    Public Function Read(ByVal startAddress As String, ByVal numberOfElements As Integer) As String() Implements IComComponent.Read
        Dim TNS As Integer
        TNS = BeginRead(startAddress, numberOfElements)

        Dim result As Integer
        result = WaitForResponse(TNS)

        If result = 0 Then
            Dim d(Responses(TNS And 255).Values.Count - 1) As String
            Responses(TNS And 255).Values.CopyTo(d, 0)
            Return d
            'Return Responses(TNS And 255).Values.ToArray
        Else
            Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException(result, "Read Failed - Result=" & result & ". " & DecodeMessage(result))
        End If
    End Function

    Public Function Read(ByVal startAddress As String) As String
        Return Read(startAddress, 1)(0)
    End Function


    '*************************************************************
    '* Overloaded method of ReadAny - that reads only one element
    '*************************************************************
    Public Function BeginRead(ByVal startAddress As String) As Integer
        Return BeginRead(startAddress, 1)
    End Function

    'Public Function BeginRead(ByVal startAddress As String, ByVal numberOfElements As Integer) As String()
    '    Return ReadAny(startAddress, numberOfElements, m_AsyncMode)
    'End Function

    Private ReadLock As New Object
    '******************************************
    '* Synchronous read of any data type
    '*  this function does not declare its return type because it dependent on the data type read
    '******************************************
    Public Function BeginRead(ByVal startAddress As String, ByVal numberOfElements As Integer) As Integer Implements IComComponent.BeginRead
        Dim data(4) As Byte
        Dim ParsedResult As New MfgControl.AdvancedHMI.Drivers.PCCCAddress(startAddress, numberOfElements, GetProcessorType)
        ParsedResult.TargetNode = m_TargetNode

        SyncLock (ReadLock)
            '* If requesting 0 elements, then default to 1
            Dim ArrayElements As Integer = numberOfElements - 1
            If ArrayElements < 0 Then
                ArrayElements = 0
            End If

            '* If reading at bit level ,then convert number bits to read to number of words
            '* Fixed a problem when reading multiple bits that span over more than 1 word
            If ParsedResult.BitNumber < 99 Then
                ArrayElements = CInt(Math.Floor(((numberOfElements + ParsedResult.BitNumber) - 1) / 16))
            End If


            '* Number of bytes to read
            Dim NumberOfBytes As Integer


            NumberOfBytes = (ArrayElements + 1) * ParsedResult.BytesPerElement


            '* If it is a multiple read of sub-elements of timers and counter, then read an array of the same consectutive sub element
            '* FIX
            If ParsedResult.SubElement > 0 AndAlso ArrayElements > 0 AndAlso (ParsedResult.FileType = &H86 Or ParsedResult.FileType = &H87) Then
                NumberOfBytes = (NumberOfBytes * 3)   '* There are 3 words per sub element (6 bytes)
            End If


            'Dim reply As Integer
            'Dim ReturnedData() As Byte

            '* 23-MAY-13 - Added AND 255 to keep from overflowing
            ParsedResult.ByteStream(0) = CByte(NumberOfBytes And 255)

            Dim TNS As Integer
            TNS = ReadRawData(ParsedResult)
 

            'ReturnedData = Responses(TNS And 255).RawData.ToArray

            'If AsyncModeIn Then
            'Dim x() As String = {"0"}
            Return TNS
            'Else
            '    Return ExtractData(ParsedResult, ReturnedData)
            'End If
        End SyncLock
    End Function

    Private Shared Function ExtractData(ByVal ParsedResult As MfgControl.AdvancedHMI.Drivers.PCCCAddress, ByVal ReturnedData() As Byte) As String()
        '* Get the element size in bytes
        Dim ElementSize As Integer = ParsedResult.BytesPerElement

        '***************************************************
        '* Extract returned data into appropriate data type
        '***************************************************
        'Dim result(Math.Floor((ParsedResult.NumberOfElements * ParsedResult.BytesPerElement) / ElementSize) - 1) As String
        '* 18-MAY-12 Changed to accomodate packet being broken up and reassembled by ReadRawData
        Dim result(CInt(Math.Floor(ReturnedData.Length / ElementSize) - 1)) As String

        Dim StringLength As Integer
        Select Case ParsedResult.FileType
            Case &H8A '* Floating point read (&H8A)
                For i As Integer = 0 To result.Length - 1
                    result(i) = CStr(BitConverter.ToSingle(ReturnedData, (i * ParsedResult.BytesPerElement)))
                Next
            Case &H8D ' * String
                For i As Integer = 0 To result.Length - 1
                    StringLength = BitConverter.ToInt16(ReturnedData, (i * ParsedResult.BytesPerElement))
                    '* The controller may falsely report the string length, so set to max allowed
                    If StringLength > 82 Then StringLength = 82

                    '* use a string builder for increased performance
                    Dim result2 As New System.Text.StringBuilder
                    Dim j As Integer = 2
                    '* Stop concatenation if a zero (NULL) is reached
                    While j < StringLength + 2 And ReturnedData((i * 84) + j + 1) > 0
                        result2.Append(Chr(ReturnedData((i * 84) + j + 1)))
                        '* Prevent an odd length string from getting a Null added on
                        If j < StringLength + 1 And (ReturnedData((i * 84) + j)) > 0 Then result2.Append(Chr(ReturnedData((i * 84) + j)))
                        j += 2
                    End While
                    result(i) = result2.ToString
                Next
            Case &H86, &H87, &H88  '* Timer, counter, control
                '* If a sub element is designated then read the same sub element for all timers
                Dim j As Integer
                If ParsedResult.SubElement > 0 Then
                    For i = 0 To ParsedResult.NumberOfElements - 1
                        j = i * 6 + (ParsedResult.SubElement * 2)
                        result(i) = CStr(BitConverter.ToInt16(ReturnedData, j))
                    Next
                Else
                    For i2 = 0 To (ParsedResult.NumberOfElements * 3) - 1
                        j = i2 * 2
                        result(i2) = CStr(BitConverter.ToInt16(ReturnedData, j))
                    Next
                End If

            Case &H91 '* Long Value read (&H91)
                    For i As Integer = 0 To result.Length - 1
                        result(i) = CStr(BitConverter.ToInt32(ReturnedData, (i * ParsedResult.BytesPerElement)))
                    Next
            Case &H92 '* MSG Value read (&H92)
                    For i As Integer = 0 To result.Length - 1
                        result(i) = BitConverter.ToString(ReturnedData, (i * ParsedResult.BytesPerElement), 50)
                    Next
            Case Else
                    For i As Integer = 0 To result.Length - 1
                        result(i) = CStr(BitConverter.ToInt16(ReturnedData, (i * ParsedResult.BytesPerElement)))
                    Next
        End Select
        'End If


        '******************************************************************************
        '* If the number of words to read is not specified, then return a single value
        '******************************************************************************
        '* Is it a bit level and N or B file?
        If ParsedResult.BitNumber >= 0 And ParsedResult.BitNumber < 99 Then
            Dim BitResult(ParsedResult.NumberOfElements - 1) As String
            Dim BitPos As Integer = ParsedResult.BitNumber
            Dim WordPos As Integer = 0
            'Dim Result(ArrayElements) As Boolean

            '* If a bit number is greater than 16, point to correct word
            '* This can happen on IO addresses (e.g. I:0/16)
            WordPos += BitPos >> 4
            BitPos = BitPos Mod 16

            '* Set array of consectutive bits
            For i As Integer = 0 To BitResult.Length - 1
                BitResult(i) = CStr((CInt(result(WordPos)) And CInt(2 ^ BitPos)) > 0)
                BitPos += 1
                If BitPos > 15 Then
                    BitPos = 0
                    WordPos += 1
                End If
            Next
            Return BitResult
        End If

        Return result
    End Function

    '***********************************************
    ' Reads values and returns them as integers
    '***********************************************
     Public Function ReadInt(ByVal startAddress As String, ByVal numberOfBytes As Integer) As Integer()
        Dim result() As String
        result = Read(startAddress, numberOfBytes)

        Dim Ints(result.Length) As Integer
        For i As Integer = 0 To result.Length - 1
            Ints(i) = CInt(result(i))
        Next

        Return Ints
    End Function

    '*********************************************************************************
    '* Read Raw File data and break up into chunks because of limits of DF1 protocol
    '*********************************************************************************
    Private Function ReadRawData(ByVal PAddressO As MfgControl.AdvancedHMI.Drivers.PCCCAddress) As Integer
        '* Create a clone to work with so we do not modifiy the original through a pointer
        Dim PAddress As New MfgControl.AdvancedHMI.Drivers.PCCCAddress(ProcessorType)
        PAddress = DirectCast(PAddressO.Clone, MfgControl.AdvancedHMI.Drivers.PCCCAddress)

        Dim NumberOfBytesToRead, FilePosition As Integer
        Dim ResultData(PAddress.ByteSize - 1) As Byte
        Dim TNS As Integer
        Dim Result As Integer

        Do While FilePosition < PAddress.ByteSize AndAlso Result = 0
            '* Set next length of data to read. Max of 236 (slc 5/03 and up)
            '* This must limit to 82 for 5/02 and below
            If PAddress.ByteSize - FilePosition < 236 Then
                NumberOfBytesToRead = PAddress.ByteSize - FilePosition
            Else
                NumberOfBytesToRead = 236
            End If

            '* The SLC 5/02 can only read &H50 bytes per read, possibly the ML1500
            'If NumberOfBytesToRead > &H50 AndAlso (ProcessorType = &H25 Or ProcessorType = &H89) Then
            If NumberOfBytesToRead > &H50 AndAlso (ProcessorType = &H25) Then
                NumberOfBytesToRead = &H50
            End If

            '* String is an exception
            If NumberOfBytesToRead > 168 AndAlso PAddress.FileType = &H8D Then
                '* Only two string elements can be read on each read (168 bytes)
                NumberOfBytesToRead = 168
            End If

            If NumberOfBytesToRead > 234 AndAlso (PAddress.FileType = &H86 OrElse PAddress.FileType = &H87) Then
                '* Timers & counters read in multiples of 6 bytes
                NumberOfBytesToRead = 234
            End If

            '* Data Monitor File is an exception
            If NumberOfBytesToRead > &H78 AndAlso PAddress.FileType = &HA4 Then
                '* Only two string elements can be read on each read (168 bytes)
                NumberOfBytesToRead = &H78
            End If

            If NumberOfBytesToRead > 0 Then
                Dim DataSize As Integer
                Dim Func As Byte

                'If PAddress.SubElement = 0 Then
                'DataSize = 3
                'Func = &HA1
                'Else

                DataSize = 4
                '* is the processor type a PLC5?
                If MfgControl.AdvancedHMI.Drivers.PCCCAddress.IsPLC5(ProcessorType) Then
                    Func = 1
                Else
                    Func = &HA2
                End If
                'End If

                '**********************************************************************
                '* Link the TNS to the original address for use by the linked polling
                '**********************************************************************
                TNS = GetNextTNSNumber(255, MyObjectID)
                Dim TNSLowerByte As Integer = TNS And 255

                PAddressO.InternallyRequested = InternalRequest
                PAddressO.TargetNode = m_TargetNode
                PLCAddressByTNS(TNSLowerByte) = PAddressO


                'Dim ByteStream(PAddress.ByteStream.Length) As Byte
                'PAddress.ByteStream.CopyTo(ByteStream, 1)
                'ByteStream(0) = NumberOfBytesToRead
                '* A PLC specifies the number of bytes at the end of the stream
                If MfgControl.AdvancedHMI.Drivers.PCCCAddress.IsPLC5(ProcessorType) Then
                    PAddress.ByteStream(PAddress.ByteStream.Length - 1) = CByte(NumberOfBytesToRead)
                Else
                    PAddress.ByteStream(0) = CByte(NumberOfBytesToRead)
                End If

                Result = PrefixAndSend(&HF, Func, PAddress.ByteStream, False, TNS)
                InternalRequest = False


                If Result = 0 Then
                    If (FilePosition + NumberOfBytesToRead < PAddress.ByteSize) Then
                        Result = WaitForResponse(TNSLowerByte)

                        '* Return status byte that came from controller
                        If Result = 0 Then
                            If DataPackets(TNSLowerByte) IsNot Nothing Then
                                If (DataPackets(TNSLowerByte).Count > 3) Then
                                    Result = DataPackets(TNSLowerByte)(3)  '* STS position in DF1 message
                                    '* If its and EXT STS, page 8-4
                                    If Result = &HF0 Then
                                        '* The EXT STS is the last byte in the packet
                                        'result = DataPackets(rTNS)(DataPackets(rTNS).Count - 2) + &H100
                                        Result = DataPackets(TNSLowerByte)(DataPackets(TNSLowerByte).Count - 1) + &H100
                                    End If
                                End If
                            Else
                                Result = -8 '* no response came back from PLC
                            End If
                        End If

                        '***************************************************
                        '* Extract returned data into appropriate data type
                        '* Transfer block of data read to the data table array
                        '***************************************************
                        '* TODO: Check array bounds
                        If Result = 0 Then
                            Dim x = DataPackets(TNSLowerByte)
                            For i As Integer = 0 To NumberOfBytesToRead - 1
                                ResultData(FilePosition + i) = DataPackets(TNSLowerByte)(i + 6)
                            Next
                        End If

                    End If

                Else
                    Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException(DecodeMessage(Result))
                End If
                FilePosition += NumberOfBytesToRead

                '* point to the next element
                If PAddress.FileType = &HA4 Then
                    PAddress.Element += CInt(NumberOfBytesToRead / &H28)
                Else
                    '* Use subelement because it works with all data file types
                    PAddress.SubElement += CInt(NumberOfBytesToRead / 2)
                End If
            End If
 
        Loop


        Return TNS
    End Function
#End Region

#Region "Data Writing"
    '*****************************************************************
    '* Write Section
    '*
    '* Address is in the form of <file type><file Number>:<offset>
    '* examples  N7:0, B3:0,
    '******************************************************************

    '* Handle one value of Integer type
    ''' <summary>
    ''' Write a single integer value to a PLC data table
    ''' The startAddress is in the common form of AB addressing (e.g. N7:0)
    ''' </summary>
    ''' <param name="startAddress"></param>
    ''' <param name="dataToWrite"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function Write(ByVal startAddress As String, ByVal dataToWrite As Integer) As Integer
        Dim temp(1) As Integer
        temp(0) = dataToWrite
        Return Write(startAddress, 1, temp)
    End Function


    '* Write an array of integers
    ''' <summary>
    ''' Write multiple consectutive integer values to a PLC data table
    ''' The startAddress is in the common form of AB addressing (e.g. N7:0)
    ''' </summary>
    ''' <param name="startAddress"></param>
    ''' <param name="numberOfElements"></param>
    ''' <param name="dataToWrite"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function Write(ByVal startAddress As String, ByVal numberOfElements As Integer, ByVal dataToWrite() As Integer) As Integer
        Dim ParsedResult As New MfgControl.AdvancedHMI.Drivers.PCCCAddress(startAddress, numberOfElements, GetProcessorType)

        Dim ConvertedData(numberOfElements * ParsedResult.BytesPerElement - 1) As Byte

        Dim i As Integer
        If ParsedResult.FileType = &H91 Then
            '* Write to a Long integer file
            While i < numberOfElements
                '******* NOT Necesary to validate because dataToWrite keeps it in range for a long
                Dim b(3) As Byte
                b = BitConverter.GetBytes(dataToWrite(i))

                ConvertedData(i * 4) = b(0)
                ConvertedData(i * 4 + 1) = b(1)
                ConvertedData(i * 4 + 2) = b(2)
                ConvertedData(i * 4 + 3) = b(3)
                i += 1
            End While
        ElseIf ParsedResult.FileType <> 0 Then
            While i < numberOfElements
                '* Validate range
                If dataToWrite(i) > 32767 Or dataToWrite(i) < -32768 Then
                    Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("Integer data out of range, must be between -32768 and 32767")
                End If

                ConvertedData(i * 2) = CByte(dataToWrite(i) And &HFF)
                ConvertedData(i * 2 + 1) = CByte((dataToWrite(i) >> 8) And &HFF)

                i += 1
            End While
        Else
            Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("Invalid Address")
        End If

        Return WriteRawData(ParsedResult, numberOfElements * ParsedResult.BytesPerElement, ConvertedData)
    End Function

    '* Handle one value of Single type
    ''' <summary>
    ''' Write a single floating point value to a data table
    ''' The startAddress is in the common form of AB addressing (e.g. F8:0)
    ''' </summary>
    ''' <param name="startAddress"></param>
    ''' <param name="dataToWrite"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function Write(ByVal startAddress As String, ByVal dataToWrite As Single) As Integer
        Dim temp(1) As Single
        temp(0) = dataToWrite
        Return Write(startAddress, 1, temp)
    End Function

    '* Write an array of Singles
    ''' <summary>
    ''' Write multiple consectutive floating point values to a PLC data table
    ''' The startAddress is in the common form of AB addressing (e.g. F8:0)
    ''' </summary>
    ''' <param name="startAddress"></param>
    ''' <param name="numberOfElements"></param>
    ''' <param name="dataToWrite"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function Write(ByVal startAddress As String, ByVal numberOfElements As Integer, ByVal dataToWrite() As Single) As Integer
        Dim ParsedResult As New MfgControl.AdvancedHMI.Drivers.PCCCAddress(startAddress, numberOfElements, GetProcessorType)

        Dim ConvertedData(numberOfElements * ParsedResult.BytesPerElement) As Byte

        Dim i As Integer
        If ParsedResult.FileType = &H8A Then
            '*Write to a floating point file
            Dim bytes(4) As Byte
            For i = 0 To numberOfElements - 1
                bytes = BitConverter.GetBytes(CSng(dataToWrite(i)))
                For j As Integer = 0 To 3
                    ConvertedData(i * 4 + j) = CByte(bytes(j))
                Next
            Next
        ElseIf ParsedResult.FileType = &H91 Then
            '* Write to a Long integer file
            While i < numberOfElements
                '* Validate range
                If dataToWrite(i) > 2147483647 Or dataToWrite(i) < -2147483648 Then
                    Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("Integer data out of range, must be between -2147483648 and 2147483647")
                End If

                Dim b(3) As Byte
                b = BitConverter.GetBytes(CInt(dataToWrite(i)))

                ConvertedData(i * 4) = b(0)
                ConvertedData(i * 4 + 1) = b(1)
                ConvertedData(i * 4 + 2) = b(2)
                ConvertedData(i * 4 + 3) = b(3)
                i += 1
            End While
        Else
            '* Write to an integer file
            While i < numberOfElements
                '* Validate range
                If dataToWrite(i) > 32767 Or dataToWrite(i) < -32768 Then
                    Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("Integer data out of range, must be between -32768 and 32767")
                End If

                ConvertedData(i * 2) = CByte(CInt(dataToWrite(i)) And &HFF)
                ConvertedData(i * 2 + 1) = CByte((CInt(dataToWrite(i)) >> 8) And &HFF)
                i += 1
            End While
        End If

        Return WriteRawData(ParsedResult, numberOfElements * ParsedResult.BytesPerElement, ConvertedData)
    End Function

    '* Write a String
    ''' <summary>
    ''' Write a string value to a string data table
    ''' The startAddress is in the common form of AB addressing (e.g. ST9:0)
    ''' </summary>
    ''' <param name="startAddress"></param>
    ''' <param name="dataToWrite"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function Write(ByVal startAddress As String, ByVal dataToWrite As String) As String Implements IComComponent.Write
        If dataToWrite Is Nothing Then
            Return "0"
        End If

        Dim ParsedResult As New MfgControl.AdvancedHMI.Drivers.PCCCAddress(startAddress, 1, GetProcessorType)

        If ParsedResult.FileType = 0 Then Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException("Invalid Address")

        If startAddress.IndexOf("ST", StringComparison.OrdinalIgnoreCase) >= 0 Then
            '* Add an extra character to compensate for characters written in pairs to integers
            'Dim ConvertedData(dataToWrite.Length + 2 + 1) As Byte
            Dim ConvertedData(83) As Byte
            dataToWrite &= Chr(0)

            ConvertedData(0) = CByte(dataToWrite.Length - 1)
            Dim i As Integer = 2
            While i <= dataToWrite.Length
                ConvertedData(i + 1) = CByte(Asc(dataToWrite.Substring(i - 2, 1)))
                ConvertedData(i) = CByte(Asc(dataToWrite.Substring(i - 1, 1)))
                i += 2
            End While
            'Array.Copy(System.Text.Encoding.Default.GetBytes(dataToWrite), 0, ConvertedData, 2, dataToWrite.Length)

            'Return WriteRawData(ParsedResult, dataToWrite.Length + 2, ConvertedData)
            Return CStr(WriteRawData(ParsedResult, 84, ConvertedData))
        ElseIf startAddress.IndexOf("L", StringComparison.OrdinalIgnoreCase) >= 0 Then
            Return CStr(Write(startAddress, CInt(dataToWrite)))
        ElseIf startAddress.IndexOf("F", StringComparison.OrdinalIgnoreCase) >= 0 Then
            Return CStr(Write(startAddress, CSng(dataToWrite)))
        Else
            Return CStr(Write(startAddress, CInt(dataToWrite)))
        End If
    End Function

    '**************************************************************
    '* Write to a PLC data file
    '*
    '**************************************************************
    Private Function WriteRawData(ByVal PAddressO As MfgControl.AdvancedHMI.Drivers.PCCCAddress, ByVal numberOfBytes As Integer, ByVal dataToWrite() As Byte) As Integer
        '* Create a clone to work with so we do not modifiy the original through a pointer
        Dim PAddress As New MfgControl.AdvancedHMI.Drivers.PCCCAddress(ProcessorType)
        PAddress = DirectCast(PAddressO.Clone, MfgControl.AdvancedHMI.Drivers.PCCCAddress)

        '* Invalid address?
        If PAddress.FileType = 0 Then
            Return -5
        End If

        '**********************************************
        '* Use a bit level function if it is bit level
        '**********************************************
        Dim FunctionNumber As Byte

        Dim FilePosition, NumberOfBytesToWrite, DataStartPosition As Integer

        Dim reply As Integer

        Do While FilePosition < numberOfBytes AndAlso reply = 0
            '* Set next length of data to read. Max of 236 (slc 5/03 and up)
            '* This must limit to 82 for 5/02 and below
            If numberOfBytes - FilePosition < 164 Then
                NumberOfBytesToWrite = numberOfBytes - FilePosition
            Else
                NumberOfBytesToWrite = 164
            End If

            '* These files seem to be a special case
            If PAddress.FileType >= &HA1 And NumberOfBytesToWrite > &H78 Then
                NumberOfBytesToWrite = &H78
            End If

            Dim DataSize As Integer = NumberOfBytesToWrite + PAddress.ByteStream.Length

            '* Is it a PLC5?
            If MfgControl.AdvancedHMI.Drivers.PCCCAddress.IsPLC5(ProcessorType) Then
                DataSize -= 1
            End If


            '* For now we are only going to allow one bit to be set/reset per call
            If PAddress.BitNumber < 16 Then DataSize = 8

            If PAddress.Element >= 255 Then DataSize += 2
            If PAddress.SubElement >= 255 Then DataSize += 2

            Dim DataW(DataSize - 1) As Byte

            ''* Byte Size
            'DataW(0) = ((NumberOfBytesToWrite And &HFF))
            ''* File Number
            'DataW(1) = (PAddress.FileNumber)
            ''* File Type
            'DataW(2) = (PAddress.FileType)
            ''* Starting Element Number
            'If PAddress.Element < 255 Then
            '    DataW(3) = (PAddress.Element)
            'Else
            '    DataW(5) = Math.Floor(PAddress.Element / 256)
            '    DataW(4) = PAddress.Element - (DataW(5) * 256) '*  calculate offset
            '    DataW(3) = 255
            'End If

            ''* Sub Element
            'If PAddress.SubElement < 255 Then
            '    DataW(DataW.Length - 1 - NumberOfBytesToWrite) = PAddress.SubElement
            'Else
            '    '* Use extended addressing
            '    DataW(DataW.Length - 1 - NumberOfBytesToWrite) = Math.Floor(PAddress.SubElement / 256)  '* 256+data(5)
            '    DataW(DataW.Length - 2 - NumberOfBytesToWrite) = PAddress.SubElement - (DataW(DataW.Length - 1 - NumberOfBytesToWrite) * 256) '*  calculate offset
            '    DataW(DataW.Length - 3 - NumberOfBytesToWrite) = 255
            'End If

            '* Are we changing a single bit?
            If PAddress.BitNumber < 16 Then
                '* 23-SEP-12 - was missing an byte
                ReDim DataW(DataW.Length)
                PAddress.ByteStream.CopyTo(DataW, 0)

                FunctionNumber = &HAB  '* Ref http://www.iatips.com/pccc_tips.html#slc5_cmds
                '* Set the mask of which bit to change
                DataW(DataW.Length - 4) = CByte(CInt(Math.Pow(2, PAddress.BitNumber)) And &HFF)
                DataW(DataW.Length - 3) = CByte(Math.Pow(2, (PAddress.BitNumber - 8)))

                If dataToWrite(0) <= 0 Then
                    '* Set bits to clear 
                    DataW(DataW.Length - 2) = 0
                    DataW(DataW.Length - 1) = 0
                Else
                    '* Bits to turn on
                    DataW(DataW.Length - 2) = CByte(CInt(Math.Pow(2, PAddress.BitNumber)) And &HFF)
                    DataW(DataW.Length - 1) = CByte(Math.Pow(2, (PAddress.BitNumber - 8)))
                End If
            Else
                DataStartPosition = DataW.Length - NumberOfBytesToWrite

                '* Prevent index out of range when numberToWrite exceeds dataToWrite.Length
                Dim ValuesToMove As Integer = NumberOfBytesToWrite - 1
                If ValuesToMove + FilePosition > dataToWrite.Length - 1 Then
                    ValuesToMove = dataToWrite.Length - 1 - FilePosition
                End If


                PAddress.ByteStream.CopyTo(DataW, 0)

                '**** PATCH - Set up to read 3 elements of timer,counters, etc, but it affected writing
                If DataW(0) = dataToWrite.Length * 3 Then
                    DataW(0) = CByte(dataToWrite.Length)
                    If PAddress.PLCAddress.IndexOf(".PRE", 0, System.StringComparison.CurrentCultureIgnoreCase) > 0 Then
                        DataW(4) = 1
                    End If
                    If PAddress.PLCAddress.IndexOf(".ACC", 0, System.StringComparison.CurrentCultureIgnoreCase) > 0 Then
                        DataW(4) = 2
                    End If
                End If
                '***************************************************************************************************

                Dim l As Integer = PAddress.ByteStream.Length

                '* Is it a PLC5?
                If MfgControl.AdvancedHMI.Drivers.PCCCAddress.IsPLC5(ProcessorType) Then
                    FunctionNumber = 0
                    l -= 1
                Else
                    FunctionNumber = &HAA
                End If


                For i As Integer = 0 To ValuesToMove
                    DataW(i + l) = dataToWrite(i + FilePosition)
                Next
                End If

            Dim TNS As Integer = GetNextTNSNumber(255, MyObjectID)
                Dim TNSLowerByte As Integer = TNS And 255
                PAddress.InternallyRequested = InternalRequest
                PAddress.TargetNode = m_TargetNode
                PLCAddressByTNS(TNSLowerByte) = PAddress

                '************************************
                '* Is it a PLC5 Bit write? 08-MAY-12
                '************************************
                If MfgControl.AdvancedHMI.Drivers.PCCCAddress.IsPLC5(ProcessorType) And PAddress.BitNumber < 16 Then
                    FunctionNumber = &H26
                    ReDim DataW(8)
                    For i As Integer = 4 To 8
                        DataW(i - 4) = PAddress.ByteStream(i)
                    Next
                    'PAddress.ByteStream.CopyTo(DataW, 0)

                    If dataToWrite(0) <= 0 Then
                        '* Clear the bit
                        '* AND mask
                        DataW(DataW.Length - 4) = CByte(255 - (CInt(2 ^ (PAddress.BitNumber)) And &HFF))
                        DataW(DataW.Length - 3) = CByte(255 - (CInt(2 ^ (PAddress.BitNumber - 8))))
                        '* OR Mask
                        DataW(DataW.Length - 2) = 0
                        DataW(DataW.Length - 1) = 0
                    Else
                        '* Set the bit
                        '* AND Mask
                        DataW(DataW.Length - 4) = &HFF
                        DataW(DataW.Length - 3) = &HFF
                        '* OR Mask
                        DataW(DataW.Length - 2) = CByte(CInt(2 ^ (PAddress.BitNumber)) And &HFF)
                        DataW(DataW.Length - 1) = CByte(CInt(2 ^ (PAddress.BitNumber - 8)))
                    End If
                End If


                reply = PrefixAndSend(&HF, FunctionNumber, DataW, False, TNS)

                FilePosition += NumberOfBytesToWrite

                If PAddress.FileType <> &HA4 Then
                    '* Use subelement because it works with all data file types
                    PAddress.SubElement += CInt(NumberOfBytesToWrite / 2)
                Else
                    '* Special case file - 28h bytes per elements
                    PAddress.Element += CInt(NumberOfBytesToWrite / &H28)
                End If
        Loop

        If reply = 0 Then
            Return 0
        Else
            Throw New MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException(DecodeMessage(reply))
        End If
    End Function
#End Region
    'End of Public Methods
#End Region



#Region "Helper"
    '****************************************************
    '* Wait for a response from PLC before returning
    '****************************************************
    'Dim MaxTicks As Integer = 85  '* 50 ticks per second
    Friend MustOverride Function WaitForResponse(ByVal rTNS As Integer) As Integer

    '**************************************************************
    '* This method implements the common application routine
    '* as discussed in the Software Layer section of the AB manual
    '**************************************************************
    Friend MustOverride Function PrefixAndSend(ByVal Command As Byte, ByVal Func As Byte, ByVal data() As Byte, ByVal Wait As Boolean, ByVal TNS As Integer) As Integer
    Friend MustOverride Function GetNextTNSNumber(ByVal max As Integer, ByVal OwnerObjectID As Int64) As Integer

    '**************************************************************
    '* This method Sends a response from an unsolicited msg
    '**************************************************************
    'Private Function SendResponse(ByVal Command As Byte, ByVal rTNS As Integer) As Integer
    '    Dim PacketSize As Integer
    '    'PacketSize = Data.Length + 5
    '    PacketSize = 5
    '    PacketSize = 3    'Ethernet/IP Preparation


    '    Dim CommandPacke(PacketSize) As Byte
    '    Dim BytePos As Integer

    '    CommandPacke(1) = m_TargetNode
    '    CommandPacke(0) = m_MyNode
    '    BytePos = 2
    '    BytePos = 0

    '    CommandPacke(BytePos) = Command
    '    CommandPacke(BytePos + 1) = 0       '* STS (status, always 0)

    '    CommandPacke(BytePos + 2) = (rTNS And 255)
    '    CommandPacke(BytePos + 3) = (rTNS >> 8)


    '************************************************
    '* Convert the message code number into a string
    '* Ref Page 8-3
    '************************************************
    Public Shared Function DecodeMessage(ByVal msgNumber As Integer) As String
        Select Case msgNumber
            Case 0
                DecodeMessage = ""
            Case -2
                Return "Not Acknowledged (NAK)"
            Case -3
                Return "No Reponse, Check COM Settings"
            Case -4
                Return "Unknown Message from DataLink Layer"
            Case -5
                Return "Invalid Address"
            Case -6
                Return "Could Not Open Com Port"
            Case -7
                Return "No data specified to data link layer"
            Case -8
                Return "No data returned from PLC"
            Case -9
                Return "Failed To Open COM Port " '& DLL(MyDLLInstance).ComPort
            Case -20
                Return "No Data Returned"
            Case -21
                Return "Received Message NAKd from invalid checksum"

                '*** Errors coming from PLC
            Case 16
                Return "Illegal Command or Format, Address may not exist or not enough elements in data file"
            Case 32
                Return "PLC Has a Problem and Will Not Communicate"
            Case 48
                Return "Remote Node Host is Misssing, Disconnected, or Shut Down"
            Case 64
                Return "Host Could Not Complete Function Due To Hardware Fault"
            Case 80
                Return "Addressing problem or Memory Protect Rungs"
            Case 96
                Return "Function not allows due to command protection selection"
            Case 112
                Return "Processor is in Program mode"
            Case 128
                Return "Compatibility mode file missing or communication zone problem"
            Case 144
                Return "Remote node cannot buffer command"
            Case 240
                Return "Error code in EXT STS Byte"

                '* EXT STS Section - 256 is added to code to distinguish EXT codes
            Case 257
                Return "A field has an illegal value"
            Case 258
                Return "Less levels specified in address than minimum for any address"
            Case 259
                Return "More levels specified in address than system supports"
            Case 260
                Return "Symbol not found"
            Case 261
                Return "Symbol is of improper format"
            Case 262
                Return "Address doesn't point to something usable"
            Case 263
                Return "File is wrong size"
            Case 264
                Return "Cannot complete request, situation has changed since the start of the command"
            Case 265
                Return "Data or file is too large"
            Case 266
                Return "Transaction size plus word address is too large"
            Case 267
                Return "Access denied, improper priviledge"
            Case 268
                Return "Condition cannot be generated - resource is not available"
            Case 269
                Return "Condition already exists - resource is already available"
            Case 270
                Return "Command cannot be executed"

            Case Else
                Return "Unknown Message - " & msgNumber
        End Select
    End Function


    Friend Sub DataLinkLayer_DataReceived(ByVal sender As Object, ByVal e As MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs)
        If (e Is Nothing) Then Exit Sub
        '**************************************************************************
        '* If the parent form property (Synchronizing Object) is set, then sync the event with its thread
        '**************************************************************************
        'Dim TNSReturned As Integer '= sender.DataPacket(e.SequenceNumber)(4) + sender.DataPacket(e.SequenceNumber)(5) * 256
        Dim TNSLowerByte As Integer '= sender.DataPacket(e.SequenceNumber)(4)

        '* 23-SEP-12
        'TNSReturned = e.TransactionNumber
        TNSLowerByte = e.TransactionNumber And 255

        If PLCAddressByTNS(TNSLowerByte) Is Nothing Then
            Dim x = PLCAddressByTNS
            Exit Sub
        End If

        If e.OwnerObjectID <> MyObjectID Then
            Exit Sub
        End If

        'If TNS1.IsMyTNS(TNSReturned, MyObjectID) Then
        '    TNS1.ReleaseNumber(TNSReturned)
        'Else
        '    Exit Sub
        'End If


        DataPackets(TNSLowerByte) = New List(Of Byte)(e.RawData)

        '* If subscriptions disabled, then do not process anything coming back
        'If PLCAddressByTNS(TNSReturned) Is Nothing Then
        '    Exit Sub
        'End If
        'If m_DisableSubscriptions AndAlso PLCAddressByTNS(TNSReturned).InternallyRequested Then
        '    Exit Sub
        'End If


        '**************************************************************
        '* Only extract and send back if this response contained data
        '**************************************************************
        '* 25-APR-12 was ">", added = for PLC5
        Dim DataStart As Integer = 6
        ' If MfgControl.AdvancedHMI.Drivers.PCCCAddress.IsPLC5(ProcessorType) Then DataStart = 5
        'DataStart = 5
        Dim d() As String
        Dim ReturnedData(DataPackets(TNSLowerByte).Count - (DataStart + 1)) As Byte
        If e.RawData.Length > DataStart Then
            '***************************************************
            '* Extract returned data into appropriate data type
            '* Transfer block of data read to the data table array
            '***************************************************
            '* TODO: Check array bounds
            For i As Integer = 0 To DataPackets(TNSLowerByte).Count - (DataStart + 1)
                ReturnedData(i) = e.RawData(i + DataStart)
            Next

            d = ExtractData(PLCAddressByTNS(TNSLowerByte), ReturnedData)

            'Dim result As New MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs(d, PLCAddressByTNS(TNSLowerByte).PLCAddress, TNSReturned)
            For i = 0 To d.Length - 1
                e.Values.Add(d(i))
            Next
            Responses(TNSLowerByte) = e


            '* 27-APR-10 Make sure this corresponds to the target node requested from
            'If PLCAddressByTNS(TNSLowerByte).TargetNode = DataPackets(TNSLowerByte)(1) Then
            If Not PLCAddressByTNS(TNSLowerByte).InternallyRequested Then
                If Not DisableEvent Then
                    OnDataReceived(e)
                End If
            Else
                '*********************************************************
                '* Check to see if this is from the Polled variable list
                '*********************************************************
                '                    Dim ParsedAddress As ParsedDataAddress = ParseAddress(PLCAddressByTNS(TNSReturned).PLCAddress)
                Dim EnoughElements As Boolean
                For i As Integer = 0 To SubscriptionList.Count - 1
                    EnoughElements = False                    '* Are there enought elements read for this request
                    If (SubscriptionList(i).Element - PLCAddressByTNS(TNSLowerByte).Element + SubscriptionList(i).NumberOfElements <= d.Length) And _
                        (SubscriptionList(i).FileType <> 134 And SubscriptionList(i).FileType <> 135 And SubscriptionList(i).FileType <> &H8B And SubscriptionList(i).FileType <> &H8C) Then
                        EnoughElements = True
                    End If
                    If (SubscriptionList(i).BitNumber < 16) And ((SubscriptionList(i).Element - PLCAddressByTNS(TNSLowerByte).Element + SubscriptionList(i).NumberOfElements) / 16 <= d.Length) Then
                        EnoughElements = True
                    End If
                    If (SubscriptionList(i).FileType = 134 Or SubscriptionList(i).FileType = 135) And (SubscriptionList(i).Element - PLCAddressByTNS(TNSLowerByte).Element + SubscriptionList(i).NumberOfElements) <= d.Length Then
                        EnoughElements = True
                    End If
                    '* IO addresses - be sure not to cross elements/card slots
                    If (SubscriptionList(i).FileType = &H8B Or SubscriptionList(i).FileType = &H8C And _
                            SubscriptionList(i).Element = PLCAddressByTNS(TNSLowerByte).Element) Then
                        '* 03-MAY-12 Added check for bitnumber being 99, ReAdded 28-SEP-12
                        Dim WordToUse As Integer
                        If SubscriptionList(i).BitNumber = 99 Then
                            WordToUse = 0
                        Else
                            WordToUse = SubscriptionList(i).BitNumber >> 4
                        End If
                        If (d.Length - 1) >= (SubscriptionList(i).Element - PLCAddressByTNS(TNSLowerByte).Element + (WordToUse)) Then
                            EnoughElements = True
                        End If
                    End If


                    If SubscriptionList(i).FileNumber = PLCAddressByTNS(TNSLowerByte).FileNumber And _
                        EnoughElements And _
                        PLCAddressByTNS(TNSLowerByte).Element <= SubscriptionList(i).Element Then ' And _
                        '((PLCAddressByTNS(TNSReturned).FileType <> &H8B And PLCAddressByTNS(TNSReturned).FileType <> &H8C) Or PLCAddressByTNS(TNSReturned).BitNumber = SubscriptionList(i).BitNumber) Then
                        'SubscriptionList(i).BitNumber = PLCAddressByTNS(TNSReturned).BitNumber Then
                        'PolledValueReturned(PLCAddressByTNS(TNSReturned).PLCAddress, d)

                        Dim BitResult(SubscriptionList(i).NumberOfElements - 1) As String
                        '* Handle timers,counters, and R (control) as exceptions because of the 3 subelements
                        If (SubscriptionList(i).FileType = 134 Or SubscriptionList(i).FileType = 135 Or SubscriptionList(i).FileType = 136) Then
                            '* If this is a bit level address for a timer or counter, then handle appropriately
                            If SubscriptionList(i).BitNumber < 16 Then
                                Try
                                    '                                    If d((SubscriptionList(i).Element- PLCAddressByTNS(TNSReturned).Element) * 3) Then
                                    BitResult(0) = CStr((CInt(d((SubscriptionList(i).Element - PLCAddressByTNS(TNSLowerByte).Element) * 3)) And CInt(2 ^ SubscriptionList(i).BitNumber)) > 0)
                                    'End If
                                Catch ex As Exception
                                    MsgBox("Error in returning data from datareceived")
                                End Try
                            Else
                                Try
                                    For k As Integer = 0 To SubscriptionList(i).NumberOfElements - 1
                                        BitResult(k) = d((SubscriptionList(i).Element - PLCAddressByTNS(TNSLowerByte).Element + k) * 3 + SubscriptionList(i).SubElement)
                                    Next
                                Catch ex As Exception
                                    MsgBox("Error in returning data from datareceived")
                                End Try
                            End If

                            Dim x As New MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs(BitResult, SubscriptionList(i).PLCAddress, e.TransactionNumber)
                            x.SubscriptionID = SubscriptionList(i).ID
                            Dim z() As Object = {Me, x}
                            Try
                                SynchronizingObject.BeginInvoke(SubscriptionList(i).dlgCallBack, z)
                            Catch ex As InvalidOperationException
                                '* Ignore this error. It's caused when the handle is not created
                            End Try

                            'SynchronizingObject.BeginInvoke(SubscriptionList(i).dlgCallBack, New Object() {BitResult})
                        Else
                            '* If its bit level, then return the individual bit
                            If SubscriptionList(i).BitNumber < 99 Then
                                '*TODO : Make this handle a rquest for multiple bits
                                Try
                                    '* Test to see if bits or integers returned
                                    'Dim x As Integer
                                    Try
                                        'x = d(0)
                                        If SubscriptionList(i).BitNumber < 16 Then
                                            BitResult(0) = CStr((CInt(d(SubscriptionList(i).Element - PLCAddressByTNS(TNSLowerByte).Element)) And CInt(2 ^ SubscriptionList(i).BitNumber)) > 0)
                                        Else
                                            Dim WordToUse As Integer = SubscriptionList(i).BitNumber >> 4
                                            Dim ModifiedBitToUse As Integer = SubscriptionList(i).BitNumber Mod 16
                                            BitResult(0) = CStr((CInt(d(SubscriptionList(i).Element - PLCAddressByTNS(TNSLowerByte).Element + (WordToUse))) And CInt(2 ^ ModifiedBitToUse)) > 0)
                                        End If
                                    Catch ex As Exception
                                        BitResult(0) = d(0)
                                    End Try
                                Catch ex As Exception
                                    MsgBox("Error in returning data from datareceived - " & ex.Message)
                                End Try
                                Dim x As New MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs(BitResult, SubscriptionList(i).PLCAddress, e.TransactionNumber)
                                x.SubscriptionID = SubscriptionList(i).ID
                                Dim z() As Object = {Me, x}
                                SynchronizingObject.BeginInvoke(SubscriptionList(i).dlgCallBack, z)

                                'SynchronizingObject.BeginInvoke(SubscriptionList(i).dlgCallBack, New Object() {BitResult})
                            Else
                                '* All other data types
                                For k As Integer = 0 To SubscriptionList(i).NumberOfElements - 1
                                    BitResult(k) = d((SubscriptionList(i).Element - PLCAddressByTNS(TNSLowerByte).Element + k))
                                Next

                                Dim x As New MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs(BitResult, SubscriptionList(i).PLCAddress, e.TransactionNumber)
                                x.SubscriptionID = SubscriptionList(i).ID
                                Dim z() As Object = {Me, x}
                                SynchronizingObject.BeginInvoke(SubscriptionList(i).dlgCallBack, z)

                                'm_SynchronizingObject.BeginInvoke(SubscriptionList(i).dlgCallBack, d(SubscriptionList(i).PLCAddress.Element- PLCAddressByTNS(TNSReturned).Element))
                                'SynchronizingObject.BeginInvoke(SubscriptionList(i).dlgCallBack, New Object() {BitResult})
                            End If
                        End If

                        'SubscriptionList(k).LastValue = values(0)
                    End If
                Next
            End If
            'End If
            '* was an error code returned?
        Else 'If DataPackets(TNSLowerByte).Count >= 7 Then
            '* TODO: Check STS byte and handle for asynchronous
            If DataPackets(TNSLowerByte)(4) <> 0 Then
                'Responded(TNSLowerByte) = True
            End If
        End If

        PLCAddressByTNS(TNSLowerByte).Responded = True
        'Responded(TNSLowerByte) = True
    End Sub

    '******************************************************************
    '* This is called when a message instruction was sent from the PLC
    '******************************************************************
    Private Sub DF1DataLink1_UnsolictedMessageRcvd()
        If SynchronizingObject IsNot Nothing Then
            Dim Parameters() As Object = {Me, EventArgs.Empty}
            SynchronizingObject.BeginInvoke(drsd, Parameters)
        Else
            RaiseEvent UnsolictedMessageRcvd(Me, System.EventArgs.Empty)
        End If
    End Sub

    Protected Overridable Sub OnDataReceived(ByVal e As EventArgs)
        If SynchronizingObject IsNot Nothing Then
            Dim Parameters() As Object = {Me, e}
            SynchronizingObject.BeginInvoke(drsd, Parameters)
        Else
            'RaiseEvent DataReceived(Me, System.EventArgs.Empty)
            RaiseEvent DataReceived(Me, e)
        End If
    End Sub


    '****************************************************************************
    '* This is required to sync the event back to the parent form's main thread
    '****************************************************************************
    Dim drsd As EventHandler = AddressOf DataReceivedSync
    'Private Sub DataReceivedSync(ByVal sender As Object, ByVal e As MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs)
    Private Sub DataReceivedSync(ByVal sender As Object, ByVal e As EventArgs)
        RaiseEvent DataReceived(sender, e)
    End Sub
    Private Sub UnsolictedMessageRcvdSync(ByVal sender As Object, ByVal e As EventArgs)
        RaiseEvent UnsolictedMessageRcvd(sender, e)
    End Sub

    Protected Sub CIPConnectionEstablished(ByVal sender As Object, e As EventArgs)
        RaiseEvent ConnectionEstablished(Me, e)
    End Sub
#End Region

End Class



'*********************************************************************************
'* This is used for linking a notification.
'* An object can request a continuous poll and get a callback when value updated
'*********************************************************************************
Friend Class PCCCSubscription
    Inherits MfgControl.AdvancedHMI.Drivers.PCCCAddress

#Region "Constructors"
    '* 22-NOV-12 changed to shared
    Private Shared CurrentID As Integer
    Public Sub New()
        CurrentID += 1
        m_ID = CurrentID
    End Sub

    Public Sub New(ByVal PLCAddress As String, ByVal NumberOfElements As Integer, ByVal ProcessorType As Integer)
        MyBase.New(PLCAddress, NumberOfElements, ProcessorType)
        '* 22-NOV-12 added
        CurrentID += 1
        m_ID = CurrentID
    End Sub
#End Region

#Region "Properties"
    Public Property dlgCallBack As EventHandler(Of MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs)
    Public Property PollRate As Integer

    Private m_ID As Integer
    Public ReadOnly Property ID As Integer
        Get
            Return m_ID
        End Get
    End Property

    'Public Property ElementsToRead As Integer
#End Region
End Class

