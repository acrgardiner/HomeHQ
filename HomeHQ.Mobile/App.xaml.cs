using HomeHQ.Mobile.Services;

namespace HomeHQ.Mobile
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            UnhandledExceptionHandler.Register();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}
