using Moq;
using Xunit;

namespace Whim.TreeLayout.Tests;

public class TestMoveWindowEdgesInDirection
{
	[Fact]
	public void MoveWindowEdgesInDirection_CannotFindWindow()
	{
		// Given
		TestTreeEngineMocks testEngine = new();
		IPoint<double> pixelsDeltas = new Point<double>() { X = 0.1, Y = 0 };

		// When
		testEngine.Engine.MoveWindowEdgesInDirection(Direction.Left, pixelsDeltas, new Mock<IWindow>().Object);

		// Then
		Assert.Equal(0.5, testEngine.LeftNode.GetWeight());
		Assert.Equal(0.5, testEngine.RightBottomNode.GetWeight());
	}

	[Fact]
	public void MoveWindowEdgesInDirection_DoLayoutHasNotBeenCalled()
	{
		// Given
		TestTreeEngineMocks testEngine = new();
		IPoint<double> pixelsDeltas = new Point<double>() { X = 0.1, Y = 0 };

		// When
		testEngine.Engine.MoveWindowEdgesInDirection(Direction.Left, pixelsDeltas, testEngine.LeftWindow.Object);

		// Then
		Assert.Equal(0.5, testEngine.LeftNode.GetWeight());
		Assert.Equal(0.5, testEngine.RightBottomNode.GetWeight());
	}

	/// <summary>
	/// You can't move the left node further left.
	/// </summary>
	[Fact]
	public void MoveWindowEdgesInDirection_Left_Left()
	{
		// Given
		TestTree tree = new();
		TestTreeEngineMocks testEngine = new();
		testEngine.Engine.DoLayout(new Location<int>() { Height = 1080, Width = 1920 }, new Mock<IMonitor>().Object);
		IPoint<double> pixelsDeltas = new Point<double>() { X = 0.1, Y = 0 };

		// When
		testEngine.Engine.MoveWindowEdgesInDirection(Direction.Left, pixelsDeltas, testEngine.LeftWindow.Object);

		// Then
		Assert.Equal(tree.Left.GetWeight(), testEngine.LeftNode.GetWeight());
		Assert.Equal(tree.Root.GetWeight(), testEngine.Engine.Root?.GetWeight());
	}

	/// <summary>
	/// Move the left node further right.
	/// </summary>
	[Fact]
	public void MoveWindowEdgesInDirection_Left_Right()
	{
		// Given
		TestTreeEngineMocks testEngine = new();
		testEngine.Engine.DoLayout(new Location<int>() { Height = 1080, Width = 1920 }, new Mock<IMonitor>().Object);
		IPoint<double> pixelsDeltas = new Point<double>() { X = 0.1, Y = 0 };

		// When
		testEngine.Engine.MoveWindowEdgesInDirection(Direction.Right, pixelsDeltas, testEngine.LeftWindow.Object);

		// Then
		Assert.Equal(0.5 + 0.1, testEngine.LeftNode.GetWeight());
		Assert.Equal(0.5 - 0.1, testEngine.RightBottomNode.Parent?.GetWeight());
	}

	/// <summary>
	/// Move RightTopLeftBottomLeft to the left.
	/// </summary>
	[Fact]
	public void MoveWindowEdgesInDirection_RightTopLeftBottomLeft_Left()
	{
		// Given
		TestTreeEngineMocks testEngine = new();
		IWindowState[] _ = testEngine.Engine
			.DoLayout(new Location<int>() { Height = 1080, Width = 1920 }, new Mock<IMonitor>().Object)
			.ToArray();
		IPoint<double> pixelsDeltas = new Point<double>() { X = 0.1, Y = 0 };

		// When
		testEngine.Engine.MoveWindowEdgesInDirection(
			Direction.Left,
			pixelsDeltas,
			testEngine.RightTopLeftBottomLeftWindow.Object
		);

		// Then
		Assert.Equal(0.5 + 0.1, testEngine.RightBottomNode.Parent?.GetWeight());
		Assert.Equal(0.5 - 0.1, testEngine.LeftNode.GetWeight());
	}

	/// <summary>
	/// Move RightBottom up.
	/// </summary>
	[Fact]
	public void MoveWindowEdgesInDirection_RightBottom_Up()
	{
		// Given
		TestTreeEngineMocks testEngine = new();
		IWindowState[] _ = testEngine.Engine
			.DoLayout(new Location<int>() { Height = 1080, Width = 1920 }, new Mock<IMonitor>().Object)
			.ToArray();
		IPoint<double> pixelsDeltas = new Point<double>() { X = 0, Y = 0.1 };

		// When
		testEngine.Engine.MoveWindowEdgesInDirection(Direction.Up, pixelsDeltas, testEngine.RightBottomWindow.Object);

		// Then
		Assert.Equal(0.5 + 0.1, testEngine.RightBottomNode.GetWeight());
		Assert.Equal(0.5 - 0.1, testEngine.RightTopLeftTopNode.Parent?.Parent?.GetWeight());
	}

	/// <summary>
	/// Move RightTopRight3 down.
	/// </summary>
	[Fact]
	public void MoveWindowEdgesInDirection_RightTopRight3_Down()
	{
		// Given
		TestTreeEngineMocks testEngine = new();
		testEngine.Engine.DoLayout(new Location<int>() { Height = 1080, Width = 1920 }, new Mock<IMonitor>().Object);
		IPoint<double> pixelsDeltas = new Point<double>() { X = 0, Y = 0.1 };

		// When
		testEngine.Engine.MoveWindowEdgesInDirection(
			Direction.Down,
			pixelsDeltas,
			testEngine.RightTopRight3Window.Object
		);

		// Then
		Assert.Equal(0.5 + 0.1, testEngine.RightTopRight3Node.Parent?.Parent?.GetWeight());
		Assert.Equal(0.5 - 0.1, testEngine.RightBottomNode.GetWeight());
	}
}
