using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace LocalNicoMyList.nicoApi
{
    public class Error
    {
        /// <summary>エラーコード</summary>
        [XmlElement]
        public string code;

        /// <summary>エラーの説明</summary>
        [XmlElement]
        public string description;
    }
}
