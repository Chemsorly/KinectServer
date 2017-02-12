using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Post_KNV_MessageClasses
{
    /// <summary>
    /// the request object sent by the application to the client
    /// </summary>
    public class AppRequestObject
    {
        /// <summary>
        /// currently not used since the answer is broadcasted via SignalR
        /// </summary>
        public String clientAddress { get; set; }
    }
}
