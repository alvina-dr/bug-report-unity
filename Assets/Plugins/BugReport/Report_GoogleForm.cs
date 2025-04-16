using System;
using System.Collections.Generic;
using System.Net.Http;
using UnityEditor.PackageManager.Requests;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.Networking;

public static class Report_GoogleForm
{
    internal static string _formUrl = "https://docs.google.com/forms/d/e/1FAIpQLSdZgfYDANlP-rzMvUQH2_QDZxdTv0F101Y36xRQM-aOlFxmYg/formResponse";

    public async static void Send(string message)
    {
        var formData = new Dictionary<string, string>()
        {
            { "entry.1525138636", message},
            {"entry.963213204", "test" }
        };

        
        try
        {
            using (HttpClient client = new HttpClient())
            {
                var content = new MultipartFormDataContent();
                content.Add(new FormUrlEncodedContent(formData));
                //var content = new FormUrlEncodedContent(formData);

                HttpResponseMessage responseMessage = await client.PostAsync(_formUrl, content);

                //var HttpContent = new MultipartFormDataContent(Guid.NewGuid().ToString());

                //HttpContent.Add(new ByteArrayContent(request.Photo), "image", "imageFileName.jpg");


                if (responseMessage.IsSuccessStatusCode)
                {
                    Debug.Log("success");
                }
                else
                {
                    Debug.Log("error");
                }
            }
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }
    }
}
