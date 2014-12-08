Public Class SubscriptionDetail
    Private m_PLCAddress As String
    Public Property PLCAddress As String
        Get
            Return m_PLCAddress
        End Get
        Set(value As String)
            m_PLCAddress = value
        End Set
    End Property

    Private m_NotificationID As Integer
    Public Property NotificationID As Integer
        Get
            Return m_NotificationID
        End Get
        Set(value As Integer)
            m_NotificationID = value
        End Set
    End Property

    Private m_Callback As EventHandler(Of SubscriptionHandlerEventArgs)
    Public Property CallBack As EventHandler(Of SubscriptionHandlerEventArgs)
        Get
            Return m_Callback
        End Get
        Set(value As EventHandler(Of SubscriptionHandlerEventArgs))
            m_Callback = value
        End Set
    End Property

    Private m_PropertyNameToSet As String
    Public Property PropertyNameToSet As String
        Get
            Return m_PropertyNameToSet
        End Get
        Set(value As String)
            m_PropertyNameToSet = value
        End Set
    End Property

    Private m_Invert As Boolean
    Public Property Invert As Boolean
        Get
            Return m_Invert
        End Get
        Set(value As Boolean)
            m_Invert = value
        End Set
    End Property

    Private m_SuccessfullySubscribed As Boolean
    Public Property SuccessfullySubscribed As Boolean
        Get
            Return m_SuccessfullySubscribed
        End Get
        Set(value As Boolean)
            m_SuccessfullySubscribed = value
        End Set
    End Property

    Public Sub New()
    End Sub

    Public Sub New(ByVal plcAddress As String, ByVal callback As EventHandler(Of SubscriptionHandlerEventArgs))
        m_PLCAddress = plcAddress
        m_Callback = callback
    End Sub


    Public Sub New(ByVal plcAddress As String, ByVal notificationID As Integer, ByVal callback As EventHandler(Of SubscriptionHandlerEventArgs))
        m_PLCAddress = plcAddress
        m_NotificationID = notificationID
        m_Callback = callback
    End Sub

    Public Sub New(ByVal plcAddress As String, ByVal notificationID As Integer, ByVal callback As EventHandler(Of SubscriptionHandlerEventArgs), ByVal propertyNameToSet As String)
        m_PLCAddress = plcAddress
        m_NotificationID = notificationID
        m_Callback = callback
        m_PropertyNameToSet = propertyNameToSet
    End Sub

    Public Sub New(ByVal plcAddress As String, ByVal notificationID As Integer, ByVal callback As EventHandler(Of SubscriptionHandlerEventArgs), ByVal propertyNameToSet As String, invert As Boolean)
        m_PLCAddress = plcAddress
        m_NotificationID = notificationID
        m_Callback = callback
        m_PropertyNameToSet = propertyNameToSet
        m_Invert = invert
    End Sub

End Class
