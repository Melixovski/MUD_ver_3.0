using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;

using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace MUD
{
    [Transaction(TransactionMode.Manual)]
    public class ViewModel : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // URL для загрузки IFC
            string websiteUrl = "https://www.spbexp.ru/bim/bim-models/arkhitekturnye-resheniya/";

            // Загрузка и подгрузка в текущий документ ревит 
            try
            {
                // Инициализация активного ревит-док
                var uiapp = commandData.Application;
                var uidoc = uiapp.ActiveUIDocument;
                var doc = uidoc.Document;

                // Загрузка IFC
                var ifcFiles = DownloadIfcFilesFromWebsite(websiteUrl).Result;

                // Load IFC files into Revit document
                foreach (var ifcFile in ifcFiles)
                {
                    LoadIfcFileIntoRevit(ifcFile, doc);
                }
            }
            catch (Exception ex)
            {
                message = "Ошибка загрузки IFC: " + ex.Message;
                return Result.Failed;
            }

            return Result.Succeeded;
        }

        private async Task<List<string>> DownloadIfcFilesFromWebsite(string websiteUrl)
        {
            using (HttpClient client = new HttpClient())
            {
                // Get the list of files from the website
                var filesPage = await client.GetStringAsync(websiteUrl);

                // Поиск ifc файлов на сайте для выгрузки
                List<string> ifcFiles = filesPage.Split(new char[] { '\\n', '\\r' }, StringSplitOptions.RemoveEmptyEntries)
                                                 .Where(f => f.EndsWith(".ifc"))
                                                 .ToList();

                // Загрузка каждого файла
                List<string> filePaths = new List<string>();
                foreach (var filename in ifcFiles)
                {
                    var fileUrl = websiteUrl + filename;
                    var fileContents = await client.GetByteArrayAsync(fileUrl);

                    var filePath = Path.Combine(Path.GetTempPath(), filename);
                    File.WriteAllBytes(filePath, fileContents);

                    filePaths.Add(filePath);
                }

                return filePaths;
            }
        }

        public void LoadIfcFileIntoRevit(string filePath, Document doc)
        {
            // Параметры импора IFC
            IFCImportOptions 
                importOptions = new IFCImportOptions();

            
            using (Transaction transaction = new Transaction(doc, "Import IFC"))
            {
                transaction.Start();

               
                object value = doc.Import(filePath, importOptions, doc.ActiveView);

               
                transaction.Commit();
            }
        }
    }
}