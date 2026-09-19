using System.Runtime.InteropServices;

namespace XB1ControllerBatteryIndicator
{
    internal sealed class XboxController
    {
        private const int ErrorSuccess = 0;

        [DllImport("xinput1_4.dll", EntryPoint = "XInputGetBatteryInformation", CallingConvention = CallingConvention.Winapi)]
        private static extern int XInputGetBatteryInformation(
            int dwUserIndex,
            byte devType,
            out XInputBatteryInformation batteryInformation);

        [StructLayout(LayoutKind.Sequential)]
        private struct XInputBatteryInformation
        {
            public BatteryType BatteryType;
            public BatteryLevel BatteryLevel;
        }

        public XboxController(UserIndex userIndex)
        {
            UserIndex = userIndex;

            XInputBatteryInformation batteryInformation;
            var result = XInputGetBatteryInformation((int)userIndex, (byte)BatteryDeviceType.Gamepad, out batteryInformation);

            IsConnected = result == ErrorSuccess;

            if (IsConnected)
            {
                BatteryType = batteryInformation.BatteryType;
                BatteryLevel = batteryInformation.BatteryLevel;
            }
            else
            {
                BatteryType = BatteryType.Disconnected;
                BatteryLevel = BatteryLevel.Empty;
            }
        }

        public bool IsConnected { get; private set; }
        public BatteryType BatteryType { get; private set; }
        public BatteryLevel BatteryLevel { get; private set; }
        public UserIndex UserIndex { get; private set; }
    }

    internal enum BatteryDeviceType : byte
    {
        Gamepad = 0x00
    }

    internal enum BatteryLevel : byte
    {
        Empty = 0x00,
        Low = 0x01,
        Medium = 0x02,
        Full = 0x03
    }

    internal enum BatteryType : byte
    {
        Disconnected = 0x00,
        Wired = 0x01,
        Alkaline = 0x02,
        Nimh = 0x03,
        Unknown = 0xFF
    }

    internal enum UserIndex : byte
    {
        One = 0x00,
        Two = 0x01,
        Three = 0x02,
        Four = 0x03
    }
}
