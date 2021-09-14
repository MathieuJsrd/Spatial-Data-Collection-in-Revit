using System;
using System.Reflection;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.AccessControl;
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
    public class c_Point2D
    {
        public double m_x;
        public double m_y;

        public double X { get { return m_x; } }
        public double Y { get { return m_y; } }
        
        public c_Point2D(double x, double y)
        {
            m_x = x;
            m_y = y;
        }

        public double DistanceFrom(c_Point2D p)
        {
            double dist = Math.Sqrt(Math.Pow(m_x - p.X, 2) + Math.Pow(m_y - p.Y, 2));
            return dist;
        }

        public bool Equals(c_Point2D p)
        {
            // return True if Points are Equals
            return p.X == m_x && p.Y == m_y;
        }
        public override string ToString()
        {
            string str = "(" + this.m_x.ToString("0.0000", CultureInfo.InvariantCulture) + "; " + this.m_y.ToString("0.0000", CultureInfo.InvariantCulture) + ")";
            return str;
        }
        public bool IsInList(List<c_Point2D> list)
        {
            foreach (c_Point2D point in list)
            {
                if (m_x == point.X && m_y == point.Y)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
