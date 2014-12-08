Public Class SubscriptionHandler
    Implements IDisposable

    Private SubscriptionList As New List(Of SubscriptionDetail)

    Public Event DisplayError As EventHandler(Of MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs)

#Region "Properties"
    '*****************************************************
    '* Property - Component to communicate to PLC through
    '*****************************************************
    Private m_CommComponent As AdvancedHMIDrivers.IComComponent
    Public Property CommComponent() As AdvancedHMIDrivers.IComComponent
        Get
            Return m_CommComponent
        End Get
        Set(ByVal value As AdvancedHMIDrivers.IComComponent)
            m_CommComponent = value
        End Set
    End Property
#End Region


#Region "Constructor/Destructor"
    Public Sub dispose() Implements System.IDisposable.Dispose
        dispose(True)
    End Sub

    '****************************************************************
    '* Control overrides dispose to clean up the component list.
    '****************************************************************
    Protected Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing Then
                If m_CommComponent IsNot Nothing Then
                    '* Unsubscribe from all
                    For i As Integer = 0 To SubscriptionList.Count - 1
                        m_CommComponent.Unsubscribe(SubscriptionList(i).NotificationID)
                    Next
                    SubscriptionList.Clear()
                End If
            End If
        Catch ex As Exception
        End Try
    End Sub
#End Region


    '******************************************************
    '* Attempt to create a subscription to the PLC driver
    '******************************************************
    Public Sub SubscribeTo(ByVal PLCAddress As String, ByVal callBack As EventHandler(Of SubscriptionHandlerEventArgs))
        SubscribeTo(PLCAddress, callBack, "")
    End Sub

    Public Sub SubscribeTo(ByVal PLCAddress As String, ByVal callBack As EventHandler(Of SubscriptionHandlerEventArgs), ByVal propertyName As String)
        '* Check to see if the subscription has already been created
        Dim index As Integer
        While index < SubscriptionList.Count AndAlso (SubscriptionList(index).CallBack <> callBack Or SubscriptionList(index).PLCAddress <> PLCAddress)
            index += 1
        End While

        '* Already subscribed and PLCAddress was changed, so unsubscribe
        If (index < SubscriptionList.Count) AndAlso SubscriptionList(index).PLCAddress <> PLCAddress Then
            m_CommComponent.Unsubscribe(SubscriptionList(index).NotificationID)
            SubscriptionList.RemoveAt(index)
        End If

        '* Is there an address to subscribe to?
        If PLCAddress <> "" Then
            Try
                If m_CommComponent IsNot Nothing Then
                    '* If subscription succeedded, save the subscription details
                    Dim temp As New SubscriptionDetail(PLCAddress, callBack)
                    temp.PropertyNameToSet = propertyName
                    If PLCAddress.ToUpper.IndexOf("NOT ") = 0 Then
                        temp.Invert = True
                    End If
                    SubscriptionList.Add(temp)
                    InitializeTryTimer(500)
                Else
                    OnDisplayError("CommComponent Property not set")
                End If
            Catch ex As MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException
                '* If subscribe fails, set up for retry
                InitializeSubscribeTry(ex, PLCAddress)
            End Try
        End If
    End Sub

    Public Sub UnsubscribeAll()
        If m_CommComponent IsNot Nothing Then
            For Each Subscript In SubscriptionList
                m_CommComponent.Unsubscribe(Subscript.NotificationID)
            Next
        End If
    End Sub

    Private Sub SubscribeToComDriver()
        If Not m_CommComponent.DisableSubscriptions Then
            For Each Subscript In SubscriptionList
                If Not Subscript.SuccessfullySubscribed Then
                    Dim address As String = Subscript.PLCAddress
                    If Subscript.Invert Then
                        address = Subscript.PLCAddress.Substring(4)
                    End If

                    Try
                        Dim NotificationID As Integer = m_CommComponent.Subscribe(address, 1, 250, AddressOf SubscribedDataReturned)
                        Subscript.NotificationID = NotificationID
                        Subscript.SuccessfullySubscribed = True
                    Catch ex As Exception
                        OnDisplayError(ex.Message)
                        InitializeSubscribeTry(ex, Subscript.PLCAddress)
                    End Try
                End If
            Next
        Else
            InitializeTryTimer(500)
        End If
    End Sub

    'Delegate Sub CallBack(ByVal sender As Object, ByVal e As MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs)

    Private Sub SubscribedDataReturned(ByVal sender As Object, ByVal e As MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs)
        For Each Subscript In SubscriptionList
            Dim address As String = Subscript.PLCAddress
            If Subscript.Invert Then
                address = Subscript.PLCAddress.Substring(4)
            End If

            If e.PlcAddress Is Nothing OrElse (String.Compare(address, e.PlcAddress, True) = 0) Then
                If Subscript.Invert Then
                    'Dim e2 As New MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs(e.ErrorId, e.ErrorMessage, e.TransactionNumber, e.OwnerObjectID)
                    'e2 = e.Clone
                    Try
                        Dim x As New System.Collections.ObjectModel.Collection(Of String)

                        Dim s As String = (CStr(Not CBool(e.Values(0))))
                        x.Add(s)
                        e.Values = x
                    Catch ex As Exception
                        Dim dbg = 0
                    End Try
                End If
                Dim a As New SubscriptionHandlerEventArgs
                a.PLCComEventArgs = e
                a.SubscriptionDetail = Subscript

                Subscript.CallBack.Invoke(sender, a)
            End If
        Next
    End Sub

    '********************************************
    '* Show the error and start the retry time
    '********************************************
    Private Sub InitializeSubscribeTry(ByVal ex As MfgControl.AdvancedHMI.Drivers.Common.PLCDriverException, ByVal PLCAddress As String)
        '* Error 1808 is from TwinCAT ADS
        If ex.ErrorCode = 1808 Then
            OnDisplayError("""" & PLCAddress & """ PLC Address not found")
        Else
            OnDisplayError(ex.Message)
        End If

        InitializeTryTimer(10000)
    End Sub

    Private Sub InitializeTryTimer(ByVal interval As Integer)
        If SubscribeTryTimer Is Nothing Then
            SubscribeTryTimer = New Windows.Forms.Timer
            SubscribeTryTimer.Interval = interval
            AddHandler SubscribeTryTimer.Tick, AddressOf SubscribeTry_Tick
        End If

        SubscribeTryTimer.Enabled = True
    End Sub



    '********************************************
    '* Keep retrying to subscribe if it failed
    '********************************************
    Private SubscribeTryTimer As Windows.Forms.Timer
    Private Sub SubscribeTry_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs)
        SubscribeTryTimer.Enabled = False
        SubscribeTryTimer.Dispose()
        SubscribeTryTimer = Nothing

        SubscribeToComDriver()
    End Sub


    Protected Overridable Sub OnDisplayError(ByVal msg As String)
        Dim e As New MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs(0, msg)

        RaiseEvent DisplayError(Me, e)
    End Sub

End Class
