// Copyright © 2013 The CefSharp Authors. All rights reserved.
//
// Use of this source code is governed by a BSD-style license that can be found in the LICENSE file.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using CefSharp.Enums;
using CefSharp.Internals;
using CefSharp.Structs;
using CefSharp.Wpf.Experimental;
using CefSharp.Wpf.Handler;
using CefSharp.Wpf.Internals;
using CefSharp.Wpf.Rendering;
using Microsoft.Win32.SafeHandles;
using CursorType = CefSharp.Enums.CursorType;
using Point = System.Windows.Point;
using Range = CefSharp.Structs.Range;
using Rect = CefSharp.Structs.Rect;
using Size = System.Windows.Size;

namespace CefSharp.Wpf
{
    /// <summary>
    /// ChromiumWebBrowser is the WPF web browser control
    /// </summary>
    /// <seealso cref="System.Windows.Controls.Control" />
    /// <seealso cref="CefSharp.Wpf.IWpfWebBrowser" />
    [TemplatePart(Name = PartImageName, Type = typeof(Image))]
    [TemplatePart(Name = PartPopupImageName, Type = typeof(Image))]
    public partial class ChromiumWebBrowser : Control, IRenderWebBrowser, IWpfWebBrowser
    {
        /// <summary>
        /// TemplatePart Name constant for the Image used to represent the browser
        /// </summary>
        public const string PartImageName = "PART_image";
        /// <summary>
        /// TemplatePart Name constant for the Image used to represent the popup
        /// overlayed on the browser
        /// </summary>
        public const string PartPopupImageName = "PART_popupImage";

        /// <summary>
        /// View Rectangle used by <see cref="GetViewRect"/>
        /// </summary>
        private Rect viewRect;
        /// <summary>
        /// Store the previous window state, used to determine if the
        /// Windows was previous <see cref="WindowState.Minimized"/>
        /// and resume rendering
        /// </summary>
        private WindowState previousWindowState;
        /// <summary>
        /// The source
        /// </summary>
        private HwndSource source;
        /// <summary>
        /// The HwndSource RootVisual (Window) - We store a reference
        /// to unsubscribe event handlers
        /// </summary>
        private Window sourceWindow;
        /// <summary>
        /// The MonitorInfo based on the current hwnd
        /// </summary>
        private MonitorInfoEx monitorInfo;
        /// <summary>
        /// The tooltip timer
        /// </summary>
        private DispatcherTimer tooltipTimer;
        /// <summary>
        /// The tool tip
        /// </summary>
        private ToolTip toolTip;
        /// <summary>
        /// The managed cef browser adapter
        /// </summary>
        private IBrowserAdapter managedCefBrowserAdapter;
        /// <summary>
        /// The ignore URI change
        /// </summary>
        private bool ignoreUriChange;
        /// <summary>
        /// Initial address
        /// </summary>
        private string initialAddress;
        /// <summary>
        /// Used to stop multiple threads trying to load the initial Url multiple times.
        /// If the Address property is bound after the browser is initialized
        /// </summary>
        private bool initialLoadCalled;
        /// <summary>
        /// Has the underlying Cef Browser been created (slightly different to initialized in that
        /// the browser is initialized in an async fashion)
        /// </summary>
        private bool browserCreated;
        /// <summary>
        /// The image that represents this browser instances
        /// </summary>
        private Image image;
        /// <summary>
        /// The popup image
        /// </summary>
        private Image popupImage;
        /// <summary>
        /// Location of the control on the screen, relative to Top/Left
        /// Used to calculate GetScreenPoint
        /// We're unable to call PointToScreen directly due to treading restrictions
        /// and calling in a sync fashion on the UI thread was problematic.
        /// </summary>
        private Point browserScreenLocation;
        /// <summary>
        /// Browser initialization settings
        /// </summary>
        private IBrowserSettings browserSettings;
        /// <summary>
        /// The request context (we deliberately use a private variable so we can throw an exception if
        /// user attempts to set after browser created)
        /// </summary>
        private IRequestContext requestContext;
        /// <summary>
        /// Keep a short term copy of IDragData, so when calling DoDragDrop, DragEnter is called, 
        /// we can reuse the drag data provided from CEF
        /// </summary>
        private IDragData currentDragData;
        /// <summary>
        /// Keep the current drag&amp;drop effects to return the appropriate effects on drag over.
        /// </summary>
        private DragDropEffects? currentDragDropEffects;
        /// <summary>
        /// A flag that indicates whether or not the designer is active
        /// NOTE: Needs to be static for OnApplicationExit
        /// </summary>
        private static bool DesignMode;

        // https://github.com/chromiumembedded/cef/issues/3427
        private bool resizeHackIgnoreOnPaint;
        private Structs.Size? resizeHackSize;

        /// <summary>
        /// This flag is set when the browser gets focus before the underlying CEF browser
        /// has been initialized.
        /// </summary>
        private bool initialFocus;

        /// <summary>
        /// The class that coordinates the positioning of the dropdown if wanted.
        /// </summary>
        internal IMousePositionTransform MousePositionTransform { get; set; }

        /// <summary>
        /// When enabled the browser will resize by 1px when it becomes visible to workaround
        /// the upstream issue
        /// Hack to work around upstream issue https://github.com/chromiumembedded/cef/issues/3427
        /// Disabled by default
        /// </summary>
        public bool ResizeHackEnabled { get; set; } = false;

        /// <summary>
        /// Number of milliseconds to wait after resizing the browser when it first
        /// becomes visible. After the delay the browser will revert to it's
        /// original size.
        /// Hack to workaround upstream issue https://github.com/chromiumembedded/cef/issues/3427
        /// </summary>
        public int ResizeHackDelayInMs { get; set; } = 50;

        /// <summary>
        /// Gets a value indicating whether this instance is disposed.
        /// </summary>
        /// <value><see langword="true" /> if this instance is disposed; otherwise, <see langword="false" />.</value>
        public bool IsDisposed
        {
            get
            {
                return Interlocked.CompareExchange(ref disposeSignaled, 1, 1) == 1;
            }
        }

        /// <summary>
        /// WPF Keyboard Handled forwards key events to the underlying browser
        /// </summary>
        public IWpfKeyboardHandler WpfKeyboardHandler { get; set; }

        /// <summary>
        /// Gets or sets the browser settings.
        /// </summary>
        /// <value>The browser settings.</value>
        public IBrowserSettings BrowserSettings
        {
            get { return browserSettings; }
            set
            {
                if (browserCreated)
                {
                    throw new Exception("Browser has already been created. BrowserSettings must be " +
                                        "set before the underlying CEF browser is created.");
                }

                //New instance is created in the constructor, if you use
                //xaml to initialize browser settings then it will also create a new
                //instance, so we dispose of the old one
                if (browserSettings != null && browserSettings.AutoDispose)
                {
                    browserSettings.Dispose();
                }

                browserSettings = value;
            }
        }
        /// <summary>
        /// Gets or sets the request context.
        /// </summary>
        /// <value>The request context.</value>
        public IRequestContext RequestContext
        {
            get { return requestContext; }
            set
            {
                if (browserCreated)
                {
                    throw new Exception("Browser has already been created. RequestContext must be " +
                                        "set before the underlying CEF browser is created.");
                }
                if (value != null && !Core.ObjectFactory.RequestContextType.IsAssignableFrom(value.UnWrap().GetType()))
                {
                    throw new Exception(string.Format("RequestContext can only be of type {0} or null", Core.ObjectFactory.RequestContextType));
                }
                requestContext = value;
            }
        }
        /// <summary>
        /// Implement <see cref="IRenderHandler"/> and control how the control is rendered
        /// </summary>
        /// <value>The render Handler.</value>
        public IRenderHandler RenderHandler { get; set; }
        /// <summary>
        /// Implement <see cref="IAccessibilityHandler" /> to handle events related to accessibility.
        /// </summary>
        /// <value>The accessibility handler.</value>
        public IAccessibilityHandler AccessibilityHandler { get; set; }

        /// <summary>
        /// Raised every time <see cref="IRenderWebBrowser.OnPaint"/> is called. You can access the underlying buffer, though it's
        /// preferable to either override <see cref="OnPaint"/> or implement your own <see cref="IRenderHandler"/> as there is no outwardly
        /// accessible locking (locking is done within the default <see cref="IRenderHandler"/> implementations).
        /// It's important to note this event is fired on a CEF UI thread, which by default is not the same as your application UI thread
        /// </summary>
        public event EventHandler<PaintEventArgs> Paint;

        /// <summary>
        /// Raised every time <see cref="IRenderWebBrowser.OnVirtualKeyboardRequested(IBrowser, TextInputMode)"/> is called. 
        /// It's important to note this event is fired on a CEF UI thread, which by default is not the same as your application UI thread
        /// </summary>
        public event EventHandler<VirtualKeyboardRequestedEventArgs> VirtualKeyboardRequested;

        /// <summary>
        /// Navigates to the previous page in the browser history. Will automatically be enabled/disabled depending on the
        /// browser state.
        /// </summary>
        /// <value>The back command.</value>
        public ICommand BackCommand { get; private set; }
        /// <summary>
        /// Navigates to the next page in the browser history. Will automatically be enabled/disabled depending on the
        /// browser state.
        /// </summary>
        /// <value>The forward command.</value>
        public ICommand ForwardCommand { get; private set; }
        /// <summary>
        /// Reloads the content of the current page. Will automatically be enabled/disabled depending on the browser state.
        /// </summary>
        /// <value>The reload command.</value>
        public ICommand ReloadCommand { get; private set; }
        /// <summary>
        /// Prints the current browser contents.
        /// </summary>
        /// <value>The print command.</value>
        public ICommand PrintCommand { get; private set; }
        /// <summary>
        /// Increases the zoom level.
        /// </summary>
        /// <value>The zoom in command.</value>
        public ICommand ZoomInCommand { get; private set; }
        /// <summary>
        /// Decreases the zoom level.
        /// </summary>
        /// <value>The zoom out command.</value>
        public ICommand ZoomOutCommand { get; private set; }
        /// <summary>
        /// Resets the zoom level to the default. (100%)
        /// </summary>
        /// <value>The zoom reset command.</value>
        public ICommand ZoomResetCommand { get; private set; }
        /// <summary>
        /// Opens up a new program window (using the default text editor) where the source code of the currently displayed web
        /// page is shown.
        /// </summary>
        /// <value>The view source command.</value>
        public ICommand ViewSourceCommand { get; private set; }
        /// <summary>
        /// Command which cleans up the Resources used by the ChromiumWebBrowser
        /// </summary>
        /// <value>The cleanup command.</value>
        public ICommand CleanupCommand { get; private set; }
        /// <summary>
        /// Stops loading the current page.
        /// </summary>
        /// <value>The stop command.</value>
        public ICommand StopCommand { get; private set; }
        /// <summary>
        /// Cut selected text to the clipboard.
        /// </summary>
        /// <value>The cut command.</value>
        public ICommand CutCommand { get; private set; }
        /// <summary>
        /// Copy selected text to the clipboard.
        /// </summary>
        /// <value>The copy command.</value>
        public ICommand CopyCommand { get; private set; }
        /// <summary>
        /// Paste text from the clipboard.
        /// </summary>
        /// <value>The paste command.</value>
        public ICommand PasteCommand { get; private set; }
        /// <summary>
        /// Select all text.
        /// </summary>
        /// <value>The select all command.</value>
        public ICommand SelectAllCommand { get; private set; }
        /// <summary>
        /// Undo last action.
        /// </summary>
        /// <value>The undo command.</value>
        public ICommand UndoCommand { get; private set; }
        /// <summary>
        /// Redo last action.
        /// </summary>
        /// <value>The redo command.</value>
        public ICommand RedoCommand { get; private set; }

        /// <inheritdoc/>
        public ICommand ToggleAudioMuteCommand { get; private set; }

        /// <summary>
        /// The dpi scale factor, if the browser has already been initialized
        /// you must manually call IBrowserHost.NotifyScreenInfoChanged for the
        /// browser to be notified of the change.
        /// </summary>
        public float DpiScaleFactor { get; set; }

        /// <summary>
        /// Initializes static members of the <see cref="ChromiumWebBrowser"/> class.
        /// </summary>
        //TODO: Currently only a single Dispatcher is supported, all controls will
        //be Shutdown on that dispatcher which may not actually be the correct Dispatcher.
        //We could potentially use a ThreadStatic variable to implement this behaviour
        //and support multiple dispatchers.
        static ChromiumWebBrowser()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(ChromiumWebBrowser),
                new FrameworkPropertyMetadata(typeof(ChromiumWebBrowser)));

            if (CefSharpSettings.ShutdownOnExit)
            {
                //Use Dispatcher.FromThread as it returns null if no dispatcher
                //is available for this thread.
                var dispatcher = Dispatcher.FromThread(Thread.CurrentThread);
                if (dispatcher == null)
                {
                    //No dispatcher then we'll rely on Application.Exit
                    var app = Application.Current;

                    if (app != null)
                    {
                        app.Exit += OnApplicationExit;
                    }
                }
                else
                {
                    dispatcher.ShutdownStarted += DispatcherShutdownStarted;
                    dispatcher.ShutdownFinished += DispatcherShutdownFinished;
                }

            }
        }

        /// <summary>
        /// Handles Dispatcher Shutdown
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">eventargs</param>
        private static void DispatcherShutdownStarted(object sender, EventArgs e)
        {
            var dispatcher = (Dispatcher)sender;

            dispatcher.ShutdownStarted -= DispatcherShutdownStarted;

            if (!DesignMode)
            {
                CefPreShutdown();
            }
        }

        private static void DispatcherShutdownFinished(object sender, EventArgs e)
        {
            var dispatcher = (Dispatcher)sender;

            dispatcher.ShutdownFinished -= DispatcherShutdownFinished;

            if (!DesignMode)
            {
                CefShutdown();
            }
        }

        /// <summary>
        /// Handles the <see cref="E:ApplicationExit" /> event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="ExitEventArgs"/> instance containing the event data.</param>
        private static void OnApplicationExit(object sender, ExitEventArgs e)
        {
            if (!DesignMode)
            {
                CefShutdown();
            }
        }

        /// <summary>
        /// Required for designer support - this method cannot be inlined as the designer
        /// will attempt to load libcef.dll and will subsequently throw an exception.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void CefPreShutdown()
        {
            Cef.PreShutdown();
        }

        /// <summary>
        /// Required for designer support - this method cannot be inlined as the designer
        /// will attempt to load libcef.dll and will subsequently throw an exception.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void CefShutdown()
        {
            Cef.Shutdown();
        }

        /// <summary>
        /// To control how <see cref="Cef.Shutdown"/> is called, this method will
        /// unsubscribe from <see cref="Application.Exit"/>,
        /// <see cref="Dispatcher.ShutdownStarted"/> and <see cref="Dispatcher.ShutdownFinished"/>.
        /// </summary>
        public static void UnregisterShutdownHandler()
        {
            //Use Dispatcher.FromThread as it returns null if no dispatcher
            //is available for this thread.
            var dispatcher = Dispatcher.FromThread(Thread.CurrentThread);
            if (dispatcher != null)
            {
                dispatcher.ShutdownStarted -= DispatcherShutdownStarted;
                dispatcher.ShutdownFinished -= DispatcherShutdownFinished;
            }

            var app = Application.Current;

            if (app != null)
            {
                app.Exit -= OnApplicationExit;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChromiumWebBrowser"/> class.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">Cef::Initialize() failed</exception>
        public ChromiumWebBrowser()
        {
            DesignMode = System.ComponentModel.DesignerProperties.GetIsInDesignMode(this);

            if (!DesignMode)
            {
                NoInliningConstructor();
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChromiumWebBrowser"/> class.
        /// Use this constructor to load the browser before it's attached to the Visual Tree.
        /// The underlying CefBrowser will be created with the specified <paramref name="size"/>.
        /// CEF requires positive values for <see cref="Size.Width"/> and <see cref="Size.Height"/>,
        /// if values less than 1 are specified then the default value of 1 will be used instead.
        /// You can subscribe to the <see cref="LoadingStateChanged"/> event and attach the browser
        /// to its parent control when Loading is complete (<see cref="LoadingStateChangedEventArgs.IsLoading"/> is false).
        /// </summary>
        /// <param name="parentWindowHwndSource">HwndSource for the Window that will host the browser.</param>
        /// <param name="initialAddress">address to be loaded when the browser is created.</param>
        /// <param name="size">size</param>
        /// <example>
        /// <code>
        /// //Obtain Hwnd from parent window
        /// var hwndSource = (HwndSource)PresentationSource.FromVisual(this);
        /// var browser = new ChromiumWebBrowser(hwndSource, "github.com", 1024, 768);
        /// browser.LoadingStateChanged += OnBrowserLoadingStateChanged;
        /// 
        /// private void OnBrowserLoadingStateChanged(object sender, LoadingStateChangedEventArgs e)
        /// {
        ///   if (e.IsLoading == false)
        ///   {
        ///     var b = (ChromiumWebBrowser)sender;
        ///     b.LoadingStateChanged -= OnBrowserLoadingStateChanged;
        ///     Dispatcher.InvokeAsync(() =>
        ///     {
        ///       //Attach to visual tree
        ///       ParentControl.Child = b;
        ///     });
        ///   }
        /// }
        /// </code>
        /// </example>
        public ChromiumWebBrowser(HwndSource parentWindowHwndSource, string initialAddress, Size size) : this(initialAddress)
        {
            CreateBrowser(parentWindowHwndSource, size);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChromiumWebBrowser"/> class.
        /// The specified <paramref name="initialAddress"/> will be loaded initially.
        /// Use this constructor if you are loading a Chrome Extension.
        /// </summary>
        /// <param name="initialAddress">address to be loaded when the browser is created.</param>
        public ChromiumWebBrowser(string initialAddress)
        {
            this.initialAddress = initialAddress;

            NoInliningConstructor();
        }

        /// <summary>
        /// Constructor logic has been moved into this method
        /// Required for designer support - this method cannot be inlined as the designer
        /// will attempt to load libcef.dll and will subsequently throw an exception.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void NoInliningConstructor()
        {
            InitializeCefInternal();

            //Add this ChromiumWebBrowser instance to a list of IDisposable objects
            // that if still alive at the time Cef.Shutdown is called will be disposed of
            // It's important all browser instances be freed before Cef.Shutdown is called.
            Cef.AddDisposable(this);
            Focusable = true;
            FocusVisualStyle = null;
            IsTabStop = true;

            Dispatcher.BeginInvoke((Action)(() => WebBrowser = this));

            Loaded += OnLoaded;
            SizeChanged += OnActualSizeChanged;

            GotKeyboardFocus += OnGotKeyboardFocus;
            LostKeyboardFocus += OnLostKeyboardFocus;

            // Drag Drop events
            DragEnter += OnDragEnter;
            DragOver += OnDragOver;
            DragLeave += OnDragLeave;
            Drop += OnDrop;

            IsVisibleChanged += OnIsVisibleChanged;

            ToolTip = toolTip = new ToolTip();
            toolTip.StaysOpen = true;
            toolTip.Visibility = Visibility.Collapsed;
            toolTip.Closed += OnTooltipClosed;

            BackCommand = new DelegateCommand(this.Back, () => CanGoBack);
            ForwardCommand = new DelegateCommand(this.Forward, () => CanGoForward);
            ReloadCommand = new DelegateCommand(this.Reload, () => !IsLoading);
            PrintCommand = new DelegateCommand(this.Print);
            ZoomInCommand = new DelegateCommand(ZoomIn);
            ZoomOutCommand = new DelegateCommand(ZoomOut);
            ZoomResetCommand = new DelegateCommand(ZoomReset);
            ViewSourceCommand = new DelegateCommand(this.ViewSource);
            CleanupCommand = new DelegateCommand(Dispose);
            StopCommand = new DelegateCommand(this.Stop);
            CutCommand = new DelegateCommand(this.Cut);
            CopyCommand = new DelegateCommand(this.Copy);
            PasteCommand = new DelegateCommand(this.Paste);
            SelectAllCommand = new DelegateCommand(this.SelectAll);
            UndoCommand = new DelegateCommand(this.Undo);
            RedoCommand = new DelegateCommand(this.Redo);
            ToggleAudioMuteCommand = new DelegateCommand(this.ToggleAudioMute);

            managedCefBrowserAdapter = ManagedCefBrowserAdapter.Create(this, true);

            browserSettings = Core.ObjectFactory.CreateBrowserSettings(autoDispose: true);

            SetupWpfKeyboardHandler();

            if (WpfKeyboardHandler == null)
            {
                WpfKeyboardHandler = new WpfKeyboardHandler(this);
            }

            PresentationSource.AddSourceChangedHandler(this, PresentationSourceChangedHandler);

            MenuHandler = new ContextMenuHandler();
            MousePositionTransform = new NoOpMousePositionTransform();

            UseLayoutRounding = true;
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="ChromiumWebBrowser"/> class.
        /// </summary>
        ~ChromiumWebBrowser()
        {
            Dispose(false);
        }

        /// <summary>
        /// Releases all resources used by the <see cref="ChromiumWebBrowser"/> object
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// If not in design mode; Releases unmanaged and - optionally - managed resources for the <see cref="ChromiumWebBrowser"/>
        /// </summary>
        /// <param name="disposing"><see langword="true" /> to release both managed and unmanaged resources; <see langword="false" /> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            // Attempt to move the disposeSignaled state from 0 to 1. If successful, we can be assured that
            // this thread is the first thread to do so, and can safely dispose of the object.
            if (Interlocked.CompareExchange(ref disposeSignaled, 1, 0) != 0)
            {
                return;
            }

            if (DesignMode)
            {
                return;
            }

            InternalDispose(disposing);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources for the <see cref="ChromiumWebBrowser"/>
        /// </summary>
        /// <param name="disposing"><see langword="true" /> to release both managed and unmanaged resources; <see langword="false" /> to release only unmanaged resources.</param>
        /// <remarks>
        /// This method cannot be inlined as the designer will attempt to load libcef.dll and will subsequently throw an exception.
        /// </remarks>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void InternalDispose(bool disposing)
        {
            if (disposing)
            {
                CanExecuteJavascriptInMainFrame = false;
                Interlocked.Exchange(ref browserInitialized, 0);

                //Stop rendering immediately so later on when we dispose of the
                //RenderHandler no further OnPaint calls take place
                //Check browser not null as it's possible to call Dispose before it's created
                if (browser?.IsDisposed == false)
                {
                    browser?.GetHost().WasHidden(true);
                }

                UiThreadRunAsync(() =>
                {
                    SetCurrentValue(IsBrowserInitializedProperty, false);
                    WebBrowser = null;
                });

                // No longer reference event listeners:
                ConsoleMessage = null;
                FrameLoadEnd = null;
                FrameLoadStart = null;
                AddressChanged = null;
                IsBrowserInitializedChanged = null;
                LoadError = null;
                LoadingStateChanged = null;
                Paint = null;
                StatusMessage = null;
                TitleChanged = null;
                VirtualKeyboardRequested = null;
                JavascriptMessageReceived = null;

                // Release reference to handlers, except LifeSpanHandler which is done after Disposing
                // ManagedCefBrowserAdapter otherwise the ILifeSpanHandler.DoClose will not be invoked.
                // We also leave FocusHandler and override with a NoFocusHandler implementation as
                // it so we can block taking Focus (we're dispoing afterall). Issue #3715
                FreeHandlersExceptLifeSpanAndFocus();

                FocusHandler = new NoFocusHandler();

                browser = null;
                BrowserCore = null;

                // In case we accidentally have a reference to the CEF drag data
                currentDragData?.Dispose();
                currentDragData = null;

                PresentationSource.RemoveSourceChangedHandler(this, PresentationSourceChangedHandler);
                // Release window event listeners if PresentationSourceChangedHandler event wasn't
                // fired before Dispose
                if (sourceWindow != null)
                {
                    sourceWindow.StateChanged -= OnWindowStateChanged;
                    sourceWindow.LocationChanged -= OnWindowLocationChanged;
                    sourceWindow = null;
                }

                // Release internal event listeners:
                Loaded -= OnLoaded;
                SizeChanged -= OnActualSizeChanged;
                GotKeyboardFocus -= OnGotKeyboardFocus;
                LostKeyboardFocus -= OnLostKeyboardFocus;

                // Release internal event listeners for Drag Drop events:
                DragEnter -= OnDragEnter;
                DragOver -= OnDragOver;
                DragLeave -= OnDragLeave;
                Drop -= OnDrop;

                IsVisibleChanged -= OnIsVisibleChanged;

                if (tooltipTimer != null)
                {
                    tooltipTimer.Tick -= OnTooltipTimerTick;
                    tooltipTimer.Stop();
                    tooltipTimer = null;
                }

                if (CleanupElement != null)
                {
                    CleanupElement.Unloaded -= OnCleanupElementUnloaded;
                }

                managedCefBrowserAdapter?.Dispose();
                managedCefBrowserAdapter = null;

                // LifeSpanHandler is set to null after managedCefBrowserAdapter.Dispose so ILifeSpanHandler.DoClose
                // is called.
                LifeSpanHandler = null;

                UnsubscribeInputLanguageChanged();

                WpfKeyboardHandler?.Dispose();
                WpfKeyboardHandler = null;

                //Take a copy of the RenderHandler then set to property to null
                //Before we dispose, reduces the changes of any OnPaint calls
                //using the RenderHandler after Dispose
                var renderHandler = RenderHandler;
                RenderHandler = null;
                renderHandler?.Dispose();

                source = null;
            }

            Cef.RemoveDisposable(this);
        }

        /// <summary>
        /// Setup the <see cref="WpfKeyboardHandler"/> used by this <see cref="ChromiumWebBrowser"/> instance.
        /// There are two main keyboard handler implementations currently <see cref="WpfImeKeyboardHandler"/>
        /// and <see cref="Internals.WpfKeyboardHandler"/>. If <see cref="WpfImeKeyboardHandler.UseImeKeyboardHandler"/>
        /// returns true for the current <see cref="System.Globalization.CultureInfo.KeyboardLayoutId"/>
        /// then the <see cref="WpfImeKeyboardHandler"/> instance will be used, otherwise the older
        /// <see cref="Internals.WpfKeyboardHandler"/> instance will be used.
        /// Override to provide your own custom keyboard handler.
        /// </summary>
        protected virtual void SetupWpfKeyboardHandler()
        {
            try
            {
                var inputLang = InputLanguageManager.Current.CurrentInputLanguage;

                var useImeKeyboardHandler = WpfImeKeyboardHandler.UseImeKeyboardHandler(inputLang.KeyboardLayoutId);

                if (useImeKeyboardHandler)
                {
                    WpfKeyboardHandler = new WpfImeKeyboardHandler(this);
                }
                else
                {
                    InputLanguageManager.Current.InputLanguageChanged += OnInputLanguageChanged;

                    WpfKeyboardHandler = new WpfKeyboardHandler(this);
                }
            }
            catch (Exception ex)
            {
                //For now we'll ignore any errors
                Trace.TraceError($"Error initializing keyboard handler: {ex.ToString()}");

                // For now we'll ignore any errors and just use the default keyboard handler
                WpfKeyboardHandler = new WpfKeyboardHandler(this);
            }
        }

        /// <summary>
        /// Gets the ScreenInfo - currently used to get the DPI scale factor.
        /// </summary>
        /// <returns>ScreenInfo containing the current DPI scale factor</returns>
        ScreenInfo? IRenderWebBrowser.GetScreenInfo()
        {
            return GetScreenInfo();
        }

        /// <summary>
        /// Gets the ScreenInfo - currently used to get the DPI scale factor.
        /// </summary>
        /// <returns>ScreenInfo containing the current DPI scale factor</returns>
        protected virtual ScreenInfo? GetScreenInfo()
        {
            Rect rect = monitorInfo.Monitor;
            Rect availableRect = monitorInfo.WorkArea;

            if (DpiScaleFactor > 1.0)
            {
                rect = rect.ScaleByDpi(DpiScaleFactor);
                availableRect = availableRect.ScaleByDpi(DpiScaleFactor);
            }

            var screenInfo = new ScreenInfo
            {
                DeviceScaleFactor = DpiScaleFactor,
                Rect = rect,
                AvailableRect = availableRect
            };

            return screenInfo;
        }

        /// <summary>
        /// Called to retrieve the view rectangle which is relative to screen coordinates.
        /// This method must always provide a non-empty rectangle.
        /// </summary>
        /// <returns>View Rectangle</returns>
        Rect IRenderWebBrowser.GetViewRect()
        {
            return GetViewRect();
        }

        /// <summary>
        /// Called to retrieve the view rectangle which is relative to screen coordinates.
        /// This method must always provide a non-empty rectangle.
        /// </summary>
        /// <returns>View Rectangle</returns>
        protected virtual Rect GetViewRect()
        {
            //Take a local copy as the value is set on a different thread,
            //Its possible the struct is set to null after our initial check.
            var resizeRect = resizeHackSize;

            if (resizeRect == null)
            {
                return viewRect;
            }

            var size = resizeRect.Value;

            return new Rect(0, 0, size.Width, size.Height);
        }

        /// <inheritdoc />
        bool IRenderWebBrowser.GetScreenPoint(int viewX, int viewY, out int screenX, out int screenY)
        {
            return GetScreenPoint(viewX, viewY, out screenX, out screenY);
        }

        /// <summary>
        /// Called to retrieve the translation from view coordinates to actual screen coordinates. 
        /// </summary>
        /// <param name="viewX">x</param>
        /// <param name="viewY">y</param>
        /// <param name="screenX">screen x</param>
        /// <param name="screenY">screen y</param>
        /// <returns>Return true if the screen coordinates were provided.</returns>
        protected virtual bool GetScreenPoint(int viewX, int viewY, out int screenX, out int screenY)
        {
            screenX = 0;
            screenY = 0;

            //We manually calculate the screen point as calling PointToScreen can only be called on the UI thread
            // in a sync fashion and it's easy for users to get themselves into a deadlock.
            if (DpiScaleFactor > 1)
            {
                screenX = (int)(browserScreenLocation.X + (viewX * DpiScaleFactor));
                screenY = (int)(browserScreenLocation.Y + (viewY * DpiScaleFactor));
            }
            else
            {
                screenX = (int)(browserScreenLocation.X + viewX);
                screenY = (int)(browserScreenLocation.Y + viewY);
            }

            return true;
        }

        /// <inheritdoc />
        bool IRenderWebBrowser.StartDragging(IDragData dragData, DragOperationsMask allowedOps, int x, int y)
        {
            return StartDragging(dragData, allowedOps, x, y);
        }

        /// <summary>
        /// Called when the user starts dragging content in the web view. 
        /// OS APIs that run a system message loop may be used within the StartDragging call.
        /// Don't call any of IBrowserHost::DragSource*Ended* methods after returning false.
        /// Call IBrowserHost.DragSourceEndedAt and DragSourceSystemDragEnded either synchronously or asynchronously to inform the web view that the drag operation has ended. 
        /// </summary>
        /// <param name="dragData"> Contextual information about the dragged content</param>
        /// <param name="allowedOps">allowed operations</param>
        /// <param name="x">is the drag start location in screen coordinates</param>
        /// <param name="y">is the drag start location in screen coordinates</param>
        /// <returns>Return true to handle the drag operation.</returns>
        protected virtual bool StartDragging(IDragData dragData, DragOperationsMask allowedOps, int x, int y)
        {
            var dataObject = new DataObject();

            dataObject.SetText(dragData.FragmentText, TextDataFormat.Text);
            dataObject.SetText(dragData.FragmentText, TextDataFormat.UnicodeText);
            dataObject.SetText(dragData.FragmentHtml, TextDataFormat.Html);

            //Clone dragData for use in OnDragEnter event handler
            currentDragData = dragData.Clone();
            currentDragData.ResetFileContents();

            // TODO: The following code block *should* handle images, but GetFileContents is
            // not yet implemented.
            //if (dragData.IsFile)
            //{
            //    var bmi = new BitmapImage();
            //    bmi.BeginInit();
            //    bmi.StreamSource = dragData.GetFileContents();
            //    bmi.EndInit();
            //    dataObject.SetImage(bmi);
            //}

            UiThreadRunAsync(delegate
            {
                if (browser != null)
                {
                    // DoDragDrop will fire DragEnter event
                    var result = DragDrop.DoDragDrop(this, dataObject, allowedOps.GetDragEffects());
                    var dragOperationMask = GetDragOperationsMask(result, currentDragDropEffects);

                    // DragData was stored so when DoDragDrop fires DragEnter we reuse a clone of the IDragData provided here
                    currentDragData = null;
                    currentDragDropEffects = null;

                    // If result or the last recorded drag drop effect were DragDropEffects.None
                    // then we'll send DragOperationsMask.None, effectively cancelling the drag operation
                    browser.GetHost().DragSourceEndedAt(x, y, dragOperationMask);
                    browser.GetHost().DragSourceSystemDragEnded();
                }
            });

            return true;
        }

        protected virtual DragOperationsMask GetDragOperationsMask(DragDropEffects result, DragDropEffects? dragEffects)
        {
            // Ensure the drag and drop operation wasn't cancelled
            var finalEffectMask = (dragEffects ?? DragDropEffects.None).GetDragOperationsMask() & result.GetDragOperationsMask();

            // We shouldn't have an instance where finalEffectMask signfies multiple effects, as the operation
            // set by UpdateDragCursor should only ever be none, move, copy, or link. However, if it does, we'll
            // just default to copy, as that reflects the defaulting behavior of GetDragEffects.
            if (finalEffectMask.HasFlag(DragOperationsMask.Every))
            {
                return DragOperationsMask.Copy;
            }

            return finalEffectMask;
        }

        /// <inheritdoc />
        void IRenderWebBrowser.UpdateDragCursor(DragOperationsMask operation)
        {
            UpdateDragCursor(operation);
        }

        /// <summary>
        /// Called when the web view wants to update the mouse cursor during a drag &amp; drop operation.
        /// </summary>
        /// <param name="operation">describes the allowed operation (none, move, copy, link). </param>
        protected virtual void UpdateDragCursor(DragOperationsMask operation)
        {
            currentDragDropEffects = operation.GetDragEffects();
        }

        /// <inheritdoc />
        void IRenderWebBrowser.OnAcceleratedPaint(PaintElementType type, Rect dirtyRect, AcceleratedPaintInfo acceleratedPaintInfo)
        {
            OnAcceleratedPaint(type == PaintElementType.Popup, dirtyRect, acceleratedPaintInfo);
        }

        /// <summary>
        /// Called when an element has been rendered to the shared texture handle.
        /// This method is only called when <see cref="IWindowInfo.SharedTextureEnabled"/> is set to true
        ///
        /// The underlying implementation uses a pool to deliver frames. As a result,
        /// the handle may differ every frame depending on how many frames are
        /// in-progress. The handle's resource cannot be cached and cannot be accessed
        /// outside of this callback. It should be reopened each time this callback is
        /// executed and the contents should be copied to a texture owned by the
        /// client application. The contents of <paramref name="acceleratedPaintInfo"/>acceleratedPaintInfo
        /// will be released back to the pool after this callback returns.
        /// </summary>
        /// <param name="isPopup">indicates whether the element is the view or the popup widget.</param>
        /// <param name="dirtyRect">contains the set of rectangles in pixel coordinates that need to be repainted</param>
        /// <param name="acceleratedPaintInfo">contains the shared handle; on Windows it is a
        /// HANDLE to a texture that can be opened with D3D11 OpenSharedResource.</param>
        protected virtual void OnAcceleratedPaint(bool isPopup, Rect dirtyRect, AcceleratedPaintInfo acceleratedPaintInfo)
        {
            RenderHandler?.OnAcceleratedPaint(isPopup, dirtyRect, acceleratedPaintInfo);
        }

        /// <inheritdoc />
        void IRenderWebBrowser.OnPaint(PaintElementType type, Rect dirtyRect, IntPtr buffer, int width, int height)
        {
            OnPaint(type == PaintElementType.Popup, dirtyRect, buffer, width, height);
        }

        /// <summary>
        /// Called when an element should be painted. Pixel values passed to this method are scaled relative to view coordinates based on the
        /// value of <see cref="ScreenInfo.DeviceScaleFactor"/> returned from <see cref="IRenderWebBrowser.GetScreenInfo"/>. To override the default behaviour
        /// override this method or implement your own <see cref="IRenderHandler"/> and assign to <see cref="RenderHandler"/>
        /// Called on the CEF UI Thread
        /// </summary>
        /// <param name="isPopup">indicates whether the element is the view or the popup widget.</param>
        /// <param name="dirtyRect">contains the set of rectangles in pixel coordinates that need to be repainted</param>
        /// <param name="buffer">The bitmap will be width * height *4 bytes in size and represents a BGRA image with an upper-left origin.
        /// The buffer should no be used outside the scope of this method, a copy should be taken. As the buffer is reused
        /// internally and potentially even freed.
        /// </param>
        /// <param name="width">width</param>
        /// <param name="height">height</param>
        protected virtual void OnPaint(bool isPopup, Rect dirtyRect, IntPtr buffer, int width, int height)
        {
            if (resizeHackIgnoreOnPaint)
            {
                return;
            }

            var paint = Paint;
            if (paint != null)
            {
                var args = new PaintEventArgs(isPopup, dirtyRect, buffer, width, height);

                paint(this, args);

                if (args.Handled)
                {
                    return;
                }
            }

            var img = isPopup ? popupImage : image;

            RenderHandler?.OnPaint(isPopup, dirtyRect, buffer, width, height, img);
        }

        /// <inheritdoc />
        void IRenderWebBrowser.OnPopupSize(Rect rect)
        {
            OnPopupSize(rect);
        }

        /// <summary>
        /// Sets the popup size and position.
        /// </summary>
        /// <param name="rect">The popup rectangle (size and position).</param>
        protected virtual void OnPopupSize(Rect rect)
        {
            UiThreadRunAsync(() => SetPopupSizeAndPositionImpl(rect));
        }

        /// <summary>
        /// Sets the popup is open.
        /// </summary>
        /// <param name="isOpen">if set to <c>true</c> [is open].</param>
        void IRenderWebBrowser.OnPopupShow(bool isOpen)
        {
            OnPopupShow(isOpen);
        }

        /// <summary>
        /// Sets the popup is open.
        /// </summary>
        /// <param name="isOpen">if set to <c>true</c> [is open].</param>
        protected virtual void OnPopupShow(bool isOpen)
        {
            UiThreadRunAsync(() =>
            {
                popupImage.Visibility = isOpen ? Visibility.Visible : Visibility.Hidden;
                MousePositionTransform.OnPopupShow(isOpen);
            });
        }

        /// <inheritdoc />
        void IRenderWebBrowser.OnCursorChange(IntPtr handle, CursorType type, CursorInfo customCursorInfo)
        {
            OnCursorChange(handle, type, customCursorInfo);
        }

        /// <summary>
        /// Sets the cursor.
        /// </summary>
        /// <param name="handle">The handle.</param>
        /// <param name="type">The type.</param>
        /// <param name="customCursorInfo">custom cursor information</param>
        protected virtual void OnCursorChange(IntPtr handle, CursorType type, CursorInfo customCursorInfo)
        {
            //Custom cursors are handled differently, for now keep standard ones executing
            //in an async fashion
            if (type == CursorType.Custom)
            {
                //When using a custom it appears we need to update the cursor in a sync fashion
                //Likely the underlying handle/buffer is being released before the cursor
                // is created when executed in an async fashion. Doesn't seem to be a problem
                //for built in cursor types
                UiThreadRunSync(() =>
                {
                    Cursor = CursorInteropHelper.Create(new SafeFileHandle(handle, ownsHandle: false));
                });
            }
            else
            {
                UiThreadRunAsync(() =>
                {
                    Cursor = CursorInteropHelper.Create(new SafeFileHandle(handle, ownsHandle: false));
                });
            }
        }

        /// <inheritdoc />
        void IRenderWebBrowser.OnImeCompositionRangeChanged(Range selectedRange, Rect[] characterBounds)
        {
            OnImeCompositionRangeChanged(selectedRange, characterBounds);
        }

        /// <summary>
        /// Called when the IME composition range has changed.
        /// </summary>
        /// <param name="selectedRange">is the range of characters that have been selected</param>
        /// <param name="characterBounds">is the bounds of each character in view coordinates.</param>
        protected virtual void OnImeCompositionRangeChanged(Range selectedRange, Rect[] characterBounds)
        {
            var imeKeyboardHandler = WpfKeyboardHandler as WpfImeKeyboardHandler;
            if (imeKeyboardHandler != null)
            {
                imeKeyboardHandler.ChangeCompositionRange(selectedRange, characterBounds);
            }
        }

        /// <inheritdoc />
        void IRenderWebBrowser.OnVirtualKeyboardRequested(IBrowser browser, TextInputMode inputMode)
        {
            OnVirtualKeyboardRequested(browser, inputMode);
        }

        /// <summary>
        /// Called when an on-screen keyboard should be shown or hidden for the specified browser. 
        /// </summary>
        /// <param name="browser">the browser</param>
        /// <param name="inputMode">specifies what kind of keyboard should be opened. If <see cref="TextInputMode.None"/>, any existing keyboard for this browser should be hidden.</param>
        protected virtual void OnVirtualKeyboardRequested(IBrowser browser, TextInputMode inputMode)
        {
            var args = new VirtualKeyboardRequestedEventArgs(browser, inputMode);

            VirtualKeyboardRequested?.Invoke(this, args);
        }

        /// <summary>
        /// Sets the address.
        /// </summary>
        /// <param name="args">The <see cref="AddressChangedEventArgs"/> instance containing the event data.</param>
        void IWebBrowserInternal.SetAddress(AddressChangedEventArgs args)
        {
            UiThreadRunAsync(() =>
            {
                ignoreUriChange = true;
                SetCurrentValue(AddressProperty, args.Address);
                ignoreUriChange = false;

                // The tooltip should obviously also be reset (and hidden) when the address changes.
                SetCurrentValue(TooltipTextProperty, null);
            });
        }

        /// <summary>
        /// Sets the loading state change.
        /// </summary>
        /// <param name="args">The <see cref="LoadingStateChangedEventArgs"/> instance containing the event data.</param>
        partial void SetLoadingStateChange(LoadingStateChangedEventArgs args)
        {
            UiThreadRunAsync(() =>
            {
                SetCurrentValue(CanGoBackProperty, args.CanGoBack);
                SetCurrentValue(CanGoForwardProperty, args.CanGoForward);
                SetCurrentValue(IsLoadingProperty, args.IsLoading);

                ((DelegateCommand)BackCommand).RaiseCanExecuteChanged();
                ((DelegateCommand)ForwardCommand).RaiseCanExecuteChanged();
                ((DelegateCommand)ReloadCommand).RaiseCanExecuteChanged();
            });
        }

        /// <summary>
        /// Sets the title.
        /// </summary>
        /// <param name="args">The <see cref="TitleChangedEventArgs"/> instance containing the event data.</param>
        void IWebBrowserInternal.SetTitle(TitleChangedEventArgs args)
        {
            UiThreadRunAsync(() => SetCurrentValue(TitleProperty, args.Title));
        }

        /// <summary>
        /// Sets the tooltip text.
        /// </summary>
        /// <param name="tooltipText">The tooltip text.</param>
        void IWebBrowserInternal.SetTooltipText(string tooltipText)
        {
            UiThreadRunAsync(() => SetCurrentValue(TooltipTextProperty, tooltipText));
        }

        /// <summary>
        /// Called when [after browser created].
        /// </summary>
        /// <param name="browser">The browser.</param>
        partial void OnAfterBrowserCreated(IBrowser browser)
        {
            UiThreadRunAsync(() =>
            {
                if (!IsDisposed)
                {
                    SetCurrentValue(IsBrowserInitializedProperty, true);

                    // Only call Load if initialAddress is null and Address is not empty
                    if (string.IsNullOrEmpty(initialAddress) && !string.IsNullOrEmpty(Address) && !initialLoadCalled)
                    {
                        Load(Address);
                    }
                }
            });

            if (initialFocus)
            {
                browser.GetHost()?.SetFocus(true);
            }
        }

        #region CanGoBack dependency property

        /// <summary>
        /// A flag that indicates whether the state of the control current supports the GoBack action (true) or not (false).
        /// </summary>
        /// <value><c>true</c> if this instance can go back; otherwise, <c>false</c>.</value>
        /// <remarks>In the WPF control, this property is implemented as a Dependency Property and fully supports data
        /// binding.</remarks>
        public bool CanGoBack
        {
            get { return (bool)GetValue(CanGoBackProperty); }
        }

        /// <summary>
        /// The can go back property
        /// </summary>
        public static DependencyProperty CanGoBackProperty = DependencyProperty.Register(nameof(CanGoBack), typeof(bool), typeof(ChromiumWebBrowser));

        #endregion

        #region CanGoForward dependency property

        /// <summary>
        /// A flag that indicates whether the state of the control currently supports the GoForward action (true) or not (false).
        /// </summary>
        /// <value><c>true</c> if this instance can go forward; otherwise, <c>false</c>.</value>
        /// <remarks>In the WPF control, this property is implemented as a Dependency Property and fully supports data
        /// binding.</remarks>
        public bool CanGoForward
        {
            get { return (bool)GetValue(CanGoForwardProperty); }
        }

        /// <summary>
        /// The can go forward property
        /// </summary>
        public static DependencyProperty CanGoForwardProperty = DependencyProperty.Register(nameof(CanGoForward), typeof(bool), typeof(ChromiumWebBrowser));

        #endregion

        #region Address dependency property

        /// <summary>
        /// The address (URL) which the browser control is currently displaying.
        /// Will automatically be updated as the user navigates to another page (e.g. by clicking on a link).
        /// </summary>
        /// <value>The address.</value>
        /// <remarks>In the WPF control, this property is implemented as a Dependency Property and fully supports data
        /// binding.</remarks>
        public string Address
        {
            get { return (string)GetValue(AddressProperty); }
            set { SetValue(AddressProperty, value); }
        }

        /// <summary>
        /// The address property
        /// </summary>
        public static readonly DependencyProperty AddressProperty =
            DependencyProperty.Register(nameof(Address), typeof(string), typeof(ChromiumWebBrowser),
                                        new UIPropertyMetadata(null, OnAddressChanged));

        /// <summary>
        /// Event called when the browser address has changed
        /// </summary>
        public event DependencyPropertyChangedEventHandler AddressChanged;

        /// <summary>
        /// Handles the <see cref="E:AddressChanged" /> event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void OnAddressChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            var owner = (ChromiumWebBrowser)sender;
            var oldValue = (string)args.OldValue;
            var newValue = (string)args.NewValue;

            owner.OnAddressChanged(oldValue, newValue);

            owner.AddressChanged?.Invoke(owner, args);
        }

        /// <summary>
        /// Called when [address changed].
        /// </summary>
        /// <param name="oldValue">The old value.</param>
        /// <param name="newValue">The new value.</param>
        protected virtual void OnAddressChanged(string oldValue, string newValue)
        {
            if (ignoreUriChange || newValue == null || !InternalIsBrowserInitialized())
            {
                return;
            }

            Load(newValue);
        }

        #endregion Address dependency property

        #region IsLoading dependency property

        /// <summary>
        /// A flag that indicates whether the control is currently loading one or more web pages (true) or not (false).
        /// </summary>
        /// <value><c>true</c> if this instance is loading; otherwise, <c>false</c>.</value>
        /// <remarks>In the WPF control, this property is implemented as a Dependency Property and fully supports data
        /// binding.</remarks>
        public bool IsLoading
        {
            get { return (bool)GetValue(IsLoadingProperty); }
        }

        /// <summary>
        /// The is loading property
        /// </summary>
        public static readonly DependencyProperty IsLoadingProperty =
            DependencyProperty.Register(nameof(IsLoading), typeof(bool), typeof(ChromiumWebBrowser), new PropertyMetadata(false));

        #endregion IsLoading dependency property

        #region IsBrowserInitialized dependency property
        /// <summary>
        /// A flag that indicates whether the WebBrowser is initialized (true) or not (false).
        /// </summary>
        /// <value><c>true</c> if this instance is browser initialized; otherwise, <c>false</c>.</value>
        /// <remarks>In the WPF control, this property is implemented as a Dependency Property and fully supports data
        /// binding.</remarks>
        public bool IsBrowserInitialized
        {
            get { return (bool)GetValue(IsBrowserInitializedProperty); }
        }

        /// <summary>
        /// The is browser initialized property
        /// </summary>
        public static readonly DependencyProperty IsBrowserInitializedProperty =
            DependencyProperty.Register(nameof(IsBrowserInitialized), typeof(bool), typeof(ChromiumWebBrowser), new PropertyMetadata(false, OnIsBrowserInitializedChanged));

        /// <summary>
        /// Event called after the underlying CEF browser instance has been created. 
        /// </summary>
        public event DependencyPropertyChangedEventHandler IsBrowserInitializedChanged;

        /// <summary>
        /// Handles the <see cref="E:IsBrowserInitializedChanged" /> event.
        /// </summary>
        /// <param name="d">The d.</param>
        /// <param name="e">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void OnIsBrowserInitializedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var owner = (ChromiumWebBrowser)d;
            var oldValue = (bool)e.OldValue;
            var newValue = (bool)e.NewValue;

            owner.OnIsBrowserInitializedChanged(oldValue, newValue);

            owner.IsBrowserInitializedChanged?.Invoke(owner, e);
        }

        /// <summary>
        /// Called when [is browser initialized changed].
        /// </summary>
        /// <param name="oldValue">if set to <c>true</c> [old value].</param>
        /// <param name="newValue">if set to <c>true</c> [new value].</param>
        protected virtual void OnIsBrowserInitializedChanged(bool oldValue, bool newValue)
        {
            if (newValue && !IsDisposed)
            {
                var task = this.GetZoomLevelAsync();
                task.ContinueWith(previous =>
                {
                    if (previous.Status == TaskStatus.RanToCompletion)
                    {
                        UiThreadRunAsync(() =>
                        {
                            if (!IsDisposed)
                            {
                                SetCurrentValue(ZoomLevelProperty, previous.Result);
                            }
                        });
                    }
                    else
                    {
                        throw new InvalidOperationException("Unexpected failure of calling CEF->GetZoomLevelAsync", previous.Exception);
                    }
                }, TaskContinuationOptions.ExecuteSynchronously);
            }
        }

        #endregion IsInitialized dependency property

        #region Title dependency property

        /// <summary>
        /// The title of the web page being currently displayed.
        /// </summary>
        /// <value>The title.</value>
        /// <remarks>This property is implemented as a Dependency Property and fully supports data binding.</remarks>
        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        /// <summary>
        /// The title property
        /// </summary>
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(ChromiumWebBrowser), new PropertyMetadata(null, OnTitleChanged));

        /// <summary>
        /// Event handler that will get called when the browser title changes
        /// </summary>
        public event DependencyPropertyChangedEventHandler TitleChanged;

        /// <summary>
        /// Handles the <see cref="E:TitleChanged" /> event.
        /// </summary>
        /// <param name="d">The d.</param>
        /// <param name="e">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var owner = (ChromiumWebBrowser)d;

            owner.TitleChanged?.Invoke(owner, e);
        }

        #endregion Title dependency property

        #region ZoomLevel dependency property

        /// <summary>
        /// The zoom level at which the browser control is currently displaying.
        /// Can be set to 0 to clear the zoom level (resets to default zoom level).
        /// NOTE: For browsers that share the same render process (same origin) this
        /// property is only updated when the browser changes its visible state.
        /// If you have two browsers visible at the same time that share the same render
        /// process then zooming one will not update this property in the other (unless
        /// the control is hidden and then shown). You can isolate browser instances
        /// using a <see cref="RequestContext"/>, they will then have their own render process
        /// regardless of the process policy. You can manually get the Zoom level using
        /// <see cref="IBrowserHost.GetZoomLevelAsync"/>
        /// </summary>
        /// <value>The zoom level.</value>
        public double ZoomLevel
        {
            get { return (double)GetValue(ZoomLevelProperty); }
            set { SetValue(ZoomLevelProperty, value); }
        }

        /// <summary>
        /// The zoom level property
        /// </summary>
        public static readonly DependencyProperty ZoomLevelProperty =
            DependencyProperty.Register(nameof(ZoomLevel), typeof(double), typeof(ChromiumWebBrowser),
                                        new UIPropertyMetadata(0d, OnZoomLevelChanged));

        /// <summary>
        /// Handles the <see cref="E:ZoomLevelChanged" /> event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void OnZoomLevelChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            var owner = (ChromiumWebBrowser)sender;
            var oldValue = (double)args.OldValue;
            var newValue = (double)args.NewValue;

            owner.OnZoomLevelChanged(oldValue, newValue);
        }

        /// <summary>
        /// Called when [zoom level changed].
        /// </summary>
        /// <param name="oldValue">The old value.</param>
        /// <param name="newValue">The new value.</param>
        protected virtual void OnZoomLevelChanged(double oldValue, double newValue)
        {
            this.SetZoomLevel(newValue);
        }

        #endregion ZoomLevel dependency property

        #region ZoomLevelIncrement dependency property

        /// <summary>
        /// Specifies the amount used to increase/decrease to ZoomLevel by
        /// By Default this value is 0.10
        /// </summary>
        /// <value>The zoom level increment.</value>
        public double ZoomLevelIncrement
        {
            get { return (double)GetValue(ZoomLevelIncrementProperty); }
            set { SetValue(ZoomLevelIncrementProperty, value); }
        }

        /// <summary>
        /// The zoom level increment property
        /// </summary>
        public static readonly DependencyProperty ZoomLevelIncrementProperty =
            DependencyProperty.Register(nameof(ZoomLevelIncrement), typeof(double), typeof(ChromiumWebBrowser), new PropertyMetadata(0.10));

        #endregion ZoomLevelIncrement dependency property

        #region CleanupElement dependency property

        /// <summary>
        /// The CleanupElement Controls when the BrowserResources will be cleaned up.
        /// The ChromiumWebBrowser will register on Unloaded of the provided Element and dispose all resources when that handler is called.
        /// By default the cleanup element is the Window that contains the ChromiumWebBrowser.
        /// if you want cleanup to happen earlier provide another FrameworkElement.
        /// Be aware that this Control is not usable anymore after cleanup is done.
        /// </summary>
        /// <value>The cleanup element.</value>
        public FrameworkElement CleanupElement
        {
            get { return (FrameworkElement)GetValue(CleanupElementProperty); }
            set { SetValue(CleanupElementProperty, value); }
        }

        /// <summary>
        /// The cleanup element property
        /// </summary>
        public static readonly DependencyProperty CleanupElementProperty =
            DependencyProperty.Register(nameof(CleanupElement), typeof(FrameworkElement), typeof(ChromiumWebBrowser), new PropertyMetadata(null, OnCleanupElementChanged));

        /// <summary>
        /// Handles the <see cref="E:CleanupElementChanged" /> event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void OnCleanupElementChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            var owner = (ChromiumWebBrowser)sender;
            var oldValue = (FrameworkElement)args.OldValue;
            var newValue = (FrameworkElement)args.NewValue;

            owner.OnCleanupElementChanged(oldValue, newValue);
        }

        /// <summary>
        /// Called when [cleanup element changed].
        /// </summary>
        /// <param name="oldValue">The old value.</param>
        /// <param name="newValue">The new value.</param>
        protected virtual void OnCleanupElementChanged(FrameworkElement oldValue, FrameworkElement newValue)
        {
            if (oldValue != null)
            {
                oldValue.Unloaded -= OnCleanupElementUnloaded;
            }

            if (newValue != null)
            {
                newValue.Unloaded -= OnCleanupElementUnloaded;
                newValue.Unloaded += OnCleanupElementUnloaded;
            }
        }

        /// <summary>
        /// Handles the <see cref="E:CleanupElementUnloaded" /> event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void OnCleanupElementUnloaded(object sender, RoutedEventArgs e)
        {
            Dispose();
        }

        #endregion CleanupElement dependency property

        #region TooltipText dependency property

        /// <summary>
        /// The text that will be displayed as a ToolTip
        /// </summary>
        /// <value>The tooltip text.</value>
        public string TooltipText
        {
            get { return (string)GetValue(TooltipTextProperty); }
        }

        /// <summary>
        /// The tooltip text property
        /// </summary>
        public static readonly DependencyProperty TooltipTextProperty =
            DependencyProperty.Register(nameof(TooltipText), typeof(string), typeof(ChromiumWebBrowser), new PropertyMetadata(null, OnTooltipTextChanged));

        /// <summary>
        /// Handles the <see cref="TooltipTextProperty" /> change.
        /// </summary>
        /// <param name="d">dependency object.</param>
        /// <param name="e">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing property change data.</param>
        private static void OnTooltipTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var owner = (ChromiumWebBrowser)d;
            var oldValue = (string)e.OldValue;
            var newValue = (string)e.NewValue;

            owner.OnTooltipTextChanged(oldValue, newValue);
        }

        /// <summary>
        /// Called when tooltip text was changed changed.
        /// </summary>
        /// <param name="oldValue">old value</param>
        /// <param name="newValue">new value</param>
        protected virtual void OnTooltipTextChanged(string oldValue, string newValue)
        {
            var timer = tooltipTimer;
            if (timer == null)
            {
                return;
            }

            // There are cases where oldValue is null and newValue is string.Empty
            // and vice versa, simply ignore when string.IsNullOrEmpty for both.
            if (string.IsNullOrEmpty(oldValue) && string.IsNullOrEmpty(newValue))
            {
                return;
            }

            if (string.IsNullOrEmpty(newValue))
            {
                if (timer.IsEnabled)
                {
                    timer.Stop();
                }

                OpenOrCloseToolTip(null);
            }
            else if (!timer.IsEnabled)
            {
                timer.Start();
            }
        }

        #endregion

        #region WebBrowser dependency property

        /// <summary>
        /// Gets or sets the WebBrowser.
        /// </summary>
        /// <value>The WebBrowser.</value>
        public IWebBrowser WebBrowser
        {
            get { return (IWebBrowser)GetValue(WebBrowserProperty); }
            set { SetValue(WebBrowserProperty, value); }
        }

        /// <summary>
        /// The WebBrowser property
        /// </summary>
        public static readonly DependencyProperty WebBrowserProperty =
            DependencyProperty.Register(nameof(WebBrowser), typeof(IWebBrowser), typeof(ChromiumWebBrowser), new UIPropertyMetadata(defaultValue: null));

        #endregion WebBrowser dependency property

        /// <summary>
        /// Handles the <see cref="E:Drop" /> event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="DragEventArgs"/> instance containing the event data.</param>
        private void OnDrop(object sender, DragEventArgs e)
        {
            if (browser != null)
            {
                var mouseEvent = GetMouseEvent(e);
                var effect = e.AllowedEffects.GetDragOperationsMask();
                browser.GetHost().DragTargetDragOver(mouseEvent, effect);
                browser.GetHost().DragTargetDragDrop(mouseEvent);
            }
        }

        /// <summary>
        /// Handles the <see cref="E:DragLeave" /> event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="DragEventArgs"/> instance containing the event data.</param>
        private void OnDragLeave(object sender, DragEventArgs e)
        {
            if (browser != null)
            {
                browser.GetHost().DragTargetDragLeave();
            }
        }

        /// <summary>
        /// Handles the <see cref="E:DragOver" /> event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="DragEventArgs"/> instance containing the event data.</param>
        private void OnDragOver(object sender, DragEventArgs e)
        {
            if (browser != null)
            {
                browser.GetHost().DragTargetDragOver(GetMouseEvent(e), e.AllowedEffects.GetDragOperationsMask());
            }
            e.Effects = currentDragDropEffects ?? DragDropEffects.None;
            e.Handled = true;
        }

        /// <summary>
        /// Handles the <see cref="E:DragEnter" /> event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="DragEventArgs"/> instance containing the event data.</param>
        private void OnDragEnter(object sender, DragEventArgs e)
        {
            if (browser != null)
            {
                var mouseEvent = GetMouseEvent(e);
                var effect = e.AllowedEffects.GetDragOperationsMask();

                //DoDragDrop will fire this handler for internally sourced Drag/Drop operations
                //we use the existing IDragData (cloned copy)

                var dragData = currentDragData ?? e.GetDragData();

                browser.GetHost().DragTargetDragEnter(dragData, mouseEvent, effect);
                browser.GetHost().DragTargetDragOver(mouseEvent, effect);
            }
        }

        private void OnInputLanguageChanged(object sender, InputLanguageEventArgs e)
        {
            // If we are already using the WpfImeKeyboardHandler then we'll ignore any changes
            if (WpfKeyboardHandler?.GetType() == typeof(WpfImeKeyboardHandler))
            {
                return;
            }

            var useImeKeyboardHandler = WpfImeKeyboardHandler.UseImeKeyboardHandler(e.NewLanguage.KeyboardLayoutId);

            if (useImeKeyboardHandler)
            {
                var oldKeyboardHandler = WpfKeyboardHandler;
                WpfKeyboardHandler = new WpfImeKeyboardHandler(this);
                oldKeyboardHandler?.Dispose();                

                InputLanguageManager.Current.InputLanguageChanged -= OnInputLanguageChanged;
            }
        }

        /// <summary>
        /// PresentationSource changed handler.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="SourceChangedEventArgs"/> instance containing the event data.</param>
        private void PresentationSourceChangedHandler(object sender, SourceChangedEventArgs args)
        {
            if (args.NewSource != null)
            {
                source = (HwndSource)args.NewSource;

                WpfKeyboardHandler.Setup(source);

                var matrix = source.CompositionTarget.TransformToDevice;

                NotifyDpiChange((float)matrix.M11);

                var window = source.RootVisual as Window;
                if (window != null)
                {
                    window.StateChanged += OnWindowStateChanged;
                    window.LocationChanged += OnWindowLocationChanged;
                    sourceWindow = window;

                    if (CleanupElement == null)
                    {
                        CleanupElement = window;
                    }
                    else if (CleanupElement is Window parent)
                    {
                        // If the CleanupElement is a window then move it to the new Window
                        if (parent != window)
                        {
                            CleanupElement = window;
                        }
                    }
                }

                browserScreenLocation = GetBrowserScreenLocation();

                monitorInfo.Init();
                MonitorInfo.GetMonitorInfoForWindowHandle(source.Handle, ref monitorInfo);
            }
            else if (args.OldSource != null)
            {
                WpfKeyboardHandler.Dispose();

                var window = args.OldSource.RootVisual as Window;
                if (window != null)
                {
                    window.StateChanged -= OnWindowStateChanged;
                    window.LocationChanged -= OnWindowLocationChanged;
                    sourceWindow = null;
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
        {
            NotifyDpiChange((float)newDpi.DpiScaleX);

            base.OnDpiChanged(oldDpi, newDpi);
        }

        private void OnWindowStateChanged(object sender, EventArgs e)
        {
            var window = (Window)sender;

            switch (window.WindowState)
            {
                case WindowState.Normal:
                case WindowState.Maximized:
                {
                    if (previousWindowState == WindowState.Minimized && IsVisible)
                    {
                        OnBrowserWasHidden(false);
                    }
                    break;
                }
                case WindowState.Minimized:
                {
                    OnBrowserWasHidden(true);
                    break;
                }
            }

            previousWindowState = window.WindowState;
        }

        /// <summary>
        /// Called when the underlying CefBrowser instance is hidden/shown
        /// Calls <see cref="IBrowserHost.WasHidden(bool)"/>.
        /// This method can be overriden to keep the browser in a visible state
        /// even when it's not displayed on screen.
        /// </summary>
        /// <param name="hidden">if true the browser will be notified that it was hidden.</param>
        protected virtual void OnBrowserWasHidden(bool hidden)
        {
            if (browser != null)
            {
                browser.GetHost().WasHidden(hidden);

                if (ResizeHackEnabled)
                {
                    if (hidden)
                    {
                        resizeHackIgnoreOnPaint = true;
                    }
                    else
                    {
                        _ = CefUiThreadRunAsync(async () =>
                        {
                            await ResizeHackRun();
                        });
                    }
                }
            }
        }

        /// <summary>
        /// Called when the Window Location Changes, the PresentationSource changes
        /// and the page loads. We manually track the position as CEF makes calls
        /// on a non-UI thread and calling Invoke in IRenderWebBrowser.GetScreenPoint
        /// makes it very easy to deadlock the browser.
        /// </summary>
        /// <returns>Returns screen coordinates of the browsers location</returns>
        protected virtual Point GetBrowserScreenLocation()
        {
            if (source != null && PresentationSource.FromVisual(this) != null)
            {
                return PointToScreen(new Point());
            }
            else
            {
                return new Point();
            }
        }

        private void OnWindowLocationChanged(object sender, EventArgs e)
        {
            //We maintain a manual reference to the controls screen location
            //(relative to top/left of the screen)
            browserScreenLocation = GetBrowserScreenLocation();
        }

        /// <summary>
        /// Create the underlying CefBrowser instance with the specified <paramref name="initialSize"/>.
        /// This method should only be used in instances where you need the browser
        /// to load before it's attached to the Visual Tree. 
        /// </summary>
        /// <param name="parentWindowHwndSource">HwndSource for the Window that will host the browser.</param>
        /// <param name="initialSize">initial size</param>
        /// <returns>Returns false if browser already created, otherwise true.</returns>
        public bool CreateBrowser(HwndSource parentWindowHwndSource, Size initialSize)
        {
            if (initialSize.IsEmpty || initialSize.Equals(new Size(0, 0)))
            {
                throw new Exception("Invalid size, must be greater than 0,0");
            }

            source = parentWindowHwndSource;

            if (!CreateOffscreenBrowser(initialSize))
            {
                return false;
            }

            RoutedEventHandler handler = null;
            handler = (sender, args) =>
            {
                Loaded -= handler;

                //If the browser has finished rendering before we attach to the Visual Tree
                //then ask for a new frame to be generated
                var host = this.GetBrowserHost();
                if (host != null)
                {
                    host.Invalidate(PaintElementType.View);
                }
            };

            Loaded += handler;

            return true;
        }

        /// <summary>
        /// Create the underlying Browser instance, can be overriden to defer control creation
        /// The browser will only be created when size &gt; Size(0,0). If you specify a positive
        /// size then the browser will be created, if the ActualWidth and ActualHeight
        /// properties are in reality still 0 then you'll likely end up with a browser that
        /// won't render.
        /// </summary>
        /// <param name="size">size of the current control, must be greater than Size(0, 0)</param>
        /// <returns>bool to indicate if browser was created. If the browser has already been created then this will return false.</returns>
        protected virtual bool CreateOffscreenBrowser(Size size)
        {
            if (browserCreated || size.IsEmpty || size.Equals(new Size(0, 0)))
            {
                return false;
            }

            var webBrowserInternal = this as IWebBrowserInternal;
            if (!webBrowserInternal.HasParent)
            {
                var windowInfo = CreateOffscreenBrowserWindowInfo(source == null ? IntPtr.Zero : source.Handle);
                //If initialAddress is set then we use that value, later in OnAfterBrowserCreated then we will
                //call Load(url) if initial address was empty.
                //Issue https://github.com/cefsharp/CefSharp/issues/2300
                managedCefBrowserAdapter.CreateBrowser(windowInfo, browserSettings, requestContext, address: initialAddress);

                //Dispose of BrowserSettings if we created it, if user created then they're responsible
                if (browserSettings.AutoDispose)
                {
                    browserSettings.Dispose();
                }

                browserSettings = null;
            }
            browserCreated = true;

            return true;
        }

        /// <summary>
        /// Override this method to handle creation of WindowInfo. This method can be used to customise aspects of
        /// browser creation including configuration of settings such as <see cref="IWindowInfo.SharedTextureEnabled"/>
        /// (used for D3D11 shared texture rendering).
        /// </summary>
        /// <param name="handle">Window handle for the HwndSource</param>
        /// <returns>Window Info</returns>
        protected virtual IWindowInfo CreateOffscreenBrowserWindowInfo(IntPtr handle)
        {
            var windowInfo = Core.ObjectFactory.CreateWindowInfo();
            windowInfo.SetAsWindowless(handle);
            return windowInfo;
        }

        /// <summary>
        /// Runs the specific Action on the Dispatcher in an async fashion
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="priority">The priority.</param>
        internal void UiThreadRunAsync(Action action, DispatcherPriority priority = DispatcherPriority.DataBind)
        {
            if (Dispatcher.CheckAccess())
            {
                action();
            }
            else if (!Dispatcher.HasShutdownStarted)
            {
                Dispatcher.BeginInvoke(action, priority);
            }
        }

        /// <summary>
        /// Runs the specific Action on the Dispatcher in an sync fashion
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="priority">The priority.</param>
        private void UiThreadRunSync(Action action, DispatcherPriority priority = DispatcherPriority.DataBind)
        {
            if (Dispatcher.CheckAccess())
            {
                action();
            }
            else if (!Dispatcher.HasShutdownStarted)
            {
                Dispatcher.Invoke(action, priority);
            }
        }

        /// <summary>
        /// Handles the <see cref="E:ActualSizeChanged" /> event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="SizeChangedEventArgs"/> instance containing the event data.</param>
        private void OnActualSizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Initialize RenderClientAdapter when WPF has calculated the actual size of current content.
            CreateOffscreenBrowser(e.NewSize);

            //NOTE: Previous we used Math.Ceiling to round the sizing up, we
            //now set UseLayoutRounding = true; on the control so the sizes are
            //already rounded to a whole number for us.
            viewRect = new Rect(0, 0, (int)e.NewSize.Width, (int)e.NewSize.Height);

            if (browser != null)
            {
                browser.GetHost().WasResized();
            }
        }

        /// <summary>
        /// Handles the <see cref="E:IsVisibleChanged" /> event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            var isVisible = (bool)args.NewValue;

            if (browser != null)
            {
                OnBrowserWasHidden(!isVisible);

                if (isVisible)
                {
                    //Fix for #1778 - When browser becomes visible we update the zoom level
                    //browsers of the same origin will share the same zoomlevel and
                    //we need to track the update, so our ZoomLevelProperty works
                    //properly
                    var host = browser.GetHost();
                    host.GetZoomLevelAsync().ContinueWith(t =>
                    {
                        if (!IsDisposed)
                        {
                            SetCurrentValue(ZoomLevelProperty, t.Result);
                        }
                    },
                    CancellationToken.None,
                    TaskContinuationOptions.OnlyOnRanToCompletion,
                    TaskScheduler.FromCurrentSynchronizationContext());
                }
            }
        }

        /// <summary>
        /// Handles the <see cref="E:Loaded" /> event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="routedEventArgs">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            // TODO: Consider making the delay here configurable.
            tooltipTimer = new DispatcherTimer(
                TimeSpan.FromSeconds(0.5),
                DispatcherPriority.Render,
                OnTooltipTimerTick,
                Dispatcher
                );
            tooltipTimer.IsEnabled = false;

            //Initial value for screen location
            browserScreenLocation = GetBrowserScreenLocation();
        }

        /// <summary>
        /// When overridden in a derived class, is invoked whenever application code or internal processes call
        /// <see cref="M:System.Windows.FrameworkElement.ApplyTemplate" />.
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (image == null)
            {
                // Create main window
                image = (Image)GetTemplateChild(PartImageName);
            }

            if (popupImage == null)
            {
                popupImage = (Image)GetTemplateChild(PartPopupImageName);
            }
        }

        /// <summary>
        /// Converts a .NET Drag event to a CefSharp MouseEvent
        /// </summary>
        /// <param name="e">The <see cref="DragEventArgs"/> instance containing the event data.</param>
        /// <returns>MouseEvent.</returns>
        private MouseEvent GetMouseEvent(DragEventArgs e)
        {
            var point = e.GetPosition(this);

            return new MouseEvent((int)point.X, (int)point.Y, e.GetModifiers());
        }

        /// <summary>
        /// Sets the popup size and position implementation.
        /// </summary>
        /// <param name="rect">The popup rectangle (size and position).</param>
        private void SetPopupSizeAndPositionImpl(Rect rect)
        {
            popupImage.Width = rect.Width;
            popupImage.Height = rect.Height;

            var point = MousePositionTransform.UpdatePopupSizeAndPosition(rect, viewRect);

            Canvas.SetLeft(popupImage, point.X);
            Canvas.SetTop(popupImage, point.Y);
        }

        /// <summary>
        /// Handles the <see cref="E:TooltipTimerTick" /> event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void OnTooltipTimerTick(object sender, EventArgs e)
        {
            // Checks to see if the control is disposed/disposing
            if (!IsDisposed)
            {
                tooltipTimer.Stop();

                OpenOrCloseToolTip(TooltipText);
            }
        }

        /// <summary>
        /// Handles the <see cref="E:TooltipClosed" /> event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void OnTooltipClosed(object sender, RoutedEventArgs e)
        {
            toolTip.Visibility = Visibility.Collapsed;

            // Set Placement to something other than PlacementMode.Mouse, so that when we re-show the tooltip in
            // UpdateTooltip(), the tooltip will be repositioned to the new mouse point.
            toolTip.Placement = PlacementMode.Absolute;
        }

        /// <summary>
        /// Open or Close the tooltip at the current mouse position.
        /// </summary>
        /// <param name="text">ToolTip text, if null or empty tooltip will be closed.</param>
        protected virtual void OpenOrCloseToolTip(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                if (toolTip.IsOpen)
                {
                    toolTip.IsOpen = false;
                }
            }
            else
            {
                // hide old tooltip before showing the new one to update the position
                if (toolTip.IsOpen)
                {
                    toolTip.IsOpen = false;
                }

                //If no ToolTip style is defined then we'll
                //use a TextBlock to ensure the Text is wrapped
                //If a style is applied leave to the user to apply relevant wrapping
                //Issue https://github.com/cefsharp/CefSharp/issues/2488
                if (toolTip.Style == null)
                {
                    toolTip.Content = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap };
                }
                else
                {
                    toolTip.SetCurrentValue(ContentControl.ContentProperty, text);
                }
                toolTip.Placement = PlacementMode.Mouse;
                toolTip.Visibility = Visibility.Visible;
                toolTip.IsOpen = true;
            }
        }

        /// <summary>
        /// Handles the <see cref="E:GotKeyboardFocus" /> event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="KeyboardFocusChangedEventArgs"/> instance containing the event data.</param>
        private void OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (browser == null)
            {
                initialFocus = true;
            }
            else
            {
                browser.GetHost().SetFocus(true);
            }
        }

        /// <summary>
        /// Handles the <see cref="E:LostKeyboardFocus" /> event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="KeyboardFocusChangedEventArgs"/> instance containing the event data.</param>
        private void OnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (browser == null)
            {
                initialFocus = false;
            }
            else
            {
                browser.GetHost().SetFocus(false);
            }
        }

        /// <summary>
        /// Invoked when an unhandled <see cref="E:System.Windows.Input.Keyboard.PreviewKeyDown" /> attached event reaches an
        /// element in its route that is derived from this class. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.KeyEventArgs" /> that contains the event data.</param>
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (!e.Handled && browser != null)
            {
                WpfKeyboardHandler.HandleKeyPress(e);
            }

            base.OnPreviewKeyDown(e);
        }

        /// <summary>
        /// Invoked when an unhandled <see cref="E:System.Windows.Input.Keyboard.PreviewKeyUp" /> attached event reaches an
        /// element in its route that is derived from this class. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.KeyEventArgs" /> that contains the event data.</param>
        protected override void OnPreviewKeyUp(KeyEventArgs e)
        {
            if (!e.Handled && browser != null)
            {
                WpfKeyboardHandler.HandleKeyPress(e);
            }

            base.OnPreviewKeyUp(e);
        }

        /// <summary>
        /// Handles the <see cref="E:PreviewTextInput" /> event.
        /// </summary>
        /// <param name="e">The <see cref="TextCompositionEventArgs"/> instance containing the event data.</param>
        protected override void OnPreviewTextInput(TextCompositionEventArgs e)
        {
            if (!e.Handled && browser != null)
            {
                WpfKeyboardHandler.HandleTextInput(e);
            }

            base.OnPreviewTextInput(e);
        }

        /// <summary>
        /// Invoked when an unhandled <see cref="E:System.Windows.Input.Mouse.MouseMove" /> attached event reaches an element in its route that is derived from this class. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseEventArgs" /> that contains the event data.</param>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            //Mouse, touch, and stylus will raise mouse event.
            //For mouse events from an actual mouse, e.StylusDevice will be null.
            //For mouse events from touch and stylus, e.StylusDevice will not be null.
            //We only handle event from mouse here.
            //If not, touch will cause duplicate events (mousemove and touchmove) and so does stylus.
            //Use e.StylusDevice == null to ensure only mouse.
            if (!e.Handled && browser != null && e.StylusDevice == null)
            {
                var point = e.GetPosition(this);
                var modifiers = e.GetModifiers();

                MousePositionTransform.TransformMousePoint(ref point);
                browser.GetHost().SendMouseMoveEvent((int)point.X, (int)point.Y, false, modifiers);
            }

            base.OnMouseMove(e);
        }

        /// <summary>
        /// Invoked when an unhandled <see cref="E:System.Windows.Input.Mouse.MouseWheel" /> attached event reaches an element in its route that is derived from this class. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseWheelEventArgs" /> that contains the event data.</param>
        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            if (!e.Handled && browser != null)
            {
                var point = e.GetPosition(this);
                var modifiers = e.GetModifiers();
                var isShiftKeyDown = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
                MousePositionTransform.TransformMousePoint(ref point);

                browser.SendMouseWheelEvent(
                    (int)point.X,
                    (int)point.Y,
                    deltaX: isShiftKeyDown ? e.Delta : 0,
                    deltaY: !isShiftKeyDown ? e.Delta : 0,
                    modifiers: modifiers);

                e.Handled = true;
            }

            base.OnMouseWheel(e);
        }

        /// <summary>
        /// Invoked when an unhandled <see cref="E:System.Windows.Input.Mouse.MouseDown" /> attached event reaches an
        /// element in its route that is derived from this class. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseButtonEventArgs" /> that contains the event data.
        /// This event data reports details about the mouse button that was pressed and the handled state.</param>
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            //Mouse, touch, and stylus will raise mouse event.
            //For mouse events from an actual mouse, e.StylusDevice will be null.
            //For mouse events from touch and stylus, e.StylusDevice will not be null.
            //We only handle event from mouse here.
            //If not, touch will cause duplicate events (mouseup and touchup) and so does stylus.
            //Use e.StylusDevice == null to ensure only mouse.
            if (e.StylusDevice == null)
            {
                Focus();
                OnMouseButton(e);

                //We should only need to capture the left button exiting the browser
                if (e.ChangedButton == MouseButton.Left && e.LeftButton == MouseButtonState.Pressed)
                {
                    //Capture/Release Mouse to allow for scrolling outside bounds of browser control (#2258).
                    //Known issue when capturing and the device has a touch screen, to workaround this issue
                    //disable WPF StylusAndTouchSupport see for details https://github.com/dotnet/wpf/issues/1323#issuecomment-513870984
                    CaptureMouse();
                }
            }

            base.OnMouseDown(e);
        }

        /// <summary>
        /// Invoked when an unhandled <see cref="E:System.Windows.Input.Mouse.MouseUp" /> routed event reaches an element in its route that is derived from this class. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseButtonEventArgs" /> that contains the event data. The event data reports that the mouse button was released.</param>
        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            //Mouse, touch, and stylus will raise mouse event.
            //For mouse events from an actual mouse, e.StylusDevice will be null.
            //For mouse events from touch and stylus, e.StylusDevice will not be null.
            //We only handle event from mouse here.
            //If not, touch will cause duplicate events (mouseup and touchup) and so does stylus.
            //Use e.StylusDevice == null to ensure only mouse.
            if (e.StylusDevice == null)
            {
                OnMouseButton(e);

                if (e.ChangedButton == MouseButton.Left && e.LeftButton == MouseButtonState.Released)
                {
                    //Release the mouse capture that we grabbed on mouse down.
                    //We won't get here if e.g. the right mouse button is pressed and released
                    //while the left is still held, but in that case the left mouse capture seems
                    //to be released implicitly (even without the left mouse SendMouseClickEvent in leave below)
                    //Use ReleaseMouseCapture over Mouse.Capture(null); as it has additional Mouse.Captured == this check
                    ReleaseMouseCapture();
                }
            }

            base.OnMouseUp(e);
        }

        /// <summary>
        /// Invoked when an unhandled <see cref="E:System.Windows.Input.Mouse.MouseLeave" /> attached event is raised on this element. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseEventArgs" /> that contains the event data.</param>
        protected override void OnMouseLeave(MouseEventArgs e)
        {
            //Mouse, touch, and stylus will raise mouse event.
            //For mouse events from an actual mouse, e.StylusDevice will be null.
            //For mouse events from touch and stylus, e.StylusDevice will not be null.
            //We only handle event from mouse here.
            //OnMouseLeave event from touch or stylus needn't to be handled.
            //Use e.StylusDevice == null to ensure only mouse.
            if (!e.Handled && browser != null && e.StylusDevice == null)
            {
                var modifiers = e.GetModifiers();
                var point = e.GetPosition(this);

                browser.GetHost().SendMouseMoveEvent((int)point.X, (int)point.Y, true, modifiers);

                ((IWebBrowserInternal)this).SetTooltipText(null);
            }

            base.OnMouseLeave(e);
        }

        /// <summary>
        /// Invoked when an unhandled <see cref="E:System.Windows.Input.Mouse.LostMouseCapture" /> attached event reaches an element in
        /// its route that is derived from this class. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseEventArgs" /> that contains event data.</param>
        protected override void OnLostMouseCapture(MouseEventArgs e)
        {
            if (!e.Handled && browser != null)
            {
                browser.GetHost().SendCaptureLostEvent();
            }

            base.OnLostMouseCapture(e);
        }

        /// <summary>
        /// Handles the <see cref="E:MouseButton" /> event.
        /// </summary>
        /// <param name="e">The <see cref="MouseButtonEventArgs"/> instance containing the event data.</param>
        private void OnMouseButton(MouseButtonEventArgs e)
        {
            if (!e.Handled && browser != null)
            {
                var modifiers = e.GetModifiers();
                var mouseUp = (e.ButtonState == MouseButtonState.Released);
                var point = e.GetPosition(this);

                if (e.ChangedButton == MouseButton.XButton1)
                {
                    if (CanGoBack && mouseUp)
                    {
                        this.Back();
                    }
                }
                else if (e.ChangedButton == MouseButton.XButton2)
                {
                    if (CanGoForward && mouseUp)
                    {
                        this.Forward();
                    }
                }
                else
                {
                    //Chromium only supports values of 1, 2 or 3.
                    //https://github.com/cefsharp/CefSharp/issues/3940
                    //Anything greater than 3 then we send click count of 1
                    var clickCount = e.ClickCount;

                    if (clickCount > 3)
                    {
                        clickCount = 1;
                    }

                    MousePositionTransform.TransformMousePoint(ref point);
                    browser.GetHost().SendMouseClickEvent((int)point.X, (int)point.Y, (MouseButtonType)e.ChangedButton, mouseUp, clickCount, modifiers);
                }

                e.Handled = true;
            }
        }

        /// <summary>
        /// Provides class handling for the <see cref="E:System.Windows.TouchDown" /> routed event that occurs when a touch presses inside this element.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.TouchEventArgs" /> that contains the event data.</param>
        protected override void OnTouchDown(TouchEventArgs e)
        {
            Focus();
            // Capture touch so touch events are still pushed to CEF even if the touch leaves the control before a TouchUp.
            // This behavior is similar to how other browsers handle touch input.
            CaptureTouch(e.TouchDevice);
            OnTouch(e);
            base.OnTouchDown(e);
        }

        /// <summary>
        /// Provides class handling for the <see cref="E:System.Windows.TouchMove" /> routed event that occurs when a touch moves while inside this element.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.TouchEventArgs" /> that contains the event data.</param>
        protected override void OnTouchMove(TouchEventArgs e)
        {
            OnTouch(e);
            base.OnTouchMove(e);
        }

        /// <summary>
        /// Provides class handling for the <see cref="E:System.Windows.TouchUp" /> routed event that occurs when a touch is released inside this element.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.TouchEventArgs" /> that contains the event data.</param>
        protected override void OnTouchUp(TouchEventArgs e)
        {
            ReleaseTouchCapture(e.TouchDevice);
            OnTouch(e);
            base.OnTouchUp(e);
        }

        /// <summary>
        /// Handles a <see cref="E:Touch" /> event.
        /// </summary>
        /// <param name="e">The <see cref="TouchEventArgs"/> instance containing the event data.</param>
        private void OnTouch(TouchEventArgs e)
        {
            if (!e.Handled && browser != null)
            {
                var modifiers = WpfExtensions.GetModifierKeys();
                var touchPoint = e.GetTouchPoint(this);
                var touchEventType = TouchEventType.Cancelled;
                switch (touchPoint.Action)
                {
                    case TouchAction.Down:
                    {
                        touchEventType = TouchEventType.Pressed;
                        break;
                    }
                    case TouchAction.Move:
                    {
                        touchEventType = TouchEventType.Moved;
                        break;
                    }
                    case TouchAction.Up:
                    {
                        touchEventType = TouchEventType.Released;
                        break;
                    }
                    default:
                    {
                        touchEventType = TouchEventType.Cancelled;
                        break;
                    }
                }

                var touchEvent = new TouchEvent()
                {
                    Id = e.TouchDevice.Id,
                    X = (float)touchPoint.Position.X,
                    Y = (float)touchPoint.Position.Y,
                    PointerType = PointerType.Touch,
                    Type = touchEventType,
                    Modifiers = modifiers,
                };

                browser.GetHost().SendTouchEvent(touchEvent);

                e.Handled = true;
            }
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            var handler = AccessibilityHandler as Experimental.Accessibility.AccessibilityHandler;

            if (handler == null)
            {
                return base.OnCreateAutomationPeer();
            }

            return handler.AutomationPeer;

        }

        /// <inheritdoc/>
        public void Load(string url)
        {
            if (IsDisposed)
            {
                return;
            }

            //If the browser is already initialized then we can call LoadUrl directly
            if (InternalIsBrowserInitialized())
            {
                // Added null check -> binding-triggered changes of Address will lead to a nullref after Dispose has been called
                // or before OnApplyTemplate has been called
                if (browser != null)
                {
                    initialLoadCalled = true;

                    using (var frame = browser.MainFrame)
                    {
                        frame.LoadUrl(url);
                    }
                }
            }
            //If CreateBrowser was called and InternalIsBrowserInitialized() == false then we need to set the Address
            //property so in OnAfterBrowserCreated the Url is loaded. If initialAddress was
            //set then the Url set here will be ignored. If we called Load(url) then historically
            //an aborted error would be raised as per https://github.com/cefsharp/CefSharp/issues/2300
            //So we ignore the call for now.
            else if (browserCreated)
            {
                UiThreadRunAsync(() =>
                {
                    Address = url;
                });
            }
            //Before browser created, set the intialAddress
            else
            {
                initialAddress = url;
            }
        }

        /// <summary>
        /// Zooms the browser in.
        /// </summary>
        private void ZoomIn()
        {
            UiThreadRunAsync(() =>
            {
                ZoomLevel = ZoomLevel + ZoomLevelIncrement;
            });
        }

        /// <summary>
        /// Zooms the browser out.
        /// </summary>
        private void ZoomOut()
        {
            UiThreadRunAsync(() =>
            {
                ZoomLevel = ZoomLevel - ZoomLevelIncrement;
            });
        }

        /// <summary>
        /// Reset the browser's zoom level to default.
        /// </summary>
        private void ZoomReset()
        {
            UiThreadRunAsync(() =>
            {
                ZoomLevel = 0;
            });
        }

        /// <summary>
        /// Manually notify the browser the DPI of the parent window has changed.
        /// The tested/expected values for <paramref name="newDpi"/> are 1.0, 1.25, 1.5, 2.0 as these
        /// correspond to 96, 120, 144, 192 DPI (referred to as 100%, 125%, 150%, 200% in the Windows GUI).
        /// </summary>
        /// <param name="newDpi">new DPI</param>
        public virtual void NotifyDpiChange(float newDpi)
        {
            //Do nothing
            if (DpiScaleFactor.Equals(newDpi))
            {
                return;
            }

            var notifyDpiChanged = DpiScaleFactor > 0 && !DpiScaleFactor.Equals(newDpi);

            DpiScaleFactor = newDpi;

            if (notifyDpiChanged && browser != null)
            {
                browser.GetHost().NotifyScreenInfoChanged();
            }

            //If the user has specified a custom RenderHandler then we'll skip this part
            //as to not override their implementation.
            //TODO: Add support for RenderHandler changing the DPI rather
            //than creating a new instance (allow users to be notified of DPI change).
            if (RenderHandler == null || RenderHandler is WritableBitmapRenderHandler || RenderHandler is DirectWritableBitmapRenderHandler)
            {
                const int DefaultDpi = 96;
                var scale = DefaultDpi * DpiScaleFactor;
                var oldRenderHandler = RenderHandler;

                if (Cef.CurrentlyOnThread(CefThreadIds.TID_UI))
                {
                    RenderHandler = new DirectWritableBitmapRenderHandler(scale, scale, invalidateDirtyRect: true);
                }
                else
                {
                    RenderHandler = new WritableBitmapRenderHandler(scale, scale);
                }

                oldRenderHandler?.Dispose();
            }
        }

        /// <summary>
        /// Waits for the page rendering to be idle for <paramref name="idleTimeInMs"/>.
        /// Rendering is considered to be idle when no <see cref="Paint"/> events have occured
        /// for <paramref name="idleTimeInMs"/>.
        /// This is useful for scenarios like taking a screen shot
        /// </summary>
        /// <param name="idleTimeInMs">optional idleTime in miliseconds, default to 500ms</param>
        /// <param name="timeout">optional timeout, if not specified defaults to thirty(30) seconds.</param>
        /// <param name="cancellationToken">optional CancellationToken</param>
        /// <returns>Task that resolves when page rendering has been idle for <paramref name="idleTimeInMs"/></returns>
        public async Task WaitForRenderIdleAsync(int idleTimeInMs = 500, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        {
            var renderIdleTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            var idleTimer = new System.Timers.Timer
            {
                Interval = idleTimeInMs,
                AutoReset = false
            };

            EventHandler<PaintEventArgs> handler = null;

            idleTimer.Elapsed += (sender, args) =>
            {
                Paint -= handler;

                idleTimer.Stop();
                idleTimer.Dispose();

                renderIdleTcs.TrySetResult(true);
            };

            //Every time Paint is called we reset our timer
            handler = (s, args) =>
            {
                try
                {
                    idleTimer?.Stop();
                    idleTimer?.Start();
                }
                catch (ObjectDisposedException)
                {
                    // NOTE: When the Elapsed (or Timeout) and Paint are fire at almost exactly
                    // the same time, the timer maybe Disposed on a different thread.
                    // https://github.com/cefsharp/CefSharp/issues/4597
                }
                catch (Exception ex)
                {
                    renderIdleTcs.TrySetException(ex);
                }
            };

            idleTimer.Start();

            Paint += handler;

            try
            {
                await TaskTimeoutExtensions.WaitAsync(renderIdleTcs.Task, timeout ?? TimeSpan.FromSeconds(30), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception)
            {
                Paint -= handler;

                idleTimer?.Stop();
                idleTimer?.Dispose();

                throw;
            }
        }

        /// <summary>
        /// Legacy keyboard handler uses WindowProc callback interceptor to forward keypress events
        /// the the browser. Use this method to revert to the previous keyboard handling behaviour
        /// </summary>
        public void UseLegacyKeyboardHandler()
        {
            if (!(WpfKeyboardHandler is WpfLegacyKeyboardHandler))
            {
                WpfKeyboardHandler.Dispose();
                WpfKeyboardHandler = new WpfLegacyKeyboardHandler(this);
                WpfKeyboardHandler.Setup(source);
            }
        }

        /// <summary>
        /// The javascript object repository, one repository per ChromiumWebBrowser instance.
        /// </summary>
        public IJavascriptObjectRepository JavascriptObjectRepository
        {
            get { return managedCefBrowserAdapter?.JavascriptObjectRepository; }
        }

        /// <summary>
        /// Returns the current IBrowser Instance
        /// </summary>
        /// <returns>browser instance or null</returns>
        public IBrowser GetBrowser()
        {
            ThrowExceptionIfDisposed();
            ThrowExceptionIfBrowserNotInitialized();

            return browser;
        }

        private async Task CefUiThreadRunAsync(Action action)
        {
            if (!IsDisposed && InternalIsBrowserInitialized())
            {
                if (Cef.CurrentlyOnThread(CefThreadIds.TID_UI))
                {
                    action();
                }
                else
                {
                    await Cef.UIThreadTaskFactory.StartNew(delegate
                    {
                        action();
                    });
                }
            }
        }

        /// <summary>
        /// Resize hack for https://github.com/chromiumembedded/cef/issues/3427/osr-rendering-bug-when-minimizing-and
        /// </summary>
        /// <returns>Task</returns>
        private async Task ResizeHackRun()
        {
            var host = browser?.GetHost();
            if (host != null && !host.IsDisposed)
            {
                resizeHackSize = new Structs.Size(viewRect.Width + 1, viewRect.Height + 1);
                host.WasResized();

                await Task.Delay(ResizeHackDelayInMs);

                if (!host.IsDisposed)
                {
                    resizeHackSize = null;
                    host.WasResized();

                    resizeHackIgnoreOnPaint = false;

                    host.Invalidate(PaintElementType.View);
                }
            }
        }

        private void UnsubscribeInputLanguageChanged()
        {
            // If we are using WpfImeKeyboardHandler then we
            // shouldn't need to unsubsribe from the handler
            if (WpfKeyboardHandler?.GetType() == typeof(WpfImeKeyboardHandler))
            {
                return;
            }

            try
            {
                // Dispose can be called on non UI thread, InputLanguageManager.Current
                // is thread specific
                UiThreadRunAsync(() =>
                {
                    var inputLangManager = InputLanguageManager.Current;

                    if (inputLangManager != null)
                    {
                        inputLangManager.InputLanguageChanged -= OnInputLanguageChanged;
                    }
                });
            }
            catch (Exception ex)
            {
                //For now we'll ignore any errors
                Trace.TraceError($"Error unsubscribing from InputLanguageChanged {ex.ToString()}");
            }
        }
    }
}
