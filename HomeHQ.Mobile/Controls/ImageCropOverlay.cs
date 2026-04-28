namespace HomeHQ.Mobile.Controls;

/// <summary>
/// Darkened overlay with a draggable, resizable crop rectangle. Crop values are 0–1 relative to the bitmap.
/// </summary>
public class ImageCropOverlay : ContentView
{
    private const double MinFraction = 0.08;
    private const double HandleSize = 28;

    private readonly AbsoluteLayout _root = new();
    private readonly BoxView _dimTop = new() { BackgroundColor = Color.FromRgba(0, 0, 0, 0.55) };
    private readonly BoxView _dimBottom = new() { BackgroundColor = Color.FromRgba(0, 0, 0, 0.55) };
    private readonly BoxView _dimLeft = new() { BackgroundColor = Color.FromRgba(0, 0, 0, 0.55) };
    private readonly BoxView _dimRight = new() { BackgroundColor = Color.FromRgba(0, 0, 0, 0.55) };
    private readonly Border _cropFrame = new()
    {
        Stroke = Colors.White,
        StrokeThickness = 2,
        // Non-null alpha so the border area receives pan gestures on all platforms.
        BackgroundColor = Color.FromRgba(255, 255, 255, 0.01),
        Padding = new Thickness(0)
    };

    private readonly BoxView _handleTL = CreateHandle();
    private readonly BoxView _handleTR = CreateHandle();
    private readonly BoxView _handleBL = CreateHandle();
    private readonly BoxView _handleBR = CreateHandle();

    private PanGestureRecognizer _movePan = null!;
    private PanGestureRecognizer _panTL = null!;
    private PanGestureRecognizer _panTR = null!;
    private PanGestureRecognizer _panBL = null!;
    private PanGestureRecognizer _panBR = null!;

    private double _dragStartCropX;
    private double _dragStartCropY;
    private double _dragStartCropW;
    private double _dragStartCropH;

    public static readonly BindableProperty CropRelativeXProperty = BindableProperty.Create(
        nameof(CropRelativeX),
        typeof(double),
        typeof(ImageCropOverlay),
        0d,
        BindingMode.TwoWay,
        propertyChanged: OnCropPropertyChanged);

    public static readonly BindableProperty CropRelativeYProperty = BindableProperty.Create(
        nameof(CropRelativeY),
        typeof(double),
        typeof(ImageCropOverlay),
        0d,
        BindingMode.TwoWay,
        propertyChanged: OnCropPropertyChanged);

    public static readonly BindableProperty CropRelativeWidthProperty = BindableProperty.Create(
        nameof(CropRelativeWidth),
        typeof(double),
        typeof(ImageCropOverlay),
        1d,
        BindingMode.TwoWay,
        propertyChanged: OnCropPropertyChanged);

    public static readonly BindableProperty CropRelativeHeightProperty = BindableProperty.Create(
        nameof(CropRelativeHeight),
        typeof(double),
        typeof(ImageCropOverlay),
        1d,
        BindingMode.TwoWay,
        propertyChanged: OnCropPropertyChanged);

    public static readonly BindableProperty SourceImageWidthProperty = BindableProperty.Create(
        nameof(SourceImageWidth),
        typeof(int),
        typeof(ImageCropOverlay),
        0,
        propertyChanged: OnCropPropertyChanged);

    public static readonly BindableProperty SourceImageHeightProperty = BindableProperty.Create(
        nameof(SourceImageHeight),
        typeof(int),
        typeof(ImageCropOverlay),
        0,
        propertyChanged: OnCropPropertyChanged);

    public double CropRelativeX
    {
        get => (double)GetValue(CropRelativeXProperty);
        set => SetValue(CropRelativeXProperty, value);
    }

    public double CropRelativeY
    {
        get => (double)GetValue(CropRelativeYProperty);
        set => SetValue(CropRelativeYProperty, value);
    }

    public double CropRelativeWidth
    {
        get => (double)GetValue(CropRelativeWidthProperty);
        set => SetValue(CropRelativeWidthProperty, value);
    }

    public double CropRelativeHeight
    {
        get => (double)GetValue(CropRelativeHeightProperty);
        set => SetValue(CropRelativeHeightProperty, value);
    }

    public int SourceImageWidth
    {
        get => (int)GetValue(SourceImageWidthProperty);
        set => SetValue(SourceImageWidthProperty, value);
    }

    public int SourceImageHeight
    {
        get => (int)GetValue(SourceImageHeightProperty);
        set => SetValue(SourceImageHeightProperty, value);
    }

    public ImageCropOverlay()
    {
        BackgroundColor = Colors.Transparent;
        Content = _root;
        _root.Children.Add(_dimTop);
        _root.Children.Add(_dimBottom);
        _root.Children.Add(_dimLeft);
        _root.Children.Add(_dimRight);
        _root.Children.Add(_cropFrame);
        _root.Children.Add(_handleTL);
        _root.Children.Add(_handleTR);
        _root.Children.Add(_handleBL);
        _root.Children.Add(_handleBR);

        _movePan = new PanGestureRecognizer();
        _movePan.PanUpdated += (_, e) => MainThread.BeginInvokeOnMainThread(() => OnMovePan(e));
        _cropFrame.GestureRecognizers.Add(_movePan);

        _panTL = CreateCornerPan(OnCornerPanTopLeft);
        _panTR = CreateCornerPan(OnCornerPanTopRight);
        _panBL = CreateCornerPan(OnCornerPanBottomLeft);
        _panBR = CreateCornerPan(OnCornerPanBottomRight);
        _handleTL.GestureRecognizers.Add(_panTL);
        _handleTR.GestureRecognizers.Add(_panTR);
        _handleBL.GestureRecognizers.Add(_panBL);
        _handleBR.GestureRecognizers.Add(_panBR);

        SizeChanged += (_, _) => Arrange();
    }

    private static BoxView CreateHandle()
    {
        return new BoxView
        {
            WidthRequest = HandleSize,
            HeightRequest = HandleSize,
            BackgroundColor = Colors.White,
            Opacity = 0.95
        };
    }

    private static PanGestureRecognizer CreateCornerPan(Action<PanUpdatedEventArgs> handler)
    {
        var pan = new PanGestureRecognizer();
        pan.PanUpdated += (_, e) => MainThread.BeginInvokeOnMainThread(() => handler(e));
        return pan;
    }

    private static void OnCropPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is ImageCropOverlay overlay)
        {
            overlay.Arrange();
        }
    }

    /// <summary>
    /// Aspect-fit rectangle where the bitmap is drawn inside this overlay (in overlay coordinates).
    /// </summary>
    private bool TryGetImageDisplayRect(out Rect r)
    {
        r = default;
        double vw = Width;
        double vh = Height;
        int iw = SourceImageWidth;
        int ih = SourceImageHeight;
        if (vw <= 0 || vh <= 0 || iw <= 0 || ih <= 0)
        {
            return false;
        }

        double scale = Math.Min(vw / iw, vh / ih);
        double dw = iw * scale;
        double dh = ih * scale;
        double ox = (vw - dw) / 2;
        double oy = (vh - dh) / 2;
        r = new Rect(ox, oy, dw, dh);
        return true;
    }

    /// <summary>Call after the viewer transform resets so dim regions stay aligned.</summary>
    public void RequestLayout()
    {
        Arrange();
    }

    private void Arrange()
    {
        if (!TryGetImageDisplayRect(out var disp))
        {
            return;
        }

        double vx = disp.X + CropRelativeX * disp.Width;
        double vy = disp.Y + CropRelativeY * disp.Height;
        double vw = CropRelativeWidth * disp.Width;
        double vh = CropRelativeHeight * disp.Height;

        AbsoluteLayout.SetLayoutBounds(_dimTop, new Rect(0, 0, Width, vy));
        AbsoluteLayout.SetLayoutBounds(_dimBottom, new Rect(0, vy + vh, Width, Math.Max(0, Height - (vy + vh))));
        AbsoluteLayout.SetLayoutBounds(_dimLeft, new Rect(0, vy, vx, vh));
        AbsoluteLayout.SetLayoutBounds(_dimRight, new Rect(vx + vw, vy, Math.Max(0, Width - (vx + vw)), vh));

        AbsoluteLayout.SetLayoutBounds(_cropFrame, new Rect(vx, vy, vw, vh));

        AbsoluteLayout.SetLayoutBounds(_handleTL, new Rect(vx - HandleSize / 2, vy - HandleSize / 2, HandleSize, HandleSize));
        AbsoluteLayout.SetLayoutBounds(_handleTR, new Rect(vx + vw - HandleSize / 2, vy - HandleSize / 2, HandleSize, HandleSize));
        AbsoluteLayout.SetLayoutBounds(_handleBL, new Rect(vx - HandleSize / 2, vy + vh - HandleSize / 2, HandleSize, HandleSize));
        AbsoluteLayout.SetLayoutBounds(_handleBR, new Rect(vx + vw - HandleSize / 2, vy + vh - HandleSize / 2, HandleSize, HandleSize));
    }

    private void OnMovePan(PanUpdatedEventArgs e)
    {
        if (!TryGetImageDisplayRect(out var disp))
        {
            return;
        }

        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _dragStartCropX = CropRelativeX;
                _dragStartCropY = CropRelativeY;
                _dragStartCropW = CropRelativeWidth;
                _dragStartCropH = CropRelativeHeight;
                break;
            case GestureStatus.Running:
                {
                    double fx = disp.Width;
                    double fy = disp.Height;
                    double nx = _dragStartCropX + e.TotalX / fx;
                    double ny = _dragStartCropY + e.TotalY / fy;
                    nx = Math.Clamp(nx, 0, 1 - _dragStartCropW);
                    ny = Math.Clamp(ny, 0, 1 - _dragStartCropH);
                    CropRelativeX = nx;
                    CropRelativeY = ny;
                    break;
                }
        }
    }

    private void OnCornerPanTopLeft(PanUpdatedEventArgs e) =>
        HandleCornerResize(e, resizeTopLeft: true);

    private void OnCornerPanTopRight(PanUpdatedEventArgs e) =>
        HandleCornerResize(e, resizeTopRight: true);

    private void OnCornerPanBottomLeft(PanUpdatedEventArgs e) =>
        HandleCornerResize(e, resizeBottomLeft: true);

    private void OnCornerPanBottomRight(PanUpdatedEventArgs e) =>
        HandleCornerResize(e, resizeBottomRight: true);

    private void HandleCornerResize(
        PanUpdatedEventArgs e,
        bool resizeTopLeft = false,
        bool resizeTopRight = false,
        bool resizeBottomLeft = false,
        bool resizeBottomRight = false)
    {
        if (!TryGetImageDisplayRect(out var disp))
        {
            return;
        }

        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _dragStartCropX = CropRelativeX;
                _dragStartCropY = CropRelativeY;
                _dragStartCropW = CropRelativeWidth;
                _dragStartCropH = CropRelativeHeight;
                break;
            case GestureStatus.Running:
                {
                    double dx = e.TotalX / disp.Width;
                    double dy = e.TotalY / disp.Height;

                    double x = _dragStartCropX;
                    double y = _dragStartCropY;
                    double w = _dragStartCropW;
                    double h = _dragStartCropH;

                    if (resizeTopLeft)
                    {
                        double nx = x + dx;
                        double ny = y + dy;
                        double nw = w - dx;
                        double nh = h - dy;
                        if (nw >= MinFraction && nh >= MinFraction &&
                            nx >= 0 && ny >= 0 && nx + nw <= 1 && ny + nh <= 1)
                        {
                            CropRelativeX = nx;
                            CropRelativeY = ny;
                            CropRelativeWidth = nw;
                            CropRelativeHeight = nh;
                        }
                    }
                    else if (resizeTopRight)
                    {
                        double ny = y + dy;
                        double nw = w + dx;
                        double nh = h - dy;
                        if (nw >= MinFraction && nh >= MinFraction &&
                            ny >= 0 && x + nw <= 1 && ny + nh <= 1)
                        {
                            CropRelativeY = ny;
                            CropRelativeWidth = nw;
                            CropRelativeHeight = nh;
                        }
                    }
                    else if (resizeBottomLeft)
                    {
                        double nx = x + dx;
                        double nw = w - dx;
                        double nh = h + dy;
                        if (nw >= MinFraction && nh >= MinFraction &&
                            nx >= 0 && nx + nw <= 1 && y + nh <= 1)
                        {
                            CropRelativeX = nx;
                            CropRelativeWidth = nw;
                            CropRelativeHeight = nh;
                        }
                    }
                    else if (resizeBottomRight)
                    {
                        double nw = w + dx;
                        double nh = h + dy;
                        if (nw >= MinFraction && nh >= MinFraction &&
                            x + nw <= 1 && y + nh <= 1)
                        {
                            CropRelativeWidth = nw;
                            CropRelativeHeight = nh;
                        }
                    }

                    break;
                }
        }
    }
}
