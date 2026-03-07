using System;
using System.IO;
using System.Net.Sockets;
using Godot;
using Robert.RobotLogic;

// ReSharper disable MemberCanBePrivate.Global

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


    public override void _Ready()
    {
        _controller = GetNode<RobotController>("../RobotController");
        _emuSettingsContainer =
            GetNode<HBoxContainer>("../FoldableContainer/ScrollContainer/VBoxContainer/EmuSettingsContainer");
        _hwSettingsContainer =
            GetNode<HBoxContainer>("../FoldableContainer/ScrollContainer/VBoxContainer/HWSettingsContainer");
        _emuConnectButton =
            GetNode<Button>("../FoldableContainer/ScrollContainer/VBoxContainer/EmuSettingsContainer/Connect");
        _emuDisconnectButton =
            GetNode<Button>("../FoldableContainer/ScrollContainer/VBoxContainer/EmuSettingsContainer/Disconnect");
        _hwConnectButton =
            GetNode<Button>("../FoldableContainer/ScrollContainer/VBoxContainer/HWSettingsContainer/Connect");
        _hwDisconnectButton =
            GetNode<Button>("../FoldableContainer/ScrollContainer/VBoxContainer/HWSettingsContainer/Disconnect");
        _errorLabel = GetNode<Label>("../FoldableContainer/ScrollContainer/VBoxContainer/ErrorLabel");
    }

    public void ShowError(string error)
    {
        _on_disconnect_pressed();
        _errorLabel.Text = error;
    }

    public void _on_mode_item_selected(int index)
    {
        switch (index)
        {
            case 0: // No accessories
                _controller.Robot = new Robot();
                break;
            case 1: // Gyromite
                _controller.Robot = new GyromiteRobot();
                break;
            case 2: // Stack-Up
                _controller.Robot = new StackUpRobot();
                break;
        }
    }

    public void _on_interface_type_item_selected(int index)
    {
        _on_disconnect_pressed();
        _errorLabel.Text = "";

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

    public void _on_emu_connect_pressed()
    {
        try
        {
            _errorLabel.Text = "";
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
                _errorLabel.Text = "Error: Port is not a valid number.";
            }
            else if (ex is SocketException)
            {
                _errorLabel.Text = $"Error: {ex.Message}";
            }
            else
            {
                throw;
            }
        }
    }

    public void _on_hw_connect_pressed()
    {
        try
        {
            _errorLabel.Text = "";
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
                _errorLabel.Text = "Error: Baud is not a valid number.";
            }
            else if (ex is UnauthorizedAccessException or IOException)
            {
                _errorLabel.Text = "Error: Unable to access serial port.";
            }
            else if (ex is ArgumentException)
            {
                _errorLabel.Text = "Error: Invalid serial port name.";
            }
            else
            {
                throw;
            }
        }
    }

    public void _on_disconnect_pressed()
    {
        IRobInterface oldInterface = _controller.Interface;
        _controller.Interface = new DummyInterface();
        oldInterface.Disconnect();

        _emuConnectButton.Visible = true;
        _emuDisconnectButton.Visible = false;
        _hwConnectButton.Visible = true;
        _hwDisconnectButton.Visible = false;
    }
}