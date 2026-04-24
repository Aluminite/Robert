using System;
using System.IO;
using System.Net.Sockets;
using Godot;
using Robert.RobotLogic;

namespace Robert;

public partial class ConfigManager : Node
{
    private RobotController _controller;
    private HBoxContainer _emuSettingsContainer;
    private HBoxContainer _hwSettingsContainer;
    private Button _emuConnectButton;
    private Button _emuDisconnectButton;
    private Button _hwConnectButton;
    private Button _hwDisconnectButton;
    private Label _errorLabel;
    private Label _speedLabel;

    public override void _Ready()
    {
        _controller = GetNode<RobotController>("../RobotController");
        Node vbox = GetNode("../FoldableContainer/ScrollContainer/VBoxContainer");
        _emuSettingsContainer = vbox.GetNode<HBoxContainer>("EmuSettingsContainer");
        _hwSettingsContainer = vbox.GetNode<HBoxContainer>("HWSettingsContainer");
        _emuConnectButton = vbox.GetNode<Button>("EmuSettingsContainer/Connect");
        _emuDisconnectButton = vbox.GetNode<Button>("EmuSettingsContainer/Disconnect");
        _hwConnectButton = vbox.GetNode<Button>("HWSettingsContainer/Connect");
        _hwDisconnectButton = vbox.GetNode<Button>("HWSettingsContainer/Disconnect");
        _errorLabel = vbox.GetNode<Label>("ErrorLabel");
        _speedLabel = vbox.GetNode<Label>("SpeedContainer/CurrentSpeed");
    }

    private void _setErrorLabelText(string text)
    {
        _errorLabel.Text = text;
        _errorLabel.Visible = text != "";
    }

    public void ShowError(string error)
    {
        _on_disconnect_pressed();
        _setErrorLabelText(error);
    }

    private void _on_mode_item_selected(int index)
    {
        switch (index)
        {
            case 0: // No accessories
                _controller.Robot = new Robot(_controller.Robot.Speed);
                break;
            case 1: // Gyromite
                _controller.Robot = new GyromiteRobot(_controller.Robot.Speed);
                break;
            case 2: // Stack-Up
                _controller.Robot = new StackUpRobot(_controller.Robot.Speed);
                break;
        }
    }

    private void _on_interface_type_item_selected(int index)
    {
        _on_disconnect_pressed();
        _setErrorLabelText("");

        switch (index)
        {
            case 0: // Dummy
                _emuSettingsContainer.Visible = false;
                _hwSettingsContainer.Visible = false;
                break;
            case 1: // Emu
                _emuSettingsContainer.Visible = true;
                _hwSettingsContainer.Visible = false;
                break;
            case 2: // Hardware
                _emuSettingsContainer.Visible = false;
                _hwSettingsContainer.Visible = true;
                break;
        }
    }

    private void _on_emu_connect_pressed()
    {
        try
        {
            _setErrorLabelText("");
            string hostname =
                GetNode<LineEdit>("../FoldableContainer/ScrollContainer/VBoxContainer/EmuSettingsContainer/Hostname")
                    .Text;
            int port = int.Parse(
                GetNode<LineEdit>("../FoldableContainer/ScrollContainer/VBoxContainer/EmuSettingsContainer/Port").Text);

            _controller.Interface = new EmuInterface(hostname, port);
            _controller.Interface.Connect();

            // connection was successful
            _emuConnectButton.Visible = false;
            _emuDisconnectButton.Visible = true;
        }
        catch (Exception ex)
        {
            if (ex is FormatException or ArgumentOutOfRangeException)
            {
                _setErrorLabelText("Error: Port is not a valid number.");
            }
            else if (ex is SocketException)
            {
                _setErrorLabelText($"Error: {ex.Message}");
            }
            else
            {
                throw;
            }
        }
    }

    private void _on_hw_connect_pressed()
    {
        try
        {
            _setErrorLabelText("");
            string serialPort =
                GetNode<LineEdit>("../FoldableContainer/ScrollContainer/VBoxContainer/HWSettingsContainer/PortName")
                    .Text;
            int baud = int.Parse(
                GetNode<LineEdit>("../FoldableContainer/ScrollContainer/VBoxContainer/HWSettingsContainer/Baud").Text);

            _controller.Interface = new HardwareInterface(serialPort, baud);
            _controller.Interface.Connect();

            // connection was successful
            _hwConnectButton.Visible = false;
            _hwDisconnectButton.Visible = true;
        }
        catch (Exception ex)
        {
            if (ex is FormatException or ArgumentOutOfRangeException)
            {
                _setErrorLabelText("Error: Baud is not a valid number.");
            }
            else if (ex is UnauthorizedAccessException or IOException)
            {
                _setErrorLabelText("Error: Unable to access serial port.");
            }
            else if (ex is ArgumentException)
            {
                _setErrorLabelText("Error: Invalid serial port name.");
            }
            else
            {
                throw;
            }
        }
    }

    private void _on_disconnect_pressed()
    {
        IRobInterface oldInterface = _controller.Interface;
        _controller.Interface = new DummyInterface();
        oldInterface.Disconnect();

        _emuConnectButton.Visible = true;
        _emuDisconnectButton.Visible = false;
        _hwConnectButton.Visible = true;
        _hwDisconnectButton.Visible = false;
    }

    private void _on_speed_value_changed(float value)
    {
        _speedLabel.Text = value.ToString("0.0") + "x";
        _controller.Robot.Speed = value;
    }
}