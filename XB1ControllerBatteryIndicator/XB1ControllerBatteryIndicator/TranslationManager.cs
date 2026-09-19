using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Resources;
using XB1ControllerBatteryIndicator.Localization;

namespace XB1ControllerBatteryIndicator
{
    internal static class TranslationManager
    {
        public static IEnumerable<CultureInfo> AvailableLanguages { get; private set; }

        static TranslationManager()
        {
            UpdateAvailableLanguages();
        }

        private static void UpdateAvailableLanguages()
        {
            var resourceManager = new ResourceManager(typeof(Strings));
            var result = new List<CultureInfo>();

            var cultures = CultureInfo.GetCultures(CultureTypes.AllCultures);
            foreach (var culture in cultures.Where(info => !Equals(info, CultureInfo.InvariantCulture)))
            {
                try
                {
                    var resourceSet = resourceManager.GetResourceSet(culture, true, false);
                    if (resourceSet != null)
                        result.Add(culture);
                }
                catch (CultureNotFoundException)
                {
                }
            }

            AvailableLanguages = result;
        }

        public static CultureInfo CurrentLanguage
        {
            get { return CultureInfo.DefaultThreadCurrentUICulture; }
            set
            {
                if (!AvailableLanguages.Contains(value))
                    value = AvailableLanguages.FirstOrDefault();

                if (Equals(CultureInfo.DefaultThreadCurrentUICulture, value))
                    return;

                CultureInfo.DefaultThreadCurrentCulture = value;
                CultureInfo.DefaultThreadCurrentUICulture = value;

                if (CultureInfo.DefaultThreadCurrentCulture != null)
                    CultureInfo.DefaultThreadCurrentCulture.ClearCachedData();

                if (CultureInfo.DefaultThreadCurrentUICulture != null)
                    CultureInfo.DefaultThreadCurrentUICulture.ClearCachedData();

                // The set of embedded translation resources cannot change at runtime,
                // so do not rescan every installed culture on each language switch.
                OnCurrentLanguageChanged();
            }
        }

        private static EventHandler _currentLanguageChangedEvent;

        public static event EventHandler CurrentLanguageChangedEvent
        {
            add { _currentLanguageChangedEvent += value; }
            remove { _currentLanguageChangedEvent -= value; }
        }

        private static void OnCurrentLanguageChanged()
        {
            var handler = _currentLanguageChangedEvent;
            if (handler != null)
                handler(null, EventArgs.Empty);
        }
    }
}
