using Xunit;

namespace Whim.CommandPalette.Tests;

public class MostRecentlyUsedMatcherTests
{
	private static void ActionSink() { }

	private static (MenuVariantRowModel[], MatcherResult<CommandItem>[]) CreateMocks(string[] items)
	{
		MenuVariantRowModel[] menuItems = new MenuVariantRowModel[items.Length];
		MatcherResult<CommandItem>[] matcherItems = new MatcherResult<CommandItem>[items.Length];

		for (int i = 0; i < items.Length; i++)
		{
			CommandItem commandItem = new() { Command = new Command(items[i], items[i], ActionSink), };
			MenuVariantRowModel menuItem = new(commandItem);

			menuItems[i] = menuItem;
			matcherItems[i] = new(menuItem, Array.Empty<FilterTextMatch>(), 0);
		}

		return (menuItems, matcherItems);
	}

	[Fact]
	public void GetFilteredItems()
	{
		// Given
		MostRecentlyUsedMatcher<CommandItem> matcher = new();
		(MenuVariantRowModel[] items, MatcherResult<CommandItem>[] matches) = CreateMocks(new string[] { "A", "B" });

		// When
		int matchCount = matcher.GetFilteredItems("A", items, matches);

		// Then
		Assert.Equal(1, matchCount);
		Assert.Equal("A", matches[0].Model.Data.Command.Title);
		Assert.Equal((uint)0, matches[0].Score);
	}

	[Fact]
	public void GetMostRecentlyUsedItems()
	{
		// Given
		MostRecentlyUsedMatcher<CommandItem> matcher = new();
		(MenuVariantRowModel[] items, MatcherResult<CommandItem>[] matches) = CreateMocks(new string[] { "A", "B" });

		// When
		int matchCount = matcher.GetMostRecentlyUsedItems(items, matches);

		// Then
		Assert.Equal(2, matchCount);
		Assert.Equal("A", matches[0].Model.Data.Command.Title);
		Assert.Equal((uint)0, matches[0].Score);
		Assert.Equal("B", matches[1].Model.Data.Command.Title);
		Assert.Equal((uint)0, matches[1].Score);
	}

	[Fact]
	public void GetMostRecentlyUsedItems_WithLastExecutionTime()
	{
		// Given
		MostRecentlyUsedMatcher<CommandItem> matcher = new();
		(MenuVariantRowModel[] items, MatcherResult<CommandItem>[] matches) = CreateMocks(new string[] { "A", "B" });
		matcher.OnMatchExecuted(items[1]);

		// When
		int matchCount = matcher.GetMostRecentlyUsedItems(items, matches);

		// Then
		Assert.Equal(2, matchCount);
		Assert.Equal("A", matches[0].Model.Data.Command.Title);
		Assert.Equal("B", matches[1].Model.Data.Command.Title);
		Assert.True(matches[1].Score > matches[0].Score);
	}

	[Fact]
	public void Match_NoMatches_PopulatedQuery()
	{
		// Given
		MostRecentlyUsedMatcher<CommandItem> matcher = new();
		(MenuVariantRowModel[] items, MatcherResult<CommandItem>[] _) = CreateMocks(new string[] { "A", "B" });

		// When
		IEnumerable<MatcherResult<CommandItem>> rowItems = matcher.Match("C", items);

		// Then
		Assert.Empty(rowItems);
	}

	[Fact]
	public void Match_NoMatches_EmptyQuery()
	{
		// Given
		MostRecentlyUsedMatcher<CommandItem> matcher = new();
		(MenuVariantRowModel[] items, MatcherResult<CommandItem>[] _) = CreateMocks(new string[] { "A", "B" });

		// When
		MatcherResult<CommandItem>[] rowItems = matcher.Match("", items).ToArray();

		// Then
		Assert.Equal(2, rowItems.Length);
		Assert.Equal("A", rowItems.ElementAt(0).Model.Data.Command.Title);
		Assert.Equal("B", rowItems.ElementAt(1).Model.Data.Command.Title);
	}
}
