using System;
using System.Runtime.Serialization;

namespace Model
{
    [DataContract]
    public class NoteResponse
    {
        private Note note;
        private String message;

        [DataMember]
        public Note Note
        {
            get { return note; }
            set { note = value; }
        }
        [DataMember]
        public String Message
        {
            get { return message; }
            set { message = value; }
        }
    }
}
