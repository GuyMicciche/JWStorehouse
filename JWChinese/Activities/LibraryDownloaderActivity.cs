using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Preferences;
using Android.Provider;
using Android.Support.V7.App;
using Android.Text;
using Android.Views;
using Android.Widget;

using ExpansionDownloader;
using ExpansionDownloader.Client;
using ExpansionDownloader.Database;
using ExpansionDownloader.Service;

using MaterialDialogs;

using Storehouse.Core;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression.Zip;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

using Debug = System.Diagnostics.Debug;

namespace JWChinese
{
    [Activity(Label = "JW Chinese", Icon = "@drawable/icon", Theme = "@style/Theme.Storehouse.Material", ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
    public partial class LibraryDownloaderActivity : ActionBarActivity, IDownloaderClient
    {
        public static int MAIN_EXPANSION_FILE_VERSION = 26;
        private int mainExpansionFileVersion;

        private ISharedPreferences preferences;

        private IDownloaderService downloaderService;
        private IDownloaderServiceConnection downloaderServiceConnection;
        private DownloaderState downloaderState;
        private bool isPaused;
        private ZipFileValidationHandler zipFileValidationHandler;

        // UI
        private TextView averageSpeedTextView;
        private View dashboardView;
        private Button openWiFiSettingsButton;
        private Button pauseButton;
        private ProgressBar progressBar;
        private TextView progressFractionTextView;
        private TextView progressPercentTextView;
        private Button resumeOnCellDataButton;
        private TextView statusTextView;
        private TextView timeRemainingTextView;
        private View useCellDataView;

        private BackgroundWorker worker = new BackgroundWorker();

        public void OnDownloadProgress(DownloadProgressInfo progress)
        {
            averageSpeedTextView.Text = string.Format("{0} Kb/s", Helpers.GetSpeedString(progress.CurrentSpeed));
            timeRemainingTextView.Text = string.Format("Time remaining: {0}", Helpers.GetTimeRemaining(progress.TimeRemaining));
            progressBar.Max = (int)(progress.OverallTotal >> 8);
            progressBar.Progress = (int)(progress.OverallProgress >> 8);
            progressPercentTextView.Text = string.Format("{0}%", progress.OverallProgress * 100 / progress.OverallTotal);
            progressFractionTextView.Text = Helpers.GetDownloadProgressString(progress.OverallProgress, progress.OverallTotal);
        }

        public void OnDownloadStateChanged(DownloaderState newState)
        {
            Debug.WriteLine("newState: " + newState);

            if (downloaderState != newState)
            {
                downloaderState = newState;
                statusTextView.Text = GetString(Helpers.GetDownloaderStringFromState(newState));
            }

            bool showDashboard = true;
            bool showCellMessage = false;
            bool paused = false;
            bool indeterminate = true;
            switch (newState)
            {
                case DownloaderState.Idle:
                case DownloaderState.Connecting:
                case DownloaderState.FetchingUrl:
                    break;
                case DownloaderState.Downloading:
                    indeterminate = false;
                    break;
                case DownloaderState.Failed:
                case DownloaderState.FailedCanceled:
                case DownloaderState.FailedFetchingUrl:
                case DownloaderState.FailedUnlicensed:
                    paused = true;
                    showDashboard = false;
                    indeterminate = false;
                    break;
                case DownloaderState.PausedNeedCellularPermission:
                case DownloaderState.PausedWifiDisabledNeedCellularPermission:
                    showDashboard = false;
                    paused = true;
                    indeterminate = false;
                    showCellMessage = true;
                    break;
                case DownloaderState.PausedByRequest:
                    paused = true;
                    indeterminate = false;
                    break;
                case DownloaderState.PausedRoaming:
                case DownloaderState.PausedSdCardUnavailable:
                    paused = true;
                    indeterminate = false;
                    break;
                default:
                    paused = true;
                    break;
            }

            if (newState != DownloaderState.Completed)
            {
                dashboardView.Visibility = showDashboard ? ViewStates.Visible : ViewStates.Gone;
                useCellDataView.Visibility = showCellMessage ? ViewStates.Visible : ViewStates.Gone;
                progressBar.Indeterminate = indeterminate;
                UpdatePauseButton(paused);
            }
            else
            {
                ValidateExpansionFiles();
            }
        }

        public void OnServiceConnected(Messenger m)
        {
            this.downloaderService = ServiceMarshaller.CreateProxy(m);
            this.downloaderService.OnClientUpdated(this.downloaderServiceConnection.GetMessenger());
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            Window.AddFlags(WindowManagerFlags.KeepScreenOn);
            SupportActionBar.SetDisplayShowHomeEnabled(true);

            App.STATE.Activity = this;
            App.FUNCTIONS.SetActionBarDrawable(this);

            preferences = PreferenceManager.GetDefaultSharedPreferences(this);
            mainExpansionFileVersion = preferences.GetInt("MainExpansionFileVersion", 1);

            if(mainExpansionFileVersion == MAIN_EXPANSION_FILE_VERSION)
            {
                StartApplication();
            }
            else
            {
#if DEBUG
                // Before we do anything, are the files we expect already here and 
                // delivered (presumably by Market) 
                // For free titles, this is probably worth doing. (so no Market 
                // request is necessary)
                var delivered = AreExpansionFilesDelivered();

                if (delivered)
                {
                    StorehouseInitializationParadigm();
                }

                if (!GetExpansionFiles())
                {
                    InitializeDownloadUI();
                }
#else
                StorehouseInitializationParadigm();
#endif
            }
        }

        private void InitializeDownloadUI()
        {
            InitializeControls();
            downloaderServiceConnection = ClientMarshaller.CreateStub(this, typeof(LibraryDownloaderService));
        }

        private void InitializeControls()
        {
            SetContentView(Resource.Layout.LibraryDownloaderActivity);

            progressBar = FindViewById<ProgressBar>(Resource.Id.progressBar);
            statusTextView = FindViewById<TextView>(Resource.Id.statusText);
            progressFractionTextView = FindViewById<TextView>(Resource.Id.progressAsFraction);
            progressPercentTextView = FindViewById<TextView>(Resource.Id.progressAsPercentage);
            averageSpeedTextView = FindViewById<TextView>(Resource.Id.progressAverageSpeed);
            timeRemainingTextView = FindViewById<TextView>(Resource.Id.progressTimeRemaining);
            dashboardView = FindViewById(Resource.Id.downloaderDashboard);
            useCellDataView = FindViewById(Resource.Id.approveCellular);
            pauseButton = FindViewById<Button>(Resource.Id.pauseButton);
            openWiFiSettingsButton = FindViewById<Button>(Resource.Id.wifiSettingsButton);
            resumeOnCellDataButton = FindViewById<Button>(Resource.Id.resumeOverCellular);

            pauseButton.Click += OnButtonOnClick;
            openWiFiSettingsButton.Click += OnOpenWiFiSettingsButtonOnClick;
            resumeOnCellDataButton.Click += OnEventHandler;

            App.STATE.SetActionBarTitle(SupportActionBar, "JW Chinese", "Storehouse Initialization");

            progressBar.ProgressDrawable.SetColorFilter(Resources.GetColor(Resource.Color.storehouse_red), PorterDuff.Mode.Multiply);
            progressBar.IndeterminateDrawable.SetColorFilter(Resources.GetColor(Resource.Color.storehouse_red), PorterDuff.Mode.Multiply);
        }

        private void HideAllControls()
        {
            progressBar.Visibility = ViewStates.Gone;
            statusTextView.Visibility = ViewStates.Gone;
            progressFractionTextView.Visibility = ViewStates.Gone;
            progressPercentTextView.Visibility = ViewStates.Gone;
            averageSpeedTextView.Visibility = ViewStates.Gone;
            timeRemainingTextView.Visibility = ViewStates.Gone;
            dashboardView.Visibility = ViewStates.Gone;
            useCellDataView.Visibility = ViewStates.Gone;
            pauseButton.Visibility = ViewStates.Gone;
            openWiFiSettingsButton.Visibility = ViewStates.Gone;
            resumeOnCellDataButton.Visibility = ViewStates.Gone;
        }
      
        private void StorehouseInitializationParadigm()
        {
            BuildStorehouseWorker();

            if (!worker.IsBusy)
            {
                worker.RunWorkerAsync();
            }
        }

        private void StartApplication()
        {
            StartActivity(typeof(MainLibraryActivity));
            Finish();
        }

        public void BuildStorehouseWorker()
        {
            worker.WorkerSupportsCancellation = true;
            worker.DoWork += (object sender, DoWorkEventArgs e) =>
            {
                MaterialDialog.Builder progress = new MaterialDialog.Builder(this);
                progress.SetTitle("Finalizing Storehouse");
                progress.SetContent("Please be patient");
                progress.SetCancelable(false);
                progress.SetProgress(true, 3);

                MaterialDialog dialog = null;

                RunOnUiThread(() =>
                {
                    dialog = progress.Show();
                });

                RunOnUiThread(() =>
                {
                    dialog.SetContent("Building English library");
                });
                App.FUNCTIONS.ExtractDatabase("english.db", this, MAIN_EXPANSION_FILE_VERSION);
                RunOnUiThread(() =>
                {
                    dialog.SetContent("Building Chinese library");

                });
                App.FUNCTIONS.ExtractDatabase("chinese.db", this, MAIN_EXPANSION_FILE_VERSION);
                RunOnUiThread(() =>
                {
                    dialog.SetContent("Building Pinyin library");
                });
                App.FUNCTIONS.ExtractDatabase("pinyin.db", this, MAIN_EXPANSION_FILE_VERSION);

                List<Library> libraries = new List<Library>();
                libraries.Add(Library.Bible);
                libraries.Add(Library.Insight);
                libraries.Add(Library.DailyText);
                libraries.Add(Library.Publications);

                App.STATE.Libraries = libraries;
                App.STATE.CurrentLibrary = libraries.First();

                App.STATE.SeekBarTextSize = Resources.GetInteger(Resource.Integer.webview_base_font_size);

                // English
                App.STATE.PrimaryLanguage = App.FUNCTIONS.GetAvailableLanguages(this)[0];
                // Chinese
                App.STATE.SecondaryLanguage = App.FUNCTIONS.GetAvailableLanguages(this)[1];
                // Pinyin
                App.STATE.PinyinLanguage = App.FUNCTIONS.GetAvailableLanguages(this)[2];
                // Set current language first to English
                App.STATE.Language = App.STATE.PrimaryLanguage.EnglishName;

                App.STATE.SaveUserPreferences();
                preferences.Edit().PutInt("MainExpansionFileVersion", MAIN_EXPANSION_FILE_VERSION).Commit();

                RunOnUiThread(() =>
                {
                    Toast.MakeText(this, "Set up complete. Have fun!", ToastLength.Long).Show();
                });

                dialog.Dismiss();

                StartApplication();
            };

            worker.RunWorkerCompleted += (object sender, RunWorkerCompletedEventArgs e) =>
            {
                Console.WriteLine("WORKER COMPLETE!");
            };
        }

        protected override void OnDestroy()
        {
            if (zipFileValidationHandler != null)
            {
                zipFileValidationHandler.ShouldCancel = true;
            }

            base.OnDestroy();
        }

        protected override void OnResume()
        {
            if (downloaderServiceConnection != null)
            {
                downloaderServiceConnection.Connect(this);
            }

            base.OnResume();
        }

        protected override void OnStop()
        {
            if (downloaderServiceConnection != null)
            {
                downloaderServiceConnection.Disconnect(this);
            }

            base.OnStop();
        }

        private bool AreExpansionFilesDelivered()
        {
            var downloads = DownloadsDatabase.GetDownloads();

            return downloads.Any() && downloads.All(x => Helpers.DoesFileExist(this, x.FileName, x.TotalBytes, false));
        }

        private void DoValidateZipFiles(object state)
        {           
            var downloads = DownloadsDatabase.GetDownloads().Select(x => Helpers.GenerateSaveFileName(this, x.FileName)).ToArray();

            var result = downloads.Any() && downloads.All(IsValidZipFile);

            RunOnUiThread(delegate
            {
                pauseButton.Click += delegate
                {
                    HideAllControls();
                    StorehouseInitializationParadigm();
                };

                dashboardView.Visibility = ViewStates.Visible;
                useCellDataView.Visibility = ViewStates.Gone;

                if (result)
                {
                    statusTextView.SetText(Resource.String.text_validation_complete);
                    pauseButton.SetText(Android.Resource.String.Ok);
                }
                else
                {
                    statusTextView.SetText(Resource.String.text_validation_failed);
                    pauseButton.SetText(Android.Resource.String.Cancel);
                }

                // I ADDED THESE METHODS
                HideAllControls();
                StorehouseInitializationParadigm();
            });
        }

        private bool GetExpansionFiles()
        {
            bool result = false;

            try
            {
                Intent launchIntent = Intent;
                var intent = new Intent(this, typeof(LibraryDownloaderActivity));
                intent.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTop);
                intent.SetAction(launchIntent.Action);

                if (launchIntent.Categories != null)
                {
                    foreach (string category in launchIntent.Categories)
                    {
                        intent.AddCategory(category);
                    }
                }

                PendingIntent pendingIntent = PendingIntent.GetActivity(this, 0, intent, PendingIntentFlags.UpdateCurrent);

                DownloadServiceRequirement startResult = DownloaderService.StartDownloadServiceIfRequired(this, pendingIntent, typeof(LibraryDownloaderService));

                if (startResult != DownloadServiceRequirement.NoDownloadRequired)
                {
                    InitializeDownloadUI();
                    result = true;
                }
            }
            catch (PackageManager.NameNotFoundException e)
            {
                Console.WriteLine("Cannot find own package! MAYDAY!");
                e.PrintStackTrace();
            }

            return result;
        }

        private bool IsValidZipFile(string filename)
        {
            zipFileValidationHandler = new ZipFileValidationHandler(filename)
            {
                UpdateUi = OnUpdateValidationUi
            };

            return File.Exists(filename) && ZipFile.Validate(zipFileValidationHandler);
        }

        private void OnButtonOnClick(object sender, EventArgs e)
        {
            if (isPaused)
            {
                downloaderService.RequestContinueDownload();
            }
            else
            {
                downloaderService.RequestPauseDownload();
            }

            UpdatePauseButton(!isPaused);
        }

        private void OnEventHandler(object sender, EventArgs args)
        {
            downloaderService.SetDownloadFlags(ServiceFlags.FlagsDownloadOverCellular);
            downloaderService.RequestContinueDownload();
            useCellDataView.Visibility = ViewStates.Gone;
        }

        private void OnOpenWiFiSettingsButtonOnClick(object sender, EventArgs e)
        {
            StartActivity(new Intent(Settings.ActionWifiSettings));
        }

        private void OnUpdateValidationUi(ZipFileValidationHandler handler)
        {
            var info = new DownloadProgressInfo(handler.TotalBytes, handler.CurrentBytes, handler.TimeRemaining, handler.AverageSpeed);

            RunOnUiThread(() => OnDownloadProgress(info));
        }

        private void UpdatePauseButton(bool paused)
        {
            isPaused = paused;
            int stringResourceId = paused ? Resource.String.text_button_resume : Resource.String.text_button_pause;
            pauseButton.SetText(stringResourceId);
        }

        private void ValidateExpansionFiles()
        {
            dashboardView.Visibility = ViewStates.Visible;
            useCellDataView.Visibility = ViewStates.Gone;
            statusTextView.SetText(Resource.String.text_verifying_download);
            pauseButton.Click += delegate
            {
                if (this.zipFileValidationHandler != null)
                {
                    this.zipFileValidationHandler.ShouldCancel = true;
                }
            };
            pauseButton.SetText(Resource.String.text_button_cancel_verify);

            ThreadPool.QueueUserWorkItem(DoValidateZipFiles);
        }        
    }

    [Service]
    public class LibraryDownloaderService : DownloaderService
    {
        protected override string PublicKey
        {
            get
            {
                return "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAnAtEDv/z61eaiFcyVOOB+OjbV5faLPLzhn6sMNSxb+HizNsslVS4sG5w/YfW6NJnkwMDa4BNPSMeHHlbEQDIoupU4f0U4+QqKNhdStsbu1TZ3WdpnCwqlsZALs+DGwq76NOP/IxIdSv7PX7Hd0cy9ZyQGu9t4Mvf+CyV+Y94XPMvsQvKFSHot+UC+GdpFzKHn9LI+fqoif2hW6oh2JC2Z7bs2BS3I7gV0jXK6aJd3ZXiGuL+iiRvi5auzRN4wv6m1jDY7mZKL62fDVqqyRwH4Q7qRdVkezWj/mCXV0HZvpFPDtWX/X3U4dtm8d3Tw4wwVwQg6GbDHN6lw8JWYgEtcwIDAQAB";
            }
        }

        protected override byte[] Salt
        {
            get
            {
                return new byte[] { 1, 43, 12, 1, 54, 98, 100, 12, 43, 2, 8, 4, 9, 5, 106, 108, 33, 45, 1, 84 };
            }
        }

        protected override string AlarmReceiverClassName
        {
            get
            {
                return typeof(LibraryDownloaderService).Name;
            }
        }
    }

    [BroadcastReceiver(Exported = false)]
    public class LibraryAlarmReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            try
            {
                DownloaderService.StartDownloadServiceIfRequired(context, intent, typeof(LibraryDownloaderService));
            }
            catch (PackageManager.NameNotFoundException e)
            {
                e.PrintStackTrace();
            }
        }
    }

    public class XAPKFile
    {
        public bool main;
        public int fileVersion;
        public long fileSize;

        public XAPKFile(bool main, int fileVersion, long fileSize)
        {
            this.main = main;
            this.fileVersion = fileVersion;
            this.fileSize = fileSize;
        }
    }
}