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
        Me.QuickStartLabel = New System.Windows.Forms.Label()
        Me.EthernetIPforPLCSLCMicroCom1 = New AdvancedHMIDrivers.EthernetIPforPLCSLCMicroCom(Me.components)
        Me.DigitalPanelMeter1 = New AdvancedHMIControls.DigitalPanelMeter()
        Me.PictureBox1 = New System.Windows.Forms.PictureBox()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.PilotLight1 = New AdvancedHMIControls.PilotLight()
        Me.PilotLight2 = New AdvancedHMIControls.PilotLight()
        Me.PilotLight3 = New AdvancedHMIControls.PilotLight()
        CType(Me.PictureBox1, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'QuickStartLabel
        '
        Me.QuickStartLabel.AutoSize = True
        Me.QuickStartLabel.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.0!, System.Drawing.FontStyle.Bold)
        Me.QuickStartLabel.ForeColor = System.Drawing.Color.White
        Me.QuickStartLabel.Location = New System.Drawing.Point(12, 9)
        Me.QuickStartLabel.Name = "QuickStartLabel"
        Me.QuickStartLabel.Size = New System.Drawing.Size(273, 104)
        Me.QuickStartLabel.TabIndex = 38
        Me.QuickStartLabel.Text = resources.GetString("QuickStartLabel.Text")
        Me.QuickStartLabel.Visible = False
        '
        'EthernetIPforPLCSLCMicroCom1
        '
        Me.EthernetIPforPLCSLCMicroCom1.DisableSubscriptions = False
        Me.EthernetIPforPLCSLCMicroCom1.IPAddress = "10.0.201.29"
        Me.EthernetIPforPLCSLCMicroCom1.MyNode = 0
        Me.EthernetIPforPLCSLCMicroCom1.PollRateOverride = 0
        Me.EthernetIPforPLCSLCMicroCom1.Port = 44818
        Me.EthernetIPforPLCSLCMicroCom1.SynchronizingObject = Me
        Me.EthernetIPforPLCSLCMicroCom1.TargetNode = 0
        '
        'DigitalPanelMeter1
        '
        Me.DigitalPanelMeter1.CommComponent = Me.EthernetIPforPLCSLCMicroCom1
        Me.DigitalPanelMeter1.DecimalPosition = 0
        Me.DigitalPanelMeter1.ForeColor = System.Drawing.Color.LightGray
        Me.DigitalPanelMeter1.KeypadFontColor = System.Drawing.Color.WhiteSmoke
        Me.DigitalPanelMeter1.KeypadMaxValue = 0.0R
        Me.DigitalPanelMeter1.KeypadMinValue = 0.0R
        Me.DigitalPanelMeter1.KeypadScaleFactor = 1.0R
        Me.DigitalPanelMeter1.KeypadText = Nothing
        Me.DigitalPanelMeter1.KeypadWidth = 300
        Me.DigitalPanelMeter1.Location = New System.Drawing.Point(191, 157)
        Me.DigitalPanelMeter1.Name = "DigitalPanelMeter1"
        Me.DigitalPanelMeter1.NumberOfDigits = 5
        Me.DigitalPanelMeter1.PLCAddressKeypad = ""
        Me.DigitalPanelMeter1.PLCAddressText = "N7:1"
        Me.DigitalPanelMeter1.PLCAddressValue = "N7:1"
        Me.DigitalPanelMeter1.PLCAddressVisible = ""
        Me.DigitalPanelMeter1.Resolution = New Decimal(New Integer() {1, 0, 0, 0})
        Me.DigitalPanelMeter1.Size = New System.Drawing.Size(336, 146)
        Me.DigitalPanelMeter1.TabIndex = 43
        Me.DigitalPanelMeter1.Text = "N7:1"
        Me.DigitalPanelMeter1.Value = 0.0R
        Me.DigitalPanelMeter1.ValueScaleFactor = New Decimal(New Integer() {1, 0, 0, 0})
        Me.DigitalPanelMeter1.ValueScaleOffset = New Decimal(New Integer() {0, 0, 0, 0})
        '
        'PictureBox1
        '
        Me.PictureBox1.BackgroundImage = Global.MfgControl.AdvancedHMI.My.Resources.Resources.AdvancedHMILogoBR
        Me.PictureBox1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom
        Me.PictureBox1.Location = New System.Drawing.Point(450, 12)
        Me.PictureBox1.Name = "PictureBox1"
        Me.PictureBox1.Size = New System.Drawing.Size(322, 47)
        Me.PictureBox1.TabIndex = 42
        Me.PictureBox1.TabStop = False
        '
        'Label1
        '
        Me.Label1.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.Label1.BackColor = System.Drawing.Color.Transparent
        Me.Label1.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.0!)
        Me.Label1.ForeColor = System.Drawing.Color.FromArgb(CType(CType(64, Byte), Integer), CType(CType(64, Byte), Integer), CType(CType(64, Byte), Integer))
        Me.Label1.Location = New System.Drawing.Point(12, 521)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(194, 32)
        Me.Label1.TabIndex = 3
        Me.Label1.Text = "For Development Source Code Visit" & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "http://www.advancedhmi.com"
        Me.Label1.TextAlign = System.Drawing.ContentAlignment.TopCenter
        '
        'PilotLight1
        '
        Me.PilotLight1.CommComponent = Me.EthernetIPforPLCSLCMicroCom1
        Me.PilotLight1.LegendPlate = MfgControl.AdvancedHMI.Controls.PilotLight.LegendPlates.Large
        Me.PilotLight1.LightColor = MfgControl.AdvancedHMI.Controls.PilotLight.LightColors.Green
        Me.PilotLight1.LightColorOff = MfgControl.AdvancedHMI.Controls.PilotLight.LightColors.White
        Me.PilotLight1.Location = New System.Drawing.Point(210, 373)
        Me.PilotLight1.Name = "PilotLight1"
        Me.PilotLight1.OutputType = MfgControl.AdvancedHMI.Controls.OutputType.MomentarySet
        Me.PilotLight1.PLCAddressClick = ""
        Me.PilotLight1.PLCAddressText = ""
        Me.PilotLight1.PLCAddressValue = ""
        Me.PilotLight1.PLCAddressVisible = ""
        Me.PilotLight1.Size = New System.Drawing.Size(75, 110)
        Me.PilotLight1.TabIndex = 45
        Me.PilotLight1.Text = "PilotLight1"
        Me.PilotLight1.Value = False
        '
        'PilotLight2
        '
        Me.PilotLight2.CommComponent = Me.EthernetIPforPLCSLCMicroCom1
        Me.PilotLight2.LegendPlate = MfgControl.AdvancedHMI.Controls.PilotLight.LegendPlates.Large
        Me.PilotLight2.LightColor = MfgControl.AdvancedHMI.Controls.PilotLight.LightColors.Green
        Me.PilotLight2.LightColorOff = MfgControl.AdvancedHMI.Controls.PilotLight.LightColors.White
        Me.PilotLight2.Location = New System.Drawing.Point(322, 373)
        Me.PilotLight2.Name = "PilotLight2"
        Me.PilotLight2.OutputType = MfgControl.AdvancedHMI.Controls.OutputType.MomentarySet
        Me.PilotLight2.PLCAddressClick = ""
        Me.PilotLight2.PLCAddressText = ""
        Me.PilotLight2.PLCAddressValue = ""
        Me.PilotLight2.PLCAddressVisible = ""
        Me.PilotLight2.Size = New System.Drawing.Size(75, 110)
        Me.PilotLight2.TabIndex = 46
        Me.PilotLight2.Text = "PilotLight2"
        Me.PilotLight2.Value = False
        '
        'PilotLight3
        '
        Me.PilotLight3.CommComponent = Me.EthernetIPforPLCSLCMicroCom1
        Me.PilotLight3.LegendPlate = MfgControl.AdvancedHMI.Controls.PilotLight.LegendPlates.Large
        Me.PilotLight3.LightColor = MfgControl.AdvancedHMI.Controls.PilotLight.LightColors.Green
        Me.PilotLight3.LightColorOff = MfgControl.AdvancedHMI.Controls.PilotLight.LightColors.White
        Me.PilotLight3.Location = New System.Drawing.Point(439, 373)
        Me.PilotLight3.Name = "PilotLight3"
        Me.PilotLight3.OutputType = MfgControl.AdvancedHMI.Controls.OutputType.MomentarySet
        Me.PilotLight3.PLCAddressClick = ""
        Me.PilotLight3.PLCAddressText = ""
        Me.PilotLight3.PLCAddressValue = ""
        Me.PilotLight3.PLCAddressVisible = ""
        Me.PilotLight3.Size = New System.Drawing.Size(75, 110)
        Me.PilotLight3.TabIndex = 47
        Me.PilotLight3.Text = "PilotLight3"
        Me.PilotLight3.Value = False
        '
        'MainForm
        '
        Me.AutoScroll = True
        Me.BackColor = System.Drawing.Color.Black
        Me.ClientSize = New System.Drawing.Size(784, 562)
        Me.Controls.Add(Me.PilotLight3)
        Me.Controls.Add(Me.PilotLight2)
        Me.Controls.Add(Me.PilotLight1)
        Me.Controls.Add(Me.DigitalPanelMeter1)
        Me.Controls.Add(Me.PictureBox1)
        Me.Controls.Add(Me.QuickStartLabel)
        Me.Controls.Add(Me.Label1)
        Me.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.ForeColor = System.Drawing.Color.White
        Me.Name = "MainForm"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.Manual
        Me.Text = "AdvancedHMI v3.88"
        CType(Me.PictureBox1, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents QuickStartLabel As System.Windows.Forms.Label
    Friend WithEvents EthernetIPforPLCSLCMicroCom1 As AdvancedHMIDrivers.EthernetIPforPLCSLCMicroCom
    Friend WithEvents DigitalPanelMeter1 As AdvancedHMIControls.DigitalPanelMeter
    Friend WithEvents PictureBox1 As System.Windows.Forms.PictureBox
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents PilotLight3 As AdvancedHMIControls.PilotLight
    Friend WithEvents PilotLight2 As AdvancedHMIControls.PilotLight
    Friend WithEvents PilotLight1 As AdvancedHMIControls.PilotLight
End Class
