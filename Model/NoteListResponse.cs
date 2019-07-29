using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Model
{
    [DataContract]
    public class NoteListResponse
    {
        private List<Note> list;
        private String message;

        [DataMember]
        public List<Note> List
        {
            get { return list; }
            set { list = value; }
        }
        [DataMember]
        public String Message
        {
            get { return message; }
            set { message = value; }
        }
    }
}
