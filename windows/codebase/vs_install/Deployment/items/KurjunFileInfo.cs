using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Deployment.items
{
    /// <summary>
    /// KurjunFileInfo
    /// </summary>
    public class KurjunFileInfo
    {
        /// <summary>
        /// Gets or sets the file MD5 sum.
        /// </summary>
        /// <value>
        /// The file MD5 sum.
        /// </value>
        public string md5Sum { get; set; }
        /// <summary>
        /// Gets or sets the file name.
        /// </summary>
        /// <value>
        /// The file name.
        /// </value>
        public string name { get; set; }
        /// <summary>
        /// Gets or sets the file size.
        /// </summary>
        /// <value>
        /// The file size.
        /// </value>
        public int size { get; set; }
        /// <summary>
        /// Gets or sets the fingerprint.
        /// </summary>
        /// <value>
        /// The file fingerprint.
        /// </value>
        public string fingerprint { get; set; }
        /// <summary>
        /// Gets or sets the file version.
        /// </summary>
        /// <value>
        /// The file version.
        /// </value>
        public string version { get; set; }
        /// <summary>
        /// Gets or sets the file identifier.
        /// </summary>
        /// <value>
        /// The file identifier.
        /// </value>
        public string id { get; set; }
    }
}
