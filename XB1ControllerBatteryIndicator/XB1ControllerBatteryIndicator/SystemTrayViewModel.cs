using System.Linq;
using System.Threading;
using Windows.UI.Notifications;
using Windows.Data.Xml.Dom;
using System;
using System.Management;
using System.Collections.ObjectModel;
using System.IO;
using System.Diagnostics;
using System.Globalization;
using System.Media;
using XB1ControllerBatteryIndicator.ShellHelpers;
using XB1ControllerBatteryIndicator.Localization;
using XB1ControllerBatteryIndicator.Properties;
using System.Security.Principal;
using Microsoft.Win32;

namespace XB1ControllerBatteryIndicator
{
    public class SystemTrayViewModel : Caliburn.Micro.Screen
    {
        private const string AppId = "NiyaShy.XB1ControllerBatteryIndicator";
        private const string ThemeRegKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
        private const string ThemeRegValueName = "SystemUsesLightTheme";
        
        private string _activeIcon;
        private XboxController _controller;
        private string _tooltipText;
        private readonly bool[] _toastShown = new bool[5];
        private volatile string _themeSuffix = "";

        private SoundPlayer _soundPlayer;

        public SystemTrayViewModel()
        {
            GetAvailableLanguages();
            TranslationManager.CurrentLanguageChangedEvent += (_, _) => GetAvailableLanguages();
            UpdateNotificationSound();

            RefreshThemeSuffix();
            ActiveIcon = $"Resources/battery_unknown{_themeSuffix}.ico";
            TryCreateShortcut();
            var th = new Thread(RefreshControllerState)
            {
                IsBackground = true
            };
            th.Start();
        }

        public string ActiveIcon
        {
            get => _activeIcon;
            private set => Set(ref _activeIcon, value);
        }

        public string TooltipText
        {
            get => _tooltipText;
            private set => Set(ref _tooltipText, value);
        }

        public ObservableCollection<CultureInfo> AvailableLanguages { get; } = new ObservableCollection<CultureInfo>();

        private void RefreshControllerState()
        {
            var lowBatteryWarningSoundPlayed = false;

            while(true)
            {
                try
                {
                    //Initialize controllers
                    var controllers = new[]
                    {
                    new XboxController(UserIndex.One), new XboxController(UserIndex.Two), new XboxController(UserIndex.Three),
                    new XboxController(UserIndex.Four)
                    };
                    //Check if at least one is present
                    _controller = controllers.FirstOrDefault(selectController => selectController.IsConnected);

                    if (_controller != null)
                    {
                        //cycle through all recognized controllers
                        foreach (var currentController in controllers)
                        {
                            var controllerIndexCaption = GetControllerIndexCaption(currentController.UserIndex);
                            if (!currentController.IsConnected) continue;
                            //check if toast was already triggered and battery is no longer empty...
                            if (currentController.BatteryLevel != BatteryLevel.Empty)
                            {
                                if (_toastShown[(int)currentController.UserIndex + 1])
                                {
                                    //...reset the notification
                                    _toastShown[(int)currentController.UserIndex + 1] = false;
                                    ToastNotificationManager.History.Remove($"Controller{currentController.UserIndex}", "ControllerToast", AppId);
                                }
                            }

                            switch (currentController.BatteryType)
                            {
                                //wired
                                case BatteryType.Wired:
                                    TooltipText = string.Format(Strings.ToolTip_Wired, controllerIndexCaption);
                                    ActiveIcon = $"Resources/battery_wired_{currentController.UserIndex.ToString().ToLower() + _themeSuffix}.ico";
                                    break;
                                //"disconnected", a controller that was detected but hasn't sent battery data yet has this state
                                case BatteryType.Disconnected:
                                    TooltipText = string.Format(Strings.ToolTip_WaitingForData, controllerIndexCaption);
                                    ActiveIcon = $"Resources/battery_disconnected_{currentController.UserIndex.ToString().ToLower() + _themeSuffix}.ico";
                                    break;
                                //this state should never happen
                                case BatteryType.Unknown:
                                    TooltipText = string.Format(Strings.ToolTip_Unknown, controllerIndexCaption);
                                    ActiveIcon = $"Resources/battery_disconnected_{currentController.UserIndex.ToString().ToLower() + _themeSuffix}.ico";
                                    break;
                                //a battery level was detected
                                default:
                                {
                                    var batteryLevelCaption = GetBatteryLevelCaption(currentController.BatteryLevel);
                                    TooltipText = string.Format(Strings.ToolTip_Wireless, controllerIndexCaption, batteryLevelCaption);
                                    ActiveIcon = $"Resources/battery_{currentController.BatteryLevel.ToString().ToLower()}_{currentController.UserIndex.ToString().ToLower() + _themeSuffix}.ico";
                                    //when "empty" state is detected...
                                    if (currentController.BatteryLevel == BatteryLevel.Empty)
                                    {
                                        //check if toast (notification) for current controller was already triggered
                                        if (_toastShown[(int)currentController.UserIndex + 1] == false)
                                        {
                                            //if not, trigger it
                                            _toastShown[(int)currentController.UserIndex + 1] = true;
                                            ShowToast(currentController.UserIndex);
                                        }
                                        //check if notification sound is enabled
                                        if (Settings.Default.LowBatteryWarningSound_Enabled)
                                        {
                                            if (Settings.Default.LowBatteryWarningSound_Loop_Enabled || !lowBatteryWarningSoundPlayed)
                                            {
                                                //Necessary to avoid crashing if the .wav file is missing
                                                try
                                                {
                                                    _soundPlayer?.Play();
                                                }
                                                catch (Exception ex)
                                                {
                                                    Debug.WriteLine(ex);
                                                }
                                                lowBatteryWarningSoundPlayed = true;
                                            }
                                        }
                                    }

                                    break;
                                }
                            }
                        }
                        Thread.Sleep(5000);
                    }
                    else
                    {
                        TooltipText = Strings.ToolTip_NoController;
                        ActiveIcon = $"Resources/battery_unknown{_themeSuffix}.ico";
                    }
                    Thread.Sleep(1000);
                }
                catch (Exception)
                {
                    // ignored, but still throttle the retry so a persistent failure
                    // (e.g. XInput DLL missing) can't spin this loop at 100% CPU
                    Thread.Sleep(1000);
                }
            }
            // ReSharper disable once FunctionNeverReturns
        }

        //try to create a start menu shortcut (required for sending toasts)
        private bool TryCreateShortcut()
        {
            var shortcutPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Microsoft\\Windows\\Start Menu\\Programs\\XB1ControllerBatteryIndicator.lnk";
            if (File.Exists(shortcutPath)) return false;
            InstallShortcut(shortcutPath);
            return true;
        }
        
        //create the shortcut
        private void InstallShortcut(string shortcutPath)
        {
            // Find the path to the current executable 
            var exePath = Process.GetCurrentProcess().MainModule?.FileName;
            var newShortcut = (IShellLinkW)new CShellLink();

            // Create a shortcut to the exe 
            ErrorHelper.VerifySucceeded(newShortcut.SetPath(exePath));
            ErrorHelper.VerifySucceeded(newShortcut.SetArguments(""));

            // Open the shortcut property store, set the AppUserModelId property 
            var newShortcutProperties = (IPropertyStore)newShortcut;

            using (var appId = new PropVariant(AppId))
            {
                ErrorHelper.VerifySucceeded(newShortcutProperties.SetValue(SystemProperties.System.AppUserModel.ID, appId));
                ErrorHelper.VerifySucceeded(newShortcutProperties.Commit());
            }

            // Commit the shortcut to disk 
            var newShortcutSave = (IPersistFile)newShortcut;

            ErrorHelper.VerifySucceeded(newShortcutSave.Save(shortcutPath, true));
        }
        //send a toast
        private void ShowToast(UserIndex controllerIndex)
        {
            var controllerId = (int)controllerIndex + 1;
            var controllerIndexCaption = GetControllerIndexCaption(controllerIndex);
            var argsDismiss = $"dismissed";
            var argsLaunch = $"{controllerId}";
            //how the content gets arranged
            var toastVisual =
                $@"<visual>
                        <binding template='ToastGeneric'>
                            <text>{string.Format(Strings.Toast_Title, controllerIndexCaption)}</text>
                            <text>{string.Format(Strings.Toast_Text, controllerIndexCaption)}</text>
                            <text>{Strings.Toast_Text2}</text>
                        </binding>
                    </visual>";
            //Button on the toast
            var toastActions =
                $"""
                     <actions>
                         <action content='{Strings.Toast_Dismiss}' arguments='{argsDismiss}'/>
                    </actions>
                 """;
            //combine content and button
            var toastXmlString =
                $"""
                     <toast scenario='reminder' launch='{argsLaunch}'>
                         {toastVisual}
                         {toastActions}
                    </toast>
                 """;

            var toastXml = new XmlDocument();
            toastXml.LoadXml(toastXmlString);
            //create the toast
            var toast = new ToastNotification(toastXml);
            toast.Activated += ToastActivated;
            toast.Dismissed += ToastDismissed;
            toast.Tag = $"Controller{controllerIndex}";
            toast.Group = "ControllerToast";
            //...and send it
            ToastNotificationManager.CreateToastNotifier(AppId).Show(toast);

        }
        //react to click on toast or button
        private void ToastActivated(ToastNotification sender, object e)
        {
            var toastArgs = e as ToastActivatedEventArgs;
            //if the return value contains a controller ID
            if (int.TryParse(toastArgs?.Arguments, out var controllerId))
            {
                //reset the toast warning (it will trigger again if battery level is still empty)
                _toastShown[controllerId] = false;
            }
            //otherwise, do nothing
        }
        private static void ToastDismissed(ToastNotification sender, object e)
        {
            //do nothing
        }

        public void ExitApplication()
        {
            System.Windows.Application.Current.Shutdown();
        }

        private static string GetBatteryLevelCaption(BatteryLevel batteryLevel)
        {
            return batteryLevel switch
            {
                BatteryLevel.Empty => Strings.BatteryLevel_Empty,
                BatteryLevel.Low => Strings.BatteryLevel_Low,
                BatteryLevel.Medium => Strings.BatteryLevel_Medium,
                BatteryLevel.High => Strings.BatteryLevel_Full,
                _ => throw new ArgumentOutOfRangeException(nameof(batteryLevel), batteryLevel, null)
            };
        }

        private string GetControllerIndexCaption(UserIndex index)
        {
            return index switch
            {
                UserIndex.One => Strings.ControllerIndex_One,
                UserIndex.Two => Strings.ControllerIndex_Two,
                UserIndex.Three => Strings.ControllerIndex_Three,
                UserIndex.Four => Strings.ControllerIndex_Four,
                _ => throw new ArgumentOutOfRangeException(nameof(index), index, null)
            };
        }

        private void GetAvailableLanguages()
        {
            AvailableLanguages.Clear();
            foreach (var language in TranslationManager.AvailableLanguages)
            {
                AvailableLanguages.Add(language);
            }
        }

        public void UpdateNotificationSound()
        {
            _soundPlayer = File.Exists(Settings.Default.wavFile) ? new SoundPlayer(Settings.Default.wavFile) : null;
        }
        public void WatchTheme()
        {
            var currentUser = WindowsIdentity.GetCurrent();
            var query = string.Format(
                CultureInfo.InvariantCulture,
                @"SELECT * FROM RegistryValueChangeEvent WHERE Hive = 'HKEY_USERS' AND KeyPath = '{0}\\{1}' AND ValueName = '{2}'",
                currentUser.User?.Value,
                ThemeRegKeyPath.Replace(@"\", @"\\"),
                ThemeRegValueName);

            try
            {
                var watcher = new ManagementEventWatcher(query);
                watcher.EventArrived += (_, _) =>
                {
                    RefreshThemeSuffix();
                };

                // Start listening for events
                watcher.Start();
            }
            catch (Exception)
            {
                // This can fail on Windows 7
            }

            RefreshThemeSuffix();
        }

        //re-reads the theme registry value and caches the result, so callers on the
        //polling thread don't hit the registry on every icon update
        private void RefreshThemeSuffix()
        {
            using var key = Registry.CurrentUser.OpenSubKey(ThemeRegKeyPath);
            var registryValueObject = key?.GetValue(ThemeRegValueName);
            if (registryValueObject == null)
            {
                _themeSuffix = "";
                return;
            }

            var registryValue = (int)registryValueObject;

            _themeSuffix = registryValue > 0 ? "-black" : "";
        }
    }
}