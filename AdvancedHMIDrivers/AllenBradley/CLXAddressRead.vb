Option Strict On
Public Class CLXAddressRead
    Inherits MfgControl.AdvancedHMI.Drivers.CLXAddress

#Region "Properties"
    Private m_TransactionNumber As UInt16
    Public Property TransactionNumber As UInt16
        Get
            Return m_TransactionNumber
        End Get
        Set(value As UInt16)
            m_TransactionNumber = value
        End Set
    End Property

    Private m_Responded As Boolean
    Public Property Responded As Boolean
        Get
            Return m_Responded
        End Get
        Set(value As Boolean)
            m_Responded = value
        End Set
    End Property
#End Region

#Region "Constructors"
    Public Sub New()
        MyBase.new()
    End Sub

    Public Sub New(ByVal tagName As String)
        MyClass.new()
        Me.TagName = tagName
    End Sub


    Public Sub New(ByVal tagName As String, ByVal transactionNumber As UInt16)
        MyClass.New(tagName)
        m_TransactionNumber = transactionNumber
    End Sub
#End Region
End Class

