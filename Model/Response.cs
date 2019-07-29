using System;
using System.Runtime.Serialization;

namespace Model
{
    [DataContract]
    public class Response
    {
        private String message;

        [DataMember]
        public String Message
        {
            get { return message; }
            set { message = value; }
        }
    }
}
