using System.Collections;
using System.ComponentModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;

namespace HomeHQ.Mobile.Controls
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class FloatingEntry : ContentView
    {
        VisualElement? _attachedInner;

        public FloatingEntry()
        {
            InitializeComponent();
            // Watch for the PART_Content ContentView to update its Content when using templates
            if (this.FindByName("PART_Content") is ContentView cv)
            {
                cv.PropertyChanged += ContentPresenter_PropertyChanged;
            }
            // If InnerContent was set directly, attach handlers
            AttachToInnerContent(InnerContent as VisualElement);
        }

        void ContentPresenter_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Handle ContentView.Content changes from the PART_Content element
            if (e.PropertyName == nameof(ContentView.Content) && sender is ContentView cv)
            {
                AttachToInnerContent(cv.Content as VisualElement);
            }
        }

        void AttachToInnerContent(VisualElement? element)
        {
            if (_attachedInner == element)
            {
                return;
            }

            if (_attachedInner != null)
            {
                _attachedInner.Focused -= Inner_Focused;
                _attachedInner.Unfocused -= Inner_Unfocused;
            }

            _attachedInner = element;

            if (_attachedInner != null)
            {
                _attachedInner.Focused += Inner_Focused;
                _attachedInner.Unfocused += Inner_Unfocused;
                // initialize focus state
                IsFocused = _attachedInner.IsFocused;
            }
        }

        void Inner_Focused(object? sender, FocusEventArgs e) => IsFocused = true;
        void Inner_Unfocused(object? sender, FocusEventArgs e) => IsFocused = false;

        // Generic content properties - allow consumer to provide any inner control or template
        public static readonly BindableProperty InnerContentProperty = BindableProperty.Create(
            nameof(InnerContent), typeof(object), typeof(FloatingEntry), null,
            propertyChanged: (bindable, _, newValue) =>
            {
                var ctrl = (FloatingEntry)bindable;
                ctrl.AttachToInnerContent(newValue as VisualElement);
            });

        public object InnerContent
        {
            get => GetValue(InnerContentProperty);
            set => SetValue(InnerContentProperty, value);
        }

        public static readonly BindableProperty InnerContentTemplateProperty = BindableProperty.Create(
            nameof(InnerContentTemplate), typeof(DataTemplate), typeof(FloatingEntry), null);

        public DataTemplate InnerContentTemplate
        {
            get => (DataTemplate)GetValue(InnerContentTemplateProperty);
            set => SetValue(InnerContentTemplateProperty, value);
        }

        public static readonly BindableProperty IsFocusedProperty = BindableProperty.Create(
            nameof(IsFocused), typeof(bool), typeof(FloatingEntry), false);

        /// <summary>Exposes focus state so callers can bind inner control focus to the floating label.</summary>
        public bool IsFocused
        {
            get => (bool)GetValue(IsFocusedProperty);
            set => SetValue(IsFocusedProperty, value);
        }

        // ── Shared / Entry properties ─────────────────────────────────────────

        public static readonly BindableProperty TitleProperty = BindableProperty.Create(
            nameof(Title), typeof(string), typeof(FloatingEntry), default(string));

        /// <summary>The floating label text shown above the control.</summary>
        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }
    }
}
