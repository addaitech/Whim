using System;
using System.Collections;
using System.Collections.Generic;

namespace Whim;

internal class CommandManager : ICommandManager
{
	private readonly Dictionary<string, ICommand> _commands = new();

	public int Count => _commands.Count;

	/// <summary>
	/// Add a command from a plugin.
	/// </summary>
	/// <param name="item"></param>
	/// <exception cref="InvalidOperationException"></exception>
	internal void AddPluginCommand(ICommand item)
	{
		if (_commands.ContainsKey(item.Id))
		{
			throw new InvalidOperationException($"Command with id '{item.Id}' already exists.");
		}

		_commands.Add(item.Id, item);
	}

	public void Add(string identifier, string title, Action callback, Func<bool>? condition = null) =>
		AddPluginCommand(new Command($"whim.custom.{identifier}", title, callback, condition));

	public ICommand? TryGetCommand(string commandId)
	{
		if (_commands.TryGetValue(commandId, out ICommand? command))
		{
			return command;
		}

		return null;
	}

	public IEnumerator<ICommand> GetEnumerator() => _commands.Values.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
