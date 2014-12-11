<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class MainForm
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    ' <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(MainForm))
        Me.PilotLight1 = New AdvancedHMIControls.PilotLight()
        Me.DF1Comm1 = New AdvancedHMIDrivers.DF1Comm(Me.components)
        Me.SuspendLayout()
        '
        'PilotLight1
        '
        Me.PilotLight1.CommComponent = Me.DF1Comm1
        Me.PilotLight1.ForeColor = System.Drawing.Color.Black
        Me.PilotLight1.LegendPlate = MfgControl.AdvancedHMI.Controls.PilotLight.LegendPlates.Large
        Me.PilotLight1.LightColor = MfgControl.AdvancedHMI.Controls.PilotLight.LightColors.Green
        Me.PilotLight1.LightColorOff = MfgControl.AdvancedHMI.Controls.PilotLight.LightColors.White
        Me.PilotLight1.Location = New System.Drawing.Point(383, 257)
        Me.PilotLight1.Name = "PilotLight1"
        Me.PilotLight1.OutputType = MfgControl.AdvancedHMI.Controls.OutputType.MomentarySet
        Me.PilotLight1.PLCAddressClick = ""
        Me.PilotLight1.PLCAddressText = ""
        Me.PilotLight1.PLCAddressValue = "I1:3"
        Me.PilotLight1.PLCAddressVisible = ""
        Me.PilotLight1.Size = New System.Drawing.Size(75, 110)
        Me.PilotLight1.TabIndex = 45
        Me.PilotLight1.Text = "Running"
        Me.PilotLight1.Value = False
        '
        'DF1Comm1
        '
        Me.DF1Comm1.BaudRate = "AUTO"
        Me.DF1Comm1.CheckSumType = MfgControl.AdvancedHMI.Drivers.DF1DataLinkLayer.ChecksumOptions.Bcc
        Me.DF1Comm1.ComPort = "COM6"
        Me.DF1Comm1.DisableSubscriptions = False
        Me.DF1Comm1.MyNode = 0
        Me.DF1Comm1.Parity = System.IO.Ports.Parity.None
        Me.DF1Comm1.PollRateOverride = 0
        Me.DF1Comm1.SynchronizingObject = Me
        Me.DF1Comm1.TargetNode = 0
        '
        'MainForm
        '
        Me.AutoScroll = True
        Me.BackColor = System.Drawing.Color.Black
        Me.ClientSize = New System.Drawing.Size(784, 562)
        Me.Controls.Add(Me.PilotLight1)
        Me.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.ForeColor = System.Drawing.Color.White
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.Name = "MainForm"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.Manual
        Me.Text = "SCADA M1188"
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents PilotLight1 As AdvancedHMIControls.PilotLight
    Friend WithEvents DF1Comm1 As AdvancedHMIDrivers.DF1Comm
End Class
