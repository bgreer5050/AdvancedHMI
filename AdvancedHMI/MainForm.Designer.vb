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
        Me.ThreeButtons1 = New AdvancedHMIControls.ThreeButtons()
        Me.ThreeButtons2 = New AdvancedHMIControls.ThreeButtons()
        Me.ThreeButtons3 = New AdvancedHMIControls.ThreeButtons()
        Me.ThreeButtons4 = New AdvancedHMIControls.ThreeButtons()
        Me.DF1Comm1 = New AdvancedHMIDrivers.DF1Comm(Me.components)
        Me.DigitalPanelMeter2 = New AdvancedHMIControls.DigitalPanelMeter()
        Me.DigitalPanelMeter3 = New AdvancedHMIControls.DigitalPanelMeter()
        Me.PilotLight1 = New AdvancedHMIControls.PilotLight()
        Me.PilotLight2 = New AdvancedHMIControls.PilotLight()
        Me.PilotLight3 = New AdvancedHMIControls.PilotLight()
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
        Me.DigitalPanelMeter1.Location = New System.Drawing.Point(105, 35)
        Me.DigitalPanelMeter1.Name = "DigitalPanelMeter1"
        Me.DigitalPanelMeter1.NumberOfDigits = 5
        Me.DigitalPanelMeter1.PLCAddressKeypad = ""
        Me.DigitalPanelMeter1.PLCAddressText = "N7:1"
        Me.DigitalPanelMeter1.PLCAddressValue = "N7:1"
        Me.DigitalPanelMeter1.PLCAddressVisible = ""
        Me.DigitalPanelMeter1.Resolution = New Decimal(New Integer() {1, 0, 0, 0})
        Me.DigitalPanelMeter1.Size = New System.Drawing.Size(219, 95)
        Me.DigitalPanelMeter1.TabIndex = 43
        Me.DigitalPanelMeter1.Text = "Run Time"
        Me.DigitalPanelMeter1.Value = 0.0R
        Me.DigitalPanelMeter1.ValueScaleFactor = New Decimal(New Integer() {1, 0, 0, 0})
        Me.DigitalPanelMeter1.ValueScaleOffset = New Decimal(New Integer() {0, 0, 0, 0})
        '
        'ThreeButtons1
        '
        Me.ThreeButtons1.Button1Text = "Setup"
        Me.ThreeButtons1.Button2Text = "Start Up"
        Me.ThreeButtons1.Button3Text = "Adjustments"
        Me.ThreeButtons1.ForeColor = System.Drawing.Color.Black
        Me.ThreeButtons1.Location = New System.Drawing.Point(110, 252)
        Me.ThreeButtons1.Name = "ThreeButtons1"
        Me.ThreeButtons1.Size = New System.Drawing.Size(150, 165)
        Me.ThreeButtons1.TabIndex = 52
        '
        'ThreeButtons2
        '
        Me.ThreeButtons2.Button1Text = "Maintenance"
        Me.ThreeButtons2.Button2Text = "Tooling"
        Me.ThreeButtons2.Button3Text = ""
        Me.ThreeButtons2.ForeColor = System.Drawing.Color.Black
        Me.ThreeButtons2.Location = New System.Drawing.Point(266, 252)
        Me.ThreeButtons2.Name = "ThreeButtons2"
        Me.ThreeButtons2.Size = New System.Drawing.Size(150, 165)
        Me.ThreeButtons2.TabIndex = 53
        '
        'ThreeButtons3
        '
        Me.ThreeButtons3.Button1Text = "Crane"
        Me.ThreeButtons3.Button2Text = "Fork Lift"
        Me.ThreeButtons3.Button3Text = "Misc"
        Me.ThreeButtons3.ForeColor = System.Drawing.Color.Black
        Me.ThreeButtons3.Location = New System.Drawing.Point(422, 252)
        Me.ThreeButtons3.Name = "ThreeButtons3"
        Me.ThreeButtons3.Size = New System.Drawing.Size(150, 165)
        Me.ThreeButtons3.TabIndex = 54
        '
        'ThreeButtons4
        '
        Me.ThreeButtons4.Button1Text = "Feed Equipment"
        Me.ThreeButtons4.Button2Text = "Auto"
        Me.ThreeButtons4.Button3Text = ""
        Me.ThreeButtons4.ForeColor = System.Drawing.Color.Black
        Me.ThreeButtons4.Location = New System.Drawing.Point(578, 252)
        Me.ThreeButtons4.Name = "ThreeButtons4"
        Me.ThreeButtons4.Size = New System.Drawing.Size(150, 165)
        Me.ThreeButtons4.TabIndex = 55
        '
        'DF1Comm1
        '
        Me.DF1Comm1.BaudRate = "AUTO"
        Me.DF1Comm1.CheckSumType = MfgControl.AdvancedHMI.Drivers.DF1DataLinkLayer.ChecksumOptions.Crc
        Me.DF1Comm1.ComPort = "COM1"
        Me.DF1Comm1.DisableSubscriptions = False
        Me.DF1Comm1.MyNode = 0
        Me.DF1Comm1.Parity = System.IO.Ports.Parity.None
        Me.DF1Comm1.PollRateOverride = 0
        Me.DF1Comm1.SynchronizingObject = Me
        Me.DF1Comm1.TargetNode = 0
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
        Me.DigitalPanelMeter2.Location = New System.Drawing.Point(330, 35)
        Me.DigitalPanelMeter2.Name = "DigitalPanelMeter2"
        Me.DigitalPanelMeter2.NumberOfDigits = 4
        Me.DigitalPanelMeter2.PLCAddressKeypad = ""
        Me.DigitalPanelMeter2.PLCAddressText = "N7:1"
        Me.DigitalPanelMeter2.PLCAddressValue = "N7:1"
        Me.DigitalPanelMeter2.PLCAddressVisible = ""
        Me.DigitalPanelMeter2.Resolution = New Decimal(New Integer() {1, 0, 0, 0})
        Me.DigitalPanelMeter2.Size = New System.Drawing.Size(187, 96)
        Me.DigitalPanelMeter2.TabIndex = 56
        Me.DigitalPanelMeter2.Text = "Pieces"
        Me.DigitalPanelMeter2.Value = 0.0R
        Me.DigitalPanelMeter2.ValueScaleFactor = New Decimal(New Integer() {1, 0, 0, 0})
        Me.DigitalPanelMeter2.ValueScaleOffset = New Decimal(New Integer() {0, 0, 0, 0})
        '
        'DigitalPanelMeter3
        '
        Me.DigitalPanelMeter3.CommComponent = Me.EthernetIPforPLCSLCMicroCom1
        Me.DigitalPanelMeter3.DecimalPosition = 0
        Me.DigitalPanelMeter3.ForeColor = System.Drawing.Color.LightGray
        Me.DigitalPanelMeter3.KeypadFontColor = System.Drawing.Color.WhiteSmoke
        Me.DigitalPanelMeter3.KeypadMaxValue = 0.0R
        Me.DigitalPanelMeter3.KeypadMinValue = 0.0R
        Me.DigitalPanelMeter3.KeypadScaleFactor = 1.0R
        Me.DigitalPanelMeter3.KeypadText = Nothing
        Me.DigitalPanelMeter3.KeypadWidth = 300
        Me.DigitalPanelMeter3.Location = New System.Drawing.Point(523, 35)
        Me.DigitalPanelMeter3.Name = "DigitalPanelMeter3"
        Me.DigitalPanelMeter3.NumberOfDigits = 4
        Me.DigitalPanelMeter3.PLCAddressKeypad = ""
        Me.DigitalPanelMeter3.PLCAddressText = "N7:1"
        Me.DigitalPanelMeter3.PLCAddressValue = "N7:1"
        Me.DigitalPanelMeter3.PLCAddressVisible = ""
        Me.DigitalPanelMeter3.Resolution = New Decimal(New Integer() {1, 0, 0, 0})
        Me.DigitalPanelMeter3.Size = New System.Drawing.Size(187, 96)
        Me.DigitalPanelMeter3.TabIndex = 57
        Me.DigitalPanelMeter3.Text = "Scrap"
        Me.DigitalPanelMeter3.Value = 0.0R
        Me.DigitalPanelMeter3.ValueScaleFactor = New Decimal(New Integer() {1, 0, 0, 0})
        Me.DigitalPanelMeter3.ValueScaleOffset = New Decimal(New Integer() {0, 0, 0, 0})
        '
        'PilotLight1
        '
        Me.PilotLight1.CommComponent = Me.EthernetIPforPLCSLCMicroCom1
        Me.PilotLight1.LegendPlate = MfgControl.AdvancedHMI.Controls.PilotLight.LegendPlates.Large
        Me.PilotLight1.LightColor = MfgControl.AdvancedHMI.Controls.PilotLight.LightColors.Green
        Me.PilotLight1.LightColorOff = MfgControl.AdvancedHMI.Controls.PilotLight.LightColors.White
        Me.PilotLight1.Location = New System.Drawing.Point(228, 136)
        Me.PilotLight1.Name = "PilotLight1"
        Me.PilotLight1.OutputType = MfgControl.AdvancedHMI.Controls.OutputType.MomentarySet
        Me.PilotLight1.PLCAddressClick = ""
        Me.PilotLight1.PLCAddressText = ""
        Me.PilotLight1.PLCAddressValue = ""
        Me.PilotLight1.PLCAddressVisible = ""
        Me.PilotLight1.Size = New System.Drawing.Size(52, 76)
        Me.PilotLight1.TabIndex = 58
        Me.PilotLight1.Text = "PilotLight1"
        Me.PilotLight1.Value = False
        '
        'PilotLight2
        '
        Me.PilotLight2.CommComponent = Me.EthernetIPforPLCSLCMicroCom1
        Me.PilotLight2.LegendPlate = MfgControl.AdvancedHMI.Controls.PilotLight.LegendPlates.Large
        Me.PilotLight2.LightColor = MfgControl.AdvancedHMI.Controls.PilotLight.LightColors.Yellow
        Me.PilotLight2.LightColorOff = MfgControl.AdvancedHMI.Controls.PilotLight.LightColors.White
        Me.PilotLight2.Location = New System.Drawing.Point(364, 136)
        Me.PilotLight2.Name = "PilotLight2"
        Me.PilotLight2.OutputType = MfgControl.AdvancedHMI.Controls.OutputType.MomentarySet
        Me.PilotLight2.PLCAddressClick = ""
        Me.PilotLight2.PLCAddressText = ""
        Me.PilotLight2.PLCAddressValue = ""
        Me.PilotLight2.PLCAddressVisible = ""
        Me.PilotLight2.Size = New System.Drawing.Size(52, 76)
        Me.PilotLight2.TabIndex = 59
        Me.PilotLight2.Text = "PilotLight2"
        Me.PilotLight2.Value = False
        '
        'PilotLight3
        '
        Me.PilotLight3.CommComponent = Me.EthernetIPforPLCSLCMicroCom1
        Me.PilotLight3.LegendPlate = MfgControl.AdvancedHMI.Controls.PilotLight.LegendPlates.Large
        Me.PilotLight3.LightColor = MfgControl.AdvancedHMI.Controls.PilotLight.LightColors.Red
        Me.PilotLight3.LightColorOff = MfgControl.AdvancedHMI.Controls.PilotLight.LightColors.White
        Me.PilotLight3.Location = New System.Drawing.Point(496, 136)
        Me.PilotLight3.Name = "PilotLight3"
        Me.PilotLight3.OutputType = MfgControl.AdvancedHMI.Controls.OutputType.MomentarySet
        Me.PilotLight3.PLCAddressClick = ""
        Me.PilotLight3.PLCAddressText = ""
        Me.PilotLight3.PLCAddressValue = ""
        Me.PilotLight3.PLCAddressVisible = ""
        Me.PilotLight3.Size = New System.Drawing.Size(52, 76)
        Me.PilotLight3.TabIndex = 60
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
        Me.Controls.Add(Me.DigitalPanelMeter3)
        Me.Controls.Add(Me.DigitalPanelMeter2)
        Me.Controls.Add(Me.ThreeButtons4)
        Me.Controls.Add(Me.ThreeButtons3)
        Me.Controls.Add(Me.ThreeButtons2)
        Me.Controls.Add(Me.ThreeButtons1)
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
    Friend WithEvents ThreeButtons1 As AdvancedHMIControls.ThreeButtons
    Friend WithEvents ThreeButtons4 As AdvancedHMIControls.ThreeButtons
    Friend WithEvents ThreeButtons3 As AdvancedHMIControls.ThreeButtons
    Friend WithEvents ThreeButtons2 As AdvancedHMIControls.ThreeButtons
    Friend WithEvents DigitalPanelMeter3 As AdvancedHMIControls.DigitalPanelMeter
    Friend WithEvents DigitalPanelMeter2 As AdvancedHMIControls.DigitalPanelMeter
    Friend WithEvents DF1Comm1 As AdvancedHMIDrivers.DF1Comm
    Friend WithEvents PilotLight3 As AdvancedHMIControls.PilotLight
    Friend WithEvents PilotLight2 As AdvancedHMIControls.PilotLight
    Friend WithEvents PilotLight1 As AdvancedHMIControls.PilotLight
End Class
