using System.Collections.Generic;

public static class Pathfinder
{
    public struct TraversalStep
    {
        public GraphEdge Edge;
        public GraphNode ArrivalNode;
    }

    public struct PathfinderResult
    {
        public List<TraversalStep> TraversalSteps;
        public List<TraversalStep> PathSteps;
    }

    public static PathfinderResult BFS(GraphNode source, GraphNode destination)
    {
        var traversal = new List<TraversalStep>();
        var visited   = new HashSet<GraphNode> { source };
        var parent    = new Dictionary<GraphNode, (GraphEdge edge, GraphNode from)>();
        var queue     = new Queue<GraphNode>();
        queue.Enqueue(source);

        bool found = false;
        while (queue.Count > 0 && !found)
        {
            var current = queue.Dequeue();

            foreach (var edge in current.edges)
            {
                var neighbor = edge.GetOtherNode(current);
                if (neighbor == null || visited.Contains(neighbor)) continue;

                visited.Add(neighbor);
                parent[neighbor] = (edge, current);
                traversal.Add(new TraversalStep { Edge = edge, ArrivalNode = neighbor });

                if (neighbor == destination) { found = true; break; }
                queue.Enqueue(neighbor);
            }
        }

        return new PathfinderResult
        {
            TraversalSteps = traversal,
            PathSteps      = ReconstructPath(source, destination, parent)
        };
    }

    public static PathfinderResult DFS(GraphNode source, GraphNode destination)
    {
        var traversal = new List<TraversalStep>();
        var visited   = new HashSet<GraphNode> { source };
        var parent    = new Dictionary<GraphNode, (GraphEdge edge, GraphNode from)>();
        var stack     = new Stack<(GraphNode node, GraphEdge edge, GraphNode from)>();

        foreach (var edge in source.edges)
        {
            var neighbor = edge.GetOtherNode(source);
            if (neighbor != null) stack.Push((neighbor, edge, source));
        }

        while (stack.Count > 0)
        {
            var (current, edge, from) = stack.Pop();
            if (visited.Contains(current)) continue;

            visited.Add(current);
            parent[current] = (edge, from);
            traversal.Add(new TraversalStep { Edge = edge, ArrivalNode = current });

            if (current == destination) break;

            foreach (var e in current.edges)
            {
                var neighbor = e.GetOtherNode(current);
                if (neighbor != null && !visited.Contains(neighbor))
                    stack.Push((neighbor, e, current));
            }
        }

        return new PathfinderResult
        {
            TraversalSteps = traversal,
            PathSteps      = ReconstructPath(source, destination, parent)
        };
    }

    private static List<TraversalStep> ReconstructPath(
        GraphNode source, GraphNode destination,
        Dictionary<GraphNode, (GraphEdge edge, GraphNode from)> parent)
    {
        var path = new List<TraversalStep>();
        var node = destination;

        while (node != source)
        {
            if (!parent.TryGetValue(node, out var info)) return new List<TraversalStep>();
            path.Add(new TraversalStep { Edge = info.edge, ArrivalNode = node });
            node = info.from;
        }

        path.Reverse();
        return path;
    }
}
