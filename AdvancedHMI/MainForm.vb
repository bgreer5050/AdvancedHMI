Public Class MainForm

     '*******************************************************************************
    '* Stop polling when the form is not visible in order to reduce communications
    '* Copy this section of code to every new form created
    '*******************************************************************************
    Private Sub Form_VisibleChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.VisibleChanged
        If components IsNot Nothing Then
            Dim drv As AdvancedHMIDrivers.IComComponent
            '*****************************
            '* Search for comm components
            '*****************************
            For i As Integer = 0 To components.Components.Count - 1
                If components.Components(i).GetType.GetInterface("AdvancedHMIDrivers.IComComponent") IsNot Nothing Then
                    '* 13-JUL-14 changed to directcast
                    drv = DirectCast(components.Components.Item(i), AdvancedHMIDrivers.IComComponent)
                    'drv = components.Components.Item(i)
                    '* Stop/Start polling based on form visibility
                    drv.DisableSubscriptions = Not Me.Visible
                End If
            Next
        End If
    End Sub

    '************************************************************
    '* This will guarantee that even hidden forms are all closed
    '* when the main application is closed
    '************************************************************
    'Private Sub DemoForm_FormClosed(ByVal sender As Object, ByVal e As System.Windows.Forms.FormClosedEventArgs) Handles Me.FormClosed
    '    Environment.Exit(0)
    'End Sub


    '**************************************
    '* Filling the form with a gradient
    '**************************************
    Private Sub MainForm_Paint(sender As Object, e As System.Windows.Forms.PaintEventArgs) Handles Me.Paint
        'Dim rect As New System.Drawing.Rectangle(0, 0, e.ClipRectangle.Width, e.ClipRectangle.Height)
        'Dim gradientBrush As New Drawing.Drawing2D.LinearGradientBrush(New Point(0, 0), New Point(0, Height), System.Drawing.Color.FromArgb(180, 100, 200), System.Drawing.Color.FromArgb(110, 200, 255))
        'e.Graphics.FillRectangle(gradientBrush, rect)
    End Sub

    Private Sub FormChangeButton1_Click(sender As System.Object, e As System.EventArgs)
        Page2.Show()
    End Sub

    Private Sub PilotLight3_Click(sender As Object, e As EventArgs)

    End Sub

    Private Sub PilotLight2_Click(sender As Object, e As EventArgs)

    End Sub

    Private Sub PilotLight1_Click(sender As Object, e As EventArgs)
        MessageBox.Show("ERROR")
    End Sub

   
  
    Private Sub MainForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
    End Sub
End Class

