using System.Windows.Data;

namespace XB1ControllerBatteryIndicator
{
    public class LocalizationExtension : Binding
    {
        public LocalizationExtension(string name) : base("[" + name + "]")
        {
            Mode = BindingMode.OneWay;
            Source = TranslationBindingProvider.Instance;
        }
    }
}