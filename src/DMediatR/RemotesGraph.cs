using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.Msagl.Miscellaneous;

namespace DMediatR
{
    /// <summary>
    /// Interface for registering a RemotesGraph class.
    /// </summary>
    public interface IRemotesGraph
    {
        Task<string> GetSvgAsync();

        string GetSvg(RemotesGraphRequest request);
    }

    /// <summary>
    /// Implementation for /remotes.svg with a minimalistic heuristic MSAGL
    /// renderer. Replace the RemotesGraph service with a class inheriting from
    /// this and overwriting GetSvg() with an embellished renderer.
    /// </summary>
    public class RemotesGraph : IRemotesGraph
    {
        private readonly IMediator _mediator;

        public RemotesGraph(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task<string> GetSvgAsync()
        {
            var request = await _mediator.Send(new RemotesGraphRequest());
            return GetSvg(request);
        }

        public virtual string GetSvg(RemotesGraphRequest request)
        {
            // Modeled after WriteToSvgSample in the MSAGL Samples.

            // Create the graph with its native Nodes and Edges using the purely
            // declarative ones from RemotesGraphRequest
            var graph = new Graph();
            foreach (var node in request.Nodes)
            {
                graph.AddNode(new Node(node.Id));
            }
            foreach (var edge in request.Edges)
            {
                graph.AddEdge(edge.SourceId, edge.Label, edge.TargetId);
            }
            graph.CreateGeometryGraph();

            // Draw the graph.
            foreach (var n in graph.Nodes)
            {
                // MSAGL Samples comment:
                // Ideally we should look at the drawing node attributes, and figure out, the required node size
                // I am not sure how to find out the size of a string rendered in SVG. Here, we just blindly assign to each node a rectangle with width 60 and height 40, and round its corners.
                // DMediatR heuristics:
                var width = n.Id.Length * 12;
                var height = 40;
                n.GeometryNode.BoundaryCurve = CurveFactory.CreateRectangleWithRoundedCorners(width, height, 3, 2, new Point(0, 0));
                n.Label.Width = n.Width * 0.6;
                n.Label.Height = 40;

                // init geometry labels as well
                foreach (var de in graph.Edges)
                {
                    // again setting the dimensions, that should depend on Drawing.Label and the viewer, blindly
                    de.Label.GeometryLabel.Width = de.Label.Text.Length * 12;
                    de.Label.GeometryLabel.Height = 60;
                    de.Attr.ArrowheadAtSource = ArrowStyle.None;
                    de.Attr.ArrowheadAtTarget = ArrowStyle.Normal;
                }
            }
            LayoutHelpers.CalculateLayout(graph.GeometryGraph, new SugiyamaLayoutSettings(), null);

            // Create the SVG from the graph.
            // The result is astonishingly nondeterministic.
            var ms = new MemoryStream();
            var writer = new StreamWriter(ms);
            var svgWriter = new SvgGraphWriter(writer.BaseStream, graph);
            svgWriter.Write();
            // get the string from MemoryStream
            ms.Position = 0;
            var sr = new StreamReader(ms);
            return sr.ReadToEnd();
        }
    }
}