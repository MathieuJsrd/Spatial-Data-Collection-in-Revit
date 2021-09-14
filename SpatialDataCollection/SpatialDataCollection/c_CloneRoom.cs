using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

﻿/*
 .----------------.  .-----------------. .----------------.  .----------------.   .----------------.  .----------------.  .----------------. 
| .--------------. || .--------------. || .--------------. || .--------------. | | .--------------. || .--------------. || .--------------. |
| |     _____    | || | ____  _____  | || |  _________   | || |  ____  ____  | | | |   ______     | || |  _________   | || |  _________   | |
| |    |_   _|   | || ||_   \|_   _| | || | |_   ___  |  | || | |_  _||_  _| | | | |  |_   _ \    | || | |_   ___  |  | || | |  _   _  |  | |
| |      | |     | || |  |   \ | |   | || |   | |_  \_|  | || |   \ \  / /   | | | |    | |_) |   | || |   | |_  \_|  | || | |_/ | | \_|  | |
| |      | |     | || |  | |\ \| |   | || |   |  _|  _   | || |    > `' <    | | | |    |  __'.   | || |   |  _|  _   | || |     | |      | |
| |     _| |_    | || | _| |_\   |_  | || |  _| |___/ |  | || |  _/ /'`\ \_  | | | |   _| |__) |  | || |  _| |___/ |  | || |    _| |_     | |
| |    |_____|   | || ||_____|\____| | || | |_________|  | || | |____||____| | | | |  |_______/   | || | |_________|  | || |   |_____|    | |
| |              | || |              | || |              | || |              | | | |              | || |              | || |              | |
| '--------------' || '--------------' || '--------------' || '--------------' | | '--------------' || '--------------' || '--------------' |
 '----------------'  '----------------'  '----------------'  '----------------'   '----------------'  '----------------'  '----------------' 
*/
// by Mathieu Josserand : mathieu.josserand@inex.fr

namespace SpatialDataCollection
{
    /*
     * This class is made to manager more easily the revit Room Objects with own properties
     */

    public class c_CloneRoom
    {
        // Members :
        string m_id;
        string m_name;
        Room m_room;
        Level m_level;
        List<s_Edge> m_exteriorShape;
        List<List<s_Edge>> m_interiorShapes;
        double m_area;

        List<c_Bed> m_beds;

        public string Id { get { return m_id; } }
        public string Name { get { return m_name; } }
        public Room Room { get { return m_room; } }
        public Level Level_ { get { return m_level; } }
        public List<s_Edge> ExteriorShape { get { return m_exteriorShape; } }
        public List<List<s_Edge>> InteriorShapes { get { return m_interiorShapes; } }
        public double Area { get { return m_area; } }

        public List<c_Bed> Beds { get { return m_beds; } set { m_beds = value; } }

        public c_CloneRoom (Room room)
        {
            // -- Init member values
            m_id = room.Id.ToString();
            m_name = room.get_Parameter(BuiltInParameter.ROOM_NAME).AsString();
            m_room = room;
            m_level = room.Level;
            m_area = UnitUtils.ConvertFromInternalUnits(room.Area, DisplayUnitType.DUT_SQUARE_METERS); // feet² en m²

            m_beds = new List<c_Bed>();

            // -- Assign : Exterior Shape && Interior Shape
            GetRoomShapesFromGroundFace(room);
        }

        void GetRoomShapesFromGroundFace(Room room)
        {
            m_interiorShapes = new List<List<s_Edge>>();

            IList<IList<BoundarySegment>> boundaries = room.GetBoundarySegments(new SpatialElementBoundaryOptions());

            // First IList corresponds to EXTERIOR LOOP :
            IList<BoundarySegment> exteriorLoop = boundaries.First();
            CurveLoop shapeLoop = new CurveLoop();

            foreach (BoundarySegment exteriorLine in exteriorLoop)
            {
                Curve curve = exteriorLine.GetCurve();
                shapeLoop.Append(curve);
            }
            // Assign exterior lines to Member
            m_exteriorShape = GetSeperationLines(shapeLoop);


            // Next ILists correspond to INTERIOR LOOPS
            foreach (IList<BoundarySegment> boundary in boundaries)
            {
                if (boundary == boundaries.First()) continue; // ignore first loop (exterior loop)
                CurveLoop interiorLoop = new CurveLoop();
                foreach (BoundarySegment interiorLine in boundary)
                {
                    Curve curve = interiorLine.GetCurve();
                    interiorLoop.Append(curve);
                }
                // Add interior lines to Member
                m_interiorShapes.Add(GetSeperationLines(interiorLoop));
            }
        }

        List<s_Edge> GetSeperationLines(CurveLoop intermediaryShape)
        {
            List<s_Edge> sepLines = new List<s_Edge>();
            foreach (Curve curve in intermediaryShape)
            {
                Line roomLine = null;
                Arc roomArc = null;

                // Detect if the curve is a Line or an Arc
                if (curve.Clone().ToString().Contains("Line"))
                    roomLine = curve as Line; //room line
                else
                    roomArc = curve as Arc; //room arc

                // Add to List the Curve (Arc/Line)
                if (roomLine != null && roomArc == null)
                {
                    if (roomLine != null)
                        sepLines.Add(new s_Edge(roomLine));
                }
                else if (roomArc != null && roomLine == null)
                {
                    // ARC CASE : To make easier : transform arcs into 2 distinct lines in which intersection point is the most higher point on the arc
                    XYZ arcMiddleXYZ = roomArc.Tessellate()[roomArc.Tessellate().Count / 2];
                    sepLines.Add(new s_Edge(roomArc.Tessellate().First(), arcMiddleXYZ));
                    sepLines.Add(new s_Edge(arcMiddleXYZ, roomArc.Tessellate().Last()));
                }
                else throw new Exception("IMPOSSIBLE CASE : curve is neither a Line nor an Arc.");
            }
            return sepLines;
        }

    }
}
