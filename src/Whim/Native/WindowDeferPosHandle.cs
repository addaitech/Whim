using System;
using System.Collections.Generic;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Whim;

/// <summary>
/// Sets the position of multiple windows at once, using <see cref="INativeManager.DeferWindowPos"/>.
/// As stated in Raymond Chen's blog post (https://devblogs.microsoft.com/oldnewthing/20050706-26/?p=35023),
/// this reduces the amount of repainting.
///
/// However, when the system has non-100% scaled monitors, for some reason we need to set the window
/// position twice, otherwise windows will have incorrect dimensions.
/// </summary>
public sealed class WindowDeferPosHandle : IDisposable
{
	private readonly IContext _context;
	private readonly List<(IWindowState windowState, HWND hwndInsertAfter, SET_WINDOW_POS_FLAGS? flags)> _windowStates =
		new();

	/// <summary>
	/// The default flags to use when setting the window position.
	/// </summary>
	public const SET_WINDOW_POS_FLAGS DefaultFlags =
		SET_WINDOW_POS_FLAGS.SWP_FRAMECHANGED
		| SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE
		| SET_WINDOW_POS_FLAGS.SWP_NOCOPYBITS
		| SET_WINDOW_POS_FLAGS.SWP_NOZORDER
		| SET_WINDOW_POS_FLAGS.SWP_NOOWNERZORDER;

	/// <summary>
	/// Create a new <see cref="WindowDeferPosHandle"/> to set the position of multiple windows at once.
	///
	/// <see cref="WindowDeferPosHandle"/> must be used in conjunction with a <c>using</c> block
	/// or statement, otherwise <see cref="INativeManager.EndDeferWindowPos"/> won't be called.
	/// </summary>
	/// <param name="context"></param>
	public WindowDeferPosHandle(IContext context)
	{
		Logger.Debug("Creating new WindowDeferPosHandle");
		_context = context;
	}

	/// <summary>
	/// Using the given <paramref name="windowState"/>, sets the window's position.
	/// </summary>
	/// <param name="windowState"></param>
	/// <param name="hwndInsertAfter">The window handle to insert show the given window behind.</param>
	/// <param name="flags">
	/// The flags to use when setting the window position. This overrides the default flags Whim sets,
	/// except when the window is maximized or minimized.
	/// </param>
	public void DeferWindowPos(
		IWindowState windowState,
		HWND? hwndInsertAfter = null,
		SET_WINDOW_POS_FLAGS? flags = null
	)
	{
		Logger.Debug($"Adding window {windowState} after {hwndInsertAfter} with flags {flags}");
		// We use HWND_BOTTOM, as modifying the Z-order of a window
		// may cause EVENT_SYSTEM_FOREGROUND to be set, which in turn
		// causes the relevant window to be focused, when the user hasn't
		// actually changed the focus.
		HWND targetHwndInsertAfter = hwndInsertAfter ?? (HWND)1; // HWND_BOTTOM
		_windowStates.Add((windowState, targetHwndInsertAfter, flags));
	}

	/// <inheritdoc />
	public void Dispose()
	{
		Logger.Debug("Disposing WindowDeferPosHandle");

		// Check to see if any monitors have non-100% scaling.
		// If so, we need to set the window position twice.
		int numPasses = 1;
		foreach (IMonitor monitor in _context.MonitorManager)
		{
			if (monitor.ScaleFactor != 100)
			{
				numPasses = 2;
				break;
			}
		}

		Logger.Debug($"Setting window position {numPasses} times");

		int count = _windowStates.Count;
		for (int i = 0; i < numPasses; i++)
		{
			using InternalWindowDeferPosHandle handle = new(_context, count);
			for (int j = 0; j < count; j++)
			{
				(IWindowState windowState, HWND hwndInsertAfter, SET_WINDOW_POS_FLAGS? flags) = _windowStates[j];
				handle.DeferWindowPos(windowState, hwndInsertAfter, flags);
			}
		}

		Logger.Debug("Finished setting window position");
	}

	private sealed class InternalWindowDeferPosHandle : IDisposable
	{
		private readonly IContext _context;
		private HDWP _hWinPosInfo;
		private readonly List<IWindow> _toMinimize;
		private readonly List<IWindow> _toMaximize;
		private readonly List<IWindow> _toNormal;

		/// <summary>
		/// Create a new <see cref="InternalWindowDeferPosHandle"/> for <paramref name="count"/> windows.
		/// This is to be used when setting the position of multiple windows at once.
		///
		/// <see cref="WindowDeferPosHandle"/> must be used in conjunction with a <c>using</c> block
		/// or statement, otherwise <see cref="INativeManager.EndDeferWindowPos"/> won't be called.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="count"></param>
		public InternalWindowDeferPosHandle(IContext context, int count)
		{
			Logger.Debug("Creating new InternalWindowDeferPosHandle");
			_context = context;
			_hWinPosInfo = context.NativeManager.BeginDeferWindowPos(count);

			_toMinimize = new List<IWindow>();
			_toMaximize = new List<IWindow>();
			_toNormal = new List<IWindow>();
		}

		public void DeferWindowPos(IWindowState windowState, HWND hwndInsertAfter, SET_WINDOW_POS_FLAGS? flags)
		{
			IWindow window = windowState.Window;

			ILocation<int>? offset = _context.NativeManager.GetWindowOffset(window.Handle);
			if (offset is null)
			{
				return;
			}

			ILocation<int> location = windowState.Location.Add(offset);

			WindowSize windowSize = windowState.WindowSize;

			SET_WINDOW_POS_FLAGS uFlags = flags ?? DefaultFlags;

			if (windowSize == WindowSize.Maximized)
			{
				_toMaximize.Add(window);
				uFlags = uFlags | SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOSIZE;
			}
			else if (windowSize == WindowSize.Minimized)
			{
				_toMinimize.Add(window);
				uFlags = uFlags | SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOSIZE;
			}
			else
			{
				_toNormal.Add(window);
			}

			_hWinPosInfo = _context.NativeManager.DeferWindowPos(
				_hWinPosInfo,
				window.Handle,
				hwndInsertAfter,
				location.X,
				location.Y,
				location.Width,
				location.Height,
				uFlags
			);
		}

		public void Dispose()
		{
			Logger.Debug("Disposing InternalWindowDeferPosHandle");
			foreach (IWindow w in _toMinimize)
			{
				if (!w.IsMinimized)
				{
					_context.NativeManager.MinimizeWindow(w.Handle);
				}
			}

			foreach (IWindow w in _toMaximize)
			{
				if (!w.IsMaximized)
				{
					_context.NativeManager.ShowWindowMaximized(w.Handle);
				}
			}

			foreach (IWindow w in _toNormal)
			{
				_context.NativeManager.ShowWindowNoActivate(w.Handle);
			}

			_context.NativeManager.EndDeferWindowPos(_hWinPosInfo);
			Logger.Debug("Disposed InternalWindowDeferPosHandle");
		}
	}
}
