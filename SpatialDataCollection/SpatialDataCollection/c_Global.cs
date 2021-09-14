using Autodesk.Revit.DB;
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
    public static class c_Global
    {
        // Public Access in whole project

        public static Document Doc;
        public static Phase Phase;
    }

    public struct s_Edge
    {
        // :: Constructors ::
        public s_Edge(XYZ start, XYZ end) // from Revit (3D) points
        {
            Start = new c_Point2D(start.X, start.Y);
            End = new c_Point2D(end.X, end.Y);
            Middle = new c_Point2D((start.X + end.X) / 2, (start.Y + end.Y) / 2);
        }
        public s_Edge(c_Point2D start, c_Point2D end) // from 2D points
        {
            Start = start;
            End = end;
            Middle = new c_Point2D((start.X + end.X) / 2, (start.Y + end.Y) / 2);
        }
        public s_Edge(Line line) // from Revit Line
        {
            Start = new c_Point2D(line.Tessellate().First().X, line.Tessellate().First().Y);
            End = new c_Point2D(line.Tessellate().Last().X, line.Tessellate().Last().Y);
            Middle = new c_Point2D((Start.X + End.X) / 2, (Start.Y + End.Y) / 2);
        }

        // Attributs
        public c_Point2D Start { get; }
        public c_Point2D End { get; }
        public c_Point2D Middle { get; }

        public override string ToString()
        {
            return "s_Edge : Start : " + Start.ToString() + "; End : " + End.ToString();
        }
    }
}
