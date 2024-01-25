using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GH_Co_Pilot_Library
{
    internal class RequiresTests
    {
        //a method to create a random filename
        public string GetRandomFileName()
        {
            return System.IO.Path.GetRandomFileName();
        }

        //a method to get the current date
        public DateTime GetCurrentDate()
        {
            return DateTime.Now;
        }


    }
}
