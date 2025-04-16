using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;

public class BugReport_GitHub : MonoBehaviour
{
    [SerializeField] private TMP_InputField _inputField;
    [SerializeField] private Image _screenImage;
    [SerializeField] private GameObject _bugReportGO;
    [SerializeField] private UI_DrawOnImage _drawOnImage;

    private const string token = "";  // Remplace par ton token
    private const string repoOwner = "alvina-dr";      // Remplace par le nom du propriétaire du repo
    private const string repoName = "bug-report-web";          // Remplace par le nom du repo

    public Texture2D _screenTexture;
    public byte[] _encodedScreen;

    private void Start()
    {
        CloseBugReport();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F12))
        {
            OpenBugReport();
        }
    }

    public void OpenBugReport()
    {
        StartCoroutine(TakeScreen(() =>
        {
            _bugReportGO.SetActive(true);
            _drawOnImage.Initialize();
        }));
    }

    public void CloseBugReport()
    {
        _bugReportGO.SetActive(false);
    }

    public void SendBugReport()
    {
        StartCoroutine(CreateGitHubFile());
        CloseBugReport();
    }

    IEnumerator TakeScreen(Action callback = null)
    {
        yield return new WaitForEndOfFrame();

        _screenTexture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        _screenTexture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0); // Capture le screen
        _screenTexture.Apply();

        Sprite screenSprite = Sprite.Create(_screenTexture, new Rect(0, 0, _screenTexture.width, _screenTexture.height), new Vector2(.5f, .5f));

        float _widthSize = _screenImage.rectTransform.sizeDelta.x;
        _screenImage.sprite = screenSprite;
        _screenImage.SetNativeSize();
        _screenImage.rectTransform.sizeDelta = new Vector2(_widthSize, _screenImage.rectTransform.sizeDelta.y * _widthSize / _screenImage.rectTransform.sizeDelta.x);

        callback?.Invoke();
    }

    IEnumerator CreateGitHubFile()
    {
        string reportFilePath = "reports/report-" + System.DateTime.Now.ToShortDateString().Replace("/", "-") + "_" + System.DateTime.Now.ToLongTimeString().Replace(":", "-") + ".json";

        string imgName = "report-" + System.DateTime.Now.ToShortDateString().Replace("/", "-") + "_" + System.DateTime.Now.ToLongTimeString().Replace(":", "-") + ".png";
        string reportImagePath = "public/reports/" + imgName;
        
        // URL de l'API pour créer le fichier
        string apiUrl = $"https://api.github.com/repos/{repoOwner}/{repoName}/contents/{reportFilePath}";
        string apiImgUrl = $"https://api.github.com/repos/{repoOwner}/{repoName}/contents/{reportImagePath}";

        yield return new WaitForEndOfFrame();

        // Le contenu que tu veux ajouter à ton fichier (encodé en Base64)
        string jsonContent = "{\r\n    " +
            "\"description\": \""+ _inputField.text + "\",\r\n" +
            "\"screenUrl\": \"/reports/" + imgName + "\"\r\n}";
        string content = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(jsonContent));

        // Créer un objet JSON pour la requête
        string jsonBody = $"{{ \"message\": \"Création du fichier depuis Unity\", \"content\": \"{content}\" }}";

        // Création de la requête POST
        UnityWebRequest request = UnityWebRequest.Put(apiUrl, jsonBody);
        request.method = UnityWebRequest.kHttpVerbPUT;
        request.SetRequestHeader("Authorization", "Bearer " + token);
        request.SetRequestHeader("Content-Type", "application/json");

        // Création de la requête POST pour l'image PNG (encodée en Base64)
        _encodedScreen = ImageConversion.EncodeToPNG(_screenTexture);
        string encodedImage = Convert.ToBase64String(_encodedScreen);
        string jsonImgBody = $"{{ \"message\": \"Création du fichier image depuis Unity\", \"content\": \"{encodedImage}\" }}";

        UnityWebRequest requestImg = UnityWebRequest.Put(apiImgUrl, jsonImgBody);
        requestImg.method = UnityWebRequest.kHttpVerbPUT;
        requestImg.SetRequestHeader("Authorization", "Bearer " + token);
        requestImg.SetRequestHeader("Content-Type", "application/json");

        // Envoie la requête et attends la réponse
        yield return request.SendWebRequest();

        // Cleaning out informations
        _inputField.text = string.Empty;

        if (request.result == UnityWebRequest.Result.Success)
        {
            // Si la requête réussit, affiche le message de succès
            Debug.Log("Fichier créé avec succès !");
        }
        else
        {
            // Si la requête échoue, affiche l'erreur et la réponse
            Debug.LogError($"Erreur lors de la création du fichier : {request.error}");
            Debug.LogError($"Réponse de l'API : {request.downloadHandler.text}");
        }

        yield return requestImg.SendWebRequest();

        if (requestImg.result == UnityWebRequest.Result.Success)
        {
            // Si la requête réussit, affiche le message de succès
            Debug.Log("Fichier créé avec succès !");
        }
        else
        {
            // Si la requête échoue, affiche l'erreur et la réponse
            Debug.LogError($"Erreur lors de la création du fichier : {requestImg.error}");
            Debug.LogError($"Réponse de l'API : {requestImg.downloadHandler.text}");
        }
    }





}
