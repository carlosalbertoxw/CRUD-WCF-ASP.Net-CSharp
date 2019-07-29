using Model;
using System;
using System.ServiceModel;

namespace CRUD_WCF_ASP.Net_CSharp
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "INote" in both code and config file together.
    [ServiceContract]
    public interface INoteService
    {
        [OperationContract]
        Response Delete(String authentication, Int32 id);

        [OperationContract]
        Response Update(String authentication, Note note);

        [OperationContract]
        NoteResponse Get(String authentication, Int32 id);

        [OperationContract]
        Response Add(String authentication, Note note);

        [OperationContract]
        NoteListResponse List(String authentication);
    }
}
