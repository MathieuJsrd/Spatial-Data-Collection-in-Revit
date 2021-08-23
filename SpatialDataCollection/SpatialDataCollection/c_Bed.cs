using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpatialDataCollection
{
    public class c_Bed
    {
        // Members
        string m_id;
        string m_name;
        List<s_Edge> m_outLines;
        Level m_level;
        string m_id_hostRoom;

        // Properties
        public string Id { get { return m_id; } }
        public string Name { get { return m_name; } }
        public List<s_Edge> OutLines { get { return m_outLines; } }
        public Level Level_ { get { return m_level; } }
        public string HostRoomId { get { return m_id_hostRoom; } }

        // :: Constructor ::
        public c_Bed(FamilyInstance fi)
        {
            // -- We build a Bed Object from a FamilyInstance Revit Object
            m_id = fi.Id.ToString();
            m_name = fi.Name;
            m_level = c_Global.Doc.GetElement(fi.LevelId) as Level;

            // The Room property in FamilyInstance does not work each time
            // https://www.revitapidocs.com/2020/37944e7a-f298-9c25-20bb-9c0c1da46f41.htm

            // This is my custom function to achieve this task
            Room hostRoom = GetHostRoom(fi);
            if (hostRoom != null) m_id_hostRoom = hostRoom.Id.ToString();

            m_outLines = GetExteriorShape(fi);
        }

        Room GetHostRoom(FamilyInstance fi)
        {
            // On récupère le centre de la porte
            LocationPoint lp = fi.Location as LocationPoint;
            XYZ fiLoc = lp.Point;
            fiLoc = new XYZ(fiLoc.X, fiLoc.Y, fiLoc.Z + 2); // THIS +2 IS VERY IMPORTANT: the height from the bottom of the bed is changed slightly
                                                            // So that when we GetRoomFromPoint, we are sure to be a little higher than the ground to be sure to be in the volume of the Room

            // Explanation why loc can be null : 
            // https://forums.autodesk.com/t5/revit-api-forum/why-is-element-location-null-for-perfectly-valid-familyinstance/m-p/8076733/highlight/true#M31755

            if (fiLoc != null)
            {
                const double CONSTANT = 2.5;

                // First test : Host Room Property
                Room hostRoom = fi.Room;
                if (hostRoom != null)
                    return hostRoom;

                // Second Test : Location itself
                XYZ searchLoc = fiLoc;
                hostRoom = c_Global.Doc.GetRoomAtPoint(searchLoc, c_Global.Phase);
                if (hostRoom != null)
                    return hostRoom;

                // Third Test : Shift according Facing Vector
                // Facing Vector is a 3D vector indicating the orientation of the equipment. 
                // With this, it can be shifted towards the interior of the room if it is too extreme at the room's borders
                XYZ shiftToGoInSpace = fi.FacingOrientation;

                XYZ locRoom1 = new XYZ(
                    fiLoc.X - (shiftToGoInSpace.X * CONSTANT),
                    fiLoc.Y - (shiftToGoInSpace.Y * CONSTANT), // multiplied by CONSTANT so that the shift is sufficient to catch the Room but not too large either
                    fiLoc.Z); // No shift for Z axis (2D geometry)
                XYZ locRoom2 = new XYZ(
                    fiLoc.X + (shiftToGoInSpace.X * CONSTANT),
                    fiLoc.Y + (shiftToGoInSpace.Y * CONSTANT), // multiplied by CONSTANT so that the shift is sufficient to catch the Room but not too large either
                    fiLoc.Z); // No shift for Z axis (2D geometry)
                
                // -- New search with new locs
                Room room1 = c_Global.Doc.GetRoomAtPoint(locRoom1, c_Global.Phase);
                Room room2 = c_Global.Doc.GetRoomAtPoint(locRoom2, c_Global.Phase);

                if (room1 != null && room2 != null && room1.Id != room2.Id)
                {
                    // there a conflict with two overlapping rooms
                    return null;
                    // --> Need to clean up room's geometries
                }
                else if (room1 != null && room2 != null && room1.Id == room2.Id)
                    // Same Room -> no conflict
                    return room1;
                else if (room1 != null && room2 == null)
                    return room1;
                else if (room1 == null && room2 != null)
                    return room2;
                else
                    return null; // 3 tests --> Fail
            }
            else
            {
                // No loc -> Impossible Case
                return null;
            }
        }

        List<s_Edge> GetExteriorShape(FamilyInstance fi)
        {
            //we're gonna make a 2D rectangle off of these two 3D coordinates
            //we're gonna use the geometry property
            //https://thebuildingcoder.typepad.com/blog/2010/01/geometry-options.html
            FamilySymbol symbol = fi.Symbol;
            if (!symbol.IsActive) symbol.Activate(); //If the symbol is active the geometry is accessible  

            List<c_Point2D> tmp = new List<c_Point2D>();

            Options options = new Options(); // default option
            GeometryElement geoEl = fi.get_Geometry(options);

            foreach (GeometryObject geoObj in geoEl)
            {
                // Get the geometry instance which contains the geometry information
                GeometryInstance geoInst = geoObj as GeometryInstance;
                if (null != geoInst)
                {
                    foreach (GeometryObject instobj in geoInst.GetInstanceGeometry())
                    {
                        Solid solid = instobj as Solid;
                        if (null == solid || 0 == solid.Faces.Size || 0 == solid.Edges.Size)
                        {
                            continue;
                        }

                        // Get the faces and edges from solid, and transform the formed points
                        foreach (Face face in solid.Faces)
                        {
                            Mesh mesh = face.Triangulate();

                            foreach (XYZ ii in mesh.Vertices)
                            {
                                XYZ point = ii;
                                c_Point2D tmp1 = new c_Point2D(Math.Round(point.X, 2), Math.Round(point.Y, 2));
                                if (!tmp1.IsInList(tmp))
                                {
                                    tmp.Add(tmp1);
                                }
                            }
                        }
                    }
                }
            }

            List<s_Edge> shape = new List<s_Edge>();

            if (tmp.Count == 0) return null;

            // TAKE THE LIST OF POINTS AND OUT OF IT THE MIN AND MAX OF x AND y AND OUT OF IT A RECTANGLE
            List<c_Point2D> rectangle = MakeRectangle_w_MinMax(tmp);

            s_Edge left_edge = new s_Edge(rectangle[0], rectangle[1]);
            s_Edge up_edge = new s_Edge(rectangle[1], rectangle[2]);
            s_Edge right_edge = new s_Edge(rectangle[2], rectangle[3]);
            s_Edge down_edge = new s_Edge(rectangle[3], rectangle[0]);

            return new List<s_Edge> { left_edge, up_edge, right_edge, down_edge };
            //now have tmp coords of the family instance
        }

        List<c_Point2D> MakeRectangle_w_MinMax(List<c_Point2D> list)
        {
            List<c_Point2D> result = new List<c_Point2D>();

            double minX = Double.MaxValue, minY = Double.MaxValue;
            double maxX = Double.MinValue, maxY = Double.MinValue;

            foreach (c_Point2D p in list)
            {
                double x = p.X, y = p.Y;
                if (minX > x) minX = x;
                if (minY > y) minY = y;
                if (maxX < x) maxX = x;
                if (maxY < y) maxY = y;
            }

            // -- On a désormais les deux points en diagonale,
            // Il faut désormais comprendre ou est le 3ème point du tiangle, (en haut ou en bas ?)

            // On fait donc la projection théorique des points haut_gauche && bas_droit
            c_Point2D left_down = new c_Point2D(minX, minY), right_up = new c_Point2D(maxX, maxY);
            c_Point2D left_up = new c_Point2D(left_down.X, right_up.Y), right_down = new c_Point2D(right_up.X, left_down.Y);

            return new List<c_Point2D> { left_down, left_up, right_up, right_down };
        }
    }
}
