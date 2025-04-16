using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class BugReport_GoogleForm : MonoBehaviour
{
    string _formUrl = "https://docs.google.com/forms/u/0/d/e/1FAIpQLSdZgfYDANlP-rzMvUQH2_QDZxdTv0F101Y36xRQM-aOlFxmYg/formResponse";
    public Texture2D sprite;
    public string fileNameList;
    public string[] fileList;
    public byte[] fileArray;

    public void SendBugReport()
    {
        //Report_GoogleForm.Send("alvina test");
        //StartCoroutine(Post("alvina test"));
    }

    public void uploadToAWS()
    {
        fileNameList = "";
        string[] files = Directory.GetFiles(".", "*.txt");
        
        files = fileList;

        Debug.Log("file length : " + files.Length);
        for (int i = 0; i < files.Length; i++)
        {
            WWWForm AWSform = new WWWForm();
            AWSform.AddField("entry.1139872813", "AffectivaLogs/${filename}");
            AWSform.AddBinaryData("entry.1139872813", File.ReadAllBytes("Assets/" + files[i]), files[i], "text/plain");
            StartCoroutine(PostAWS(AWSform));
            fileNameList += files[i].Replace(@".\", "") + "  ||  ";
        }
    }

    IEnumerator PostAWS(WWWForm form)
    {
        //UnityWebRequest www = new UnityWebRequest(_formUrl, form);
        UnityWebRequest www = UnityWebRequest.Post(_formUrl, form);

        //float elapsedTime = 0.0f;
        //while (!www.isDone)
        //{
        //    elapsedTime += Time.deltaTime;
        //    //Matrix4x4 wait time is 20s
        //    if (elapsedTime >= 20f)
        //    {
        //        break;
        //    }
        //    yield return www.SendWebRequest();
        //}

        yield return www.SendWebRequest();


        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log(www.error);
        }
        else
        {
            Debug.Log("Finished Uploading Screenshot");
        }
    }

    public IEnumerator Post(Texture2D image)
    {
        // Créer le formulaire
        WWWForm form = new WWWForm();

        // Ajouter des champs texte au formulaire (exemple)
        form.AddField("entry.1525138636", "Message1");
        form.AddField("entry.963213204", "Message2");

        // Encoder l'image en PNG
        byte[] imageBytes = image.EncodeToPNG();

        // Ajouter l'image dans le formulaire avec le bon nom de champ et le type MIME
        form.AddBinaryData("entry.1334155566", imageBytes, "screenshot.png", "image/png");

        // Créer la requête HTTP
        UnityWebRequest www = UnityWebRequest.Post(_formUrl, form);

        // Définir l'en-tête Content-Type si nécessaire
        www.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");

        // Attendre la fin de l'envoi
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            // Afficher une erreur si l'envoi échoue
            Debug.LogError("Erreur lors de l'envoi : " + www.error);
            Debug.Log("Réponse serveur : " + www.downloadHandler.text);  // Affiche la réponse du serveur

        }
        else
        {
            // Afficher un message de succès
            Debug.Log("Image envoyée avec succès !");
        }
    }

    // Exemple d'appel avec une capture d'écran
    public void CaptureAndSend()
    {
        // Prendre une capture d'écran de la scène
        //Texture2D screenshot = ScreenCapture.CaptureScreenshotAsTexture();

        // Lancer l'envoi de l'image
        StartCoroutine(Post(sprite));
    }

    public void uploadToDrive()
    {
        string[] files = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.txt");
        for (int i = 0; i < files.Length; i++)
        {
            string boundary = "----------" + DateTime.Now.Ticks.ToString("x");
            ServicePointManager.ServerCertificateValidationCallback = MyRemoteCertificateValidationCallback;
            HttpWebRequest webrequest = (HttpWebRequest)WebRequest.Create(_formUrl);
            webrequest.ContentType = "multipart/form-data; boundary=" + boundary;
            webrequest.Method = "POST";

            // Build up the post message header  
            StringBuilder sb = new StringBuilder();
            sb.Append("--");
            sb.Append(boundary);
            sb.Append("\r\n");
            sb.Append("Content-Disposition: form-data; name=\"");
            sb.Append("file"); // file form name
            sb.Append("\"; filename=\"");
            sb.Append(Path.GetFileName(files[i]));
            sb.Append("\"");
            sb.Append("\r\n");
            sb.Append("Content-Type: ");
            sb.Append("text/plain");
            sb.Append("\r\n");
            sb.Append("\r\n");

            string postHeader = sb.ToString();
            byte[] postHeaderBytes = Encoding.UTF8.GetBytes(postHeader);

            // Build the trailing boundary string as a byte array  
            // ensuring the boundary appears on a line by itself  
            byte[] boundaryBytes = Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");

            FileStream fileStream = new FileStream(files[i], FileMode.Open, FileAccess.Read);
            long length = postHeaderBytes.Length + fileStream.Length + boundaryBytes.Length;
            webrequest.ContentLength = length;

            Stream requestStream = webrequest.GetRequestStream();

            // Write out our post header  
            requestStream.Write(postHeaderBytes, 0, postHeaderBytes.Length);

            // Write out the file contents  
            byte[] buffer = new Byte[checked((uint)Math.Min(4096, (int)fileStream.Length))];
            int bytesRead = 0;
            while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
                requestStream.Write(buffer, 0, bytesRead);

            // Write out the trailing boundary  
            requestStream.Write(boundaryBytes, 0, boundaryBytes.Length);
            try
            {
                WebResponse response = webrequest.GetResponse();
                Stream s = response.GetResponseStream();
                StreamReader sr = new StreamReader(s);
                Debug.Log("sr.ReadToEnd() ====  " + sr.ReadToEnd());
            }
            catch (Exception e)
            {
                Debug.Log(e);
            }
        }
    }

    public bool MyRemoteCertificateValidationCallback(System.Object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
    {
        bool isOk = true;
        // If there are errors in the certificate chain, look at each error to determine the cause.
        if (sslPolicyErrors != SslPolicyErrors.None)
        {
            for (int i = 0; i < chain.ChainStatus.Length; i++)
            {
                if (chain.ChainStatus[i].Status != X509ChainStatusFlags.RevocationStatusUnknown)
                {
                    chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
                    chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
                    chain.ChainPolicy.UrlRetrievalTimeout = new TimeSpan(0, 1, 0);
                    chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllFlags;
                    bool chainIsValid = chain.Build((X509Certificate2)certificate);
                    if (!chainIsValid)
                    {
                        isOk = false;
                    }
                }
            }
        }
        return isOk;
    }
}
