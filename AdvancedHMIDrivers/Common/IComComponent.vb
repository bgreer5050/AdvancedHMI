Public Interface IComComponent
    Delegate Sub ReturnValues(ByVal values As String())
    Function Subscribe(ByVal plcAddress As String, ByVal numberOfElements As Int16, ByVal pollRate As Integer, ByVal callback As EventHandler(Of MfgControl.AdvancedHMI.Drivers.Common.PlcComEventArgs)) As Integer
    Function Unsubscribe(ByVal id As Integer) As Integer
    Function BeginRead(ByVal startAddress As String, ByVal numberOfElements As Integer) As Integer
    Function Read(ByVal startAddress As String, ByVal numberOfElements As Integer) As String()
    Function Write(ByVal startAddress As String, ByVal dataToWrite As String) As String
    Property DisableSubscriptions() As Boolean
End Interface
