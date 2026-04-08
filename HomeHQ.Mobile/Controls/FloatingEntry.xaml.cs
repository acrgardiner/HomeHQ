using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;

namespace HomeHQ.Mobile.Controls
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class FloatingEntry : ContentView
    {
        public FloatingEntry()
        {
            InitializeComponent();
        }

        // Bindable properties for Title, Text, Placeholder
        public static readonly BindableProperty TitleProperty = BindableProperty.Create(
            nameof(Title), typeof(string), typeof(FloatingEntry), default(string));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public static readonly BindableProperty TextProperty = BindableProperty.Create(
            nameof(Text), typeof(string), typeof(FloatingEntry), default(string), BindingMode.TwoWay);

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public static readonly BindableProperty PlaceholderProperty = BindableProperty.Create(
            nameof(Placeholder), typeof(string), typeof(FloatingEntry), default(string));

        public string Placeholder
        {
            get => (string)GetValue(PlaceholderProperty);
            set => SetValue(PlaceholderProperty, value);
        }
    }
}
