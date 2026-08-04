using System;
using Android.App;
using Android.Runtime;

namespace SCAssistant.UnoApp.Droid;

[Application(
    Label = "@string/ApplicationName",
    LargeHeap = true,
    HardwareAccelerated = true,
    Theme = "@style/Theme.App.Starting"
)]
public class Application : Microsoft.UI.Xaml.NativeApplication
{
    public Application(IntPtr javaReference, JniHandleOwnership transfer)
        : base(() => new App(), javaReference, transfer)
    {
    }
}
