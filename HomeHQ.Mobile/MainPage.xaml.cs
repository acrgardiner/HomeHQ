namespace HomeHQ.Mobile
{
    public partial class MainPage : ContentPage
    {
        int count = 0;

        public MainPage()
        {
            // Forces the CI/CD compiler to resolve the MauiIcon type
            _ = new MauiIcons.Core.MauiIcon();

            InitializeComponent();
            // Temporary Workaround for url styled namespace in xaml removed to avoid build-time dependency
        }

        private void OnCounterClicked(object? sender, EventArgs e)
        {
            count++;

            if (count == 1)
            {
                CounterBtn.Text = $"Clicked {count} time";
            }
            else
            {
                CounterBtn.Text = $"Clicked {count} times";
            }

            SemanticScreenReader.Announce(CounterBtn.Text);
        }
    }
}
