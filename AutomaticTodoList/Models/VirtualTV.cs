// Full credit to GStefanowich for this brilliant idea!
//
// Originally from the "Forecaster Texts" mod.
// https://github.com/GStefanowich/SDV-Forecaster/blob/master/VirtualTV.cs
//
// Originally licensed under the MIT license.

using StardewValley.Objects;

namespace AutomaticTodoList.Models;

/// <summary>
/// Virtual TV exists as an extension of TV
/// Why? Because <see cref="TV.getRerunWeek"/> and <see cref="TV.getWeeklyRecipe"/> are protected methods.
/// By creating our own virtual TV we can introduce Accessor Methods!
/// </summary>
public sealed class VirtualTV : TV
{
    public VirtualTV() : base() { }

    public int GetRerunWeek() => base.getRerunWeek();

    public string[] GetWeeklyRecipe() => base.getWeeklyRecipe();
}
