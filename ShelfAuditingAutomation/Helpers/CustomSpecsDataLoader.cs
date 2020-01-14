using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using ShelfAuditingAutomation.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace ShelfAuditingAutomation.Helpers
{
    public class CustomSpecsDataLoader
    {
        private static readonly string SpecsDataFileName = "Assets\\specsData.json";
        private static readonly string SpecsDataUrl = "https://intelligentkioskstore.blob.core.windows.net/shelf-auditing/specsData.json";

        public static async Task<List<SpecsData>> GetData()
        {
            try
            {
                CloudBlockBlob blob = new CloudBlockBlob(new Uri(SpecsDataUrl));
                string text = await blob.DownloadTextAsync();
                return JsonConvert.DeserializeObject<List<SpecsData>>(text);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            return null;
        }

        public static List<SpecsData> GetBuiltInData()
        {
            try
            {
                string content = File.ReadAllText(SpecsDataFileName);
                return JsonConvert.DeserializeObject<List<SpecsData>>(content);
            }
            catch (Exception)
            {
                return new List<SpecsData>();
            }
        }

        public static List<SpecsData> DeserializeData(string content)
        {
            try
            {
                return JsonConvert.DeserializeObject<List<SpecsData>>(content);
            }
            catch (Exception)
            {
                return new List<SpecsData>();
            }
        }

        public static bool ValidateData(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return false;
            }

            try
            {
                JsonConvert.DeserializeObject<List<SpecsData>>(content);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
