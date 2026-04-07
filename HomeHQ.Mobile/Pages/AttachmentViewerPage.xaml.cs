using HomeHQ.Mobile.ViewModels;

namespace HomeHQ.Mobile.Pages;

public partial class AttachmentViewerPage : ContentPage
{
    public AttachmentViewerPage(AttachmentViewerViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    // Tracking for pinch/pan
    double currentScale = 1;
    double startScale = 1;
    double xOffset = 0;
    double yOffset = 0;

    void OnPinchUpdated(object sender, PinchGestureUpdatedEventArgs e)
    {
        if (sender is not VisualElement element)
        {
            return;
        }

        if (e.Status == GestureStatus.Started)
        {
            // store the current scale and set the anchor based on the pinch origin
            startScale = element.Scale;

            // Use the provided ScaleOrigin (relative to the element)
            element.AnchorX = e.ScaleOrigin.X;
            element.AnchorY = e.ScaleOrigin.Y;
        }
        else if (e.Status == GestureStatus.Running)
        {
            // calculate the new scale
            currentScale = startScale * e.Scale;
            currentScale = Math.Max(1, Math.Min(currentScale, 4));

            // if we don't have measurements yet, skip
            if (element.Width <= 0 || element.Height <= 0 || Width <= 0 || Height <= 0)
                return;

            // compute how much the element is being scaled relative to its center
            double renderedX = element.X + element.AnchorX * element.Width;
            double renderedY = element.Y + element.AnchorY * element.Height;

            double deltaX = renderedX / Width;
            double deltaY = renderedY / Height;

            double targetX = xOffset - (deltaX) * (element.Width * (currentScale - startScale));
            double targetY = yOffset - (deltaY) * (element.Height * (currentScale - startScale));

            element.Scale = currentScale;
            element.TranslationX = targetX;
            element.TranslationY = targetY;
        }
        else if (e.Status == GestureStatus.Completed)
        {
            // store offsets for panning
            xOffset = element.TranslationX;
            yOffset = element.TranslationY;

            // if scale returned to 1, reset offsets to prevent drifting
            if (Math.Abs(element.Scale - 1) < 0.01)
            {
                element.TranslationX = 0;
                element.TranslationY = 0;
                xOffset = 0;
                yOffset = 0;
                startScale = 1;
                currentScale = 1;
            }
        }
    }

    void OnPanUpdated(object sender, PanUpdatedEventArgs e)
    {
        if (sender is not VisualElement element)
        {
            return;
        }

        switch (e.StatusType)
        {
            case GestureStatus.Running:
                // Translate the image
                element.TranslationX = xOffset + e.TotalX;
                element.TranslationY = yOffset + e.TotalY;
                break;

            case GestureStatus.Completed:
                // Store the translation applied during the pan
                xOffset = element.TranslationX;
                yOffset = element.TranslationY;
                break;
        }
    }
}
