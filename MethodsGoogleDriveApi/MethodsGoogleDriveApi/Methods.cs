using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Requests;
using Google.Apis.Upload;
using MimeTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MethodsGoogleDriveApi
{
    public class Methods
    {
        public static List<string> FileList(DriveService service)
        {
            var request = service.Files.List();
            request.Fields = "files(id, name)";
            var result = request.Execute();
            var files = result.Files;

            if (files != null && files.Any())
            {
                var list = new List<string>();
                foreach (var item in files)
                {
                    list.Add(item.Name);
                    list.Add(item.Id);
                }
                return list;
            }
            else
                return null;
        }

        //Salvando ou Atualizando arquivos ja existentes localmente
        //~Se o arquivo não existe é criado um novo
        //~Se já existe realizado um Update
        public static void CreateOrUpdateFile(DriveService service, string folderId, string filePath)
        {
            var fileMetadata = new File()
            {
                Name = System.IO.Path.GetFileName(filePath),
                Parents = new List<string> { folderId },
                MimeType = MimeTypeMap.GetMimeType(System.IO.Path.GetExtension(filePath))
            };

            var id = FindId(service, fileMetadata.Name);

            ResumableUpload<File, File> request;

            using (var stream = new System.IO.FileStream(fileMetadata.Name,
                             System.IO.FileMode.Open, System.IO.FileAccess.Read))
            {
                if (id == null && !id.Any())
                {
                    request = service.Files.Create(fileMetadata, stream, fileMetadata.MimeType);

                }
                else
                {
                    request = service.Files.Update(fileMetadata, id.First(), stream, fileMetadata.MimeType);
                }
                request.Upload();
            }
        }

        //Retorna um list pois o GDrive trabalha com arquivos de mesmo Nome
        public static string[] FindId(DriveService service, string name, bool findOnTrashed = false)
        {
            var request = service.Files.List();
            request.Q = string.Format("name = '{0}'", name);

            //Default true
            if (!findOnTrashed)
            {
                request.Q += " and trashed = false";
            }
            request.Fields = "files(id)";

            var listRequest = request.Execute();
            var files = listRequest.Files;
            var result = new List<string>();

            if (files != null && files.Any())
            {
                foreach (var arquivo in files)
                {
                    result.Add(arquivo.Id);
                }
            }
            return result.ToArray();
        }

        // retorna true se não existir um arquivo com este nome
        public static bool HasNameFile(DriveService service, string name)
        {
            return FindId(service, name).Count() < 0;
        }

        public static void DownloadFile(DriveService service, string name, string localPath)
        {
            var ids = FindId(service, name);

            if (ids != null && ids.Any())
            {
                var request = service.Files.Get(ids.First());
                using (var stream = new System.IO.FileStream(name,
                    System.IO.FileMode.Create, System.IO.FileAccess.Write))
                {
                    request.Download(stream);
                }
            }
        }

        public static void DeleteFile(DriveService service, string name, string id = null)
        {
            if (id == null)
            {
                var ids = FindId(service, name);
                if (ids != null && ids.Any())
                {
                    foreach (var item in ids)
                    {
                        var request = service.Files.Delete(item);
                        request.Execute();
                    }
                }
            }
            else
            {
                var request = service.Files.Delete(id);
                request.Execute();
            }
        }
        //Retorna uma lista de arquivos paginados
        public static List<string> ListPageFiles(DriveService servico, int numberFilesForPage)
        {
            var request = servico.Files.List();
            request.Fields = "nextPageToken, files(id, name)";
            request.Q = "trashed=false";
            // Default 100, máximo 1000.
            request.PageSize = numberFilesForPage;

            var result = request.Execute();
            var files = result.Files;
            var list = new List<string>();

            while (files != null && files.Any())
            {
                foreach (var arquivo in files)
                {
                    list.Add(arquivo.Name);
                    list.Add(arquivo.Id);
                }

                if (result.NextPageToken != null)
                {
                    request.PageToken = result.NextPageToken;
                    result = request.Execute();
                    files = result.Files;
                    list.Add("<<Proxima pagina>>");

                    foreach (var item in files)
                    {
                        list.Add(item.Name);
                    }
                }
                else
                {
                    files = null;
                }
            }
            return list;
        }

        public static void CreateFolder(DriveService service, string folderName, string idFolderBase = null)
        {
            var folderMetadata = new File()
            {
                Name = folderName,
                MimeType = "application/vnd.google-apps.folder"
            };

            if (idFolderBase == null)
            {
                service.Files.Create(folderMetadata).Execute();
            }
            else
            {
                folderMetadata.Parents = new List<string>() { idFolderBase };
                service.Files.Create(folderMetadata).Execute();
            }
        }

        //Cria um arquivo na pasta pré determinada
        public static void CreateFileInFolder(DriveService service, string fileName, string folderId)
        {

            var fileMetadata = new File()
            {
                Name = fileName
            };
            if (folderId == null)
            {
                service.Files.Create(fileMetadata).Execute();
            }
            else
            {
                fileMetadata.Parents = new List<string>() { folderId };
                service.Files.Create(fileMetadata).Execute();
            }

        }

        public static Permission CreatePermission(string type, string emailAddress, bool Writer, bool Domain)
        {
            if (Domain)
            {
                return new Permission()
                {
                    Type = type,
                    Role = "writer",
                    Domain = emailAddress
                };
            }
            else
                if (Writer)
            {
                return new Permission()
                {
                    Type = type,
                    Role = "writer",
                    EmailAddress = emailAddress
                };
            }
            else
            {
                return new Permission()
                {
                    Type = type,
                    Role = "reader",
                    EmailAddress = emailAddress
                };
            }

        }

        //Apenas edit pois não há suporte para criar folder ao mesmo tempo inserir permissoes        
        public static void EditPermission(DriveService service, Permission permission, string Id)
        {
            var batch = new BatchRequest(service);

            BatchRequest.OnResponse<Permission> callback = delegate (
                                  Permission permissionCb, RequestError error,
                                              int index, HttpResponseMessage message)
            {
                if (error != null)
                {
                    throw new Exception();
                }
            };

            var request = service.Permissions.Create(permission, Id);
            request.Fields = "id";
            batch.Queue(request, callback);
            request.Execute();
        }
    }
}
