using System;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Xml.Linq;
using Microsoft.Win32;
using XB1ControllerBatteryIndicator.Localization;

namespace XB1ControllerBatteryIndicator
{
    /// <summary>
    /// Interaction logic for SystemTrayView.xaml.
    /// </summary>
    public partial class SystemTrayView : Window
    {
        private const string AppId = "XB1ControllerBatteryIndicator";
        private const string VersionFeedUrl =
            "https://raw.githubusercontent.com/wefalltomorrow/XB1ControllerBatteryIndicator/master/current_version.xml";
        private const string AutoStartRegistryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

        private SystemTrayViewModel ViewModel
        {
            get { return DataContext as SystemTrayViewModel; }
        }

        public SystemTrayView()
        {
            InitializeComponent();
            ShowInTaskbar = false;

            var language = new CultureInfo(Properties.Settings.Default.Language);
            TranslationManager.CurrentLanguage = language;

            if (Properties.Settings.Default.UpdateCheck)
                CheckForUpdateAsync(false);
        }

        private void StartWithWindows()
        {
            var exePath = Process.GetCurrentProcess().MainModule.FileName;

            using (var key = Registry.CurrentUser.CreateSubKey(AutoStartRegistryPath))
            {
                if (key == null)
                    throw new InvalidOperationException("Unable to open the Windows startup registry key.");

                // Quote the path so installations under folders containing spaces work correctly.
                key.SetValue(AppId, "\\\"" + exePath + "\\\"");
            }
        }

        private void RemoveAutoStart()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(AutoStartRegistryPath, true))
            {
                if (key != null)
                    key.DeleteValue(AppId, false);
            }
        }

        private async Task CheckForUpdateAsync(bool manual)
        {
            if (!manual && !Properties.Settings.Default.UpdateCheck)
                return;

            try
            {
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

                string xml;
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(5);
                    xml = await client.GetStringAsync(VersionFeedUrl);
                }

                var document = XDocument.Parse(xml);
                var root = document.Root;
                if (root == null || !string.Equals(root.Name.LocalName, AppId, StringComparison.Ordinal))
                    throw new InvalidOperationException("The update feed has an unexpected format.");

                var versionElement = root.Element("version");
                var urlElement = root.Element("url");

                Version newVersion;
                if (versionElement == null ||
                    !Version.TryParse(versionElement.Value, out newVersion) ||
                    urlElement == null ||
                    string.IsNullOrWhiteSpace(urlElement.Value))
                {
                    throw new InvalidOperationException("The update feed is missing required values.");
                }

                var currentVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                if (currentVersion.CompareTo(newVersion) >= 0)
                {
                    if (manual)
                    {
                        MessageBox.Show(
                            this,
                            Strings.UpdateCheck_UpToDate_Body,
                            Strings.UpdateCheck_UpToDate_Title,
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }

                    return;
                }

                var newVersionText = newVersion.ToString();
                if (!manual &&
                    string.Equals(
                        Properties.Settings.Default.LastDismissedUpdateVersion,
                        newVersionText,
                        StringComparison.OrdinalIgnoreCase))
                {
                    AppLogger.Info("Update " + newVersionText + " is available but was previously dismissed.");
                    return;
                }

                AppLogger.Info("Update available: " + currentVersion + " -> " + newVersionText);

                var result = MessageBox.Show(
                    this,
                    string.Format(Strings.NewVersionAvailable_Body, AppId),
                    Strings.NewVersionAvailable_Title,
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    Properties.Settings.Default.LastDismissedUpdateVersion = string.Empty;
                    Properties.Settings.Default.Save();

                    Process.Start(new ProcessStartInfo(urlElement.Value)
                    {
                        UseShellExecute = true
                    });
                }
                else
                {
                    Properties.Settings.Default.LastDismissedUpdateVersion = newVersionText;
                    Properties.Settings.Default.Save();
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("Update check failed.", ex);

                if (manual)
                {
                    MessageBox.Show(
                        this,
                        Strings.UpdateCheck_Failed_Body,
                        Strings.UpdateCheck_Failed_Title,
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
        }

        private void AutoStart_Click(object sender, RoutedEventArgs e)
        {
            var item = sender as MenuItem;
            if (item == null)
                return;

            Properties.Settings.Default.AutoStart = item.IsChecked;
            Properties.Settings.Default.Save();

            try
            {
                if (item.IsChecked)
                    StartWithWindows();
                else
                    RemoveAutoStart();

                AppLogger.Info("Start with Windows " + (item.IsChecked ? "enabled." : "disabled."));
            }
            catch (Exception ex)
            {
                AppLogger.Error("Failed to change Start with Windows setting.", ex);
            }
        }

        private async void Update_Click(object sender, RoutedEventArgs e)
        {
            var item = sender as MenuItem;
            if (item == null)
                return;

            Properties.Settings.Default.UpdateCheck = item.IsChecked;
            Properties.Settings.Default.Save();

            if (item.IsChecked)
                await CheckForUpdateAsync(false);
        }

        private async void CheckNow_Click(object sender, RoutedEventArgs e)
        {
            await CheckForUpdateAsync(true);
        }

        private void HideWhenDisconnected_Click(object sender, RoutedEventArgs e)
        {
            var item = sender as MenuItem;
            if (item != null && ViewModel != null)
                ViewModel.SetHideWhenDisconnected(item.IsChecked);
        }

        private void ThemeMode_Click(object sender, RoutedEventArgs e)
        {
            var item = sender as MenuItem;
            int mode;

            if (item != null &&
                item.Tag != null &&
                int.TryParse(item.Tag.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out mode) &&
                ViewModel != null)
            {
                ViewModel.SetThemeMode(mode);
            }
        }

        private void WarningThreshold_Click(object sender, RoutedEventArgs e)
        {
            var item = sender as MenuItem;
            int threshold;

            if (item != null &&
                item.Tag != null &&
                int.TryParse(item.Tag.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out threshold) &&
                ViewModel != null)
            {
                ViewModel.SetWarningThreshold(threshold);
            }
        }

        private void LowBatteryWarningSound_Enabled_Click(object sender, RoutedEventArgs e)
        {
            var item = sender as MenuItem;
            if (item == null)
                return;

            Properties.Settings.Default.LowBatteryWarningSound_Enabled = item.IsChecked;
            Properties.Settings.Default.Save();
        }

        private void LowBatteryWarningSound_Loop_Enabled_Click(object sender, RoutedEventArgs e)
        {
            var item = sender as MenuItem;
            if (item == null)
                return;

            Properties.Settings.Default.LowBatteryWarningSound_Loop_Enabled = item.IsChecked;
            Properties.Settings.Default.Save();
        }

        private void LanguageItem_OnClick(object sender, RoutedEventArgs e)
        {
            var selectedLanguage = (CultureInfo)((FrameworkElement)e.OriginalSource).DataContext;
            TranslationManager.CurrentLanguage = selectedLanguage;

            Properties.Settings.Default.Language = selectedLanguage.Name;
            Properties.Settings.Default.Save();
        }
    }

    // Enables values stored in Settings to be used directly in XAML bindings.
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
            Source = Properties.Settings.Default;
            Mode = BindingMode.TwoWay;
        }
    }
}
