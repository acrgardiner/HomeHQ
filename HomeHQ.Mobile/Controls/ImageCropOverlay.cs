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
    private PanGestureRecognizer _selectionPan = null!;
    private TapGestureRecognizer _tap = null!;

    private bool _isSelecting;
    private bool _hasSelection;
    private double _selectStartViewX;
    private double _selectStartViewY;
    private bool _nativeTouchAttached;

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
        0d,
        BindingMode.TwoWay,
        propertyChanged: OnCropPropertyChanged);

    public static readonly BindableProperty CropRelativeHeightProperty = BindableProperty.Create(
        nameof(CropRelativeHeight),
        typeof(double),
        typeof(ImageCropOverlay),
        0d,
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

    private void OnTapped()
    {
        if (!TryGetImageDisplayRect(out var disp))
        {
            return;
        }

        if (_hasSelection)
        {
            // Toggle off selection on tap
            _hasSelection = false;
            CropRelativeWidth = 0;
            CropRelativeHeight = 0;
            Arrange();
            return;
        }

        // Create a small centered selection
        double centerX = disp.X + disp.Width / 2;
        double centerY = disp.Y + disp.Height / 2;
        double size = Math.Min(disp.Width, disp.Height) * 0.25;
        double left = centerX - size / 2;
        double top = centerY - size / 2;

        double nx = (left - disp.X) / disp.Width;
        double ny = (top - disp.Y) / disp.Height;
        double nw = size / disp.Width;
        double nh = size / disp.Height;

        nx = Math.Clamp(nx, 0, 1 - nw);
        ny = Math.Clamp(ny, 0, 1 - nh);

        CropRelativeX = nx;
        CropRelativeY = ny;
        CropRelativeWidth = Math.Max(nw, MinFraction);
        CropRelativeHeight = Math.Max(nh, MinFraction);
        _hasSelection = true;
        Arrange();
    }

    private void AttachNativeTouchIfNeeded()
    {
        if (_nativeTouchAttached)
        {
            return;
        }

#if ANDROID
        try
        {
            var handler = _root.Handler;
            if (handler?.PlatformView is Android.Views.View nativeView)
            {
                nativeView.Touch += OnNativeTouch;
                _nativeTouchAttached = true;
            }
        }
        catch
        {
            // Ignore and fall back to gesture recognizers
        }
#endif
    }

#if ANDROID
    private void OnNativeTouch(object? sender, Android.Views.View.TouchEventArgs e)
    {
        // We only care about down/move/up sequence to create selection exactly where pressed
        var ev = e.Event;
        if (ev == null)
        {
            return;
        }

        if (!TryGetImageDisplayRect(out var disp))
        {
            return;
        }

        // Convert raw x/y (pixels) to view coordinates (Maui uses device-independent units)
        var metrics = Android.App.Application.Context.Resources.DisplayMetrics;
        float density = metrics.Density;
        double x = ev.GetX() / density;
        double y = ev.GetY() / density;

        switch (ev.Action)
        {
            case Android.Views.MotionEventActions.Down:
                // If tap inside existing selection, let existing gestures handle it
                if (_hasSelection)
                {
                    double vx = disp.X + CropRelativeX * disp.Width;
                    double vy = disp.Y + CropRelativeY * disp.Height;
                    double vw = CropRelativeWidth * disp.Width;
                    double vh = CropRelativeHeight * disp.Height;
                    var cropRect = new Rect(vx, vy, vw, vh);
                    if (cropRect.Contains(x, y))
                    {
                        _isSelecting = false;
                        return;
                    }
                }

                _isSelecting = true;
                _selectStartViewX = Math.Clamp(x, disp.X, disp.X + disp.Width);
                _selectStartViewY = Math.Clamp(y, disp.Y, disp.Y + disp.Height);
                double relX = (_selectStartViewX - disp.X) / disp.Width;
                double relY = (_selectStartViewY - disp.Y) / disp.Height;
                CropRelativeX = relX;
                CropRelativeY = relY;
                CropRelativeWidth = 0;
                CropRelativeHeight = 0;
                _hasSelection = true;
                Arrange();
                break;

            case Android.Views.MotionEventActions.Move:
                if (!_isSelecting)
                {
                    return;
                }

                double curX = Math.Clamp(ev.GetX() / density, disp.X, disp.X + disp.Width);
                double curY = Math.Clamp(ev.GetY() / density, disp.Y, disp.Y + disp.Height);
                double left = Math.Min(_selectStartViewX, curX);
                double top = Math.Min(_selectStartViewY, curY);
                double right = Math.Max(_selectStartViewX, curX);
                double bottom = Math.Max(_selectStartViewY, curY);

                double nx = (left - disp.X) / disp.Width;
                double ny = (top - disp.Y) / disp.Height;
                double nw = (right - left) / disp.Width;
                double nh = (bottom - top) / disp.Height;

                nw = Math.Max(nw, MinFraction);
                nh = Math.Max(nh, MinFraction);

                nx = Math.Clamp(nx, 0, 1 - nw);
                ny = Math.Clamp(ny, 0, 1 - nh);

                CropRelativeX = nx;
                CropRelativeY = ny;
                CropRelativeWidth = nw;
                CropRelativeHeight = nh;
                break;

            case Android.Views.MotionEventActions.Up:
            case Android.Views.MotionEventActions.Cancel:
                _isSelecting = false;
                break;
        }

        // Indicate we handled the touch so default gestures don't also pick it up
        e.Handled = true;
    }
#endif

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

        // Pan on the overall root to start a new selection when the overlay has no selection
        _selectionPan = new PanGestureRecognizer();
        _selectionPan.PanUpdated += (_, e) => MainThread.BeginInvokeOnMainThread(() => OnSelectionPan(e));
        _root.GestureRecognizers.Add(_selectionPan);

        _tap = new TapGestureRecognizer();
        _tap.Tapped += (_, _) => MainThread.BeginInvokeOnMainThread(() => OnTapped());
        _root.GestureRecognizers.Add(_tap);

        _panTL = CreateCornerPan(OnCornerPanTopLeft);
        _panTR = CreateCornerPan(OnCornerPanTopRight);
        _panBL = CreateCornerPan(OnCornerPanBottomLeft);
        _panBR = CreateCornerPan(OnCornerPanBottomRight);
        _handleTL.GestureRecognizers.Add(_panTL);
        _handleTR.GestureRecognizers.Add(_panTR);
        _handleBL.GestureRecognizers.Add(_panBL);
        _handleBR.GestureRecognizers.Add(_panBR);

        SizeChanged += (_, _) => Arrange();

        // Start with no selection so user can click-and-drag to create one
        _hasSelection = false;

        // Attach native touch on Android when handler becomes available so we can get exact press coordinates
        _root.HandlerChanged += (_, _) => AttachNativeTouchIfNeeded();
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
            // Show selection only when it's meaningfully smaller than the full image and above the minimum size.
            // This avoids treating a default full-image crop (1.0) as an active selection.
            bool isFull = Math.Abs(overlay.CropRelativeWidth - 1.0) < 1e-6 && Math.Abs(overlay.CropRelativeHeight - 1.0) < 1e-6;
            overlay._hasSelection = !isFull &&
                                     overlay.CropRelativeWidth >= MinFraction &&
                                     overlay.CropRelativeHeight >= MinFraction;
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

        if (!_hasSelection)
        {
            // No selection: hide frame/handles and remove dimming
            _cropFrame.IsVisible = false;
            _handleTL.IsVisible = false;
            _handleTR.IsVisible = false;
            _handleBL.IsVisible = false;
            _handleBR.IsVisible = false;
            _dimTop.IsVisible = false;
            _dimBottom.IsVisible = false;
            _dimLeft.IsVisible = false;
            _dimRight.IsVisible = false;
            return;
        }

        // Ensure visuals are visible when there is a selection
        _cropFrame.IsVisible = true;
        _handleTL.IsVisible = true;
        _handleTR.IsVisible = true;
        _handleBL.IsVisible = true;
        _handleBR.IsVisible = true;
        _dimTop.IsVisible = true;
        _dimBottom.IsVisible = true;
        _dimLeft.IsVisible = true;
        _dimRight.IsVisible = true;

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

    private void OnSelectionPan(PanUpdatedEventArgs e)
    {
        if (!TryGetImageDisplayRect(out var disp))
        {
            return;
        }

        switch (e.StatusType)
        {
            case GestureStatus.Started:
                // If there's already a selection and the user started inside it, do not begin a new selection
                // PanUpdated doesn't provide initial pointer location directly; use the current translation as delta from start
                // We capture the starting touch position using the gesture's touch origin: the easiest approach is to use the current translation
                // as starting at the current position on the view. To get a reliable start point, use the position of the pan on the root by
                // checking the touch via the event args' TotalX/TotalY being zero at start and using remaining code to compute positions.
                // Instead, read the last known touch by using a small helper: we'll capture position from the LayoutBounds of the pan container

                // Use the current pointer position relative to the view by requesting the object's bounds and using center as a fallback.
                // For MAUI PanUpdated we don't receive the pointer position on Started, so we derive it as the previous touch: use GestureOrigin at center of view
                _selectStartViewX = Math.Clamp(disp.X + disp.Width / 2, disp.X, disp.X + disp.Width);
                _selectStartViewY = Math.Clamp(disp.Y + disp.Height / 2, disp.Y, disp.Y + disp.Height);

                // If existing selection and start point is inside, ignore selection start
                if (_hasSelection)
                {
                    double vx = disp.X + CropRelativeX * disp.Width;
                    double vy = disp.Y + CropRelativeY * disp.Height;
                    double vw = CropRelativeWidth * disp.Width;
                    double vh = CropRelativeHeight * disp.Height;
                    var cropRect = new Rect(vx, vy, vw, vh);
                    if (cropRect.Contains(_selectStartViewX, _selectStartViewY))
                    {
                        _isSelecting = false;
                        break;
                    }
                }

                _isSelecting = true;
                // initialize crop to zero area at the start point
                double relX = (_selectStartViewX - disp.X) / disp.Width;
                double relY = (_selectStartViewY - disp.Y) / disp.Height;
                CropRelativeX = relX;
                CropRelativeY = relY;
                CropRelativeWidth = 0;
                CropRelativeHeight = 0;
                _hasSelection = true;
                Arrange();
                break;

            case GestureStatus.Running:
                if (!_isSelecting)
                {
                    return;
                }

                // Current pointer = start + total translation
                double curX = _selectStartViewX + e.TotalX;
                double curY = _selectStartViewY + e.TotalY;

                // Clamp inside image display
                curX = Math.Clamp(curX, disp.X, disp.X + disp.Width);
                curY = Math.Clamp(curY, disp.Y, disp.Y + disp.Height);

                double sx = _selectStartViewX;
                double sy = _selectStartViewY;
                double left = Math.Min(sx, curX);
                double top = Math.Min(sy, curY);
                double right = Math.Max(sx, curX);
                double bottom = Math.Max(sy, curY);

                double nx = (left - disp.X) / disp.Width;
                double ny = (top - disp.Y) / disp.Height;
                double nw = (right - left) / disp.Width;
                double nh = (bottom - top) / disp.Height;

                // enforce minimum fraction
                nw = Math.Max(nw, MinFraction);
                nh = Math.Max(nh, MinFraction);

                // ensure within bounds
                nx = Math.Clamp(nx, 0, 1 - nw);
                ny = Math.Clamp(ny, 0, 1 - nh);

                CropRelativeX = nx;
                CropRelativeY = ny;
                CropRelativeWidth = nw;
                CropRelativeHeight = nh;
                break;

            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                _isSelecting = false;
                break;
        }
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
