using Android;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Widget;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using Silk.NET.Windowing;
using Stride.Engine;
using Stride.Starter;
using System;
using System.Collections.Generic;

namespace AndStrideGame
{
    [Activity(MainLauncher = true,
              Icon = "@mipmap/gameicon",
              Label = "@string/app_name",
              ScreenOrientation = ScreenOrientation.Portrait,
              Theme = "@android:style/Theme.Material.Light.NoActionBar.Fullscreen",
              ConfigurationChanges = ConfigChanges.UiMode | ConfigChanges.Orientation | ConfigChanges.KeyboardHidden | ConfigChanges.ScreenSize)]
    public class AndStrideGameActivity : StrideActivity
    {
        private const int GameAppPermissionRequestCode = 123456;

        private bool esVersionFailed;

        protected Game Game;

        protected override void OnRun()
        {
            if (esVersionFailed)
            {
                // Create a dummy view to bypass Stride, but at least run the error dialog we created in OnCreate
                var options = default(ViewOptions);
                options.API = GraphicsAPI.None;
                var view = Silk.NET.Windowing.Window.GetView(options);
                view.Run();

                view.Dispose();
                return;
            }

            base.OnRun();

            Game = new Game();
            Game.Run(GameContext);
        }

        protected override void OnDestroy()
        {
            Game?.Dispose();

            base.OnDestroy();
        }

        protected override void OnPause()
        {
            try { base.OnPause(); }
            catch (Exception) { }   // Crashes when esVersionFailed is true, so just suppress it
        }

        protected override void OnResume()
        {
            try { base.OnResume(); }
            catch (Exception) { }   // Crashes when esVersionFailed is true, so just suppress it
        }

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            var activityMananger = GetSystemService(ActivityService) as ActivityManager;
            System.Diagnostics.Debug.Assert(activityMananger is not null);
            var configInfo = activityMananger.DeviceConfigurationInfo;
            System.Diagnostics.Debug.WriteLine($"ReqGlEsVersion: {configInfo?.ReqGlEsVersion}");
            System.Diagnostics.Debug.WriteLine($"GlEsVersion: {configInfo?.GlEsVersion}");

            const int MinimumOpenGlEsVersion = 0x30000;     // Requires at least OpenGL EL 3.0
            esVersionFailed = configInfo?.ReqGlEsVersion < MinimumOpenGlEsVersion;
            if (esVersionFailed)
            {
                using var builder = new AlertDialog.Builder(this);
                string errorMessage = "Application requires OpenGL ES 3.0 or higher to run.";
#if DEBUG
                errorMessage += "\r\nIf running on an emulator, remember to set the renderer to API level to 3.0 or higher in the advanced setting.";
#endif
                var dialog = builder
                               .SetTitle("Unsupported Hardware Error")!
                               .SetMessage(errorMessage)!
                               .SetNeutralButton("OK", (dialog, id) =>
                               {
                                   CloseApplication();
                               })!
                               .Create();
                System.Diagnostics.Debug.Assert(dialog is not null);
                dialog.Show();
                return;
            }

            try
            {
                // Add all required permissions to a list
                var requiredPermissions = new List<string>()
                {
                    Manifest.Permission.Bluetooth,
                    Manifest.Permission.BluetoothAdmin,
                };
                if (OperatingSystem.IsAndroidVersionAtLeast(31))
                {
                    requiredPermissions.Add(Manifest.Permission.BluetoothScan);
                    requiredPermissions.Add(Manifest.Permission.BluetoothConnect);
                }

                // Request permissions from requiredPermissions
                /// Always triggers <see cref="OnRequestPermissionsResult"/>, even if all permissions are already granted.
                ActivityCompat.RequestPermissions(this, requiredPermissions.ToArray(), GameAppPermissionRequestCode);

                // Only run base.OnCreate if all permissions are granted, otherwise it would only throw an expception anyway, else close the app
                bool anyPermissionsDenied = false;

                foreach (string permission in requiredPermissions)
                {
                    if (ContextCompat.CheckSelfPermission(this, permission) == Permission.Denied)
                    {
                        // A permission is denied, set to true; we need all permissions granted!
                        anyPermissionsDenied = true;
                    }
                }
                if (!anyPermissionsDenied)
                {
                    base.OnCreate(savedInstanceState);
                }
                else { CloseApplication(); }
            }
            catch (Exception ex)
            {
                CloseApplication();
            }
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            if (OperatingSystem.IsAndroidVersionAtLeast(23))
            {
                base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            }
            if (requestCode == GameAppPermissionRequestCode)
            {
                if (grantResults.Length <= 0)
                {
                    // If user interaction was interrupted, the permission request is cancelled and you
                    // receive empty arrays.
                    Toast.MakeText(this, "User interaction was cancelled. Please make sure to allow the requested permissions and restart the app.", ToastLength.Long).Show();
                }
                else if (grantResults[0] == PermissionChecker.PermissionGranted)
                {
                    // Permission was granted.
                    Toast.MakeText(this, "Permission Granted!", ToastLength.Long).Show();
                }
                else
                {
                    // Permission denied.
                    Toast.MakeText(this, "Permission Denied. Please make sure to allow the requested permissions and restart the app.", ToastLength.Long).Show();
                }
            }
        }

        private void CloseApplication()
        {
            FinishAffinity();
        }
    }
}
