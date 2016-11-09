using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace hypster_voice.Controllers
{
    public class getResultController : Controller
    {
        //
        // GET: /getResult/

        //
        // GET: /getResult/

        public string Index()
        {
            string CURR_COMMAND = "";


            System.Runtime.Caching.ObjectCache i_chache = System.Runtime.Caching.MemoryCache.Default;
            if (i_chache["Member_Voice_"] != null)
            {
                CURR_COMMAND = (string)i_chache["Member_Voice_"];
            }

            return "C:" + CURR_COMMAND;
        }




        


    }
}
