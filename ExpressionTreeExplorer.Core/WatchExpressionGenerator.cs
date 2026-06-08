using System.Collections.Generic;

namespace ExpressionTreeExplorer.Core;

public static class WatchExpressionGenerator
{
    public static string Generate(ExpressionNode root, string targetPath)
    {
        if (root.Path == targetPath)
            return string.Empty;

        var segments = new List<string>();
        if (!CollectSegments(root, targetPath, segments))
            return string.Empty;

        return "." + string.Join(".", segments);
    }

    private static bool CollectSegments(ExpressionNode node, string targetPath, List<string> segments)
    {
        foreach (var child in node.Children)
        {
            if (child.Path == targetPath || targetPath.StartsWith(child.Path + "/"))
            {
                if (!string.IsNullOrEmpty(child.RelationToParent))
                    segments.Add(child.RelationToParent);

                if (child.Path == targetPath)
                    return true;

                return CollectSegments(child, targetPath, segments);
            }
        }

        return false;
    }
}
