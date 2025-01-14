using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO.VectorTiles.Tiles;

namespace NetTopologySuite.IO.VectorTiles.Tilers
{
    internal static class PolygonTiler
    {
        /// <summary>
        /// Returns all the tiles this polygon is part of and the polygonal portion in it..
        /// </summary>
        /// <param name="polygon">The linestring.</param>
        /// <param name="zoom">The zoom.</param>
        /// <param name="margin">The margin for the tiles polygon (in %)</param>
        /// <returns>An enumerable of all tiles.</returns>
        public static IEnumerable<(ulong, IPolygonal)> Tiles(Polygon polygon, int zoom, int margin = 5)
        {
            // Get the envelope
            var envelope = polygon.EnvelopeInternal;
            var lt = Tile.CreateAroundLocation(envelope.MaxY, envelope.MinX, zoom);
            if (lt == null) throw new Exception();
            var rb = Tile.CreateAroundLocation(envelope.MinY, envelope.MaxX, zoom);
            if (rb == null) throw new Exception();

            // Compute the possible tile range
            var tileRange = new TileRange(lt.X, lt.Y, rb.X,rb.Y, zoom);

            // Build a prepared geometry to perform faster intersection predicate
            var prep = Geometries.Prepared.PreparedGeometryFactory.Prepare(polygon);

            // Test polygon tiles.
            foreach (var tile in tileRange)
            {
                if (tile == null) continue;
                var testPolygon = tile.ToPolygon(margin);
                
                if (prep.Contains(testPolygon))
                {
                    yield return (tile.Id, testPolygon);
                }
                else if (prep.Intersects(testPolygon))
                {
                    Geometry result;

                    try
                    {
                        // Compute the intersection geometry
                        result = polygon.Intersection(testPolygon);
                    }
                    catch (Exception ex)
                    {
                        //NTS sometimes throws error for non-noded linestring when calculating intersections of polygons.
                        //This is resolved in a pre-release version of NTS.
                        //https://github.com/NetTopologySuite/NetTopologySuite/discussions/530#discussioncomment-888410
                        System.Diagnostics.Debug.WriteLine(ex.Message + "\nSkipping polygon clipping.");
                        result = polygon;
                    }

                    // Only return if result is polygonal
                    if (result is IPolygonal polygonalResult)
                        yield return (tile.Id, polygonalResult);
                }
            }
        }
    }
}
