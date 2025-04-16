using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static UnityEngine.Experimental.Rendering.RayTracingAccelerationStructure;

public class UI_DrawOnImage : MonoBehaviour, IDragHandler
{
    // The image we are going to edit at runtime
    private Image drawImage;
    // The sprite that the Image component references
    private Sprite drawSprite;
    // The texture of the drawSprite, the actual .png you are editing
    private Texture2D drawTexture;
    private RectTransform _rectTransform;

    //The position of the mouse in the last frame
    Vector2 previousDragPosition;

    //The array used to reset the image to be empty
    Color[] resetColorsArray;
    //The color filled into the "resetColorsArray"
    Color resetColor;

    //The color array of the changes to be applied
    Color32[] currentColors;

    [Header("Paint settings")]
    [SerializeField] private Color _paintColor;
    [SerializeField] private int _pencilSize;

    void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        drawImage = GetComponent<Image>();

        resetColor = new Color(0, 0, 0, 0);
    }

    private void Update()
    {
        KeyboardInput();
    }

    //Call this whenever the image this script is attached has changed
    //most uses can probably simply call initalize at the beginning
    public void Initialize()
    {
        drawSprite = drawImage.sprite;
        drawTexture = drawSprite.texture;

        // fill the array with our reset color so it can be easily reset later on
        resetColorsArray = new Color[(int)drawSprite.rect.width * (int)drawSprite.rect.height];
        for (int x = 0; x < resetColorsArray.Length; x++)
            resetColorsArray[x] = resetColor;
    }

    void KeyboardInput()
    {
        // We have different undo/redo controls in the editor,
        // so that way you don't accidentally undo something in the scene
#if UNITY_EDITOR
        bool isShiftHeldDown = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        bool isZHeldDown = Input.GetKeyDown(KeyCode.Z);
        bool isYHeldDown = Input.GetKeyDown(KeyCode.Y);

        //if (isShiftHeldDown &&
        //    isZHeldDown &&
        //    drawSettings.CanUndo())
        //{
        //    // if there's something to undo, pull the last state off of the stack, and apply those changes
        //    currentColors = drawSettings.Undo(drawTexture.GetPixels32());
        //    ApplyCurrentColors();
        //}

        //if (isShiftHeldDown &&
        //isYHeldDown &&
        //    drawSettings.CanRedo())
        //{
        //    currentColors = drawSettings.Redo(drawTexture.GetPixels32());
        //    ApplyCurrentColors();
        //}
        //These controls only take effect if we build the game! See: Platform dependent compilation
#else
        bool isControlHeldDown = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        bool isZHeldDown = Input.GetKeyDown(KeyCode.Z);
        if (isControlHeldDown && isZHeldDown) {
            if (Input.GetKey(KeyCode.LeftShift) && 
                drawViewModel.CanRedo()) {
                currentColors = drawViewModel.Redo(drawTexture.GetPixels32());
                ApplyCurrentColors();
            } else if (drawViewModel.CanUndo()) {
                currentColors = drawViewModel.Undo(drawTexture.GetPixels32());
                ApplyCurrentColors();
            }
        }
#endif
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
        if (localCursor.x < _rectTransform.rect.width &&
            localCursor.y < _rectTransform.rect.height &&
            localCursor.x > 0 &&
            localCursor.y > 0)
        {
            float rectToPixelScale = drawImage.sprite.rect.width / _rectTransform.rect.width;
            localCursor = new Vector2(localCursor.x * rectToPixelScale, localCursor.y * rectToPixelScale);
            Paint(localCursor);
            previousDragPosition = localCursor;
        }
        else
        {
            previousDragPosition = Vector2.zero;
        }


    }

    // Pass in a point in PIXEL coordinates
    // Changes the surrounding pixels of the pixelPosition to the drawSetting.drawColor
    public void Paint(Vector2 pixelPosition)
    {
        //grab the current image state
        currentColors = drawTexture.GetPixels32();

        if (previousDragPosition == Vector2.zero)
        {
            // If this is the first frame in a drag, color the pixels around the mouse
            MarkPixelsToColour(pixelPosition);
        }
        else
        {
            // Color between where we are this frame, and where our mouse was last frame
            ColorBetween(previousDragPosition, pixelPosition);
        }
        ApplyCurrentColors();

        previousDragPosition = pixelPosition;
    }

    //Color the pixels around the centerPoint
    public void MarkPixelsToColour(Vector2 centerPixel)
    {
        int centerX = (int)centerPixel.x;
        int centerY = (int)centerPixel.y;

        for (int x = centerX - _pencilSize; x <= centerX + _pencilSize; x++)
        {
            // Check if the X wraps around the image, so we don't draw pixels on the other side of the image
            if (x >= (int)drawSprite.rect.width || x < 0)
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
        int arrayPosition = (y * (int)drawSprite.rect.width) + x;

        // Check if this is a valid position
        if (arrayPosition > currentColors.Length || arrayPosition < 0)
        {
            return;
        }

        currentColors[arrayPosition] = _paintColor;
    }

    public void ApplyCurrentColors()
    {
        drawTexture.SetPixels32(currentColors);
        drawTexture.Apply();
    }

    // Changes every pixel to be the reset colour
    public void ResetTexture()
    {
        drawTexture.SetPixels(resetColorsArray);
        drawTexture.Apply();
    }

}
