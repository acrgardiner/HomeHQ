namespace HomeHQ.Mobile.Controls;

/// <summary>
/// Darkened overlay with a draggable, resizable crop rectangle. Crop values are 0–1 relative to the bitmap.
/// Opens with no visible crop; the user presses on the image and drags to define a rectangle, then can move or resize it.
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
#if !ANDROID
    private PointerGestureRecognizer _rubberBandPointer = null!;
#endif

    private bool _isSelecting;
    private bool _hasSelection;
    private double _selectStartViewX;
    private double _selectStartViewY;
    private bool _nativeTouchAttached;
    private bool _rubberBandStartPendingValid;
    private double _rubberBandStartPendingX;
    private double _rubberBandStartPendingY;

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
        if (!_hasSelection)
        {
            return;
        }

        // Clear crop with a tap (rubber-band is press + drag only).
        _hasSelection = false;
        CropRelativeX = 0;
        CropRelativeY = 0;
        CropRelativeWidth = 0;
        CropRelativeHeight = 0;
        Arrange();
    }

#if !ANDROID
    private void OnRubberBandPointerPressed(object? sender, PointerEventArgs e)
    {
        if (_isSelecting)
        {
            return;
        }

        if (!TryGetImageDisplayRect(out var disp))
        {
            return;
        }

        Point? pt = e.GetPosition(this);
        if (pt is null)
        {
            return;
        }

        double px = pt.Value.X;
        double py = pt.Value.Y;
        if (!disp.Contains(px, py))
        {
            return;
        }

        if (_hasSelection)
        {
            double vx = disp.X + CropRelativeX * disp.Width;
            double vy = disp.Y + CropRelativeY * disp.Height;
            double vw = CropRelativeWidth * disp.Width;
            double vh = CropRelativeHeight * disp.Height;
            var cropRect = new Rect(vx, vy, vw, vh);
            if (cropRect.Contains(px, py))
            {
                return;
            }
        }

        _rubberBandStartPendingValid = true;
        _rubberBandStartPendingX = px;
        _rubberBandStartPendingY = py;
    }

    private void OnRubberBandPointerReleased(object? sender, PointerEventArgs e)
    {
        if (!_isSelecting)
        {
            _rubberBandStartPendingValid = false;
        }
    }
#endif

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

#if !ANDROID
        _rubberBandPointer = new PointerGestureRecognizer();
        _rubberBandPointer.PointerPressed += (_, e) => MainThread.BeginInvokeOnMainThread(() => OnRubberBandPointerPressed(_, e));
        _rubberBandPointer.PointerReleased += (_, e) => MainThread.BeginInvokeOnMainThread(() => OnRubberBandPointerReleased(_, e));
        _root.GestureRecognizers.Add(_rubberBandPointer);
#endif

        // Pan on the overall root to rubber-band a new crop from the press location (non-Android: paired with pointer capture above).
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
            // Show chrome when there is a real crop, or while the user is dragging out a new rectangle (sizes may be zero briefly).
            // Full-image values (1×1) from the page VM mean "no crop chosen yet" — same as no selection.
            bool isFull = Math.Abs(overlay.CropRelativeWidth - 1.0) < 1e-6 && Math.Abs(overlay.CropRelativeHeight - 1.0) < 1e-6;
            bool hasSizedSelection = !isFull &&
                                      overlay.CropRelativeWidth >= MinFraction &&
                                      overlay.CropRelativeHeight >= MinFraction;
            overlay._hasSelection = overlay._isSelecting || hasSizedSelection;
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
#if ANDROID
        if (_nativeTouchAttached)
        {
            // Down/move/up with real coordinates are handled in OnNativeTouch.
            return;
        }
#endif

        if (!TryGetImageDisplayRect(out var disp))
        {
            return;
        }

        switch (e.StatusType)
        {
            case GestureStatus.Started:
                {
                    bool fromPointer = _rubberBandStartPendingValid;
                    double startX;
                    double startY;
                    if (fromPointer)
                    {
                        startX = Math.Clamp(_rubberBandStartPendingX, disp.X, disp.X + disp.Width);
                        startY = Math.Clamp(_rubberBandStartPendingY, disp.Y, disp.Y + disp.Height);
                    }
#if ANDROID
                    else
                    {
                        // Rare fallback if native touch failed to attach.
                        startX = Math.Clamp(disp.X + disp.Width / 2, disp.X, disp.X + disp.Width);
                        startY = Math.Clamp(disp.Y + disp.Height / 2, disp.Y, disp.Y + disp.Height);
                    }
#else
                    else
                    {
                        // Without a pointer-press anchor, pan alone would pick the wrong origin.
                        return;
                    }
#endif

                    if (_hasSelection)
                    {
                        double vx = disp.X + CropRelativeX * disp.Width;
                        double vy = disp.Y + CropRelativeY * disp.Height;
                        double vw = CropRelativeWidth * disp.Width;
                        double vh = CropRelativeHeight * disp.Height;
                        var cropRect = new Rect(vx, vy, vw, vh);
                        if (cropRect.Contains(startX, startY))
                        {
                            if (fromPointer)
                            {
                                _rubberBandStartPendingValid = false;
                            }

                            _isSelecting = false;
                            break;
                        }
                    }

                    if (fromPointer)
                    {
                        _rubberBandStartPendingValid = false;
                    }

                    _isSelecting = true;
                    _selectStartViewX = startX;
                    _selectStartViewY = startY;

                    double relX = (_selectStartViewX - disp.X) / disp.Width;
                    double relY = (_selectStartViewY - disp.Y) / disp.Height;
                    CropRelativeX = relX;
                    CropRelativeY = relY;
                    CropRelativeWidth = 0;
                    CropRelativeHeight = 0;
                    Arrange();
                    break;
                }

            case GestureStatus.Running:
                if (!_isSelecting)
                {
                    return;
                }

                double curX = _selectStartViewX + e.TotalX;
                double curY = _selectStartViewY + e.TotalY;
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

                nw = Math.Max(nw, MinFraction);
                nh = Math.Max(nh, MinFraction);

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
                _rubberBandStartPendingValid = false;
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
