using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.ProcessPower.DataLinks;
using Autodesk.ProcessPower.DataObjects;
using Autodesk.ProcessPower.PlantInstance;
using Autodesk.ProcessPower.ProjectManager;
using SupportSpanCheck.Models;
using System;
using Autodesk.ProcessPower.PnP3dObjects;
using System.Collections.Generic;
using System.Linq;

namespace SupportSpanCheck.Services
{
    public class SupportData
    {
        public ObjectId Id { get; set; }
        public Point3d Position { get; set; }
        public int RowId { get; set; }
        public string LineNumber { get; set; } = "";
        public Extents3d Extents { get; set; }
    }

    public class LineGeom
    {
        public Extents3d Extents { get; set; }
        public string LineNumber { get; set; } = "";
        public string PipeSize { get; set; } = "";
        public double InsulationThickness { get; set; }
        public List<Tuple<Point3d, Point3d, double>> InternalEdges { get; set; } = new List<Tuple<Point3d, Point3d, double>>();
    }

    public class PortNode
    {
        public Point3d Position { get; set; }
        public List<GraphEdge> Edges { get; set; } = new List<GraphEdge>();
        public SupportData? AttachedSupport { get; set; }
    }

    public class GraphEdge
    {
        public PortNode Target { get; set; } = null!;
        public double Distance { get; set; }
    }

    public class PipeGraph
    {
        public List<PortNode> Nodes { get; set; } = new List<PortNode>();

        public PortNode GetOrAddNode(Point3d pos)
        {
            foreach (var n in Nodes)
            {
                if (n.Position.DistanceTo(pos) < 0.1) return n;
            }
            var newNode = new PortNode { Position = pos };
            Nodes.Add(newNode);
            return newNode;
        }

        public void AddEdge(Point3d p1, Point3d p2, double distance = 0)
        {
            if (p1.DistanceTo(p2) < 0.1 && distance == 0) return;
            double dist = distance > 0 ? distance : p1.DistanceTo(p2);
            var n1 = GetOrAddNode(p1);
            var n2 = GetOrAddNode(p2);
            n1.Edges.Add(new GraphEdge { Target = n2, Distance = dist });
            n2.Edges.Add(new GraphEdge { Target = n1, Distance = dist });
        }

        public double GetShortestPathDistance(SupportData startSupp, SupportData endSupp)
        {
            var startNode = GetOrAddNode(startSupp.Position);
            var endNode = GetOrAddNode(endSupp.Position);

            var distances = new Dictionary<PortNode, double>();
            var queue = new HashSet<PortNode>();

            foreach (var n in Nodes)
            {
                distances[n] = double.MaxValue;
                queue.Add(n);
            }
            distances[startNode] = 0;

            while (queue.Count > 0)
            {
                var u = queue.OrderBy(n => distances[n]).First();
                queue.Remove(u);

                if (distances[u] == double.MaxValue) break;
                if (u == endNode) return distances[u];

                foreach (var edge in u.Edges)
                {
                    if (!queue.Contains(edge.Target)) continue;
                    double alt = distances[u] + edge.Distance;
                    if (alt < distances[edge.Target])
                    {
                        distances[edge.Target] = alt;
                    }
                }
            }
            return double.MaxValue;
        }
    }

    public static class Plant3DService
    {
        public static List<SupportSpanItem> ScanSupports(IEnumerable<SpanStandard> standards, out bool hasMissingSize, out int orphanCount)
        {
            hasMissingSize = false;
            orphanCount = 0;
            var results = new List<SupportSpanItem>();
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return results;

            var db = doc.Database;
            var ed = doc.Editor;

            using (doc.LockDocument())
            using (var tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    DataLinksManager dlm = null;
                    try
                    {
                        PlantProject currentProj = PlantApplication.CurrentProject;
                        if (currentProj != null)
                        {
                            Project pipingProj = currentProj.ProjectParts["Piping"];
                            if (pipingProj != null)
                            {
                                dlm = pipingProj.DataLinksManager;
                            }
                        }
                    }
                    catch { }

                    if (dlm == null)
                    {
                        dlm = DataLinksManager.GetManager(db);
                    }

                    if (dlm == null)
                    {
                        ed.WriteMessage("\nKhông tìm thấy DataLinksManager. Đảm bảo bản vẽ là Plant 3D project drawing.");
                        return results;
                    }

                    PnPDatabase pnpDb = dlm.GetPnPDatabase();

                    var lineInsulation = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
                    try
                    {
                        var groupTables = new string[] { "P3dLineGroup", "PipingLineGroup" };
                        foreach (var tableName in groupTables)
                        {
                            var t = pnpDb.Tables[tableName];
                            if (t != null && t.Columns.Contains("InsulationThickness"))
                            {
                                foreach (PnPRow r in t.Select())
                                {
                                    double thk = 0;
                                    if (double.TryParse(r["InsulationThickness"]?.ToString(), out thk) && thk > 0)
                                    {
                                        string name = t.Columns.Contains("Name") ? r["Name"]?.ToString() ?? "" : "";
                                        string tag = t.Columns.Contains("Tag") ? r["Tag"]?.ToString() ?? "" : "";
                                        string number = t.Columns.Contains("Number") ? r["Number"]?.ToString() ?? "" : "";

                                        if (!string.IsNullOrEmpty(name)) lineInsulation[name] = thk;
                                        if (!string.IsNullOrEmpty(tag)) lineInsulation[tag] = thk;
                                        if (!string.IsNullOrEmpty(number)) lineInsulation[number] = thk;
                                    }
                                }
                            }
                        }
                    }
                    catch { }

                    var allSupports = new List<SupportData>();
                    var lineGeometries = new List<LineGeom>();

                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForRead);
                    foreach (ObjectId objId in btr)
                    {
                        if (objId.IsNull || objId.IsErased) continue;

                        Entity? ent = tr.GetObject(objId, OpenMode.ForRead) as Entity;
                        if (ent == null) continue;

                        if (!(ent is Part) && !(ent is BlockReference) && !(ent is Autodesk.ProcessPower.PnP3dObjects.Support)) continue;

                        int rowId = 0;
                        try
                        {
                            rowId = dlm.FindAcPpRowId(objId);
                        }
                        catch
                        {
                            // Ignore ObjectDoesNotHaveLink exception
                            continue;
                        }

                        if (rowId <= 0) continue;

                        PnPRow row = pnpDb.GetRow(rowId);
                        if (row == null || row.Table == null) continue;

                        // Identify if it's a support
                        bool isSupport = row.Table.Name.IndexOf("Support", StringComparison.OrdinalIgnoreCase) >= 0;
                        if (!isSupport)
                        {
                            try 
                            {
                                if (row.Table.Columns.Contains("Class") && row["Class"]?.ToString() == "Support")
                                    isSupport = true;
                                else if (row.Table.Columns.Contains("PartCategory") && row["PartCategory"]?.ToString() == "Support")
                                    isSupport = true;
                            }
                            catch { }
                        }

                        string lineNumber = string.Empty;
                        try 
                        {
                            lineNumber = row["LineNumberTag"]?.ToString() ?? "";
                        }
                        catch { }

                        if (string.IsNullOrEmpty(lineNumber) || lineNumber == "?") 
                        {
                            lineNumber = "Unassigned";
                        }

                        string size = string.Empty;
                        try 
                        {
                            size = row["Size"]?.ToString() ?? "";
                            if (string.IsNullOrEmpty(size)) size = row["NominalDiameter"]?.ToString() ?? "";
                        }
                        catch { }

                        double insThk = 0;
                        if (lineInsulation.TryGetValue(lineNumber, out double thk2))
                        {
                            insThk = thk2;
                        }
                        else
                        {
                            try
                            {
                                object val = row["InsulationThickness"];
                                if (val != null) double.TryParse(val.ToString(), out insThk);
                            }
                            catch { }
                        }

                        if (isSupport)
                        {
                            Point3d pos = Point3d.Origin;
                            try
                            {
                                var ext = ent.GeometricExtents;
                                pos = new Point3d(
                                    (ext.MinPoint.X + ext.MaxPoint.X) / 2,
                                    (ext.MinPoint.Y + ext.MaxPoint.Y) / 2,
                                    (ext.MinPoint.Z + ext.MaxPoint.Z) / 2
                                );
                            }
                            catch
                            {
                                continue;
                            }

                            allSupports.Add(new SupportData { 
                                Id = objId, 
                                Position = pos, 
                                RowId = rowId, 
                                LineNumber = lineNumber,
                                Extents = ent.GeometricExtents
                            });
                        }
                        else if (lineNumber != "Unassigned")
                        {
                            try
                            {
                                var geom = new LineGeom { Extents = ent.GeometricExtents, LineNumber = lineNumber, PipeSize = size, InsulationThickness = insThk };
                                Part part = ent as Part;
                                if (part != null)
                                {
                                    var ports = part.GetPorts(PortType.All);
                                    if (ports != null && ports.Count >= 2)
                                    {
                                        var ptList = ports.Cast<Port>().ToList();
                                        if (ptList.Count == 2)
                                        {
                                            double dot = ptList[0].Direction.DotProduct(ptList[1].Direction);
                                            if (dot > 0.95) // U-Bend / Return Bend
                                            {
                                                double dia = ptList[0].Position.DistanceTo(ptList[1].Position);
                                                double arcLength = Math.PI * (dia / 2.0);
                                                geom.InternalEdges.Add(new Tuple<Point3d, Point3d, double>(ptList[0].Position, ptList[1].Position, arcLength));
                                            }
                                            else
                                            {
                                                var p0 = GetIntersection(ptList[0].Position, -ptList[0].Direction, ptList[1].Position, -ptList[1].Direction);
                                                if (p0.HasValue && p0.Value.DistanceTo(ptList[0].Position) < 5000.0) 
                                                {
                                                    geom.InternalEdges.Add(new Tuple<Point3d, Point3d, double>(ptList[0].Position, p0.Value, 0));
                                                    geom.InternalEdges.Add(new Tuple<Point3d, Point3d, double>(p0.Value, ptList[1].Position, 0));
                                                }
                                                else
                                                {
                                                    geom.InternalEdges.Add(new Tuple<Point3d, Point3d, double>(ptList[0].Position, ptList[1].Position, 0));
                                                }
                                            }
                                        }
                                        else if (ptList.Count > 2)
                                        {
                                            Point3d? center = null;
                                            for (int i = 0; i < ptList.Count && !center.HasValue; i++)
                                            {
                                                for (int j = i + 1; j < ptList.Count && !center.HasValue; j++)
                                                {
                                                    center = GetIntersection(ptList[i].Position, -ptList[i].Direction, ptList[j].Position, -ptList[j].Direction);
                                                    if (center.HasValue && center.Value.DistanceTo(ptList[i].Position) > 5000.0) center = null;
                                                }
                                            }
                                            
                                            if (center.HasValue)
                                            {
                                                foreach (var pt in ptList)
                                                {
                                                    geom.InternalEdges.Add(new Tuple<Point3d, Point3d, double>(pt.Position, center.Value, 0));
                                                }
                                            }
                                            else
                                            {
                                                for (int i = 0; i < ptList.Count; i++)
                                                {
                                                    for (int j = i + 1; j < ptList.Count; j++)
                                                    {
                                                        geom.InternalEdges.Add(new Tuple<Point3d, Point3d, double>(ptList[i].Position, ptList[j].Position, 0));
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                lineGeometries.Add(geom);
                            }
                            catch { }
                        }
                    }

                    // Resolve Unassigned supports and Snap all supports to centerline
                    foreach (var supp in allSupports)
                    {
                        double bestDist = double.MaxValue;
                        Tuple<Point3d, Point3d, double> bestEdge = null;
                        Point3d bestProj = supp.Position;
                        LineGeom bestGeom = null;

                        foreach (var geom in lineGeometries)
                        {
                            if (supp.LineNumber != "Unassigned" && supp.LineNumber != geom.LineNumber) continue;

                            foreach (var edge in geom.InternalEdges)
                            {
                                Point3d proj = ProjectPointOnSegment(supp.Position, edge.Item1, edge.Item2);
                                double dist = proj.DistanceTo(supp.Position);
                                if (dist < bestDist && dist < 500.0) // Must be within 500mm
                                {
                                    bestDist = dist;
                                    bestEdge = edge;
                                    bestProj = proj;
                                    bestGeom = geom;
                                }
                            }
                        }

                        if (bestEdge != null)
                        {
                            if (supp.LineNumber == "Unassigned") supp.LineNumber = bestGeom.LineNumber;

                            bestGeom.InternalEdges.Remove(bestEdge);
                            
                            double customLen = bestEdge.Item3;
                            if (customLen > 0 && bestEdge.Item1.DistanceTo(bestEdge.Item2) > 0.1)
                            {
                                double totalLen = bestEdge.Item1.DistanceTo(bestEdge.Item2);
                                double ratio1 = bestEdge.Item1.DistanceTo(bestProj) / totalLen;
                                bestGeom.InternalEdges.Add(new Tuple<Point3d, Point3d, double>(bestEdge.Item1, bestProj, customLen * ratio1));
                                bestGeom.InternalEdges.Add(new Tuple<Point3d, Point3d, double>(bestProj, bestEdge.Item2, customLen * (1 - ratio1)));
                            }
                            else
                            {
                                bestGeom.InternalEdges.Add(new Tuple<Point3d, Point3d, double>(bestEdge.Item1, bestProj, 0));
                                bestGeom.InternalEdges.Add(new Tuple<Point3d, Point3d, double>(bestProj, bestEdge.Item2, 0));
                            }

                            supp.Position = bestProj;
                        }
                        else
                        {
                            orphanCount++;
                        }
                    }

                    // Group by LineNumber
                    var supportsByLine = new Dictionary<string, List<SupportData>>();
                    foreach (var supp in allSupports)
                    {
                        if (!supportsByLine.ContainsKey(supp.LineNumber))
                            supportsByLine[supp.LineNumber] = new List<SupportData>();
                        supportsByLine[supp.LineNumber].Add(supp);
                    }

                    int index = 1;

                    foreach (var kvp in supportsByLine)
                    {
                        string lineNum = kvp.Key;
                        var supports = kvp.Value;

                        if (supports.Count < 2) continue;

                        var graph = new PipeGraph();
                        foreach (var geom in lineGeometries.Where(g => g.LineNumber == lineNum))
                        {
                            foreach (var edge in geom.InternalEdges)
                            {
                                graph.AddEdge(edge.Item1, edge.Item2, edge.Item3);
                            }
                        }

                        var validSupports = new List<SupportData>();
                        foreach (var supp in supports)
                        {
                            var node = graph.GetOrAddNode(supp.Position);
                            node.AttachedSupport = supp;
                            validSupports.Add(supp);
                        }

                        if (validSupports.Count < 2) continue;

                        var spans = ExtractSpansUsingGraph(validSupports, graph);

                        var geoms = lineGeometries.Where(g => g.LineNumber == lineNum && !string.IsNullOrEmpty(g.PipeSize)).ToList();
                        
                        // Extract base sizes (e.g. "100x50" -> "100") and find the most frequent one
                        var validSizes = geoms.Select(g => g.PipeSize.Split('x', 'X')[0].Trim())
                                              .Where(s => !string.IsNullOrEmpty(s))
                                              .ToList();
                        
                        string lineSize = "";
                        if (validSizes.Count > 0)
                        {
                            lineSize = validSizes.GroupBy(s => s).OrderByDescending(g => g.Count()).First().Key;
                        }
                        
                        double maxInsThk = geoms.Count > 0 ? geoms.Max(g => g.InsulationThickness) : 0;
                        bool isInsulated = maxInsThk > 0;

                        bool isFound;
                        double maxSpan = SizeConverter.GetMaxSpan(lineSize, isInsulated, standards, out isFound);
                        if (!isFound && !string.IsNullOrEmpty(lineSize)) hasMissingSize = true;

                        int nodeCounter = 1;
                        var nodeLabels = new Dictionary<SupportData, int>();

                        foreach (var span in spans)
                        {
                            if (!nodeLabels.ContainsKey(span.Item1)) nodeLabels[span.Item1] = nodeCounter++;
                            if (!nodeLabels.ContainsKey(span.Item2)) nodeLabels[span.Item2] = nodeCounter++;

                            double actualSpan = span.Item3;

                            results.Add(new SupportSpanItem
                            {
                                Index = index++,
                                LineNumber = lineNum,
                                SpanLabel = $"{nodeLabels[span.Item1]} -> {nodeLabels[span.Item2]}",
                                PipeSize = lineSize,
                                InsulationThickness = maxInsThk,
                                MaxSpan = maxSpan,
                                ActualSpan = actualSpan,
                                IsViolated = actualSpan > maxSpan,
                                Support1Id = span.Item1.Id,
                                Support2Id = span.Item2.Id
                            });
                        }
                    }

                    tr.Commit();
                }
                catch (Exception ex)
                {
                    ed.WriteMessage($"\nLỗi khi quét Support: {ex.Message}");
                }
            }

            return results.OrderBy(r => r.LineNumber).ThenBy(r => r.SpanLabel).ToList();
        }

        private static Point3d? GetIntersection(Point3d p1, Vector3d d1, Point3d p2, Vector3d d2)
        {
            Vector3d cross = d1.CrossProduct(d2);
            if (cross.Length < 1e-5) return null;

            Vector3d p2p1 = p2 - p1;
            double t1 = p2p1.CrossProduct(d2).DotProduct(cross) / cross.DotProduct(cross);
            return p1 + d1 * t1;
        }

        private static Point3d ProjectPointOnSegment(Point3d pt, Point3d a, Point3d b)
        {
            Vector3d ab = b - a;
            double lenSq = ab.DotProduct(ab);
            if (lenSq < 1e-6) return a;
            double t = (pt - a).DotProduct(ab) / lenSq;
            t = Math.Max(0.0, Math.Min(1.0, t));
            return a + ab * t;
        }

        private class Edge
        {
            public SupportData Node1 { get; set; } = null!;
            public SupportData Node2 { get; set; } = null!;
            public double Distance { get; set; }
        }

        private static List<Tuple<SupportData, SupportData, double>> ExtractSpansUsingGraph(List<SupportData> supports, PipeGraph graph)
        {
            var edges = new List<Edge>();
            for (int i = 0; i < supports.Count; i++)
            {
                for (int j = i + 1; j < supports.Count; j++)
                {
                    double dist = graph.GetShortestPathDistance(supports[i], supports[j]);
                    if (dist < double.MaxValue)
                    {
                        edges.Add(new Edge
                        {
                            Node1 = supports[i],
                            Node2 = supports[j],
                            Distance = dist
                        });
                    }
                }
            }

            edges = edges.OrderBy(e => e.Distance).ToList();

            var parent = new Dictionary<SupportData, SupportData>();
            foreach (var s in supports) parent[s] = s;

            SupportData Find(SupportData s)
            {
                if (parent[s] != s)
                    parent[s] = Find(parent[s]);
                return parent[s];
            }

            void Union(SupportData s1, SupportData s2)
            {
                parent[Find(s1)] = Find(s2);
            }

            var mstEdges = new List<Edge>();
            foreach (var edge in edges)
            {
                if (Find(edge.Node1) != Find(edge.Node2))
                {
                    Union(edge.Node1, edge.Node2);
                    mstEdges.Add(edge);
                }
            }

            var spans = new List<Tuple<SupportData, SupportData, double>>();
            foreach (var e in mstEdges)
            {
                spans.Add(new Tuple<SupportData, SupportData, double>(e.Node1, e.Node2, e.Distance));
            }

            return spans;
        }

        public static void ZoomAndHighlight(ObjectId id1, ObjectId id2)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var ed = doc.Editor;
            var db = doc.Database;

            using (doc.LockDocument())
            using (var tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    Entity? ent1 = tr.GetObject(id1, OpenMode.ForRead) as Entity;
                    Entity? ent2 = tr.GetObject(id2, OpenMode.ForRead) as Entity;

                    if (ent1 != null && ent2 != null)
                    {
                        // 1. Highlight
                        ObjectId[] ids = new ObjectId[] { id1, id2 };
                        ed.SetImpliedSelection(ids);

                        // 2. Zoom Extents using COM
                        Extents3d ext = ent1.GeometricExtents;
                        ext.AddExtents(ent2.GeometricExtents);

                        double offset = 500.0;
                        Point3d minPt = new Point3d(ext.MinPoint.X - offset, ext.MinPoint.Y - offset, ext.MinPoint.Z - offset);
                        Point3d maxPt = new Point3d(ext.MaxPoint.X + offset, ext.MaxPoint.Y + offset, ext.MaxPoint.Z + offset);

                        dynamic acadApp = Application.AcadApplication;
                        acadApp.ZoomWindow(
                            new double[] { minPt.X, minPt.Y, minPt.Z },
                            new double[] { maxPt.X, maxPt.Y, maxPt.Z }
                        );
                    }
                    tr.Commit();
                }
                catch (Exception ex)
                {
                    ed.WriteMessage($"\nLỗi khi Zoom/Highlight: {ex.Message}");
                }
            }
        }
    }
}
