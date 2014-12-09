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
        Me.EthernetIPforPLCSLCMicroCom1 = New AdvancedHMIDrivers.EthernetIPforPLCSLCMicroCom(Me.components)
        Me.DigitalPanelMeter1 = New AdvancedHMIControls.DigitalPanelMeter()
        Me.PilotLight1 = New AdvancedHMIControls.PilotLight()
        Me.PilotLight2 = New AdvancedHMIControls.PilotLight()
        Me.PilotLight3 = New AdvancedHMIControls.PilotLight()
        Me.DigitalPanelMeter2 = New AdvancedHMIControls.DigitalPanelMeter()
        Me.BasicButton1 = New AdvancedHMIControls.BasicButton()
        Me.BasicButton2 = New AdvancedHMIControls.BasicButton()
        Me.BasicButton3 = New AdvancedHMIControls.BasicButton()
        Me.BasicButton4 = New AdvancedHMIControls.BasicButton()
        Me.BasicButton5 = New AdvancedHMIControls.BasicButton()
        Me.BasicButton6 = New AdvancedHMIControls.BasicButton()
        Me.BasicButton7 = New AdvancedHMIControls.BasicButton()
        Me.BasicButton8 = New AdvancedHMIControls.BasicButton()
        Me.BasicButton9 = New AdvancedHMIControls.BasicButton()
        Me.BasicButton10 = New AdvancedHMIControls.BasicButton()
        Me.BasicButton11 = New AdvancedHMIControls.BasicButton()
        Me.BasicButton12 = New AdvancedHMIControls.BasicButton()
        Me.SuspendLayout()
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
        Me.DigitalPanelMeter1.Location = New System.Drawing.Point(116, 73)
        Me.DigitalPanelMeter1.Name = "DigitalPanelMeter1"
        Me.DigitalPanelMeter1.NumberOfDigits = 5
        Me.DigitalPanelMeter1.PLCAddressKeypad = ""
        Me.DigitalPanelMeter1.PLCAddressText = "N7:1"
        Me.DigitalPanelMeter1.PLCAddressValue = "N7:1"
        Me.DigitalPanelMeter1.PLCAddressVisible = ""
        Me.DigitalPanelMeter1.Resolution = New Decimal(New Integer() {1, 0, 0, 0})
        Me.DigitalPanelMeter1.Size = New System.Drawing.Size(261, 113)
        Me.DigitalPanelMeter1.TabIndex = 43
        Me.DigitalPanelMeter1.Text = "Run Minutes"
        Me.DigitalPanelMeter1.Value = 0.0R
        Me.DigitalPanelMeter1.ValueScaleFactor = New Decimal(New Integer() {1, 0, 0, 0})
        Me.DigitalPanelMeter1.ValueScaleOffset = New Decimal(New Integer() {0, 0, 0, 0})
        '
        'PilotLight1
        '
        Me.PilotLight1.CommComponent = Me.EthernetIPforPLCSLCMicroCom1
        Me.PilotLight1.ForeColor = System.Drawing.Color.Black
        Me.PilotLight1.LegendPlate = MfgControl.AdvancedHMI.Controls.PilotLight.LegendPlates.Large
        Me.PilotLight1.LightColor = MfgControl.AdvancedHMI.Controls.PilotLight.LightColors.Green
        Me.PilotLight1.LightColorOff = MfgControl.AdvancedHMI.Controls.PilotLight.LightColors.White
        Me.PilotLight1.Location = New System.Drawing.Point(241, 206)
        Me.PilotLight1.Name = "PilotLight1"
        Me.PilotLight1.OutputType = MfgControl.AdvancedHMI.Controls.OutputType.MomentarySet
        Me.PilotLight1.PLCAddressClick = ""
        Me.PilotLight1.PLCAddressText = ""
        Me.PilotLight1.PLCAddressValue = ""
        Me.PilotLight1.PLCAddressVisible = ""
        Me.PilotLight1.Size = New System.Drawing.Size(75, 110)
        Me.PilotLight1.TabIndex = 45
        Me.PilotLight1.Text = "Running"
        Me.PilotLight1.Value = False
        '
        'PilotLight2
        '
        Me.PilotLight2.CommComponent = Me.EthernetIPforPLCSLCMicroCom1
        Me.PilotLight2.ForeColor = System.Drawing.Color.Black
        Me.PilotLight2.LegendPlate = MfgControl.AdvancedHMI.Controls.PilotLight.LegendPlates.Large
        Me.PilotLight2.LightColor = MfgControl.AdvancedHMI.Controls.PilotLight.LightColors.Green
        Me.PilotLight2.LightColorOff = MfgControl.AdvancedHMI.Controls.PilotLight.LightColors.White
        Me.PilotLight2.Location = New System.Drawing.Point(353, 206)
        Me.PilotLight2.Name = "PilotLight2"
        Me.PilotLight2.OutputType = MfgControl.AdvancedHMI.Controls.OutputType.MomentarySet
        Me.PilotLight2.PLCAddressClick = ""
        Me.PilotLight2.PLCAddressText = ""
        Me.PilotLight2.PLCAddressValue = ""
        Me.PilotLight2.PLCAddressVisible = ""
        Me.PilotLight2.Size = New System.Drawing.Size(75, 110)
        Me.PilotLight2.TabIndex = 46
        Me.PilotLight2.Text = "Setup"
        Me.PilotLight2.Value = False
        '
        'PilotLight3
        '
        Me.PilotLight3.CommComponent = Me.EthernetIPforPLCSLCMicroCom1
        Me.PilotLight3.ForeColor = System.Drawing.Color.Black
        Me.PilotLight3.LegendPlate = MfgControl.AdvancedHMI.Controls.PilotLight.LegendPlates.Large
        Me.PilotLight3.LightColor = MfgControl.AdvancedHMI.Controls.PilotLight.LightColors.Green
        Me.PilotLight3.LightColorOff = MfgControl.AdvancedHMI.Controls.PilotLight.LightColors.White
        Me.PilotLight3.Location = New System.Drawing.Point(470, 206)
        Me.PilotLight3.Name = "PilotLight3"
        Me.PilotLight3.OutputType = MfgControl.AdvancedHMI.Controls.OutputType.MomentarySet
        Me.PilotLight3.PLCAddressClick = ""
        Me.PilotLight3.PLCAddressText = ""
        Me.PilotLight3.PLCAddressValue = ""
        Me.PilotLight3.PLCAddressVisible = ""
        Me.PilotLight3.Size = New System.Drawing.Size(75, 110)
        Me.PilotLight3.TabIndex = 47
        Me.PilotLight3.Text = "Down"
        Me.PilotLight3.Value = False
        '
        'DigitalPanelMeter2
        '
        Me.DigitalPanelMeter2.CommComponent = Me.EthernetIPforPLCSLCMicroCom1
        Me.DigitalPanelMeter2.DecimalPosition = 0
        Me.DigitalPanelMeter2.ForeColor = System.Drawing.Color.LightGray
        Me.DigitalPanelMeter2.KeypadFontColor = System.Drawing.Color.WhiteSmoke
        Me.DigitalPanelMeter2.KeypadMaxValue = 0.0R
        Me.DigitalPanelMeter2.KeypadMinValue = 0.0R
        Me.DigitalPanelMeter2.KeypadScaleFactor = 1.0R
        Me.DigitalPanelMeter2.KeypadText = Nothing
        Me.DigitalPanelMeter2.KeypadWidth = 300
        Me.DigitalPanelMeter2.Location = New System.Drawing.Point(400, 73)
        Me.DigitalPanelMeter2.Name = "DigitalPanelMeter2"
        Me.DigitalPanelMeter2.NumberOfDigits = 5
        Me.DigitalPanelMeter2.PLCAddressKeypad = ""
        Me.DigitalPanelMeter2.PLCAddressText = ""
        Me.DigitalPanelMeter2.PLCAddressValue = ""
        Me.DigitalPanelMeter2.PLCAddressVisible = ""
        Me.DigitalPanelMeter2.Resolution = New Decimal(New Integer() {1, 0, 0, 0})
        Me.DigitalPanelMeter2.Size = New System.Drawing.Size(253, 110)
        Me.DigitalPanelMeter2.TabIndex = 48
        Me.DigitalPanelMeter2.Text = "Scrap Counter"
        Me.DigitalPanelMeter2.Value = 0.0R
        Me.DigitalPanelMeter2.ValueScaleFactor = New Decimal(New Integer() {1, 0, 0, 0})
        Me.DigitalPanelMeter2.ValueScaleOffset = New Decimal(New Integer() {0, 0, 0, 0})
        '
        'BasicButton1
        '
        Me.BasicButton1.CommComponent = Me.EthernetIPforPLCSLCMicroCom1
        Me.BasicButton1.ForeColor = System.Drawing.Color.Black
        Me.BasicButton1.Highlight = False
        Me.BasicButton1.HighlightColor = System.Drawing.Color.Green
        Me.BasicButton1.Location = New System.Drawing.Point(136, 340)
        Me.BasicButton1.MaximumHoldTime = 3000
        Me.BasicButton1.MinimumHoldTime = 500
        Me.BasicButton1.Name = "BasicButton1"
        Me.BasicButton1.OutputType = MfgControl.AdvancedHMI.Controls.OutputType.MomentarySet
        Me.BasicButton1.PLCAddressClick = ""
        Me.BasicButton1.PLCAddressHighlight = ""
        Me.BasicButton1.PLCAddressSelectTextAlternate = ""
        Me.BasicButton1.PLCAddressText = ""
        Me.BasicButton1.PLCAddressVisible = ""
        Me.BasicButton1.SelectTextAlternate = False
        Me.BasicButton1.Size = New System.Drawing.Size(122, 33)
        Me.BasicButton1.TabIndex = 49
        Me.BasicButton1.Text = "Setup"
        Me.BasicButton1.TextAlternate = Nothing
        Me.BasicButton1.UseVisualStyleBackColor = True
        Me.BasicButton1.ValueToWrite = 0
        '
        'BasicButton2
        '
        Me.BasicButton2.CommComponent = Me.EthernetIPforPLCSLCMicroCom1
        Me.BasicButton2.ForeColor = System.Drawing.Color.Black
        Me.BasicButton2.Highlight = False
        Me.BasicButton2.HighlightColor = System.Drawing.Color.Green
        Me.BasicButton2.Location = New System.Drawing.Point(549, 418)
        Me.BasicButton2.MaximumHoldTime = 3000
        Me.BasicButton2.MinimumHoldTime = 500
        Me.BasicButton2.Name = "BasicButton2"
        Me.BasicButton2.OutputType = MfgControl.AdvancedHMI.Controls.OutputType.MomentarySet
        Me.BasicButton2.PLCAddressClick = ""
        Me.BasicButton2.PLCAddressHighlight = ""
        Me.BasicButton2.PLCAddressSelectTextAlternate = ""
        Me.BasicButton2.PLCAddressText = ""
        Me.BasicButton2.PLCAddressVisible = ""
        Me.BasicButton2.SelectTextAlternate = False
        Me.BasicButton2.Size = New System.Drawing.Size(122, 33)
        Me.BasicButton2.TabIndex = 50
        Me.BasicButton2.Text = "BasicButton2"
        Me.BasicButton2.TextAlternate = Nothing
        Me.BasicButton2.UseVisualStyleBackColor = True
        Me.BasicButton2.ValueToWrite = 0
        '
        'BasicButton3
        '
        Me.BasicButton3.CommComponent = Me.EthernetIPforPLCSLCMicroCom1
        Me.BasicButton3.ForeColor = System.Drawing.Color.Black
        Me.BasicButton3.Highlight = False
        Me.BasicButton3.HighlightColor = System.Drawing.Color.Green
        Me.BasicButton3.Location = New System.Drawing.Point(549, 379)
        Me.BasicButton3.MaximumHoldTime = 3000
        Me.BasicButton3.MinimumHoldTime = 500
        Me.BasicButton3.Name = "BasicButton3"
        Me.BasicButton3.OutputType = MfgControl.AdvancedHMI.Controls.OutputType.MomentarySet
        Me.BasicButton3.PLCAddressClick = ""
        Me.BasicButton3.PLCAddressHighlight = ""
        Me.BasicButton3.PLCAddressSelectTextAlternate = ""
        Me.BasicButton3.PLCAddressText = ""
        Me.BasicButton3.PLCAddressVisible = ""
        Me.BasicButton3.SelectTextAlternate = False
        Me.BasicButton3.Size = New System.Drawing.Size(122, 33)
        Me.BasicButton3.TabIndex = 51
        Me.BasicButton3.Text = "BasicButton3"
        Me.BasicButton3.TextAlternate = Nothing
        Me.BasicButton3.UseVisualStyleBackColor = True
        Me.BasicButton3.ValueToWrite = 0
        '
        'BasicButton4
        '
        Me.BasicButton4.CommComponent = Me.EthernetIPforPLCSLCMicroCom1
        Me.BasicButton4.ForeColor = System.Drawing.Color.Black
        Me.BasicButton4.Highlight = False
        Me.BasicButton4.HighlightColor = System.Drawing.Color.Green
        Me.BasicButton4.Location = New System.Drawing.Point(549, 340)
        Me.BasicButton4.MaximumHoldTime = 3000
        Me.BasicButton4.MinimumHoldTime = 500
        Me.BasicButton4.Name = "BasicButton4"
        Me.BasicButton4.OutputType = MfgControl.AdvancedHMI.Controls.OutputType.MomentarySet
        Me.BasicButton4.PLCAddressClick = ""
        Me.BasicButton4.PLCAddressHighlight = ""
        Me.BasicButton4.PLCAddressSelectTextAlternate = ""
        Me.BasicButton4.PLCAddressText = ""
        Me.BasicButton4.PLCAddressVisible = ""
        Me.BasicButton4.SelectTextAlternate = False
        Me.BasicButton4.Size = New System.Drawing.Size(122, 33)
        Me.BasicButton4.TabIndex = 52
        Me.BasicButton4.Text = "Maintenance"
        Me.BasicButton4.TextAlternate = Nothing
        Me.BasicButton4.UseVisualStyleBackColor = True
        Me.BasicButton4.ValueToWrite = 0
        '
        'BasicButton5
        '
        Me.BasicButton5.CommComponent = Me.EthernetIPforPLCSLCMicroCom1
        Me.BasicButton5.ForeColor = System.Drawing.Color.Black
        Me.BasicButton5.Highlight = False
        Me.BasicButton5.HighlightColor = System.Drawing.Color.Green
        Me.BasicButton5.Location = New System.Drawing.Point(412, 418)
        Me.BasicButton5.MaximumHoldTime = 3000
        Me.BasicButton5.MinimumHoldTime = 500
        Me.BasicButton5.Name = "BasicButton5"
        Me.BasicButton5.OutputType = MfgControl.AdvancedHMI.Controls.OutputType.MomentarySet
        Me.BasicButton5.PLCAddressClick = ""
        Me.BasicButton5.PLCAddressHighlight = ""
        Me.BasicButton5.PLCAddressSelectTextAlternate = ""
        Me.BasicButton5.PLCAddressText = ""
        Me.BasicButton5.PLCAddressVisible = ""
        Me.BasicButton5.SelectTextAlternate = False
        Me.BasicButton5.Size = New System.Drawing.Size(122, 33)
        Me.BasicButton5.TabIndex = 53
        Me.BasicButton5.Text = "BasicButton5"
        Me.BasicButton5.TextAlternate = Nothing
        Me.BasicButton5.UseVisualStyleBackColor = True
        Me.BasicButton5.ValueToWrite = 0
        '
        'BasicButton6
        '
        Me.BasicButton6.CommComponent = Me.EthernetIPforPLCSLCMicroCom1
        Me.BasicButton6.ForeColor = System.Drawing.Color.Black
        Me.BasicButton6.Highlight = False
        Me.BasicButton6.HighlightColor = System.Drawing.Color.Green
        Me.BasicButton6.Location = New System.Drawing.Point(412, 379)
        Me.BasicButton6.MaximumHoldTime = 3000
        Me.BasicButton6.MinimumHoldTime = 500
        Me.BasicButton6.Name = "BasicButton6"
        Me.BasicButton6.OutputType = MfgControl.AdvancedHMI.Controls.OutputType.MomentarySet
        Me.BasicButton6.PLCAddressClick = ""
        Me.BasicButton6.PLCAddressHighlight = ""
        Me.BasicButton6.PLCAddressSelectTextAlternate = ""
        Me.BasicButton6.PLCAddressText = ""
        Me.BasicButton6.PLCAddressVisible = ""
        Me.BasicButton6.SelectTextAlternate = False
        Me.BasicButton6.Size = New System.Drawing.Size(122, 33)
        Me.BasicButton6.TabIndex = 54
        Me.BasicButton6.Text = "BasicButton6"
        Me.BasicButton6.TextAlternate = Nothing
        Me.BasicButton6.UseVisualStyleBackColor = True
        Me.BasicButton6.ValueToWrite = 0
        '
        'BasicButton7
        '
        Me.BasicButton7.CommComponent = Me.EthernetIPforPLCSLCMicroCom1
        Me.BasicButton7.ForeColor = System.Drawing.Color.Black
        Me.BasicButton7.Highlight = False
        Me.BasicButton7.HighlightColor = System.Drawing.Color.Green
        Me.BasicButton7.Location = New System.Drawing.Point(412, 340)
        Me.BasicButton7.MaximumHoldTime = 3000
        Me.BasicButton7.MinimumHoldTime = 500
        Me.BasicButton7.Name = "BasicButton7"
        Me.BasicButton7.OutputType = MfgControl.AdvancedHMI.Controls.OutputType.MomentarySet
        Me.BasicButton7.PLCAddressClick = ""
        Me.BasicButton7.PLCAddressHighlight = ""
        Me.BasicButton7.PLCAddressSelectTextAlternate = ""
        Me.BasicButton7.PLCAddressText = ""
        Me.BasicButton7.PLCAddressVisible = ""
        Me.BasicButton7.SelectTextAlternate = False
        Me.BasicButton7.Size = New System.Drawing.Size(122, 33)
        Me.BasicButton7.TabIndex = 55
        Me.BasicButton7.Text = "Break"
        Me.BasicButton7.TextAlternate = Nothing
        Me.BasicButton7.UseVisualStyleBackColor = True
        Me.BasicButton7.ValueToWrite = 0
        '
        'BasicButton8
        '
        Me.BasicButton8.CommComponent = Me.EthernetIPforPLCSLCMicroCom1
        Me.BasicButton8.ForeColor = System.Drawing.Color.Black
        Me.BasicButton8.Highlight = False
        Me.BasicButton8.HighlightColor = System.Drawing.Color.Green
        Me.BasicButton8.Location = New System.Drawing.Point(274, 418)
        Me.BasicButton8.MaximumHoldTime = 3000
        Me.BasicButton8.MinimumHoldTime = 500
        Me.BasicButton8.Name = "BasicButton8"
        Me.BasicButton8.OutputType = MfgControl.AdvancedHMI.Controls.OutputType.MomentarySet
        Me.BasicButton8.PLCAddressClick = ""
        Me.BasicButton8.PLCAddressHighlight = ""
        Me.BasicButton8.PLCAddressSelectTextAlternate = ""
        Me.BasicButton8.PLCAddressText = ""
        Me.BasicButton8.PLCAddressVisible = ""
        Me.BasicButton8.SelectTextAlternate = False
        Me.BasicButton8.Size = New System.Drawing.Size(122, 33)
        Me.BasicButton8.TabIndex = 56
        Me.BasicButton8.TextAlternate = Nothing
        Me.BasicButton8.UseVisualStyleBackColor = True
        Me.BasicButton8.ValueToWrite = 0
        '
        'BasicButton9
        '
        Me.BasicButton9.CommComponent = Me.EthernetIPforPLCSLCMicroCom1
        Me.BasicButton9.ForeColor = System.Drawing.Color.Black
        Me.BasicButton9.Highlight = False
        Me.BasicButton9.HighlightColor = System.Drawing.Color.Green
        Me.BasicButton9.Location = New System.Drawing.Point(274, 379)
        Me.BasicButton9.MaximumHoldTime = 3000
        Me.BasicButton9.MinimumHoldTime = 500
        Me.BasicButton9.Name = "BasicButton9"
        Me.BasicButton9.OutputType = MfgControl.AdvancedHMI.Controls.OutputType.MomentarySet
        Me.BasicButton9.PLCAddressClick = ""
        Me.BasicButton9.PLCAddressHighlight = ""
        Me.BasicButton9.PLCAddressSelectTextAlternate = ""
        Me.BasicButton9.PLCAddressText = ""
        Me.BasicButton9.PLCAddressVisible = ""
        Me.BasicButton9.SelectTextAlternate = False
        Me.BasicButton9.Size = New System.Drawing.Size(122, 33)
        Me.BasicButton9.TabIndex = 57
        Me.BasicButton9.Text = "Fork Lift"
        Me.BasicButton9.TextAlternate = Nothing
        Me.BasicButton9.UseVisualStyleBackColor = True
        Me.BasicButton9.ValueToWrite = 0
        '
        'BasicButton10
        '
        Me.BasicButton10.CommComponent = Me.EthernetIPforPLCSLCMicroCom1
        Me.BasicButton10.ForeColor = System.Drawing.Color.Black
        Me.BasicButton10.Highlight = False
        Me.BasicButton10.HighlightColor = System.Drawing.Color.Green
        Me.BasicButton10.Location = New System.Drawing.Point(274, 340)
        Me.BasicButton10.MaximumHoldTime = 3000
        Me.BasicButton10.MinimumHoldTime = 500
        Me.BasicButton10.Name = "BasicButton10"
        Me.BasicButton10.OutputType = MfgControl.AdvancedHMI.Controls.OutputType.MomentarySet
        Me.BasicButton10.PLCAddressClick = ""
        Me.BasicButton10.PLCAddressHighlight = ""
        Me.BasicButton10.PLCAddressSelectTextAlternate = ""
        Me.BasicButton10.PLCAddressText = ""
        Me.BasicButton10.PLCAddressVisible = ""
        Me.BasicButton10.SelectTextAlternate = False
        Me.BasicButton10.Size = New System.Drawing.Size(122, 33)
        Me.BasicButton10.TabIndex = 58
        Me.BasicButton10.Text = "Crane"
        Me.BasicButton10.TextAlternate = Nothing
        Me.BasicButton10.UseVisualStyleBackColor = True
        Me.BasicButton10.ValueToWrite = 0
        '
        'BasicButton11
        '
        Me.BasicButton11.CommComponent = Me.EthernetIPforPLCSLCMicroCom1
        Me.BasicButton11.ForeColor = System.Drawing.Color.Black
        Me.BasicButton11.Highlight = False
        Me.BasicButton11.HighlightColor = System.Drawing.Color.Green
        Me.BasicButton11.Location = New System.Drawing.Point(136, 418)
        Me.BasicButton11.MaximumHoldTime = 3000
        Me.BasicButton11.MinimumHoldTime = 500
        Me.BasicButton11.Name = "BasicButton11"
        Me.BasicButton11.OutputType = MfgControl.AdvancedHMI.Controls.OutputType.MomentarySet
        Me.BasicButton11.PLCAddressClick = ""
        Me.BasicButton11.PLCAddressHighlight = ""
        Me.BasicButton11.PLCAddressSelectTextAlternate = ""
        Me.BasicButton11.PLCAddressText = ""
        Me.BasicButton11.PLCAddressVisible = ""
        Me.BasicButton11.SelectTextAlternate = False
        Me.BasicButton11.Size = New System.Drawing.Size(122, 33)
        Me.BasicButton11.TabIndex = 59
        Me.BasicButton11.Text = "Length Change"
        Me.BasicButton11.TextAlternate = Nothing
        Me.BasicButton11.UseVisualStyleBackColor = True
        Me.BasicButton11.ValueToWrite = 0
        '
        'BasicButton12
        '
        Me.BasicButton12.CommComponent = Me.EthernetIPforPLCSLCMicroCom1
        Me.BasicButton12.ForeColor = System.Drawing.Color.Black
        Me.BasicButton12.Highlight = False
        Me.BasicButton12.HighlightColor = System.Drawing.Color.Green
        Me.BasicButton12.Location = New System.Drawing.Point(136, 379)
        Me.BasicButton12.MaximumHoldTime = 3000
        Me.BasicButton12.MinimumHoldTime = 500
        Me.BasicButton12.Name = "BasicButton12"
        Me.BasicButton12.OutputType = MfgControl.AdvancedHMI.Controls.OutputType.MomentarySet
        Me.BasicButton12.PLCAddressClick = ""
        Me.BasicButton12.PLCAddressHighlight = ""
        Me.BasicButton12.PLCAddressSelectTextAlternate = ""
        Me.BasicButton12.PLCAddressText = ""
        Me.BasicButton12.PLCAddressVisible = ""
        Me.BasicButton12.SelectTextAlternate = False
        Me.BasicButton12.Size = New System.Drawing.Size(122, 33)
        Me.BasicButton12.TabIndex = 60
        Me.BasicButton12.Text = "Startup"
        Me.BasicButton12.TextAlternate = Nothing
        Me.BasicButton12.UseVisualStyleBackColor = True
        Me.BasicButton12.ValueToWrite = 0
        '
        'MainForm
        '
        Me.AutoScroll = True
        Me.BackColor = System.Drawing.Color.Black
        Me.ClientSize = New System.Drawing.Size(784, 562)
        Me.Controls.Add(Me.BasicButton12)
        Me.Controls.Add(Me.BasicButton11)
        Me.Controls.Add(Me.BasicButton10)
        Me.Controls.Add(Me.BasicButton9)
        Me.Controls.Add(Me.BasicButton8)
        Me.Controls.Add(Me.BasicButton7)
        Me.Controls.Add(Me.BasicButton6)
        Me.Controls.Add(Me.BasicButton5)
        Me.Controls.Add(Me.BasicButton4)
        Me.Controls.Add(Me.BasicButton3)
        Me.Controls.Add(Me.BasicButton2)
        Me.Controls.Add(Me.BasicButton1)
        Me.Controls.Add(Me.DigitalPanelMeter2)
        Me.Controls.Add(Me.PilotLight3)
        Me.Controls.Add(Me.PilotLight2)
        Me.Controls.Add(Me.PilotLight1)
        Me.Controls.Add(Me.DigitalPanelMeter1)
        Me.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.ForeColor = System.Drawing.Color.White
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.Name = "MainForm"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.Manual
        Me.Text = "SCADA M1188"
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents EthernetIPforPLCSLCMicroCom1 As AdvancedHMIDrivers.EthernetIPforPLCSLCMicroCom
    Friend WithEvents DigitalPanelMeter1 As AdvancedHMIControls.DigitalPanelMeter
    Friend WithEvents PilotLight3 As AdvancedHMIControls.PilotLight
    Friend WithEvents PilotLight2 As AdvancedHMIControls.PilotLight
    Friend WithEvents PilotLight1 As AdvancedHMIControls.PilotLight
    Friend WithEvents DigitalPanelMeter2 As AdvancedHMIControls.DigitalPanelMeter
    Friend WithEvents BasicButton12 As AdvancedHMIControls.BasicButton
    Friend WithEvents BasicButton11 As AdvancedHMIControls.BasicButton
    Friend WithEvents BasicButton10 As AdvancedHMIControls.BasicButton
    Friend WithEvents BasicButton9 As AdvancedHMIControls.BasicButton
    Friend WithEvents BasicButton8 As AdvancedHMIControls.BasicButton
    Friend WithEvents BasicButton7 As AdvancedHMIControls.BasicButton
    Friend WithEvents BasicButton6 As AdvancedHMIControls.BasicButton
    Friend WithEvents BasicButton5 As AdvancedHMIControls.BasicButton
    Friend WithEvents BasicButton4 As AdvancedHMIControls.BasicButton
    Friend WithEvents BasicButton3 As AdvancedHMIControls.BasicButton
    Friend WithEvents BasicButton2 As AdvancedHMIControls.BasicButton
    Friend WithEvents BasicButton1 As AdvancedHMIControls.BasicButton
End Class
