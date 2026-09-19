using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Management;
using System.Media;
using System.Security.Principal;
using System.Threading;
using Microsoft.Win32;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using XB1ControllerBatteryIndicator.Localization;
using XB1ControllerBatteryIndicator.Properties;
using XB1ControllerBatteryIndicator.ShellHelpers;

namespace XB1ControllerBatteryIndicator
{
    public class SystemTrayViewModel : Caliburn.Micro.Screen
    {
        private const string AppId = "NiyaShy.XB1ControllerBatteryIndicator";
        private const string ThemeRegKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
        private const string ThemeRegValueName = "SystemUsesLightTheme";
        private const int ControllerCount = 4;
        private const int PollIntervalMs = 1000;
        private const int DisplayRotationIntervalMs = 5000;

        private string _activeIcon;
        private string _tooltipText;
        private readonly bool[] _toastShown = new bool[ControllerCount];
        private readonly bool[] _lowBatterySoundPlayed = new bool[ControllerCount];
        private readonly DateTime[] _nextLoopSoundUtc = new DateTime[ControllerCount];
        private volatile string _themeSuffix = string.Empty;
        private SoundPlayer _soundPlayer;
        private ManagementEventWatcher _themeWatcher;

        public SystemTrayViewModel()
        {
            GetAvailableLanguages();
            TranslationManager.CurrentLanguageChangedEvent += (sender, args) => GetAvailableLanguages();
            UpdateNotificationSound();

            RefreshThemeSuffix();
            ActiveIcon = "Resources/battery_unknown" + _themeSuffix + ".ico";

            TryCreateShortcut();

            var pollingThread = new Thread(RefreshControllerState)
            {
                IsBackground = true,
                Name = "Xbox controller battery polling"
            };
            pollingThread.Start();
        }

        public string ActiveIcon
        {
            get { return _activeIcon; }
            private set { Set(ref _activeIcon, value); }
        }

        public string TooltipText
        {
            get { return _tooltipText; }
            private set { Set(ref _tooltipText, value); }
        }

        public ObservableCollection<CultureInfo> AvailableLanguages { get; } = new ObservableCollection<CultureInfo>();

        private void RefreshControllerState()
        {
            var displayControllerIndex = -1;
            var nextDisplayRotationUtc = DateTime.MinValue;

            while (true)
            {
                try
                {
                    var controllers = new[]
                    {
                        new XboxController(UserIndex.One),
                        new XboxController(UserIndex.Two),
                        new XboxController(UserIndex.Three),
                        new XboxController(UserIndex.Four)
                    };

                    var nowUtc = DateTime.UtcNow;
                    var anyConnected = false;

                    for (var i = 0; i < controllers.Length; i++)
                    {
                        var controller = controllers[i];

                        if (!controller.IsConnected)
                        {
                            ResetControllerAlertState(i, controller.UserIndex);
                            continue;
                        }

                        anyConnected = true;
                        ProcessControllerAlerts(controller, i, nowUtc);
                    }

                    if (!anyConnected)
                    {
                        displayControllerIndex = -1;
                        nextDisplayRotationUtc = DateTime.MinValue;
                        TooltipText = Strings.ToolTip_NoController;
                        ActiveIcon = "Resources/battery_unknown" + _themeSuffix + ".ico";
                    }
                    else
                    {
                        if (displayControllerIndex < 0 || !controllers[displayControllerIndex].IsConnected)
                        {
                            displayControllerIndex = FindNextConnectedController(controllers, -1);
                            nextDisplayRotationUtc = nowUtc.AddMilliseconds(DisplayRotationIntervalMs);
                        }
                        else if (nowUtc >= nextDisplayRotationUtc)
                        {
                            displayControllerIndex = FindNextConnectedController(controllers, displayControllerIndex);
                            nextDisplayRotationUtc = nowUtc.AddMilliseconds(DisplayRotationIntervalMs);
                        }

                        if (displayControllerIndex >= 0)
                            UpdateDisplayedController(controllers[displayControllerIndex]);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }

                // Always throttle the polling loop, including after exceptions.
                Thread.Sleep(PollIntervalMs);
            }
        }

        private static int FindNextConnectedController(XboxController[] controllers, int currentIndex)
        {
            for (var offset = 1; offset <= controllers.Length; offset++)
            {
                var candidate = (currentIndex + offset + controllers.Length) % controllers.Length;
                if (controllers[candidate].IsConnected)
                    return candidate;
            }

            return -1;
        }

        private void ProcessControllerAlerts(XboxController controller, int controllerIndex, DateTime nowUtc)
        {
            if (controller.BatteryLevel != BatteryLevel.Empty)
            {
                if (_toastShown[controllerIndex])
                {
                    _toastShown[controllerIndex] = false;
                    ToastNotificationManager.History.Remove(
                        "Controller" + controller.UserIndex,
                        "ControllerToast",
                        AppId);
                }

                _lowBatterySoundPlayed[controllerIndex] = false;
                _nextLoopSoundUtc[controllerIndex] = DateTime.MinValue;
                return;
            }

            if (!_toastShown[controllerIndex])
            {
                _toastShown[controllerIndex] = true;
                ShowToast(controller.UserIndex);
            }

            if (!Settings.Default.LowBatteryWarningSound_Enabled)
                return;

            if (Settings.Default.LowBatteryWarningSound_Loop_Enabled)
            {
                if (nowUtc >= _nextLoopSoundUtc[controllerIndex])
                {
                    PlayLowBatterySound();
                    _nextLoopSoundUtc[controllerIndex] = nowUtc.AddMilliseconds(DisplayRotationIntervalMs);
                }
            }
            else if (!_lowBatterySoundPlayed[controllerIndex])
            {
                PlayLowBatterySound();
                _lowBatterySoundPlayed[controllerIndex] = true;
            }
        }

        private void ResetControllerAlertState(int controllerIndex, UserIndex userIndex)
        {
            if (_toastShown[controllerIndex])
            {
                _toastShown[controllerIndex] = false;
                try
                {
                    ToastNotificationManager.History.Remove(
                        "Controller" + userIndex,
                        "ControllerToast",
                        AppId);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            }

            _lowBatterySoundPlayed[controllerIndex] = false;
            _nextLoopSoundUtc[controllerIndex] = DateTime.MinValue;
        }

        private void PlayLowBatterySound()
        {
            try
            {
                if (_soundPlayer != null)
                    _soundPlayer.Play();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private void UpdateDisplayedController(XboxController controller)
        {
            var controllerIndexCaption = GetControllerIndexCaption(controller.UserIndex);
            var controllerIndexName = controller.UserIndex.ToString().ToLowerInvariant();

            switch (controller.BatteryType)
            {
                case BatteryType.Wired:
                    TooltipText = string.Format(Strings.ToolTip_Wired, controllerIndexCaption);
                    ActiveIcon = "Resources/battery_wired_" + controllerIndexName + _themeSuffix + ".ico";
                    break;

                case BatteryType.Disconnected:
                    // XInput can report a connected controller while battery data is not ready yet.
                    TooltipText = string.Format(Strings.ToolTip_WaitingForData, controllerIndexCaption);
                    ActiveIcon = "Resources/battery_disconnected_" + controllerIndexName + _themeSuffix + ".ico";
                    break;

                case BatteryType.Unknown:
                    TooltipText = string.Format(Strings.ToolTip_Unknown, controllerIndexCaption);
                    ActiveIcon = "Resources/battery_disconnected_" + controllerIndexName + _themeSuffix + ".ico";
                    break;

                default:
                    var batteryLevelCaption = GetBatteryLevelCaption(controller.BatteryLevel);
                    TooltipText = string.Format(
                        Strings.ToolTip_Wireless,
                        controllerIndexCaption,
                        batteryLevelCaption);
                    ActiveIcon = "Resources/battery_" +
                                 controller.BatteryLevel.ToString().ToLowerInvariant() +
                                 "_" +
                                 controllerIndexName +
                                 _themeSuffix +
                                 ".ico";
                    break;
            }
        }

        private bool TryCreateShortcut()
        {
            var shortcutPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) +
                               "\\Microsoft\\Windows\\Start Menu\\Programs\\XB1ControllerBatteryIndicator.lnk";

            if (File.Exists(shortcutPath))
                return false;

            InstallShortcut(shortcutPath);
            return true;
        }

        private void InstallShortcut(string shortcutPath)
        {
            var exePath = Process.GetCurrentProcess().MainModule.FileName;
            var newShortcut = (IShellLinkW)new CShellLink();

            ErrorHelper.VerifySucceeded(newShortcut.SetPath(exePath));
            ErrorHelper.VerifySucceeded(newShortcut.SetArguments(string.Empty));

            var newShortcutProperties = (IPropertyStore)newShortcut;

            using (var appId = new PropVariant(AppId))
            {
                ErrorHelper.VerifySucceeded(
                    newShortcutProperties.SetValue(SystemProperties.System.AppUserModel.ID, appId));
                ErrorHelper.VerifySucceeded(newShortcutProperties.Commit());
            }

            var newShortcutSave = (IPersistFile)newShortcut;
            ErrorHelper.VerifySucceeded(newShortcutSave.Save(shortcutPath, true));
        }

        private void ShowToast(UserIndex controllerIndex)
        {
            var controllerId = (int)controllerIndex;
            var controllerIndexCaption = GetControllerIndexCaption(controllerIndex);
            var argsDismiss = "dismissed";
            var argsLaunch = controllerId.ToString(CultureInfo.InvariantCulture);

            var toastVisual =
                @"<visual>
                    <binding template='ToastGeneric'>
                        <text>" + string.Format(Strings.Toast_Title, controllerIndexCaption) + @"</text>
                        <text>" + string.Format(Strings.Toast_Text, controllerIndexCaption) + @"</text>
                        <text>" + Strings.Toast_Text2 + @"</text>
                    </binding>
                  </visual>";

            var toastActions =
                @"<actions>
                    <action content='" + Strings.Toast_Dismiss + "' arguments='" + argsDismiss + @"'/>
                  </actions>";

            var toastXmlString =
                @"<toast scenario='reminder' launch='" + argsLaunch + @"'>" +
                    toastVisual +
                    toastActions +
                  "</toast>";

            var toastXml = new XmlDocument();
            toastXml.LoadXml(toastXmlString);

            var toast = new ToastNotification(toastXml);
            toast.Activated += ToastActivated;
            toast.Dismissed += ToastDismissed;
            toast.Tag = "Controller" + controllerIndex;
            toast.Group = "ControllerToast";

            ToastNotificationManager.CreateToastNotifier(AppId).Show(toast);
        }

        private void ToastActivated(ToastNotification sender, object e)
        {
            var toastArgs = e as ToastActivatedEventArgs;
            int controllerId;

            if (toastArgs != null &&
                int.TryParse(toastArgs.Arguments, out controllerId) &&
                controllerId >= 0 &&
                controllerId < ControllerCount)
            {
                _toastShown[controllerId] = false;
            }
        }

        private void ToastDismissed(ToastNotification sender, object e)
        {
        }

        public void ExitApplication()
        {
            if (_themeWatcher != null)
            {
                try
                {
                    _themeWatcher.Stop();
                    _themeWatcher.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            }

            System.Windows.Application.Current.Shutdown();
        }

        private static string GetBatteryLevelCaption(BatteryLevel batteryLevel)
        {
            switch (batteryLevel)
            {
                case BatteryLevel.Empty:
                    return Strings.BatteryLevel_Empty;
                case BatteryLevel.Low:
                    return Strings.BatteryLevel_Low;
                case BatteryLevel.Medium:
                    return Strings.BatteryLevel_Medium;
                case BatteryLevel.Full:
                    return Strings.BatteryLevel_Full;
                default:
                    throw new ArgumentOutOfRangeException("batteryLevel", batteryLevel, null);
            }
        }

        private static string GetControllerIndexCaption(UserIndex index)
        {
            switch (index)
            {
                case UserIndex.One:
                    return Strings.ControllerIndex_One;
                case UserIndex.Two:
                    return Strings.ControllerIndex_Two;
                case UserIndex.Three:
                    return Strings.ControllerIndex_Three;
                case UserIndex.Four:
                    return Strings.ControllerIndex_Four;
                default:
                    throw new ArgumentOutOfRangeException("index", index, null);
            }
        }

        private void GetAvailableLanguages()
        {
            AvailableLanguages.Clear();

            foreach (var language in TranslationManager.AvailableLanguages)
                AvailableLanguages.Add(language);
        }

        public void UpdateNotificationSound()
        {
            _soundPlayer = File.Exists(Settings.Default.wavFile)
                ? new SoundPlayer(Settings.Default.wavFile)
                : null;
        }

        public void WatchTheme()
        {
            var currentUser = WindowsIdentity.GetCurrent();
            var userSid = currentUser.User != null ? currentUser.User.Value : string.Empty;

            var query = string.Format(
                CultureInfo.InvariantCulture,
                @"SELECT * FROM RegistryValueChangeEvent WHERE Hive = 'HKEY_USERS' AND KeyPath = '{0}\\{1}' AND ValueName = '{2}'",
                userSid,
                ThemeRegKeyPath.Replace(@"\", @"\\"),
                ThemeRegValueName);

            try
            {
                _themeWatcher = new ManagementEventWatcher(query);
                _themeWatcher.EventArrived += (sender, args) => RefreshThemeSuffix();
                _themeWatcher.Start();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                if (_themeWatcher != null)
                {
                    _themeWatcher.Dispose();
                    _themeWatcher = null;
                }
            }

            RefreshThemeSuffix();
        }

        private void RefreshThemeSuffix()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(ThemeRegKeyPath))
                {
                    var registryValueObject = key != null ? key.GetValue(ThemeRegValueName) : null;
                    if (registryValueObject == null)
                    {
                        _themeSuffix = string.Empty;
                        return;
                    }

                    var registryValue = Convert.ToInt32(registryValueObject, CultureInfo.InvariantCulture);
                    _themeSuffix = registryValue > 0 ? "-black" : string.Empty;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                _themeSuffix = string.Empty;
            }
        }
    }
}
