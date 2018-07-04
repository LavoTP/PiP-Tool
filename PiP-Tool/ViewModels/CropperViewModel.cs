﻿using System;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using PiP_Tool.DataModel;
using PiP_Tool.Interfaces;
using PiP_Tool.Native;

namespace PiP_Tool.ViewModels
{
    public class CropperViewModel : ViewModelBase, ICloseable
    {

        #region public

        public ICommand ClosingCommand { get; }
        public event EventHandler<EventArgs> RequestClose;

        /// <summary>
        /// Gets selected region
        /// </summary>
        public NativeStructs.Rect SelectedRegion => new NativeStructs.Rect(Left, Top, Width + Left, Top + Height);

        /// <summary>
        /// Gets or sets top property of the window
        /// </summary>
        public int WindowTop
        {
            get => _windowTop;
            set
            {
                _windowTop = value;
                RaisePropertyChanged();
            }
        }
        /// <summary>
        /// Gets or sets left property of the window
        /// </summary>
        public int WindowLeft
        {
            get => _windowLeft;
            set
            {
                _windowLeft = value;
                RaisePropertyChanged();
            }
        }
        /// <summary>
        /// Gets or sets width property of the window
        /// </summary>
        public int WindowWidth
        {
            get => _windowWidth;
            set
            {
                _windowWidth = value;
                RaisePropertyChanged();
            }
        }
        /// <summary>
        /// Gets or sets height property of the window
        /// </summary>
        public int WindowHeight
        {
            get => _windowHeight;
            set
            {
                _windowHeight = value;
                RaisePropertyChanged();
            }
        }
        /// <summary>
        /// Gets or sets canvas margin (border of the window to crop)
        /// </summary>
        public Thickness CanvasMargin
        {
            get => _canvasMargin;
            set
            {
                _canvasMargin = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets max height property of the region to crop
        /// </summary>
        public int MaxHeight
        {
            get => _maxHeight;
            set
            {
                _maxHeight = value;
                UpdateBottom();
                RaisePropertyChanged();
            }
        }
        /// <summary>
        /// Gets or sets max width property of the region to crop
        /// </summary>
        public int MaxWidth
        {
            get => _maxWidth;
            set
            {
                _maxWidth = value;
                UpdateRight();
                RaisePropertyChanged();
            }
        }
        /// <summary>
        /// Gets or sets min height property of the region to crop
        /// </summary>
        public int MinHeight
        {
            get => _minHeight;
            set
            {
                _minHeight = value;
                RaisePropertyChanged();
            }
        }
        /// <summary>
        /// Gets or sets min width property of the region to crop
        /// </summary>
        public int MinWidth
        {
            get => _minWidth;
            set
            {
                _minWidth = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets height property of the region to crop
        /// </summary>
        public int Height
        {
            get => _height;
            set
            {
                _height = value;
                UpdateBottom();
                RaisePropertyChanged();
            }
        }
        /// <summary>
        /// Gets or sets width property of the region to crop
        /// </summary>
        public int Width
        {
            get => _width;
            set
            {
                _width = value;
                UpdateRight();
                RaisePropertyChanged();
            }
        }
        /// <summary>
        /// Gets or sets top property of the region to crop
        /// </summary>
        public int Top
        {
            get => _top;
            set
            {
                _top = value;
                UpdateBottom();
                RaisePropertyChanged();
            }
        }
        /// <summary>
        /// Gets or sets left property of the region to crop
        /// </summary>
        public int Left
        {
            get => _left;
            set
            {
                _left = value;
                UpdateRight();
                RaisePropertyChanged();
            }
        }
        /// <summary>
        /// Gets or sets bottom property of the region to crop
        /// Updated by <see cref="UpdateBottom"/>
        /// </summary>
        public int Bottom
        {
            get => _bottom;
            set
            {
                _bottom = value;
                RaisePropertyChanged();
            }
        }
        /// <summary>
        /// Gets or sets right property of the region to crop
        /// Updated by <see cref="UpdateRight"/>
        /// </summary>
        public int Right
        {
            get => _right;
            set
            {
                _right = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        #region private

        private int _windowTop;
        private int _windowLeft;
        private int _windowWidth;
        private int _windowHeight;
        private Thickness _canvasMargin;

        private int _maxHeight;
        private int _maxWidth;
        private int _minHeight;
        private int _minWidth;

        private int _height;
        private int _width;
        private int _top;
        private int _left;
        private int _bottom;
        private int _right;

        private NativeStructs.Rect _sizeRestriction;
        private WindowInfo _windowInfo;

        #endregion

        /// <inheritdoc />
        /// <summary>
        /// Constructor
        /// </summary>
        public CropperViewModel()
        {
            ClosingCommand = new RelayCommand(ClosingCommandExecute);
            MessengerInstance.Register<WindowInfo>(this, Init);
            MessengerInstance.Register<Action<NativeStructs.Rect>>(this, StartPip);
        }

        /// <summary>
        /// Called from <see cref="MainViewModel"/> to init window to crop
        /// </summary>
        /// <param name="windowInfo">data of the window to crop</param>
        private void Init(WindowInfo windowInfo)
        {
            MessengerInstance.Unregister<WindowInfo>(this);
            _windowInfo = windowInfo;
            if (windowInfo == null)
            {
                ClosingCommandExecute();
                RequestClose?.Invoke(this, EventArgs.Empty);
                return;
            }
            _windowInfo.SetAsForegroundWindow();
            _sizeRestriction = _windowInfo.Rect - _windowInfo.Border;

            WindowTop = _windowInfo.Position.Y;
            WindowLeft = _windowInfo.Position.X;
            WindowWidth = _windowInfo.Rect.Width;
            WindowHeight = _windowInfo.Rect.Height;
            CanvasMargin = new Thickness(_windowInfo.Border.Left, _windowInfo.Border.Top, _windowInfo.Border.Right, _windowInfo.Border.Bottom);

            MaxHeight = _sizeRestriction.Height;
            MaxWidth = _sizeRestriction.Width;
            MinHeight = 100;
            MinWidth = 100;

            Top = 0;
            Left = 0;
            Height = MinHeight;
            Width = MinWidth;
            
            SetAsForegroundWindow();
        }

        /// <summary>
        /// Gets this window
        /// </summary>
        /// <returns>This window</returns>
        private Window ThisWindow()
        {
            var windowsList = Application.Current.Windows.Cast<Window>();
            return windowsList.FirstOrDefault(window => window.DataContext == this);
        }
        
        /// <summary>
        /// Set this window as foreground window
        /// </summary>
        public void SetAsForegroundWindow()
        {
            var thisWindow = ThisWindow();
            if (thisWindow == null)
                return;
            thisWindow.Show();
            thisWindow.Activate();
            thisWindow.Topmost = true;
            thisWindow.Topmost = false;
            thisWindow.Focus();
            var handle = new WindowInteropHelper(thisWindow).Handle;
            NativeMethods.SetForegroundWindow(handle);
        }

        /// <summary>
        /// Called by <see cref="Height"/> or <see cref="Top"/> to update <see cref="Bottom"/>
        /// </summary>
        private void UpdateBottom()
        {
            Bottom = MaxHeight - (Top + Height);
        }

        /// <summary>
        /// Called by <see cref="Width"/> or <see cref="Left"/> to update <see cref="Right"/>
        /// </summary>
        private void UpdateRight()
        {
            Right = MaxWidth - (Left + Width);
        }

        /// <summary>
        /// Called from <see cref="MainViewModel"/> to get cropped region and start PiP mode
        /// </summary>
        /// <param name="cb">Callback with cropped region</param>
        private void StartPip(Action<NativeStructs.Rect> cb)
        {
            MessengerInstance.Unregister<Action<NativeStructs.Rect>>(this);
            cb(SelectedRegion);
        }

        #region commands

        /// <summary>
        /// Executed when the window is closing. Unregister messengers
        /// </summary>
        private void ClosingCommandExecute()
        {
            MessengerInstance.Unregister<WindowInfo>(this);
            MessengerInstance.Unregister<Action<NativeStructs.Rect>>(this);
        }

        #endregion

    }
}
