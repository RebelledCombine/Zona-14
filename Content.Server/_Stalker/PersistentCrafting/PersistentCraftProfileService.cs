using System;
using System.Collections.Generic;
using Content.Shared._Stalker.PersistentCrafting;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;

namespace Content.Server._Stalker.PersistentCrafting;

public sealed class PersistentCraftProfileService
{
    private readonly IPrototypeManager _prototype;
    private readonly PersistentCraftBranchRegistry _branchRegistry;
    private readonly IReadOnlyList<PersistentCraftNodePrototype> _nodeCache;
    private readonly ISawmill _sawmill;
    private readonly HashSet<string> _reusablePath = new();

    /// <summary>
    /// Auto-unlock nodes (Cost ≤ 0), sorted in topological order. // Zona14: translated comment
    /// Guarantee: if node A is a prerequisite of node B, then A comes before B in the list. // Zona14: translated comment
    /// Built once in the constructor using Kahn's algorithm, after which // Zona14: translated comment
    /// <see cref="EnsureAutoTierNodesUnlocked"/> executes in a single O(m) pass, // Zona14: translated comment
    /// where m is the number of auto-unlock nodes. // Zona14: translated comment
    /// </summary>
    private readonly List<PersistentCraftNodePrototype> _autoUnlockTopological;

    public PersistentCraftProfileService(
        IPrototypeManager prototype,
        PersistentCraftBranchRegistry branchRegistry,
        IReadOnlyList<PersistentCraftNodePrototype> nodeCache)
    {
        _prototype = prototype;
        _branchRegistry = branchRegistry;
        _nodeCache = nodeCache;
        _sawmill = Logger.GetSawmill("persistent-craft.profile");
        _autoUnlockTopological = BuildAutoUnlockTopologicalOrder(nodeCache);
    }

    /// <summary>
    /// Kahn's algorithm: builds topological order for auto-unlock nodes. // Zona14: translated comment
    /// Only edges between auto-unlock nodes are considered; prerequisites // Zona14: translated comment
    /// with Cost > 0 (manual nodes) are treated as "external" — they are checked // Zona14: translated comment
    /// at runtime via profile.UnlockedNodes.Contains. // Zona14: translated comment
    /// </summary>
    private static List<PersistentCraftNodePrototype> BuildAutoUnlockTopologicalOrder(
        IReadOnlyList<PersistentCraftNodePrototype> allNodes)
    {
        // Collect only auto-unlock nodes into a dictionary // Zona14: translated comment
        var autoNodes = new Dictionary<string, PersistentCraftNodePrototype>();
        for (var i = 0; i < allNodes.Count; i++)
        {
            var node = allNodes[i];
            if (PersistentCraftingHelper.IsAutoUnlockedNode(node))
                autoNodes[node.ID] = node;
        }

        if (autoNodes.Count == 0)
            return new List<PersistentCraftNodePrototype>();

        // Calculate in-degree and build dependents graph. // Zona14: translated comment
        // Edge prerequisiteId → nodeId means: "when prerequisiteId is processed, // Zona14: translated comment
        // decrement in-degree of nodeId". Only edges between auto-unlock nodes are considered. // Zona14: translated comment
        var inDegree = new Dictionary<string, int>(autoNodes.Count);
        var dependents = new Dictionary<string, List<string>>(autoNodes.Count);

        foreach (var node in autoNodes.Values)
        {
            if (!inDegree.ContainsKey(node.ID))
                inDegree[node.ID] = 0;

            for (var i = 0; i < node.Prerequisites.Count; i++)
            {
                var prereqId = node.Prerequisites[i];
                if (!autoNodes.ContainsKey(prereqId))
                    continue;

                // prereqId → node.ID: when prereq is processed, node.ID gets -1 to in-degree // Zona14: translated comment
                inDegree.TryGetValue(node.ID, out var current);
                inDegree[node.ID] = current + 1;

                if (!dependents.TryGetValue(prereqId, out var depList))
                {
                    depList = new List<string>();
                    dependents[prereqId] = depList;
                }

                depList.Add(node.ID);
            }
        }

        // BFS (Kahn's algorithm): start with nodes that have no auto-unlock prerequisites // Zona14: translated comment
        var queue = new Queue<string>();
        foreach (var (nodeId, degree) in inDegree)
        {
            if (degree == 0)
                queue.Enqueue(nodeId);
        }

        var result = new List<PersistentCraftNodePrototype>(autoNodes.Count);
        while (queue.Count > 0)
        {
            var nodeId = queue.Dequeue();
            result.Add(autoNodes[nodeId]);

            if (!dependents.TryGetValue(nodeId, out var deps))
                continue;

            for (var i = 0; i < deps.Count; i++)
            {
                var depId = deps[i];
                inDegree[depId]--;
                if (inDegree[depId] == 0)
                    queue.Enqueue(depId);
            }
        }

        // If result.Count < autoNodes.Count — there is a cycle among auto-unlock nodes. // Zona14: translated comment
        // ValidateNodeCycles in CraftingSystem already warns about this at startup, // Zona14: translated comment
        // so here we simply skip cyclic nodes (they won't be included in the list). // Zona14: translated comment

        return result;
    }

    public Dictionary<string, PersistentCraftBranchProfile> CreateDefaultBranchProfiles()
    {
        var result = new Dictionary<string, PersistentCraftBranchProfile>(_branchRegistry.OrderedBranchIds.Count);

        for (var i = 0; i < _branchRegistry.OrderedBranchIds.Count; i++)
        {
            var branch = _branchRegistry.OrderedBranchIds[i];
            result[branch] = new PersistentCraftBranchProfile();
        }

        return result;
    }

    public Dictionary<string, PersistentCraftBranchProfile> BuildBranchProfiles(IEnumerable<PersistentCraftBranchSaveData> branches)
    {
        var result = CreateDefaultBranchProfiles();

        foreach (var branch in branches)
        {
            if (string.IsNullOrWhiteSpace(branch.Branch) || !result.ContainsKey(branch.Branch))
                continue;

            result[branch.Branch] = new PersistentCraftBranchProfile
            {
                TotalEarnedPoints = branch.TotalEarnedPoints,
            };
        }

        return result;
    }

    public HashSet<string> SanitizeUnlockedNodes(IEnumerable<string> unlockedNodes, string characterName)
    {
        var sanitized = new HashSet<string>();

        foreach (var nodeId in unlockedNodes)
        {
            if (string.IsNullOrWhiteSpace(nodeId))
                continue;

            if (!_prototype.TryIndex<PersistentCraftNodePrototype>(nodeId, out _))
            {
                _sawmill.Warning($"[PersistentCraft] Missing node prototype '{nodeId}' in profile '{characterName}', removing stale unlock.");
                continue;
            }

            sanitized.Add(nodeId);
        }

        return sanitized;
    }

    public void EnsureAutoTierNodesUnlocked(PersistentCraftProfileComponent profile)
    {
        // Single pass over the precomputed topological order of auto-unlock nodes — O(m). // Zona14: translated comment
        // Thanks to topological sorting, by the time a node is processed all its // Zona14: translated comment
        // auto-unlock prerequisites have already been considered. Prerequisites with Cost > 0 // Zona14: translated comment
        // (manual nodes) are checked via profile.UnlockedNodes.Contains. // Zona14: translated comment
        for (var i = 0; i < _autoUnlockTopological.Count; i++)
        {
            var node = _autoUnlockTopological[i];

            if (profile.UnlockedNodes.Contains(node.ID))
                continue;

            var allPrerequisitesMet = true;
            for (var j = 0; j < node.Prerequisites.Count; j++)
            {
                if (!profile.UnlockedNodes.Contains(node.Prerequisites[j]))
                {
                    allPrerequisitesMet = false;
                    break;
                }
            }

            if (allPrerequisitesMet)
                profile.UnlockedNodes.Add(node.ID);
        }
    }

    public bool HasNodeUnlockedOrAutoAvailable(PersistentCraftProfileComponent profile, string nodeId)
    {
        return PersistentCraftNodeRules.HasNodeUnlockedOrAutoAvailable(
            nodeId,
            profile.UnlockedNodes.Contains,
            ResolveNodePrototypeOrNull,
            _reusablePath);
    }

    public bool AreNodePrerequisitesMet(PersistentCraftProfileComponent profile, PersistentCraftNodePrototype node)
    {
        return PersistentCraftNodeRules.ArePrerequisitesMet(
            node,
            profile.UnlockedNodes.Contains,
            ResolveNodePrototypeOrNull,
            _reusablePath);
    }

    public int GetAvailableBranchPoints(PersistentCraftProfileComponent profile, string branch)
    {
        var branchProfile = GetOrCreateBranchProfile(profile, branch);
        var spent = GetSpentBranchPoints(profile, branch);
        return Math.Max(0, branchProfile.TotalEarnedPoints - spent);
    }

    public int GetTotalEarnedBranchPoints(PersistentCraftProfileComponent profile, string branch)
    {
        return GetOrCreateBranchProfile(profile, branch).TotalEarnedPoints;
    }

    public int GetSpentBranchPoints(PersistentCraftProfileComponent profile, string branch)
    {
        var spent = 0;

        for (var i = 0; i < _nodeCache.Count; i++)
        {
            var node = _nodeCache[i];
            if (node.Branch != branch || node.Cost <= 0 || !profile.UnlockedNodes.Contains(node.ID))
                continue;

            spent += node.Cost;
        }

        return spent;
    }

    public List<PersistentCraftBranchState> BuildBranchStates(PersistentCraftProfileComponent profile)
    {
        // Count spent points in a single pass over nodes — for all branches at once. // Zona14: translated comment
        var spentByBranch = new Dictionary<string, int>(_branchRegistry.OrderedBranchIds.Count);
        for (var i = 0; i < _nodeCache.Count; i++)
        {
            var node = _nodeCache[i];
            if (node.Cost <= 0 || !profile.UnlockedNodes.Contains(node.ID))
                continue;

            spentByBranch.TryGetValue(node.Branch, out var current);
            spentByBranch[node.Branch] = current + node.Cost;
        }

        var result = new List<PersistentCraftBranchState>(_branchRegistry.OrderedBranchIds.Count);
        for (var i = 0; i < _branchRegistry.OrderedBranchIds.Count; i++)
        {
            var branch = _branchRegistry.OrderedBranchIds[i];
            var branchProfile = GetOrCreateBranchProfile(profile, branch);
            spentByBranch.TryGetValue(branch, out var spent);
            var available = Math.Max(0, branchProfile.TotalEarnedPoints - spent);
            result.Add(new PersistentCraftBranchState(branch, available, spent));
        }

        return result;
    }


    public PersistentCraftBranchProfile GetOrCreateBranchProfile(PersistentCraftProfileComponent profile, string branch)
    {
        return GetOrCreateBranchProfile(profile.BranchProgress, branch);
    }

    public PersistentCraftBranchProfile GetOrCreateBranchProfile(
        Dictionary<string, PersistentCraftBranchProfile> branches,
        string branch)
    {
        if (!branches.TryGetValue(branch, out var profile))
        {
            profile = new PersistentCraftBranchProfile();
            branches[branch] = profile;
        }

        return profile;
    }

    private PersistentCraftNodePrototype? ResolveNodePrototypeOrNull(string nodeId)
    {
        return _prototype.TryIndex<PersistentCraftNodePrototype>(nodeId, out var node)
            ? node
            : null;
    }

}
