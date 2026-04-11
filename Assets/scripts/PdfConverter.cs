using UnityEngine;
using System.IO;
using PdfSharp.Pdf;
using PdfSharp.Drawing;

public class PdfConverter : MonoBehaviour
{
    [Header("Certificate Settings")]
    [Tooltip("The base certificate background image. Must be marked as 'Read/Write Enabled' in its Import Settings.")]
    public Texture2D certificateBackground;

    /// <summary>
    /// Generates the PDF using the text strings provided by your Cert.cs script.
    /// </summary>
    public void GeneratePdf(string nameText, string dateText, string languageText)
    {
        if (certificateBackground == null)
        {
            Debug.LogError("PDF Error: No background texture assigned to PdfConverter!");
            return;
        }

        // 1. Create a new PDF Document
        PdfDocument document = new PdfDocument();
        document.Info.Title = $"{nameText} - Certificate";

        // 2. Add a Landscape Page
        PdfPage page = document.AddPage();
        page.Orientation = PdfSharp.PageOrientation.Landscape;
        XGraphics gfx = XGraphics.FromPdfPage(page);

        // 3. Draw the Background Image
        // We convert the Unity Texture2D into a format PDFsharp can read
        byte[] imageBytes = certificateBackground.EncodeToPNG();
        using (MemoryStream stream = new MemoryStream(imageBytes))
        {
            XImage bgImage = XImage.FromStream(stream);
            gfx.DrawImage(bgImage, 0, 0, page.Width, page.Height);
        }

        // 4. Setup PDF Fonts (Using standard OS fonts)
        XFont nameFont = new XFont("Arial", 45, XFontStyle.Bold);
        XFont detailFont = new XFont("Arial", 20, XFontStyle.Regular);
        XBrush textBrush = XBrushes.DarkSlateGray;

        XStringFormat centerFormat = new XStringFormat();
        centerFormat.Alignment = XStringAlignment.Center;

        // 5. Draw the Text onto the PDF
        // Note: You will likely need to adjust the "Y" coordinates (250, 350, 400) 
        // below so they line up perfectly with the blank spaces on your specific image.

        // Draw Name
        gfx.DrawString(nameText, nameFont, textBrush, new XRect(0, 250, page.Width, 50), centerFormat);

        // Draw Date
        gfx.DrawString(dateText, detailFont, textBrush, new XRect(0, 350, page.Width, 30), centerFormat);

        // Draw Language
        gfx.DrawString(languageText, detailFont, textBrush, new XRect(0, 400, page.Width, 30), centerFormat);

        // 6. Save and Download the PDF
        string fileName = $"Certificate_{nameText.Replace(" ", "_")}.pdf";
        string savePath = Path.Combine(Application.persistentDataPath, fileName);

        document.Save(savePath);
        Debug.Log($">>> SUCCESS! Certificate saved to: {savePath}");

        // Optional: Automatically open the PDF to show the user
        Application.OpenURL("file://" + savePath);
    }
}