namespace Robert_CLI;

public class Config
{
    public required InterfaceType InterfaceType { get; init; }
    public string? SerialPort { get; init; }
    public int? BaudRate { get; init; }
    public string? Host { get; init; }
    public int? Port { get; init; }

    public static Config GenerateConfig()
    {
        while (true)
        {
            Console.Write("(H)ardware or (S)oftware interface? ");
            switch (Console.ReadLine()!.ToLower())
            {
                case "h":
                    Console.Write("Serial port name? ");
                    string serialPort = Console.ReadLine()!;
                    int? baudRate = null;
                    while (baudRate is null)
                    {
                        Console.Write("Serial baud rate? ");
                        try
                        {
                            baudRate = int.Parse(Console.ReadLine()!);
                        }
                        catch (FormatException)
                        {
                            Console.WriteLine("Invalid number.");
                        }
                    }

                    return new Config
                        { InterfaceType = InterfaceType.Hardware, SerialPort = serialPort, BaudRate = baudRate };
                case "s":
                    Console.Write("Hostname? ");
                    string host = Console.ReadLine()!;
                    int? port = null;
                    while (port is null)
                    {
                        Console.Write("Port number? ");
                        try
                        {
                            port = int.Parse(Console.ReadLine()!);
                        }
                        catch (FormatException)
                        {
                            Console.WriteLine("Invalid number.");
                        }
                    }

                    return new Config { InterfaceType = InterfaceType.Software, Host = host, Port = port };
            }
        }
    }

    public static IRobInterface ReadConfig(Config config)
    {
        switch (config.InterfaceType)
        {
            case InterfaceType.Hardware:
                if (config is { SerialPort: not null, BaudRate: not null })
                {
                    return new HardwareInterface(config.SerialPort, config.BaudRate.Value);
                }

                throw new InvalidDataException("Serial port and baud rate must be specified");

            case InterfaceType.Software:
                if (config is { Host: not null, Port: not null })
                {
                    return new EmuInterface(config.Host ?? string.Empty, config.Port.GetValueOrDefault());
                }

                throw new InvalidDataException("Host and port must be specified");

            default:
                throw new InvalidDataException("Invalid interface type in the configuration file");
        }
    }
}