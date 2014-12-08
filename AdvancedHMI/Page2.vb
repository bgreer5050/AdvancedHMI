Public Class Page2


    '*******************************************************************************
    '* Stop polling when the form is not visible in order to reduce communications
    '*******************************************************************************
    Private Sub Form_VisibleChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.VisibleChanged
        If components IsNot Nothing Then
            Dim drv As AdvancedHMIDrivers.IComComponent
            '*****************************
            '* Search for comm components
            '*****************************
            For i As Integer = 0 To components.Components.Count - 1
                If components.Components(i).GetType.GetInterface("AdvancedHMIDrivers.IComComponent") IsNot Nothing Then
                    drv = components.Components.Item(i)
                    '* Stop/Start polling based on form visibility
                    drv.DisableSubscriptions = Not Me.Visible
                End If
            Next
        End If
    End Sub


    Private Sub ReturnToMainButton_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click
        MainForm.Show()
        Me.Hide()
    End Sub
End Class