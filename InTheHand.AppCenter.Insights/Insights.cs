using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Xamarin
{
    public class Insights
    {
       
        internal  static bool _isDebug = false;
        
        public static void Identify(string uid, string key, string value)
        {
            Identify(uid, new Dictionary<string, string> { { key, value } });
        }
        
        public static void Identify(string uid, IDictionary<string, string> table = null)
        {
            if (!_isDebug)
            {
                CustomProperties properties = new CustomProperties();
                properties.Set("Unique ID", uid);

                if (table != null)
                {
                    foreach (KeyValuePair<string, string> item in table)
                    {
                        properties.Set(item.Key, item.Value);
                    }
                }

                AppCenter.SetCustomProperties(properties);
            }
        }

        public static void Initialize(string apiKey, bool blockOnStartupCrashes = false)
        {
            _isDebug = apiKey == DebugModeKey;

            // don't startup analytics if using debug key
            if (!_isDebug)
            {
                if (!AppCenter.Configured)
                    AppCenter.Start(apiKey, typeof(Analytics), typeof(Crashes));

                
            }
        }

        /// <summary>
        /// Not used.
        /// </summary>
        /// <returns></returns>
        [Obsolete("PurgePendingCrashReports is ignored for AppCenter", false)]
        public static async Task PurgePendingCrashReports()
        {
        }

        public static void Report(Exception exception = null, Severity warningLevel = 0)
        {
            Report(exception, null, warningLevel);
        }

        public static void Report(Exception exception, string key, string value, Severity warningLevel = 0)
        {
            Report(exception, new Dictionary<string, string> { { key, value } }, warningLevel);
        }

        public static void Report(Exception exception, IDictionary<string, string> extraData, Severity warningLevel = 0)
        {
            if (!_isDebug)
            {
                string name = "Unknown Error";
                IDictionary<string, string> properties = extraData == null ? new Dictionary<string, string>() : extraData;

                if (exception != null)
                {
                    name = exception.GetType().Name;
                    properties.Add("Message", exception.Message);
                    if (!string.IsNullOrEmpty(exception.StackTrace))
                    {
                        properties.Add("StackTrace", exception.StackTrace);
                    }
                }

                properties.Add("Severity", warningLevel.ToString());

                Analytics.TrackEvent(name, properties);
            }
        }

        /// <summary>
        /// Not used.
        /// </summary>
        /// <returns></returns>
        [Obsolete("Save is ignored for AppCenter", false)]
        public static async Task Save()
        {
        }

        public static void Track(string trackIdentifier, string key, string value)
        {
            Track(trackIdentifier, new Dictionary<string, string> { { key, value } });
        }

        public static void Track(string trackIdentifier, IDictionary<string, string> table = null)
        {
            Analytics.TrackEvent(trackIdentifier, table);
        }

        public static ITrackHandle TrackTime(string trackIdentifier, string key, string value)
        {
            return TrackTime(trackIdentifier, new Dictionary<string, string> { { key, value } });
        }
        public static ITrackHandle TrackTime(string trackIdentifier, IDictionary<string, string> table = null)
        {
            return new TrackHandle(trackIdentifier, table);
        }

        [Obsolete("DisableCollection is ignored for AppCenter", false)]
        public static bool DisableCollection
        {
            get;set;
        }

        [Obsolete("DisableCollectionTypes is ignored for AppCenter", false)]
        public static CollectionTypes DisableCollectionTypes
        {
            get; set;
        }

        public static bool DisableDataTransmission
        {
            get
            {
                if (!_isDebug)
                {
                    var t = AppCenter.IsEnabledAsync();
                    t.Wait();
                    return !t.Result;
                }

                return false;
            }
            set
            {
                if (!_isDebug)
                {
                    AppCenter.SetEnabledAsync(!value);
                }
            }
        }

        public static bool DisableExceptionCatching
        {
            get
            {
                if (!_isDebug)
                {
                    var t = Crashes.IsEnabledAsync();
                    t.Wait();
                    return !t.Result;
                }

                return false;
            }
            set
            {
                if (!_isDebug)
                {
                    Crashes.SetEnabledAsync(!value);
                }
            }
        }

        [Obsolete("ForceDataTransmission is ignored for AppCenter", false)]
        public static bool ForceDataTransmission
        {
            get;set;
        }

        public static bool IsInitialized
        {
            get
            {
                return _isDebug || AppCenter.Configured;
            }
        }

        public static string DebugModeKey = "DEBUG";

        public enum CollectionTypes
        {
            None,
            HardwareInfo,
            Jailbroken,
            Locale,
            NetworkInfo,
            OsInfo,
        }

        public enum Severity
        {
            Warning = 0,
            Error,
            Critical,
        }

        public enum ReportSeverity
        {
            Warning = 0,
            Error,
        }

        private static event HasPendingCrashReportEventHandler hasPendingCrashReport;

        public static event HasPendingCrashReportEventHandler HasPendingCrashReport
        {
            add
            {
                hasPendingCrashReport += value;

                Task.Run(async () =>
                {
                    bool hasCrashed = await Crashes.HasCrashedInLastSessionAsync();
                    if (hasCrashed)
                    {
                        bool isStartup = false;
                        var errorReport = await Crashes.GetLastSessionCrashReportAsync();
                        if (errorReport != null && (errorReport.AppErrorTime - errorReport.AppStartTime).TotalSeconds < 5)
                            isStartup = true;

                        hasPendingCrashReport?.Invoke(null, isStartup);
                    }
                });
            }

            remove
            {
                hasPendingCrashReport -= value;
            }
        }

        public delegate void HasPendingCrashReportEventHandler(object sender, bool isStartupCrash);


        public static class Traits
        {
            public const string Address = "Address";
            public const string Age = "Age";
            public const string Avatar = "Avatar";
            public const string CreatedAt = "CreatedAt";
            public const string DateOfBirth = "DateOfBirth";
            public const string Description = "Description";
            public const string Email = "Email";
            public const string FirstName = "FirstName";
            public const string Gender = "Gender";
            public const string GuestIdentifier = "GuestIdentifier";
            public const string LastName = "LastName";
            public const string Name = "Name";
            public const string Phone = "Phone";
            public const string Website = "Website";
        }
    }

    public interface ITrackHandle
    {
        void Start();
        void Stop();

        IDictionary<string, string> Data
        {
            get;
        }
    }

    internal class TrackHandle : ITrackHandle
    {
        private string _name = "Unknown";
        private DateTimeOffset _startTime = DateTimeOffset.MinValue;
        private IDictionary<string, string> _data;

        internal TrackHandle(string name, IDictionary<string, string> data)
        {
            _name = name;
            _data = data == null ? new Dictionary<string, string>() : data;
        }
        public IDictionary<string, string> Data
        {
            get
            {
                return _data;
            }
        }

        public void Start()
        {
            _startTime = DateTimeOffset.Now;
        }

        public void Stop()
        {
            if (_startTime == DateTimeOffset.MinValue)
                throw new InvalidOperationException("Start not called");

            _data.Add("Duration", string.Format("{0:f0}s",(DateTimeOffset.Now - _startTime).TotalSeconds));

            if (!Insights._isDebug)
            {
                Analytics.TrackEvent(_name, _data);
            }
        }

    }
}
