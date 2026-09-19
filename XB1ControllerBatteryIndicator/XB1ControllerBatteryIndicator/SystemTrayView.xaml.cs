using System.Windows;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Data;
using System.Xml;
using XB1ControllerBatteryIndicator.Localization;

namespace XB1ControllerBatteryIndicator
{
    /// <summary>
    ///     Interaction logic for SystemTrayView.xaml
    /// </summary>
    public partial class SystemTrayView : Window
    {
        private SystemTrayViewModel ViewModel => DataContext as SystemTrayViewModel;

        public SystemTrayView()
        {
            InitializeComponent();
            CheckForUpdate();
            this.ShowInTaskbar = false;

            var language = new CultureInfo(Properties.Settings.Default.Language);
            TranslationManager.CurrentLanguage = language;
        }
        RegistryKey autoStartKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
        private string appID = "XB1ControllerBatteryIndicator";
        string xmlUrl = "http://xb1cbi.kienai.de/current_version.xml";

        //create autostart registry key
        private void StartWithWindows()
        {
            var processModule = Process.GetCurrentProcess().MainModule;
            if (processModule == null) return;
            var exePath = processModule.FileName;
            autoStartKey.SetValue(appID, exePath);
        }
        //remove autostart key
        private void RemoveAutoStart()
        {
            autoStartKey.DeleteValue(appID, false);
        }
        //check if a newer version is available
        private void CheckForUpdate()
        {
            var updateCheck = Properties.Settings.Default.UpdateCheck;
            if (updateCheck != true) return;
            Version newVersion = null;
            var updateUrl = "";
            var reader = new XmlTextReader(xmlUrl);
            try
            {
                reader.MoveToContent();
                var elementName = "";
                if (reader.NodeType == XmlNodeType.Element && reader.Name == appID)
                {
                    while (reader.Read())
                    {
                        switch (reader.NodeType)
                        {
                            case XmlNodeType.Element:
                                elementName = reader.Name;
                                break;
                            case XmlNodeType.Text when reader.HasValue:
                                switch (elementName)
                                {
                                    case "version":
                                        newVersion = new Version(reader.Value);
                                        break;
                                    case "url":
                                        updateUrl = reader.Value;
                                        break;
                                }
                                break;
                        }
                    }
                }
            }
            catch (Exception)
            {
                // ignored
            }
            finally
            {
                reader.Close();
            }

            if ((newVersion == null) || (updateUrl == "")) return;
            var curVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            if (curVersion.CompareTo(newVersion) >= 0) return;
            var title = Strings.NewVersionAvailable_Title;
            var question = string.Format(Strings.NewVersionAvailable_Body, appID);
            if (MessageBoxResult.Yes == MessageBox.Show(this, question, title, MessageBoxButton.YesNo, MessageBoxImage.Question))
            {
                System.Diagnostics.Process.Start(updateUrl);
            }
        }
        //autostart-checkbox was clicked
        private void AutoStart_Click(object sender, RoutedEventArgs e)
        {
            //for whatever reason the autostart-Bool always had the reverse value here, so I had to negate it for the check to work...
            var autorunCheck = !Properties.Settings.Default.AutoStart;
            if (autorunCheck == false)
            {
                Properties.Settings.Default.AutoStart = true;
                Properties.Settings.Default.Save();
                this.StartWithWindows();
            }
            else
            {
                Properties.Settings.Default.AutoStart = false;
                Properties.Settings.Default.Save();
                this.RemoveAutoStart();
            }
        }
        //update-checkbox was clicked
        private void Update_Click(object sender, RoutedEventArgs e)
        {
            //as with the autostart-bool, this one has to be negated too...
            var updateCheck = !Properties.Settings.Default.UpdateCheck;
            if (updateCheck == false)
            {
                Properties.Settings.Default.UpdateCheck = true;
                Properties.Settings.Default.Save();
                this.CheckForUpdate();
            }
            else
            {
                Properties.Settings.Default.UpdateCheck = false;
                Properties.Settings.Default.Save();
            }
        }
        //lowBatteryWarningSound_Enabled-checkbox was clicked
        private void LowBatteryWarningSound_Enabled_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("LowBatteryWarningSound_Enabled_Click");

            var lowBatteryWarningSoundEnabled = !Properties.Settings.Default.LowBatteryWarningSound_Enabled;
            if (lowBatteryWarningSoundEnabled == false)
            {
                var openWav = new OpenFileDialog
                {
                    DefaultExt = ".wav",
                    Filter = "WAV audio (*.wav)|*.wav",
                    InitialDirectory = "C:\\Windows\\media\\"
                };
                var wavResult = openWav.ShowDialog(Application.Current.MainWindow);
                if (wavResult.GetValueOrDefault())
                {
                    Debug.WriteLine(openWav.FileName);
                    Properties.Settings.Default.wavFile = openWav.FileName;
                    Properties.Settings.Default.LowBatteryWarningSound_Enabled = true;
                }
                else
                {
                    Properties.Settings.Default.wavFile = string.Empty;
                    Properties.Settings.Default.LowBatteryWarningSound_Enabled = false;
                }
            }
            else
            {
                Properties.Settings.Default.wavFile = string.Empty;
                Properties.Settings.Default.LowBatteryWarningSound_Enabled = false;
            }
            Properties.Settings.Default.Save();
            ViewModel.UpdateNotificationSound();
        }
        //lowBatteryWarningSound_Loop_Enabled-checkbox was clicked
        private void LowBatteryWarningSound_Loop_Enabled_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("LowBatteryWarningSound_Loop_Enabled_Click");

            var lowBatteryWarningSoundEnabled = !Properties.Settings.Default.LowBatteryWarningSound_Loop_Enabled;
            Properties.Settings.Default.LowBatteryWarningSound_Loop_Enabled = lowBatteryWarningSoundEnabled == false;
            Properties.Settings.Default.Save();
        }
        //a language item checkbox was clicked
        private void LanguageItem_OnClick(object sender, RoutedEventArgs e)
        {
            var selectedLanguage = (CultureInfo)((FrameworkElement)e.OriginalSource).DataContext;
            TranslationManager.CurrentLanguage = selectedLanguage;

            Properties.Settings.Default.Language = selectedLanguage.Name;
            Properties.Settings.Default.Save();
        }
    }
    //this enabled using the values stored in the settings file to be used in XAML
    public class SettingBindingExtension : Binding
    {
        public SettingBindingExtension()
        {
            Initialize();
        }

        public SettingBindingExtension(string path)
            : base(path)
        {
            Initialize();
        }

        private void Initialize()
        {
            this.Source = Properties.Settings.Default;
            this.Mode = BindingMode.TwoWay;
        }
    }
}