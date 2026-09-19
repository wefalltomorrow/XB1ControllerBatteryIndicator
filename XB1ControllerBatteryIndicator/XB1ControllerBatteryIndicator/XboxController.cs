using System.Runtime.InteropServices;

namespace XB1ControllerBatteryIndicator;

public class XboxController
{
    [DllImport("xinput1_4.dll", EntryPoint = "XInputGetBatteryInformation")]
    private static extern int XInputGetBatteryInformation(int dwUserIndex, byte devType, out XInputBatteryInformation pBatteryInformation);
    
    [StructLayout(LayoutKind.Sequential)]
    private struct XInputBatteryInformation
    {
        public BatteryType BatteryType;
        public BatteryLevel BatteryLevel;
    }

    public bool IsConnected => this.BatteryType != BatteryType.Disconnected;
    public BatteryType BatteryType { get; }
    public UserIndex UserIndex { get; }
    public BatteryLevel BatteryLevel { get; }

    public XboxController(UserIndex userIndex)
    {
        UserIndex = userIndex;
        if (XInputGetBatteryInformation((byte)UserIndex, (byte)DeviceType.Gamepad, out var pBatteryInformation) == 0)
        {
            BatteryType = pBatteryInformation.BatteryType;
            BatteryLevel = pBatteryInformation.BatteryLevel;
        }
        else
        {
            BatteryType = BatteryType.Disconnected;
        }
    }
}

public enum DeviceType : byte
{
    Gamepad = 0x00,
}

public enum BatteryLevel : byte
{
    Empty = 0x00,
    Low = 0x01,
    Medium = 0x02,
    High = 0x03,
}

public enum BatteryType : byte
{
    Disconnected = 0x00,
    Wired = 0x01,
    Alkaline = 0x02,
    Nimh = 0x03,
    Unknown = 0xFF,
}

public enum UserIndex : byte
{
    One = 0x00,
    Two = 0x01,
    Three = 0x02,
    Four = 0x03,
}