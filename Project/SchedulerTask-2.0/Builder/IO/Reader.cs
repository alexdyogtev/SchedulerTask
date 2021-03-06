using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using CommonTypes.Party;
using CommonTypes.Operation;
using CommonTypes.Equipment;
using CommonTypes.Calendar;

namespace Builder.IO
{
    public static class Reader
    {
        ///// <summary>
        ///// Список для хранения партий
        ///// </summary>
        //private static List<IParty> partys;

        ///// <summary>
        ///// 
        ///// </summary>
        //private static Dictionary<int, IOperation> operations;

        /// <summary>
        /// 
        /// </summary>
        private static Dictionary<int, IEquipment> equipments;

        private static XDocument sdata;
        private static XDocument tdata;
        private static DateTime begin;
        //private static DateTime end;
        private static string path;
        private static string datapattern = "dd.MM.yyyy";
        //private static string dtpattern = "MM.dd.yyyy H:mm:ss";
        //private static Dictionary<int, IEquipment> eqdic;
        //private static Dictionary<int, IOperation> opdic;
        //private static List<IParty> partlist;
        private static XNamespace df;
        private static ICalendar calendar;


        ///// <summary>
        ///// констуктор ридера
        ///// </summary>
        ///// <param name="folderPath"> Путь к .xml файлам.</param>
        //public Reader(string folderPath)
        //{

        //    sdata = XDocument.Load(folderPath + "system.xml");
        //    tdata = XDocument.Load(folderPath + "tech.xml");
        //}

        public static void SetFolderPath(string folderPath)
        {
            path = folderPath;
            try
            {
                sdata = XDocument.Load(folderPath + "system.xml");
            }
            catch (Exception e)
            {
                throw new Exception("По указанному пути не найден файл system.xml");
            }

            try
            {
                tdata = XDocument.Load(folderPath + "tech.xml");
            }
            catch (Exception e)
            {
                throw new Exception("По указанному пути не найден файл tech.xml");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="partys"> Список партий</param>
        /// <param name="operations">Список операций</param>
        /// <param name="equipments_">Список оборудований</param>
        public static void ReadData(out List<IParty> partys, out Dictionary<int, IOperation> operations, out Dictionary<int, IEquipment> equipments_)
        {
            partys = null;
            operations = null;
            equipments = null;

            List<Interval> intlist = new List<Interval>();
            List<Interval> doneintlist = new List<Interval>();
            equipments = new Dictionary<int, IEquipment>();

            DateTime start = new DateTime();
            DateTime end = new DateTime();
            XElement root = sdata.Root;
            df = sdata.Root.Name.Namespace;

            foreach (XElement elm in root.Descendants(df + "CalendarInformation"))
            {
                if (elm.Attribute("date_begin") != null)
                {
                    string date = elm.Attribute("date_begin").Value;
                    DateTime.TryParseExact(date, datapattern, null, System.Globalization.DateTimeStyles.None, out start);
                }
                if (elm.Attribute("date_end") != null)
                {
                    string date = elm.Attribute("date_end").Value;
                    DateTime.TryParseExact(date, datapattern, null, System.Globalization.DateTimeStyles.None, out end);
                }
                foreach (XElement eg in elm.Elements(df + "EquipmentGroup"))
                {
                    foreach (XElement inc in eg.Elements(df + "Include"))
                    {
                        DateTime tmpdata = start;
                        while (tmpdata != end)
                        {
                            if ((int)tmpdata.DayOfWeek == int.Parse(inc.Attribute("day_of_week").Value))
                            {
                                int ind = inc.Attribute("time_period").Value.IndexOf("-");
                                int sh = int.Parse(inc.Attribute("time_period").Value.Substring(0, 1));
                                int eh = int.Parse(inc.Attribute("time_period").Value.Substring(ind + 1, 2));

                                intlist.Add(new Interval(new DateTime(tmpdata.Year, tmpdata.Month, tmpdata.Day, sh, 0, 0), new DateTime(tmpdata.Year, tmpdata.Month, tmpdata.Day, eh, 0, 0)));
                            }
                            tmpdata = tmpdata.AddDays(1);
                        }
                    }
                    foreach (XElement exc in eg.Elements(df + "Exclude"))
                    {
                        foreach (Interval t in intlist)
                        {
                            if ((int)t.GetStartTime().DayOfWeek == int.Parse(exc.Attribute("day_of_week").Value))
                            {
                                int ind = exc.Attribute("time_period").Value.IndexOf("-");
                                int sh = int.Parse(exc.Attribute("time_period").Value.Substring(0, 2));
                                int eh = int.Parse(exc.Attribute("time_period").Value.Substring(ind + 1, 2));

                                DateTime dt = t.GetStartTime().AddHours(-t.GetStartTime().Hour);
                                Interval tmpint;
                                doneintlist.Add(SeparateInterval(t, dt.AddHours(sh), dt.AddHours(eh), out tmpint));
                                doneintlist.Add(tmpint);
                            }
                        }
                    }
                }
            }

            calendar = new Calendar(doneintlist);
            
            
            foreach (XElement elm in root.Descendants(df + "EquipmentInformation").Elements(df + "EquipmentGroup"))
            {
                ReadEquipment(elm, null);
            }

            root = tdata.Root;
            df = root.Name.Namespace;
            partys = new List<IParty>();
           
            operations = new Dictionary<int, IOperation>();
            List<IOperation> tmpOperationsp;
            foreach (XElement product in root.Descendants(df + "Product"))
            {
                foreach (XElement part in product.Elements(df + "Part"))
                {
                    DateTime.TryParseExact(part.Attribute("date_begin").Value, datapattern, null, System.Globalization.DateTimeStyles.None, out begin);
                    DateTime.TryParseExact(part.Attribute("date_end").Value, datapattern, null, System.Globalization.DateTimeStyles.None, out end);
                    Party parent = new Party(begin, end, int.Parse(part.Attribute("priority").Value), part.Attribute("name").Value, int.Parse(part.Attribute("num_products").Value));
                    tmpOperationsp = ReadOperations(part, parent, operations);
                    foreach (IOperation op in tmpOperationsp)
                    {
                        parent.AddOperationToForParty(op);
                    }
                    foreach (XElement subpart in part.Elements(df + "SubPart"))
                    {
                        Party sp = new Party(subpart.Attribute("name").Value, int.Parse(subpart.Attribute("num_products").Value));
                        tmpOperationsp = ReadOperations(subpart, parent, operations);
                        foreach (IOperation op in tmpOperationsp)
                        {
                            sp.AddOperationToForParty(op);
                        }
                        parent.AddSubParty(sp);
                        //partys.Add(sp);
                    }
                    partys.Add(parent);
                }
            }
            equipments_ = equipments;
        }

        private static void ReadEquipment(XElement group, GroupEquipment parent)
        {
            GroupEquipment tmp = null;
            if (parent == null)
            {
               tmp = new GroupEquipment(calendar, int.Parse(group.Attribute("id").Value), -1, group.Attribute("name").Value);
            }
            
            else
            {
                tmp = new GroupEquipment(calendar, int.Parse(group.Attribute("id").Value), parent.GetId(), group.Attribute("name").Value);
                parent.AddEquipment(tmp);
            }
            equipments.Add(tmp.GetId(), tmp);

            foreach (XElement sgroup in group.Elements(df + "EquipmentGroup")) ReadEquipment(sgroup, tmp);
                
            foreach (XElement eq in group.Elements(df + "Equipment"))
            {
                SingleEquipment stmp = new SingleEquipment(calendar, int.Parse(eq.Attribute("id").Value), tmp.GetId(), eq.Attribute("name").Value);
                equipments.Add(stmp.GetId(), stmp);
                tmp.AddEquipment(stmp);
            }
        }

        private static List<IOperation> ReadOperations(XElement part, Party parent, Dictionary<int, IOperation> opdic)
        {
            Dictionary<int, List<int>> pop = new Dictionary<int, List<int>>();
            List<IOperation> tmpop = new List<IOperation>();
            foreach (XElement oper in part.Elements(df + "Operation"))
            {
                List<int> tmp_ = new List<int>();
                if (oper.Elements(df + "Previous") != null)
                {
                    foreach (XElement prop in oper.Elements(df + "Previous"))
                    {
                        tmp_.Add(int.Parse(prop.Attribute("id").Value));
                    }
                    pop.Add(int.Parse(oper.Attribute("id").Value), tmp_);
                }
                int id = int.Parse(oper.Attribute("id").Value);
                int duration = int.Parse(oper.Attribute("duration").Value);
                int group = int.Parse(oper.Attribute("equipmentgroup").Value);
                string name = oper.Attribute("name").Value;
                TimeSpan duration_t=new TimeSpan(duration, 0, 0);
                IEquipment equipment_ = equipments[group];
                Operation tmp = new Operation(id, name,duration_t , new List<IOperation>(), equipment_, parent);
                tmpop.Add(tmp);
                opdic.Add(id, tmp);
            }
            foreach (IOperation o in tmpop)
            {
                if (pop[o.GetId()]!=null)
                {
                    for(int i = 0; i < pop[o.GetId()].Count; i++)
                    {
                        o.AddPrevOperation(opdic[pop[o.GetId()][i]]);
                    }
                }
            }
            return tmpop;
        }
        
        private static Interval SeparateInterval(Interval ii, DateTime start, DateTime end, out Interval oi)
        {
            oi = new Interval(end, ii.GetEndTime());
            return new Interval(ii.GetStartTime(), start);
        }
        
        public static void UpdateCalendars(DateTime start_data, DateTime end_data)
        {
            List<Interval> intlist = new List<Interval>();
            List<Interval> doneintlist = new List<Interval>();
            sdata.Save(path + "system.xml");
            sdata = XDocument.Load(path + "system.xml");
            df = sdata.Root.Name.Namespace;
            XElement root = sdata.Root;
            XElement elm = XElement.Parse(root.FirstNode.ToString());
            XElement eg = XElement.Parse(elm.FirstNode.ToString());
            IEnumerable<XElement> l = eg.Elements();
            if (l.First().Name == df + "Include") { }
            IEnumerable<XElement> l1 = l.Elements(df + "Include");
            foreach (XElement inc in eg.Elements(df + "Include"))
            {
                DateTime tmpdata = start_data;
                while ((tmpdata.Day != end_data.Day) || (tmpdata.Month != end_data.Month) || (tmpdata.Year != end_data.Year)) //при старой проверке из-за несовпадения часов был бесконечный цикл и вылет за границы
                {
                    if ((int)tmpdata.DayOfWeek == int.Parse(inc.Attribute("day_of_week").Value))
                    {
                        int ind = inc.Attribute("time_period").Value.IndexOf("-");
                        int sh = int.Parse(inc.Attribute("time_period").Value.Substring(0, 1));
                        int eh = int.Parse(inc.Attribute("time_period").Value.Substring(ind + 1, 2));

                        intlist.Add(new Interval(new DateTime(tmpdata.Year, tmpdata.Month, tmpdata.Day, sh, 0, 0), new DateTime(tmpdata.Year, tmpdata.Month, tmpdata.Day, eh, 0, 0)));
                    }
                    tmpdata = tmpdata.AddDays(1);
                }
            }
            foreach (XElement exc in eg.Elements(df + "Exclude"))
            {
                foreach (Interval t in intlist)
                {
                    if ((int)t.GetStartTime().DayOfWeek == int.Parse(exc.Attribute("day_of_week").Value))
                    {
                        int ind = exc.Attribute("time_period").Value.IndexOf("-");
                        int sh = int.Parse(exc.Attribute("time_period").Value.Substring(0, 2));
                        int eh = int.Parse(exc.Attribute("time_period").Value.Substring(ind + 1, 2));

                        DateTime dt = t.GetStartTime().AddHours(-t.GetStartTime().Hour);
                        Interval tmpint;
                        doneintlist.Add(SeparateInterval(t, dt.AddHours(sh), dt.AddHours(eh), out tmpint));
                        doneintlist.Add(tmpint);
                    }
                }
            }

            foreach (XElement eq in root.Descendants(df + "Equipment"))
            {
                equipments[int.Parse(eq.Attribute("id").Value)].GetCalendar().AddIntervals(doneintlist);
            }
        }
    }
}
