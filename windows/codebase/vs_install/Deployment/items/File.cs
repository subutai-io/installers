using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Deployment.items
{
    class File
    {
        private int[] md5Sum;
        private string name;

        public string Md5Sum
        {
            get
            {
                string md5 = "";
                foreach (var el in md5Sum)
                {
                    md5 += string.Format("{0:x}", el);
                }
                return md5;
            }
        }

        public string Name
        {
            get { return name; }
        }
    }
}
