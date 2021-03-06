﻿using System;
using System.Collections.Generic;

namespace CommonTypes.Calendar
{
    /// <summary>
    /// Class for calendar.
    /// </summary>
    public class Calendar : ICalendar
    {
        private List<Interval> calendar;

        public Calendar(List<Interval> intervallist)
        {
            calendar = new List<Interval>(intervallist);
            calendar.Sort();
        }

        public void AddIntervals(List<Interval> intervallist)
        {
            calendar.AddRange(intervallist);
            calendar.Sort();
        }

        /// <summary>
        /// возвращаем true, если попали хоть в один из интервалов календаря;
        /// false - иначе;
        /// дополнительно возвращаем индекс интервала (-1 означает, что ни в какой из интервалов не попали)
        /// </summary>
        public bool IsInterval(DateTime T, out int intervalindex)
        {
            for (int i = 0; i < calendar.Count; i++)
            {
                DateTime starttime = calendar[i].GetStartTime();
                DateTime endtime = calendar[i].GetEndTime();
                if ((T >= starttime) && (T <= endtime))
                { intervalindex = i; return true; }
            }

            intervalindex = -1;
            return false;
        }

        public bool WillRelease(DateTime T, TimeSpan t, int intervalindex)
        {
            bool isRelease = true;
            TimeSpan intervallasting = calendar[intervalindex].GetEndTime() - T;
            TimeSpan tmptime = t;

            if (t <= intervallasting) return true;

            while (tmptime > intervallasting)
            {
                tmptime = tmptime - intervallasting;
                //if (intervalindex == calendar.Count - 1) intervalindex = 0;
                intervalindex += 1;
                if (intervalindex < calendar.Count)
                {
                    intervallasting = calendar[intervalindex].GetEndTime() - calendar[intervalindex].GetStartTime();
                }
                else
                {
                    isRelease = false;
                }
            }
            return isRelease;
        }

        /// <summary>
        /// вернуть индекс интервала в календаре, в который попадает заданное время T
        /// если не попадает ни в один из интервалов - найти индекс ближайшего возможного
        /// flag = true, если попали в интервал false - иначе
        /// </summary>       
        public int FindInterval(DateTime T, out bool flag)
        {
            if (T < calendar[0].GetStartTime())
            {
                flag = false;
                return 0;
            }

            for (int j = 0; j < calendar.Count; j++)
            {
                DateTime starttime = calendar[j].GetStartTime();
                DateTime endtime = calendar[j].GetEndTime();
                if ((T >= starttime) && (T <= endtime)) { flag = true; return j; }
            }

            for (int i = 0; i < calendar.Count-1; i++)
                if ((T > calendar[i].GetEndTime()) && (T < calendar[i + 1].GetStartTime())) { flag = false; return i + 1; }
            
            flag = false;
            return -1;
        }

        /// <summary>
        /// Вернуть время, в которое закончится выполнение операции;
        /// </summary>
        /// <param name="T">Время начала операции o</param>
        /// <param name="t">Длительность операции</param>
        /// <param name="intervalindex">Индекс интервала в календаре</param>
        public DateTime GetTimeofRelease(DateTime T, TimeSpan t, int intervalindex)
        {
            TimeSpan intervallasting = calendar[intervalindex].GetEndTime() - T;
            TimeSpan tmptime = t;

            if (t <= intervallasting) return T + t;

            while (tmptime > intervallasting)
            {
                tmptime = tmptime - intervallasting;
                //if (intervalindex == calendar.Count - 1) intervalindex = 0;
                intervalindex += 1;
                intervallasting = calendar[intervalindex].GetEndTime() - calendar[intervalindex].GetStartTime();
            }
            return calendar[intervalindex].GetStartTime() + tmptime;
        }

        /// <summary>
        /// вернуть время ближайшего возможного времени начала выполнения операции
        /// </summary> 
        /// <param name="T">Время начала операции o</param>
        public DateTime GetNearestStart(DateTime T)
        {
            bool flag;
            int index = FindInterval(T, out flag);
            if (flag == false)
                return calendar[index].GetStartTime();
            else return T;
        }
        
        public Interval GetInterval(int index)
        {
            return calendar[index];
        }

        public TimeSpan GetTimeInTwentyFourHours()
        {
            TimeSpan hours=new TimeSpan(0,0,0);
            DateTime flag = new DateTime(calendar[0].GetStartTime().Year, calendar[0].GetStartTime().Month, calendar[0].GetStartTime().Day);
            foreach (Interval i in calendar)
            {
                if ((i.GetEndTime().Year == flag.Year) && (i.GetEndTime().Month == flag.Month) && (i.GetEndTime().Day == flag.Day))
                {
                    hours = hours + (i.GetEndTime() - i.GetStartTime());
                }
                else
                {
                    if ((i.GetStartTime().Year == flag.Year) && (i.GetStartTime().Month == flag.Month) && (i.GetStartTime().Day == flag.Day))
                    {
                        hours = hours + ((flag + new TimeSpan(1,0,0,0)) - i.GetStartTime());
                    }
                    break;
                }
            }
            return hours;
        }

        public TimeSpan GetRealWorkTime(DateTime startTime, DateTime endTime, out bool inFirstInterval,out bool inLastInterval)
        {
            TimeSpan hours = new TimeSpan(0, 0, 0);
            int firstInterval = FindInterval(startTime, out inFirstInterval);
            int lastInterval = FindInterval(endTime, out inLastInterval);
            if ((lastInterval == -1)&&(!(inLastInterval))) { lastInterval = calendar.Count - 1; }
            if (( startTime > calendar[calendar.Count-1].GetEndTime()) || (endTime < calendar[0].GetStartTime()))
            {
                return hours;
            }
            if (inFirstInterval)
            {
                hours = hours + (calendar[firstInterval].GetEndTime() - startTime);
            }
            else
            {
                hours = hours + (calendar[firstInterval].GetEndTime() - calendar[firstInterval].GetStartTime());
            }
            for (int i = firstInterval+1; i < lastInterval;i++)
            {
                hours = hours + (calendar[i].GetEndTime() - calendar[i].GetStartTime());
            }
            if (inLastInterval)
            {
                hours = hours + (endTime - calendar[lastInterval].GetStartTime());
            }
            else
            {
               
                hours = hours + (calendar[lastInterval].GetEndTime() - calendar[lastInterval].GetStartTime());
            }
            return hours;
        }
    }
}
