using System;
using System.Runtime.Serialization;

namespace Model
{
    [DataContract]
    public class Note
    {
        [DataMember]
        public Int32 Id { get; set; }
        [DataMember]
        public String Title { get; set; }
        [DataMember]
        public String Text { get; set; }
    }
}
