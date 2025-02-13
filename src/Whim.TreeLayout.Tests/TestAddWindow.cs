using Moq;
using Xunit;

namespace Whim.TreeLayout.Tests;

public class TestAddWindow
{
	[Fact]
	public void Add_Root()
	{
		Mock<IWorkspace> workspace = new();
		Mock<IWorkspaceManager> workspaceManager = new();
		workspaceManager.Setup(w => w.ActiveWorkspace).Returns(workspace.Object);

		Mock<IContext> context = new();
		context.Setup(x => x.WorkspaceManager).Returns(workspaceManager.Object);

		TreeLayoutEngine engine = new(context.Object);

		Mock<IWindow> window = new();
		engine.Add(window.Object);

		Assert.Equal(engine.Root, new WindowNode(window.Object));
	}

	[Fact]
	public void Add_TestTree()
	{
		TestTreeEngineMocks testEngine = new();

		TestTree tree =
			new(
				leftWindow: testEngine.LeftWindow,
				rightTopLeftTopWindow: testEngine.RightTopLeftTopWindow,
				rightBottomWindow: testEngine.RightBottomWindow,
				rightTopRight1Window: testEngine.RightTopRight1Window,
				rightTopRight2Window: testEngine.RightTopRight2Window,
				rightTopRight3Window: testEngine.RightTopRight3Window,
				rightTopLeftBottomLeftWindow: testEngine.RightTopLeftBottomLeftWindow,
				rightTopLeftBottomRightTopWindow: testEngine.RightTopLeftBottomRightTopWindow,
				rightTopLeftBottomRightBottomWindow: testEngine.RightTopLeftBottomRightBottomWindow
			);
		Assert.Equal(tree.Root, testEngine.Engine.Root);
	}

	[Fact]
	public void Add_UnequalSplitNode()
	{
		// Given
		Mock<IMonitor> monitor = new();
		Mock<IMonitorManager> monitorManager = new();
		Mock<IWorkspace> activeWorkspace = new();
		Mock<IWorkspaceManager> workspaceManager = new();
		Mock<IContext> context = new();

		monitor.Setup(m => m.WorkingArea.Width).Returns(1920);
		monitor.Setup(m => m.WorkingArea.Height).Returns(1080);
		monitorManager.Setup(m => m.ActiveMonitor).Returns(monitor.Object);
		context.Setup(x => x.MonitorManager).Returns(monitorManager.Object);

		workspaceManager.Setup(x => x.ActiveWorkspace).Returns(activeWorkspace.Object);
		context.Setup(x => x.WorkspaceManager).Returns(workspaceManager.Object);

		TreeLayoutEngine engine = new(context.Object) { AddNodeDirection = Direction.Right };
		IWindowState[] _ = engine
			.DoLayout(new Location<int>() { Height = 1080, Width = 1920 }, new Mock<IMonitor>().Object)
			.ToArray();

		Mock<IWindow> window1 = new();
		Mock<IWindow> window2 = new();
		Mock<IWindow> window3 = new();

		engine.Add(window1.Object);
		engine.Add(window2.Object);

		workspaceManager.Setup(w => w.ActiveWorkspace.LastFocusedWindow).Returns(window1.Object);

		// When
		engine.MoveWindowEdgesInDirection(Direction.Right, new Point<double>() { X = 0.1, Y = 0 }, window1.Object);

		engine.Add(window3.Object);

		// Then
		SplitNode root = (engine.Root as SplitNode)!;
		Assert.Equal(0.3d, root[0].weight);
		Assert.Equal(0.3d, root[1].weight);
		Assert.Equal(0.4d, root[2].weight);
	}

	/// <summary>
	/// This is for when the currently window being added to the tree is also the last focused window.
	/// This can occur when the window is being docked from a floating state.
	/// </summary>
	[Fact]
	public void Add_CurrentlyFocusedWindow()
	{
		Mock<IWindow> window = new();

		Mock<IWorkspace> workspace = new();
		workspace.Setup(w => w.LastFocusedWindow).Returns(window.Object);

		Mock<IWorkspaceManager> workspaceManager = new();
		workspaceManager.Setup(w => w.ActiveWorkspace).Returns(workspace.Object);

		Mock<IContext> context = new();
		context.Setup(x => x.WorkspaceManager).Returns(workspaceManager.Object);

		TreeLayoutEngine engine = new(context.Object) { window.Object };

		Assert.Equal(engine.Root, new WindowNode(window.Object));
		Assert.Single(engine);
	}
}
