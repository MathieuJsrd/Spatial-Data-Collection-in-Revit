using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using System.Windows.Forms;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.Attributes;

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

// By Mathieu Josserand : mathieu.josserand@inex.fr

// Set Up your Project for Revit :
// Part 1 : https://archi-lab.net/building-revit-plug-ins-with-visual-studio-part-one/
// Part 2 : https://archi-lab.net/building-revit-plug-ins-with-visual-studio-part-two/

namespace SpatialDataCollection
{
    [Transaction(TransactionMode.Manual)]
    class c_Start : IExternalCommand
    {
        //  :: Main function ::
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // -- Please, consider Main Document is the Architect Model containing <Room Object>
            Document architecteDocument = commandData.Application.ActiveUIDocument.Document;
            
            List<Room> rooms = GetAllRevitRooms(architecteDocument);
            
            // Need to assign these values as public 
            c_Global.Doc = architecteDocument;
            c_Global.Phase = architecteDocument.GetElement(rooms.First().get_Parameter(BuiltInParameter.ROOM_PHASE).AsElementId()) as Phase;

            Dictionary<string, c_CloneRoom> cloneRooms = ConvertRevitRoomsIntoCloneRooms(rooms);

            // -- Collect all Beds in model
            List<c_Bed> beds = GetAllBeds();

            // -- Assign Beds to CloneRooms (update CloneRoom objects)
            cloneRooms = UpdateCloneRoomsWithDoorsWindowsFurnitures(cloneRooms, beds);

            // --> Now, we have all the room shape data !!

            // Let's display this on Window Form
            f_Draw f = new f_Draw(cloneRooms);
            f.Show();


            MessageBox.Show("Success");
            return Result.Succeeded;
        }

        #region Functions

        List<Room> GetAllRevitRooms(Document doc)
        {
            List<Room> returnedList = new List<Room>();
            FilteredElementCollector collection = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Rooms);
            foreach(Element e in collection)
            {
                // Revit presents so many exceptions, that we have to make some controls before adding a <Room>
                try 
                {
                    if (e == null) continue;
                    Room r = e as Room;
                    // -- Need to check that the loc is valid object
                    XYZ loc = ((LocationPoint)e.Location).Point;
                    // -- Need to collect BB to make sure that the room is valid object
                    BoundingBoxXYZ bb = r.get_BoundingBox(doc.ActiveView); 
                    XYZ max = bb.Max;
                    XYZ min = bb.Min; 
                    // -- valid room !
                    returnedList.Add(r);
                }
                catch { continue; /*not valid object*/ }
            }
            return returnedList;
        }

        /// <summary>
        /// Convert Revit 'Room' into 'CloneRoom' is important because it's at this stage that we collect the Room Exterior Shape
        /// </summary>
        /// <param name="rooms"></param>
        /// <returns></returns>
        Dictionary<string, c_CloneRoom> ConvertRevitRoomsIntoCloneRooms(List<Room> rooms)
        {
            Dictionary<string, c_CloneRoom> cloneRooms = new Dictionary<string, c_CloneRoom>();
            foreach (Room room in rooms)
            {
                c_CloneRoom cr = new c_CloneRoom(room);
                cloneRooms.Add(cr.Id, cr);
            }
            return cloneRooms;
        }

        List<FamilyInstance> GetAllValidFurnituresOfOneType(BuiltInCategory bic)
        {
            List<FamilyInstance> furnitures = new List<FamilyInstance>();

            FilteredElementCollector furnitureCollection = new FilteredElementCollector(c_Global.Doc).OfCategory(bic);
            
            foreach (Element e in furnitureCollection)
            {
                // Same as <Rooms>, there're a lot of invalid Objects, so we need to make some checks
                try
                {
                    FamilyInstance fi = e as FamilyInstance;
                    LocationPoint lp = fi.Location as LocationPoint;
                    XYZ center = lp.Point;
                    FamilySymbol fs = fi.Symbol;
                    if (center != null)
                        furnitures.Add(fi); // --> valid object
                }
                catch { continue; /* not valid object */ }
            }
            return furnitures;
        }

        List<c_Bed> GetAllBeds()
        {
            List<c_Bed> all_beds = new List<c_Bed>();

            // 1- Get Valid Furniture (Beds are revit family Furnitures)
            List<FamilyInstance> fi_beds = GetAllValidFurnituresOfOneType(BuiltInCategory.OST_Furniture);

            // 2- Watch out : in OST_furniture there are not only bed objects, so we have to filter them !
            List<string> familyAccepted = new List<string> { "lit", "bed" }; // lit in french, bed in english -> according your family names

            foreach (FamilyInstance fi in fi_beds)
            {
                // 3 -- Apply Filter ...
                if (!fi.Name.ToLower().Contains("lit") 
                    && !fi.Name.ToLower().Contains("bed")) continue;

                // 4-- Build New Element C# Constructor : get our own <Bed> object with id, level, exterior shape
                all_beds.Add(new c_Bed(fi));
            }
            return all_beds;
        }

        Dictionary<string, c_CloneRoom> UpdateCloneRoomsWithDoorsWindowsFurnitures(
        Dictionary<string, c_CloneRoom> rooms,
        List<c_Bed> beds)
        {
                foreach (c_Bed bed in beds)
                {
                    string hostRoomId = bed.HostRoomId;

                    if (hostRoomId != null) rooms[hostRoomId].Beds.Add(bed);
                }
                return rooms;
        }

        #endregion
    }
}
