Public Class EthernetIPforMicro800Com
    '**********************************************************************************************
    '* AdvancedHMI Driver
    '* http://www.advancedhmi.com
    '* Ethernet/IP for Micro800 Series
    '*
    '* Archie Jacobs
    '* Manufacturing Automation, LLC
    '* support@advancedhmi.com
    '* 09-NOV-14
    '*
    '*
    '* Copyright 2010,2013 Archie Jacobs
    '*
    '* This class implements the Ethernet/IP protocol.
    '*
    '* NOTICE : If you received this code without a complete AdvancedHMI solution
    '* please report to sales@advancedhmi.com
    '*
    '* Distributed under the GNU General Public License (www.gnu.org)
    '*
    '* This program is free software; you can redistribute it and/or
    '* as published by the Free Software Foundation; either version 2
    '* of the License, or (at your option) any later version.
    '*
    '* This program is distributed in the hope that it will be useful,
    '* but WITHOUT ANY WARRANTY; without even the implied warranty of
    '* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    '* GNU General Public License for more details.

    '* You should have received a copy of the GNU General Public License
    '* along with this program; if not, write to the Free Software
    '* Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
    '**********************************************************************************************
    Inherits EthernetIPforCLXCom

    Protected Overrides Sub CreateDLLInstance()
        'MyBase.CreateDLLInstance()
        If Me.DesignMode Then Exit Sub

        If DLL(0) IsNot Nothing Then
            '* At least one DLL instance already exists,
            '* so check to see if it has the same IP address
            '* if so, reuse the instance, otherwise create a new one
            Dim i As Integer
            While DLL(i) IsNot Nothing AndAlso (DLL(i).EIPEncap.IPAddress <> IPAddress) AndAlso i < 11
                i += 1
            End While
            MyDLLInstance = i
        End If

        If MyDLLInstance > DLL.Length Then
            '* TODO:
            MsgBox("A limit of " & DLL.Length & " driver instances")
            Exit Sub
        End If

        If DLL(MyDLLInstance) Is Nothing Then
            Try
                DLL(MyDLLInstance) = New MfgControl.AdvancedHMI.Drivers.CIP
            Catch ex As Exception
                MsgBox(ex.Message)
            End Try

            DLL(MyDLLInstance).EIPEncap.IPAddress = IPAddress
            DLL(MyDLLInstance).EIPEncap.Port = Port
            '* Set to 0 for Micro800 since it has no backplane
            DLL(MyDLLInstance).ConnectionPathPort = 0
            DLL(MyDLLInstance).ProcessorSlot = 0
        End If

        AddHandler DLL(MyDLLInstance).DataReceived, AddressOf DataLinkLayer_DataReceived
        AddHandler DLL(MyDLLInstance).ComError, AddressOf ComErrorHandler
        AddHandler DLL(MyDLLInstance).ConnectionEstablished, AddressOf CIPConnectionEstablished
        AddHandler DLL(MyDLLInstance).ConnectionClosed, AddressOf CIPConnectionClosed
        DLL(MyDLLInstance).ConnectionCount += 1

    End Sub
End Class
