#nullable enable
using System.Collections.Generic;
using System.Linq;

namespace Firebot.Tasks.Map;

/// <summary>A squad spot: its machine's specialization (the game's Specialization as an int) and crew (hero codes).</summary>
public sealed record CrewSpot(int Role, IReadOnlyList<int> Crew);

/// <summary>
///     Decides the war machines' crews before any click. Pure: no game types, so the tests project
///     compiles it in directly.
/// </summary>
public static class CrewPlanner
{
    /// <summary>
    ///     Each spot's target crew, spots in the same order (front first). Crew sizes stay as they are,
    ///     plus the free heroes dealt one at a time to the smallest crew, front first. Then each machine
    ///     takes heroes of its own specialization (a Tank hero on the tank machine): those already there
    ///     stay, then the free ones, then those sitting on a machine they don't match. Whoever is left -
    ///     all of them when no hero matches - stays where they are or fills the remaining slots.
    /// </summary>
    public static List<List<int>> Plan(IReadOnlyList<CrewSpot> spots, IReadOnlyDictionary<int, int> heroRoles, int perMachine)
    {
        var crewed = spots.SelectMany(s => s.Crew).Where(heroRoles.ContainsKey).ToHashSet();
        var sizes = spots.Select(s => s.Crew.Count(heroRoles.ContainsKey)).ToArray();
        for (var free = heroRoles.Count - crewed.Count; free > 0; free--)
        {
            var open = Enumerable.Range(0, spots.Count).Where(i => sizes[i] < perMachine).ToList();
            if (open.Count == 0) break;
            sizes[open.OrderBy(i => sizes[i]).First()]++;
        }

        var target = spots.Select(_ => new List<int>()).ToList();
        var pool = heroRoles.Keys.ToHashSet();
        var freeFirst = pool.OrderBy(h => crewed.Contains(h) ? 1 : 0).ThenBy(h => h).ToList();

        void Take(int i, IEnumerable<int> heroes)
        {
            foreach (var hero in heroes.ToList())
            {
                if (target[i].Count >= sizes[i]) return;
                if (pool.Remove(hero)) target[i].Add(hero);
            }
        }

        bool Matches(int hero, int i) => heroRoles[hero] == spots[i].Role;

        for (var i = 0; i < spots.Count; i++) Take(i, spots[i].Crew.Where(h => pool.Contains(h) && Matches(h, i)));
        for (var i = 0; i < spots.Count; i++) Take(i, freeFirst.Where(h => pool.Contains(h) && Matches(h, i)));
        for (var i = 0; i < spots.Count; i++) Take(i, spots[i].Crew);
        for (var i = 0; i < spots.Count; i++) Take(i, freeFirst);
        return target;
    }

    /// <summary>How many heroes change crew (or join one) going from the spots' crews to target.</summary>
    public static int Moves(IReadOnlyList<CrewSpot> spots, List<List<int>> target) =>
        spots.Select((s, i) => target[i].Count(h => !s.Crew.Contains(h))).Sum();
}
