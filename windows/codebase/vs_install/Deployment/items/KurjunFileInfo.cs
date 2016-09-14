using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using DevExpress.Utils.Drawing.Helpers;

namespace Deployment.items
{
    public class KurjunFileInfo
    {
        public string md5Sum { get; set; }
        public string name { get; set; }
        public int size { get; set; }
        public string fingerprint { get; set; }
        public string version { get; set; }
        public string id { get; set; }
    }
}
