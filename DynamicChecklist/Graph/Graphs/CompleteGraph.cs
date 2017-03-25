﻿using DynamicChecklist.Graph.Vertices;
using Microsoft.Xna.Framework;
using QuickGraph.Algorithms.Observers;
using QuickGraph.Algorithms.ShortestPath;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicChecklist.Graph.Graphs
{
    public class CompleteGraph : StardewGraph
    {
        public List<PartialGraph> PartialGraphs { get; private set; } = new List<PartialGraph>();
        private List<GameLocation> gameLocations;
        private DijkstraShortestPathAlgorithm<StardewVertex, StardewEdge> dijkstra;
        private VertexDistanceRecorderObserver<StardewVertex, StardewEdge> distObserver;
        private VertexPredecessorRecorderObserver<StardewVertex, StardewEdge> predecessorObserver;

        public CompleteGraph(List<GameLocation> gameLocations)
        {
            this.gameLocations = gameLocations;
        }
        public void Populate()
        {
            foreach(GameLocation location in gameLocations)
            {
                var partialGraph = new PartialGraph(location);
                partialGraph.Populate();
                PartialGraphs.Add(partialGraph);
            }
            foreach (PartialGraph pgSource in PartialGraphs)
            {
                foreach (PartialGraph pgTarget in PartialGraphs)
                {
                    if (pgSource != pgTarget)
                    {
                        ConnectPartialGraph(pgSource, pgTarget);
                    }                   
                }
            }
            foreach(PartialGraph partialGraph in PartialGraphs)
            {
                AddVertexRange(partialGraph.Vertices);
                AddEdgeRange(partialGraph.Edges);
            }

            Func<StardewEdge, double> edgeCost = (x) => x.Cost;          
            dijkstra = new DijkstraShortestPathAlgorithm<StardewVertex, StardewEdge>(this, edgeCost);
            distObserver = new VertexDistanceRecorderObserver<StardewVertex, StardewEdge>(edgeCost);
            distObserver.Attach(dijkstra);
            predecessorObserver = new VertexPredecessorRecorderObserver<StardewVertex, StardewEdge>();
            predecessorObserver.Attach(dijkstra);

        }
        private void ConnectPartialGraph(PartialGraph pgSource, PartialGraph pgTarget)
        {
            foreach (StardewVertex vertex in pgSource.Vertices)
            {
                if (vertex is WarpVertex)
                {
                    var warpVertex = (WarpVertex)vertex;
                    if (warpVertex.TargetLocation == pgTarget.Location)
                    {
                        var newVertex = new StardewVertex(pgTarget.Location, warpVertex.TargetPosition);
                        pgTarget.AddVertex(newVertex);
                        var newEdge = new StardewEdge(vertex, newVertex, "Partial graph connection");
                        pgSource.AddEdge(newEdge);
                        foreach (StardewVertex targetVertex in pgTarget.Vertices)
                        {
                            // Player vertex only needs to connect away from itself, all warp vertices and the target vertex must have an edge going to them
                            if(targetVertex != pgTarget.PlayerVertex) 
                            {
                                var e = new StardewEdge(newVertex, targetVertex, $"From {newVertex.Location} to {targetVertex.Location}");
                                pgTarget.AddEdge(e);
                            }
                        }
                    }

                }
            }
        }
        public List<Step> CalculatePathToTarget(GameLocation sourceLocation, GameLocation targetLocation)
        {
            var partialGraphs = FindPartialGraph(sourceLocation);
            var playerVertex = partialGraphs.PlayerVertex;

            dijkstra.Compute(playerVertex);
            // TODO Figure out return type
            // TODO Fix bug in graph creation
            var b = distObserver;
            var c = predecessorObserver;

            var targetVertex = FindPartialGraph(targetLocation).TargetVertex;
            var path = (IEnumerable<StardewEdge>)(new List<StardewEdge>());
            var success = predecessorObserver.TryGetPath(targetVertex, out path);
            var pathSimple = new List<Step>();
            if (success)
            {
                foreach(var pathPart in path)
                {
                    pathSimple.Add(new Step(pathPart.Source.Location, pathPart.Source.Position));
                }
                return pathSimple;
            }
            else
            {
                return null;
            }
        }
        public void SetTargetLocation(GameLocation location, Vector2 position)
        {
            FindPartialGraph(location).TargetVertex.SetPosition(position);
        }
        public void SetPlayerLocation(GameLocation location, Vector2 position)
        {
            FindPartialGraph(location).PlayerVertex.SetPosition(position);
        }
        private PartialGraph FindPartialGraph(GameLocation loc)
        {
            foreach (PartialGraph p in PartialGraphs)
            {
                if(p.Location == loc)
                {
                    return p;
                }
            }
            throw new Exception();
        }
    }
}
public struct Step
{
    public GameLocation Location { get; }
    public Vector2 Position { get; }
    public Step(GameLocation location, Vector2 position)
    {
        Location = location;
        Position = position;
    }
}