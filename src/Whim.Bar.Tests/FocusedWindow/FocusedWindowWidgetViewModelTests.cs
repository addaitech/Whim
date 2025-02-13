using Moq;
using Xunit;

namespace Whim.Bar.Tests;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope")]
public class FocusedWindowWidgetViewModelTests
{
	private class Wrapper
	{
		public Mock<IContext> Context { get; } = new();
		public Mock<IWorkspaceManager> WorkspaceManager { get; } = new();
		public Mock<IWindowManager> WindowManager { get; } = new();
		public Mock<IWorkspace> Workspace { get; } = new();
		public Mock<IMonitor> Monitor { get; } = new();

		public Wrapper()
		{
			Context.SetupGet(c => c.WorkspaceManager).Returns(WorkspaceManager.Object);
			Context.SetupGet(c => c.WindowManager).Returns(WindowManager.Object);
			WorkspaceManager
				.Setup(wm => wm.GetEnumerator())
				.Returns(new List<IWorkspace> { Workspace.Object }.GetEnumerator());
		}
	}

	[Fact]
	public void Title_SameMonitor()
	{
		// Given
		Wrapper wrapper = new();
		FocusedWindowWidgetViewModel viewModel =
			new(wrapper.Context.Object, wrapper.Monitor.Object, FocusedWindowWidget.GetTitle);

		Mock<IWindow> window = new();
		window.SetupGet(w => w.Title).Returns("title");

		wrapper.WorkspaceManager.Setup(wm => wm.GetMonitorForWindow(window.Object)).Returns(wrapper.Monitor.Object);

		// When
		Assert.PropertyChanged(
			viewModel,
			nameof(viewModel.Title),
			() =>
			{
				wrapper.WindowManager.Raise(
					wm => wm.WindowFocused += null,
					new WindowEventArgs() { Window = window.Object }
				);
			}
		);

		// Then
		Assert.Equal("title", viewModel.Title);
	}

	[Fact]
	public void Title_DifferentMonitor()
	{
		// Given
		Wrapper wrapper = new();
		FocusedWindowWidgetViewModel viewModel =
			new(wrapper.Context.Object, wrapper.Monitor.Object, FocusedWindowWidget.GetTitle);

		Mock<IWindow> window = new();
		window.SetupGet(w => w.Title).Returns("title");

		Mock<IWindow> otherWindow = new();
		otherWindow.SetupGet(w => w.Title).Returns("other title");

		wrapper.WorkspaceManager.Setup(wm => wm.GetMonitorForWindow(window.Object)).Returns(wrapper.Monitor.Object);
		wrapper.WorkspaceManager
			.Setup(wm => wm.GetMonitorForWindow(otherWindow.Object))
			.Returns(new Mock<IMonitor>().Object);

		// When
		wrapper.WindowManager.Raise(wm => wm.WindowFocused += null, new WindowEventArgs() { Window = window.Object });
		Assert.PropertyChanged(
			viewModel,
			nameof(viewModel.Title),
			() =>
			{
				wrapper.WindowManager.Raise(
					wm => wm.WindowFocused += null,
					new WindowEventArgs() { Window = otherWindow.Object }
				);
			}
		);

		// Then
		Assert.Null(viewModel.Title);
	}

	[Fact]
	public void Dispose()
	{
		// Given
		Wrapper wrapper = new();
		FocusedWindowWidgetViewModel viewModel =
			new(wrapper.Context.Object, wrapper.Monitor.Object, FocusedWindowWidget.GetTitle);

		// When
		viewModel.Dispose();

		// Then
		wrapper.WindowManager.VerifyRemove(wm => wm.WindowFocused -= It.IsAny<EventHandler<WindowEventArgs>>());
	}
}
