using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_DrawOnImage : MonoBehaviour, IDragHandler, IEndDragHandler
{
    // The image we are going to edit at runtime
    private Image _drawImage;
    // The sprite that the Image component references
    private Sprite _drawSprite;
    // The texture of the drawSprite, the actual .png you are editing
    private Texture2D _drawTexture;
    private RectTransform _rectTransform;

    //The position of the mouse in the last frame
    Vector2 _previousDragPosition;

    //The array used to reset the image to be empty
    Color[] _resetColorsArray;
    //The color filled into the "resetColorsArray"
    Color _resetColor;

    //The color array of the changes to be applied
    Color32[] _currentColors;

    [Header("Paint settings")]
    [SerializeField] private Color _paintColor;
    [SerializeField] private int _pencilSize;

    void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _drawImage = GetComponent<Image>();

        _resetColor = new Color(0, 0, 0, 0);
    }

    //Call this whenever the image this script is attached has changed
    //most uses can probably simply call initalize at the beginning
    public void Initialize()
    {
        _drawSprite = _drawImage.sprite;
        _drawTexture = _drawSprite.texture;
        _resetColorsArray = _drawTexture.GetPixels();
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 localCursor = Vector2.zero;
        //This method transforms the mouse position, to a position relative to the image's pivot
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _rectTransform, eventData.position, eventData.pressEventCamera, out localCursor))
        {
            return;
        }

        //Check if the cursor is over the image
        if (localCursor.x < _rectTransform.rect.width / 2 &&
            localCursor.y < _rectTransform.rect.height / 2 &&
            localCursor.x > -_rectTransform.rect.width / 2 &&
            localCursor.y > -_rectTransform.rect.height / 2)
        {
            float rectToPixelScale = _drawImage.sprite.rect.width / _rectTransform.rect.width;
            localCursor = new Vector2(localCursor.x * rectToPixelScale + _drawSprite.rect.width / 2, localCursor.y * rectToPixelScale + _drawSprite.rect.height / 2);
            Paint(localCursor);
            _previousDragPosition = localCursor;
        }
        else
        {
            _previousDragPosition = Vector2.zero;
        }
    }

    //Reset the previosDragPosition so that our brush knows the next drag is a new line
    public void OnEndDrag(PointerEventData eventData)
    {
        _previousDragPosition = Vector2.zero;
    }

    // Pass in a point in PIXEL coordinates
    // Changes the surrounding pixels of the pixelPosition to the drawSetting.drawColor
    public void Paint(Vector2 pixelPosition)
    {
        //grab the current image state
        _currentColors = _drawTexture.GetPixels32();

        if (_previousDragPosition == Vector2.zero)
        {
            // If this is the first frame in a drag, color the pixels around the mouse
            MarkPixelsToColour(pixelPosition);
        }
        else
        {
            // Color between where we are this frame, and where our mouse was last frame
            ColorBetween(_previousDragPosition, pixelPosition);
        }
        ApplyCurrentColors();

        _previousDragPosition = pixelPosition;
    }

    //Color the pixels around the centerPoint
    public void MarkPixelsToColour(Vector2 centerPixel)
    {
        int centerX = (int)centerPixel.x;
        int centerY = (int)centerPixel.y;

        for (int x = centerX - _pencilSize; x <= centerX + _pencilSize; x++)
        {
            // Check if the X wraps around the image, so we don't draw pixels on the other side of the image
            if (x >= (int)_drawSprite.rect.width || x < 0)
                continue;

            for (int y = centerY - _pencilSize; y <= centerY + _pencilSize; y++)
            {
                MarkPixelToChange(x, y);
            }
        }
    }

    // Mark the pixels to be changed from startPoint to endPoint
    public void ColorBetween(Vector2 startPoint, Vector2 endPoint)
    {
        // Get the distance from start to finish
        float distance = Vector2.Distance(startPoint, endPoint);

        Vector2 cur_position = startPoint;

        // Calculate how many times we should interpolate between start_point and end_point based on the amount of time that has passed since the last update
        float lerp_steps = 1 / distance;

        for (float lerp = 0; lerp <= 1; lerp += lerp_steps)
        {
            cur_position = Vector2.Lerp(startPoint, endPoint, lerp);
            MarkPixelsToColour(cur_position);
        }
    }

    public void MarkPixelToChange(int x, int y)
    {
        // Need to transform x and y coordinates to flat coordinates of array
        int arrayPosition = (y * (int)_drawSprite.rect.width) + x;

        // Check if this is a valid position
        if (arrayPosition > _currentColors.Length || arrayPosition < 0)
        {
            return;
        }

        _currentColors[arrayPosition] = _paintColor;
    }

    public void ApplyCurrentColors()
    {
        _drawTexture.SetPixels32(_currentColors);
        _drawTexture.Apply();
    }

    // Changes every pixel to be the reset colour
    public void ResetTexture()
    {
        _drawTexture.SetPixels(_resetColorsArray);
        _drawTexture.Apply();
    }

}
