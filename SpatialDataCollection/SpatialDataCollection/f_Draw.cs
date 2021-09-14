using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

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
    public partial class f_Draw : System.Windows.Forms.Form
    {
        double m_minX;
        double m_minY;
        double m_maxX;
        double m_maxY;

        const double MAGNIFICATION = 2;

        Dictionary<string, c_CloneRoom> m_cloneRooms;

        public f_Draw(Dictionary<string, c_CloneRoom> cloneRooms)
        {
            InitializeComponent();

            m_cloneRooms = cloneRooms;

            // -- Get Max & Min values to display in Window Form surroundings (according Revit coordinates)
            double max_X = int.MinValue;
            double max_Y = int.MinValue;
            double min_X = int.MaxValue;
            double min_Y = int.MaxValue;
            foreach (c_CloneRoom room in cloneRooms.Values)
            {
                List<s_Edge> edges = room.ExteriorShape;

                if (edges == null) continue;

                foreach (s_Edge edge in edges)
                {
                    c_Point2D start = edge.Start;
                    c_Point2D end = edge.End;

                    if (max_X < start.X) max_X = start.X;
                    if (max_Y < start.Y) max_Y = start.Y;

                    if (max_X < end.X) max_X = end.X;
                    if (max_Y < end.Y) max_Y = end.Y;

                    if (min_X > start.X) min_X = start.X;
                    if (min_Y > start.Y) min_Y = start.Y;

                    if (min_X > end.X) min_X = end.X;
                    if (min_Y > end.Y) min_Y = end.Y;
                }
            }

            m_minX = min_X * MAGNIFICATION;
            m_minY = min_Y * MAGNIFICATION;
            m_maxX = max_X * MAGNIFICATION;
            m_maxY = max_Y * MAGNIFICATION;
        }

        private void f_Draw_Paint(object sender, PaintEventArgs e)
        {
            #region DRAW LINES

            Graphics graphic = e.Graphics;
            Pen p_blue = new Pen(Color.Blue, 2);
            Pen p_red = new Pen(Color.Red, 2);

            foreach (c_CloneRoom room in m_cloneRooms.Values)
            {
                // -- Filter only one level for clarity
                if (room.Level_.Name != m_cloneRooms.Values.First().Level_.Name) continue;  // ... ( --> display the Level of the first Room in List)

                List <s_Edge> edges = room.ExteriorShape;

                if (edges == null) continue;

                foreach (s_Edge edge in edges)
                {
                    #region Draw Room

                    c_Point2D start = edge.Start;
                    c_Point2D end = edge.End;

                    graphic.DrawLine(
                        p_blue,
                        (float)((start.X * MAGNIFICATION) + Math.Abs(m_minX) + 20),
                        (float)((start.Y * MAGNIFICATION) + Math.Abs(m_minY) + 20),
                        (float)((end.X * MAGNIFICATION) + Math.Abs(m_minX) + 20),
                        (float)((end.Y * MAGNIFICATION) + Math.Abs(m_minY)) + 20); // + 20 : shift

                    #endregion
                }

                foreach (c_Bed bed in room.Beds)
                {
                    try
                    {
                        List<s_Edge> outlines = bed.OutLines;

                        foreach (s_Edge edge in outlines)
                        {
                            c_Point2D start = edge.Start;
                            c_Point2D end = edge.End;

                            graphic.DrawLine(
                                p_red,
                                (float)((start.X * MAGNIFICATION) + Math.Abs(m_minX) + 20),
                                (float)((start.Y * MAGNIFICATION) + Math.Abs(m_minY) + 20),
                                (float)((end.X * MAGNIFICATION) + Math.Abs(m_minX) + 20),
                                (float)((end.Y * MAGNIFICATION) + Math.Abs(m_minY)) + 20); // + 20 : pour décaler un petit peu sur le coin haut gauche
                        }
                    }
                    catch
                    { }
                }

            }
            graphic.Dispose();

            #endregion
        }
    }
}
