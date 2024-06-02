using Android.App;
using Android.Content.PM;

using Stride.Engine;
using Stride.Starter;

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
        protected Game Game;

        protected override void OnRun()
        {
            base.OnRun();

            Game = new Game();
            Game.Run(GameContext);
        }

        protected override void OnDestroy()
        {
            Game.Dispose();

            base.OnDestroy();
        }
    }
}
