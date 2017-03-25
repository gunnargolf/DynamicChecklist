﻿using DynamicChecklist.Graph.Edges;
using DynamicChecklist.Graph.Vertices;
using Microsoft.Xna.Framework;
using QuickGraph;
using QuickGraph.Graphviz.Dot;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicChecklist.Graph
{
    public class PartialGraph : StardewGraph
    {
        public MovableVertex PlayerVertex { get; private set; }
        public MovableVertex TargetVertex { get; private set; }

        public List<PlayerEdge> PlayerEdges { get; private set; }
        //public List<StardewEdge> TargetEdges { get; private set; } // only connected from outside the partial graph

        public GameLocation Location { get; private set; }

        public PartialGraph(GameLocation location) : base()
        {
            Location = location;
        }
        public void Populate()
        {
            var vertexToInclude = new List<WarpVertex>();
            var warps = Location.warps;
            for (int i = 0; i < warps.Count; i++)
            {
                var warp = warps.ElementAt(i);
                var vertexNew = new WarpVertex(Location, new Vector2(warp.X, warp.Y), Game1.getLocationFromName(warp.TargetName), new Vector2(warp.TargetX, warp.TargetY));
                bool shouldAdd = true;
                foreach (WarpVertex extWarpIncluded in vertexToInclude)
                {
                    if (vertexNew.TargetLocation == extWarpIncluded.TargetLocation && StardewVertex.Distance(vertexNew, extWarpIncluded) < 5)
                    {
                        shouldAdd = false;
                        break;
                    }
                }
                if (shouldAdd)
                {
                    vertexToInclude.Add(vertexNew);
                    
                    AddVertex(vertexToInclude.Last());
                }

            }
            for (int i = 0; i < vertexToInclude.Count; i++)
            {
                var vertex1 = vertexToInclude.ElementAt(i);


                for (int j = 0; j < vertexToInclude.Count; j++)
                {
                    var LocTo = Game1.getLocationFromName(Location.warps.ElementAt(j).TargetName);
                    var vertex2 = vertexToInclude.ElementAt(j);
                    var path = PathFindController.findPath(new Point((int)vertex1.Position.X, (int)vertex1.Position.Y), new Point((int)vertex2.Position.X, (int)vertex2.Position.Y), new PathFindController.isAtEnd(PathFindController.isAtEndPoint), Location, Game1.player, 9999);
                    // TODO Use Pathfinder distance
                    double dist;
                    string edgeLabel;
                    if (path != null)
                    {
                        dist = (float)path.Count;
                        // TODO Player can run diagonally. Account for that.
                        edgeLabel = Location.Name + " - " + dist + "c";

                    }
                    else
                    {
                        dist = (int)StardewVertex.Distance(vertex1, vertex2);
                        edgeLabel = Location.Name + " - " + dist + "d";
                    }
                    var edge = new StardewEdge(vertex1, vertex2, edgeLabel);
                    AddEdge(edge);

                }
                AddVertex(vertex1);
            }
            AddPlayerVertex(new MovableVertex(Location, new Vector2(0,0)));
            AddTargetVertex(new MovableVertex(Location, new Vector2(0, 0)));
            ConnectPlayerVertex();
        }
        private void AddPlayerVertex(MovableVertex vertex)
        {
            if (PlayerVertex == null)
            {
                AddVertex(vertex);
                PlayerVertex = vertex;
            }
            else
            {
                throw new InvalidOperationException("Player vertex already added");
            }
        }
        private void ConnectPlayerVertex()
        {
            PlayerEdges = new List<PlayerEdge>();
            foreach (StardewVertex vertex in this.Vertices)
            {
                if(vertex != PlayerVertex)
                {
                    var newEdge = new PlayerEdge(PlayerVertex, vertex);
                    AddEdge(newEdge);
                    PlayerEdges.Add(newEdge);
                }
            }
        }
        [Obsolete] // Maybe needed later for pathfinder calculation
        public void UpdatePlayerEdgeCosts(Vector2 position)
        {
            PlayerVertex.SetPosition(position);
            foreach(PlayerEdge edge in PlayerEdges)
            {
                
            }
        }

        private void AddTargetVertex(MovableVertex w)
        {
            if (TargetVertex == null)
            {
                AddVertex(w);
                TargetVertex = w;
            }
            else
            {
                throw new InvalidOperationException("Target vertex already added");
            }
        }
        public class ExtendedWarp : Warp
        {
            public GameLocation OriginLocation;
            public GameLocation TargetLocation;
            public string Label;

            public ExtendedWarp(Warp w, GameLocation originLocation) : base(w.X, w.Y, w.TargetName, w.TargetX, w.TargetY, false)
            {
                this.OriginLocation = originLocation;
                TargetLocation = Game1.getLocationFromName(w.TargetName);
                this.Label = originLocation.name + " to " + w.TargetName;
            }

            public static bool AreCorresponding(ExtendedWarp warp1, ExtendedWarp warp2)
            {
                if (warp1.OriginLocation == warp2.TargetLocation && warp1.TargetLocation == warp2.OriginLocation)
                {
                    if (Math.Abs(warp1.X - warp2.TargetX) + Math.Abs(warp1.Y - warp2.TargetY) < 5)
                    {
                        return true;
                    }
                }
                return false;
            }
            public static int Distance(ExtendedWarp warp1, ExtendedWarp warp2)
            {
                return Math.Abs(warp1.X - warp2.X) + Math.Abs(warp1.Y - warp2.Y);
            }
        }
    }
}