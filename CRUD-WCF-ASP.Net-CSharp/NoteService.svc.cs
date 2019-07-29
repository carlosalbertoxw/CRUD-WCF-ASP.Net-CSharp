using System;
using System.Collections.Generic;
using Model;
using Data;
using Utilities;
using System.Diagnostics;

namespace CRUD_WCF_ASP.Net_CSharp
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Note" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select Note.svc or Note.svc.cs at the Solution Explorer and start debugging.
    public class NoteService : INoteService
    {
        private String token;
        private NoteDTO noteDTO;

        public NoteService()
        {
            token = "qwerty";
            noteDTO = new NoteDTO();
        }

        public Response Add(string authentication, Note note)
        {
            Response response = new Response();
            try
            {
                if (authentication == token)
                {
                    if (noteDTO.add(note))
                    {
                        response.Message = Message.SUCCESSFUL_SAVE;
                    }
                    else
                    {
                        response.Message = Message.ERROR_SAVE;
                    }
                }
                else
                {
                    response.Message = Message.ERROR_SESSION_VALIDATION;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
                response.Message = Message.ERROR_PROCESSING_DATA;
            }
            return response;
        }

        public Response Delete(string authentication, int id)
        {
            Response response = new Response();
            try
            {
                if (authentication == token)
                {
                    if (noteDTO.delete(id))
                    {
                        response.Message = Message.SUCCESSFUL_DELETE;
                    }
                    else
                    {
                        response.Message = Message.ERROR_DELETE;
                    }
                }
                else
                {
                    response.Message = Message.ERROR_SESSION_VALIDATION;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
                response.Message = Message.ERROR_PROCESSING_DATA;
            }
            return response;
        }

        public NoteResponse Get(string authentication, int id)
        {
            NoteResponse response = new NoteResponse();
            try
            {
                if (authentication == token)
                {
                    Note note = noteDTO.get(id);
                    if (note != null)
                    {
                        response.Note = note;
                        response.Message = "OK";
                    }
                    else
                    {
                        response.Message = Message.ERROR_CONSULT;
                    }
                }
                else
                {
                    response.Message = Message.ERROR_SESSION_VALIDATION;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
                response.Message = Message.ERROR_PROCESSING_DATA;
            }
            return response;
        }

        public NoteListResponse List(string authentication)
        {
            NoteListResponse response = new NoteListResponse();
            try
            {
                if (authentication == token)
                {
                    List<Note> list = noteDTO.list();
                    if (list != null)
                    {
                        response.List = list;
                        response.Message = "OK";
                    }
                    else
                    {
                        response.Message = Message.ERROR_CONSULT;
                    }
                }
                else
                {
                    response.Message = Message.ERROR_SESSION_VALIDATION;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
                response.Message = Message.ERROR_PROCESSING_DATA;
            }
            return response;
        }

        public Response Update(string authentication, Note note)
        {
            Response response = new Response();
            try
            {
                if (authentication == token)
                {
                    if (noteDTO.update(note))
                    {
                        response.Message = Message.SUCCESSFUL_UPDATE;
                    }
                    else
                    {
                        response.Message = Message.ERROR_UPDATE;
                    }
                }
                else
                {
                    response.Message = Message.ERROR_SESSION_VALIDATION;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
                response.Message = Message.ERROR_PROCESSING_DATA;
            }
            return response;
        }
    }
}
