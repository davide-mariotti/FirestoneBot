using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using UnityEngine;
using Logger = Firebot.Core.Logger;

namespace Firebot.GameModel.Base;

public class GameElement
{
    // Shared across every instance (fresh GameElements are constructed constantly) so the same
    // recurring failure is only ever logged once per session instead of on every single check.
    private static readonly HashSet<string> LoggedFailures = new();

    // Every path in this codebase resolves from one of a small, fixed set of scene-wide singleton
    // roots (menusRoot, battleRoot) that exist for the whole game session and are never destroyed
    // or recreated - only their CHILDREN change (popups open/close, pooled list items get reused).
    // GameObject.Find(name) is an unindexed, scene-wide search and was being repeated for every
    // single GameElement/GameButton/GameText access; caching just this top-level lookup (never the
    // relative Transform.Find below it) is safe because it never touches pooled/dynamic content -
    // every child path is still resolved fresh on every call, exactly as before. Unity's overloaded
    // null-check on a destroyed UnityEngine.Object self-heals this automatically if a root were ever
    // torn down (e.g. a scene reload), so a cached entry can never get stuck stale.
    private static readonly Dictionary<string, GameObject> RootObjectCache = new();

    private static GameObject FindRootCached(string rootName)
    {
        if (RootObjectCache.TryGetValue(rootName, out var cached) && cached != null) return cached;

        var found = GameObject.Find(rootName);
        if (found != null) RootObjectCache[rootName] = found;
        return found;
    }

    private readonly string _className;

    public GameElement(string path = null, GameElement parent = null, Transform transform = null)
    {
        _className = GetType().Name;

        if (transform != null)
        {
            var transformPath = GetGameObjectPath(transform);
            Path = BuildPath(transformPath, path);
        }
        else if (parent != null)
        {
            var parentPath = !string.IsNullOrEmpty(parent.Path) ? parent.Path : GetGameObjectPath(parent.Root);
            Path = BuildPath(parentPath, path);
        }
        else
            Path = CleanPath(path);

        if (string.IsNullOrEmpty(Path))
            DebugOnce("init-empty-path", "[FAILED] GameElement initialized with empty path.");
    }

    protected string Path { get; }

    // Always resolve from Path; do not cache transforms.
    // ResolvePath already logs the specific reason on failure, so nothing is logged here.
    protected Transform Root => ResolvePath(Path);

    public string Name => Root?.name ?? string.Empty;

    /// <summary>Full resolved path string - for diagnostics/logging only (see BotManager's flying-
    /// bonus scout), where the caller needs the whole path, not just the leaf Name.</summary>
    public string FullPath => Path;

    private static string CleanPath(string path) =>
        string.IsNullOrEmpty(path) ? path : Regex.Replace(path, @"/+", "/").Trim('/');

    private Transform ResolvePath(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        var slashIndex = path.IndexOf('/');

        if (slashIndex == -1)
        {
            var obj = FindRootCached(path);
            if (obj == null)
                DebugOnce($"root-missing:{path}", $"[FAILED] Root Object not found in scene: {path}");
            return obj?.transform;
        }

        var rootName = path[..slashIndex];
        var rootObj = FindRootCached(rootName);

        if (rootObj == null)
        {
            DebugOnce($"root-missing:{rootName}",
                $"[FAILED] Root '{rootName}' missing. Hierarchy search aborted. Path: {path}");
            return null;
        }

        var relativePath = path[(slashIndex + 1)..];
        var result = rootObj.transform.Find(relativePath);

        if (result == null)
            DebugOnce($"path-broken:{path}",
                $"[FAILED] Path broken: '{rootName}' exists, but child '{relativePath}' is missing. Full: {path}");

        return result;
    }

    public virtual bool IsVisible()
    {
        var currentRoot = Root;
        if (currentRoot == null) return false; // ResolvePath already logged why.

        var success = currentRoot.gameObject.activeInHierarchy;
        if (!success)
            DebugOnce($"hidden:{Path}", $"[FAILED] Element is hidden or inactive. Path: {Path}");

        return success;
    }

    public bool TryGetComponent<T>(out T component) where T : Component
    {
        component = null;
        var currentRoot = Root;
        if (currentRoot == null) return false;

        var success = currentRoot.TryGetComponent(out component);
        if (!success)
            DebugOnce($"component-missing:{typeof(T).Name}:{Path}",
                $"[FAILED] Component <{typeof(T).Name}> missing on: {currentRoot.name}. Path: {Path}");

        return success;
    }

    /// <summary>
    ///     Every real Component's type name on this GameObject - for diagnostics only (see BotManager's
    ///     flying-bonus scout). Note: an Il2Cpp interop type that hasn't been "seen" via a specific
    ///     TryGetComponent&lt;T&gt; call elsewhere resolves generically as "Component" instead of its
    ///     real class name (confirmed live, 2026-09-20, investigating Store's HUD button) - a custom
    ///     script's real name may not show up here even though it's genuinely present.
    /// </summary>
    public IEnumerable<string> GetComponentTypeNames()
    {
        var currentRoot = Root;
        if (currentRoot == null) yield break;

        foreach (var component in currentRoot.GetComponents<Component>())
            yield return component.GetType().Name;
    }

    public IEnumerable<GameElement> GetChildren()
    {
        var currentRoot = Root;
        if (currentRoot == null) yield break; // ResolvePath already logged why.

        for (var i = 0; i < currentRoot.childCount; i++)
            yield return new GameElement(transform: currentRoot.GetChild(i));
    }

    /// <summary>
    ///     Recursively searches every descendant (active or not - Transform hierarchy search finds
    ///     inactive/destroyed-and-respawned-elsewhere objects too, unlike GameObject.Find) whose name
    ///     matches namePredicate, up to maxDepth levels deep. For finding something whose exact parent
    ///     path is unknown - e.g. a dynamically spawned object with no fixed, documented location, see
    ///     the flying-bonus-hunter scout in BotManager.
    /// </summary>
    public IEnumerable<GameElement> FindDescendants(Func<string, bool> namePredicate, int maxDepth = 12)
    {
        var root = Root;
        if (root == null) yield break;

        foreach (var found in SearchChildren(root, namePredicate, maxDepth))
            yield return found;
    }

    private static IEnumerable<GameElement> SearchChildren(Transform parent, Func<string, bool> namePredicate,
        int depthLeft)
    {
        if (depthLeft <= 0) yield break;

        for (var i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            if (namePredicate(child.name)) yield return new GameElement(transform: child);

            foreach (var found in SearchChildren(child, namePredicate, depthLeft - 1))
                yield return found;
        }
    }

    public GameElement GetChild(int i)
    {
        try
        {
            return new GameElement(transform: Root.GetChild(i));
        }
        catch (Exception e)
        {
            Debug($" [EXCEPTION] Failed to get child at index {i}. Path: {Path}. Exception: {e}");
            return null;
        }
    }

    private static string GetGameObjectPath(Transform transform)
    {
        if (transform == null) return string.Empty;
        var path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }

        return path;
    }

    private static string BuildPath(string basePath, string path)
    {
        if (string.IsNullOrEmpty(basePath))
            return CleanPath(path);

        if (string.IsNullOrEmpty(path))
            return CleanPath(basePath);

        return CleanPath($"{basePath}/{path}");
    }

    protected void Debug(string message, [CallerMemberName] string member = "", [CallerLineNumber] int line = 0)
        => Logger.Debug($"[{_className}::{member}:{line}] {message}");

    private void DebugOnce(string key, string message, [CallerMemberName] string member = "",
        [CallerLineNumber] int line = 0)
    {
        if (LoggedFailures.Add(key)) Debug(message, member, line);
    }
}
