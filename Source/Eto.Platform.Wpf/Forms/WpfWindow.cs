using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Eto.Forms;
using Eto.Drawing;
using sw = System.Windows;
using swm = System.Windows.Media;
using swc = System.Windows.Controls;
using System.Runtime.InteropServices;
using Eto.Platform.Wpf.CustomControls;
using Eto.Platform.Wpf.Forms.Menu;

namespace Eto.Platform.Wpf.Forms
{
	public interface IWpfWindow
	{
		sw.Window Control { get; }
	}

	public abstract class WpfWindow<T, W> : WpfContainer<T, W>, IWindow, IWpfWindow
		where T : sw.Window
		where W : Window
	{
		Icon icon;
		MenuBar menu;
		ToolBar toolBar;
		swc.DockPanel main;
		swc.ContentControl menuHolder;
		swc.ContentControl toolBarHolder;
		swc.DockPanel content;
		Size? initialClientSize;

		public swc.DockPanel Content
		{
			get { return content; }
		}

		public override void Initialize ()
		{
			base.Initialize ();

			Control.SizeToContent = sw.SizeToContent.WidthAndHeight;
			main = new swc.DockPanel ();
			content = new swc.DockPanel ();
			menuHolder = new swc.ContentControl { IsTabStop = false };
			toolBarHolder = new swc.ContentControl { IsTabStop = false };
			content.Background = System.Windows.SystemColors.ControlBrush;
			swc.DockPanel.SetDock (menuHolder, swc.Dock.Top);
			swc.DockPanel.SetDock (toolBarHolder, swc.Dock.Top);
			main.Children.Add (menuHolder);
			main.Children.Add (toolBarHolder);
			main.Children.Add (content);
			Control.Content = main;
			Control.Loaded += delegate {
				if (initialClientSize != null) {
					UpdateClientSize (initialClientSize.Value);
					initialClientSize = null;
				}
			};
			// needed to handle Application.Terminating event
			HandleEvent (Window.ClosingEvent);
		}

		public override void AttachEvent (string handler)
		{
			switch (handler) {
				case Window.ClosedEvent:
					Control.Closed += delegate {
						Widget.OnClosed (EventArgs.Empty);
					};
					break;
				case Window.ClosingEvent:
					Control.Closing += (sender, e) => {
						Widget.OnClosing (e);
						if (!e.Cancel && sw.Application.Current.Windows.Count == 1) {
							// last window closing, so call OnTerminating to let the app abort terminating
							Application.Instance.OnTerminating (e);
						}
					};
					break;
				case Window.MaximizedEvent:
					Control.StateChanged += (sender, e) => {
						if (Control.WindowState == sw.WindowState.Maximized) {
							Widget.OnMaximized (EventArgs.Empty);
						}
					};
					break;
				case Window.MinimizedEvent:
					Control.StateChanged += (sender, e) => {
						if (Control.WindowState == sw.WindowState.Minimized) {
							Widget.OnMinimized (EventArgs.Empty);
						}
					};
					break;
				default:
					base.AttachEvent (handler);
					break;
			}
		}

		protected virtual void UpdateClientSize (Size size)
		{
			var xdiff = Control.ActualWidth - content.ActualWidth;
			var ydiff = Control.ActualHeight - content.ActualHeight;
			Control.Width = size.Width + xdiff;
			Control.Height = size.Height + ydiff;
			Control.SizeToContent = sw.SizeToContent.Manual;
		}

		public ToolBar ToolBar
		{
			get { return toolBar; }
			set
			{
				toolBar = value;
				if (toolBar != null) {
					toolBarHolder.Content = toolBar.ControlObject;
				}
				else
					toolBarHolder.Content = null;
			}
		}

		public void Close ()
		{
			Control.Close ();
		}

		void CopyKeyBindings (swc.ItemCollection items)
		{
			foreach (var item in items.OfType<swc.MenuItem>()) {
				this.Control.InputBindings.AddRange (item.InputBindings);
				if (item.HasItems)
					CopyKeyBindings (item.Items);
			}
		}

		public MenuBar Menu
		{
			get { return menu; }
			set
			{
				menu = value;
				if (menu != null) {
					var handler = (MenuBarHandler)menu.Handler;
					menuHolder.Content = handler.Control;
					CopyKeyBindings (handler.Control.Items);
				}
				else {
					menuHolder.Content = null;
				}
			}
		}

		public Icon Icon
		{
			get { return icon; }
			set
			{
				icon = value;
				if (value != null) {
					Control.Icon = (swm.ImageSource)icon.ControlObject;
				}
			}
		}

		public virtual bool Resizable
		{
			get { return Control.ResizeMode == sw.ResizeMode.CanResize || Control.ResizeMode == sw.ResizeMode.CanResizeWithGrip; }
			set
			{
				if (value) Control.ResizeMode = sw.ResizeMode.CanResizeWithGrip;
				else Control.ResizeMode = sw.ResizeMode.CanMinimize;
			}
		}

		public void Minimize ()
		{
			Control.WindowState = sw.WindowState.Minimized;
		}

		public override Size ClientSize
		{
			get
			{
				if (Control.IsLoaded)
					return new Size ((int)content.ActualWidth, (int)content.ActualHeight);
				else
					return initialClientSize ?? Size.Empty;
			}
			set
			{
				if (Control.IsLoaded)
					UpdateClientSize (value);
				else
					initialClientSize = value;
			}
		}

		public override Size Size
		{
			get { return base.Size; }
			set
			{
				Control.SizeToContent = sw.SizeToContent.Manual;
				base.Size = value;
			}
		}

		public override object ContainerObject
		{
			get { return Control; }
		}

		public override void SetLayout (Layout layout)
		{
			content.Children.Clear ();
			content.Children.Add ((sw.UIElement)layout.ControlObject);
		}

		public string Title
		{
			get { return Control.Title; }
			set { Control.Title = value; }
		}


		public Point Location
		{
			get
			{
				return new Point ((int)Control.Left, (int)Control.Top);
			}
			set
			{
				Control.Left = value.X;
				Control.Top = value.Y;
			}
		}

		public WindowState State
		{
			get
			{
				switch (Control.WindowState) {
					case sw.WindowState.Maximized:
						return WindowState.Maximized;
					case sw.WindowState.Minimized:
						return WindowState.Minimized;
					case sw.WindowState.Normal:
						return WindowState.Normal;
					default:
						throw new NotSupportedException ();
				}
			}
			set
			{
				switch (value) {
				case WindowState.Maximized:
					Control.WindowState = sw.WindowState.Maximized;
					if (!Control.IsLoaded)
						Control.SizeToContent = sw.SizeToContent.Manual;
					break;
				case WindowState.Minimized:
					Control.WindowState = sw.WindowState.Minimized;
					if (!Control.IsLoaded)
						Control.SizeToContent = sw.SizeToContent.WidthAndHeight;
					break;
				case WindowState.Normal:
					Control.WindowState = sw.WindowState.Normal;
					if (!Control.IsLoaded)
						Control.SizeToContent = sw.SizeToContent.WidthAndHeight;
					break;
				default:
					throw new NotSupportedException ();
				}
			}
		}

		public Rectangle? RestoreBounds
		{
			get { return Control.RestoreBounds.ToEto (); }
		}


		public override Size? MinimumSize
		{
			get
			{
				if (Control.MinWidth > 0 && Control.MinHeight > 0)
					return new Size ((int)Control.MinWidth, (int)Control.MinHeight);
				else
					return null;
			}
			set
			{
				if (value != null) {
					Control.MinWidth = value.Value.Width;
					Control.MinHeight = value.Value.Height;
				}
				else {
					Control.MinHeight = 0;
					Control.MinWidth = 0;
				}
			}
		}

		sw.Window IWpfWindow.Control
		{
			get { return this.Control; }
		}

		public double Opacity
		{
			get { return Control.Opacity; }
			set
			{
				if (value != 1.0) {
					if (Control.IsLoaded) {
						GlassHelper.BlurBehindWindow (Control);
						//GlassHelper.ExtendGlassFrame (Control);
						Control.Opacity = value;
					}
					else {
						Control.Loaded += delegate {
							GlassHelper.BlurBehindWindow (Control);
							//GlassHelper.ExtendGlassFrame (Control);
							Control.Opacity = value;
						};
					}
				}
				else {
					Control.Opacity = value;
				}
			}
		}

		public override bool HasFocus
		{
			get { return Control.IsActive && ((ApplicationHandler)Application.Instance.Handler).IsActive; }
		}

		public override Color BackgroundColor
		{
			get
			{
				var brush = Control.Background as System.Windows.Media.SolidColorBrush;
				if (brush != null) return brush.Color.ToEto ();
				else return Colors.Black;
			}
			set
			{
				Control.Background = new System.Windows.Media.SolidColorBrush (value.ToWpf ());
			}
		}
	}
}
