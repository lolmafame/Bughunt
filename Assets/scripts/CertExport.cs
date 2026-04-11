using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using PdfSharp.Pdf;
using PdfSharp.Drawing;

public class CertExport : MonoBehaviour
{
    [Header("Certificate Panel to Capture")]
    [Tooltip("Drag the 'Panel' RectTransform (the exact container you want captured — nothing outside it will appear).")]
    [SerializeField] private RectTransform certificatePanel;

    [Header("PDF Output")]
    [Tooltip("Subfolder inside the user's Documents folder.")]
    [SerializeField] private string outputSubFolder = "Certificates";

    [Header("Feedback UI (Optional)")]
    [SerializeField] private GameObject exportingIndicator;
    [SerializeField] private TMP_Text statusMessageText;

    // ─────────────────────────────────────────────────────────────────────────

    public void OnDownloadButtonPressed()
    {
        StartCoroutine(CaptureAndExport());
    }

    // ─────────────────────────────────────────────────────────────────────────

    private IEnumerator CaptureAndExport()
    {
        SetStatus("Generating PDF...", true);

        // ── STEP 1: Hide every UI object that is NOT inside certificatePanel ──
        // We collect all root-level Canvas children (or scene root objects) that
        // are NOT an ancestor of certificatePanel and temporarily disable them.
        List<GameObject> hidden = HideEverythingExceptPanel(certificatePanel);

        // Wait TWO frames: one to process the SetActive calls, one to let Unity
        // actually re-render the scene without the hidden objects.
        yield return null;
        yield return new WaitForEndOfFrame();

        byte[] pngBytes = null;
        try
        {
            pngBytes = CapturePanelRegion(certificatePanel);
        }
        finally
        {
            // ── STEP 2: Restore all hidden objects no matter what ─────────────
            foreach (GameObject go in hidden)
                if (go != null) go.SetActive(true);
        }

        SetStatus("", false);

        if (pngBytes == null || pngBytes.Length == 0)
        {
            Debug.LogError(">>> CERT EXPORT: Capture returned empty bytes.");
            yield break;
        }

        // ── STEP 3: Write PDF ─────────────────────────────────────────────────
        try
        {
            string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string folder = Path.Combine(docs, outputSubFolder);
            Directory.CreateDirectory(folder);

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string fileName = $"Certificate_{timestamp}.pdf";
            string fullPath = Path.Combine(folder, fileName);

            WritePDF(pngBytes, fullPath);
            OpenFolderInExplorer(folder);

            SetStatus($"Saved!\nDocuments/{outputSubFolder}/{fileName}", false);
            Debug.Log($">>> CERT EXPORT: Saved to {fullPath}");
        }
        catch (Exception ex)
        {
            SetStatus("Export failed. See console.", false);
            Debug.LogError($">>> CERT EXPORT ERROR: {ex.Message}\n{ex.StackTrace}");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // HIDE / RESTORE
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Walks up from certificatePanel to find the top-level Canvas, then disables
    /// every direct child of that Canvas that is NOT an ancestor of (or equal to)
    /// certificatePanel. Returns the list of objects we disabled so they can be
    /// re-enabled afterwards.
    /// </summary>
    private List<GameObject> HideEverythingExceptPanel(RectTransform panel)
    {
        List<GameObject> disabled = new List<GameObject>();

        // Find the root Canvas ancestor
        Canvas rootCanvas = panel.GetComponentInParent<Canvas>();
        if (rootCanvas == null)
        {
            Debug.LogWarning(">>> CERT EXPORT: No Canvas found above panel — skipping hide step.");
            return disabled;
        }

        // Build the set of ancestors of 'panel' (including itself) so we never
        // hide anything that is needed to display the panel.
        HashSet<Transform> ancestors = new HashSet<Transform>();
        Transform t = panel;
        while (t != null)
        {
            ancestors.Add(t);
            t = t.parent;
        }

        // Disable every direct child of the root Canvas that is NOT an ancestor
        Transform canvasRoot = rootCanvas.transform;
        foreach (Transform child in canvasRoot)
        {
            if (!ancestors.Contains(child) && child.gameObject.activeSelf)
            {
                child.gameObject.SetActive(false);
                disabled.Add(child.gameObject);
            }
        }

        return disabled;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CAPTURE — locked to the Panel's RectTransform bounds
    // ─────────────────────────────────────────────────────────────────────────

    private byte[] CapturePanelRegion(RectTransform panel)
    {
        Rect r = GetScreenRect(panel);
        int x = Mathf.RoundToInt(r.x);
        int y = Mathf.RoundToInt(r.y);
        int w = Mathf.RoundToInt(r.width);
        int h = Mathf.RoundToInt(r.height);

        // Clamp to screen bounds (safety)
        x = Mathf.Clamp(x, 0, Screen.width);
        y = Mathf.Clamp(y, 0, Screen.height);
        w = Mathf.Clamp(w, 1, Screen.width - x);
        h = Mathf.Clamp(h, 1, Screen.height - y);

        Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(x, y, w, h), 0, 0);
        tex.Apply();

        byte[] png = tex.EncodeToPNG();
        Destroy(tex);
        return png;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PDF WRITING
    // ─────────────────────────────────────────────────────────────────────────

    private void WritePDF(byte[] pngBytes, string filePath)
    {
        Texture2D tempTex = new Texture2D(2, 2);
        tempTex.LoadImage(pngBytes);
        int imgW = tempTex.width;
        int imgH = tempTex.height;
        Destroy(tempTex);

        const double dpi = 96.0;
        const double ptPerInch = 72.0;
        double pageW = imgW / dpi * ptPerInch;
        double pageH = imgH / dpi * ptPerInch;

        PdfDocument doc = new PdfDocument();
        doc.Info.Title = "Certificate of Completion";

        PdfPage page = doc.AddPage();
        page.Width = XUnit.FromPoint(pageW);
        page.Height = XUnit.FromPoint(pageH);

        using (XGraphics gfx = XGraphics.FromPdfPage(page))
        using (MemoryStream ms = new MemoryStream(pngBytes))
        {
            XImage img = XImage.FromStream(ms);
            gfx.DrawImage(img, 0, 0, pageW, pageH);
        }

        doc.Save(filePath);
        doc.Close();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────────────────────────────

    private Rect GetScreenRect(RectTransform rt)
    {
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        // corners[0] = bottom-left, corners[2] = top-right
        return new Rect(
            corners[0].x,
            corners[0].y,
            corners[2].x - corners[0].x,
            corners[2].y - corners[0].y
        );
    }

    private void OpenFolderInExplorer(string path)
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        System.Diagnostics.Process.Start("explorer.exe", path.Replace("/", "\\"));
#elif UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
        System.Diagnostics.Process.Start("open", path);
#elif UNITY_STANDALONE_LINUX
        System.Diagnostics.Process.Start("xdg-open", path);
#endif
    }

    private void SetStatus(string message, bool isLoading)
    {
        if (exportingIndicator != null) exportingIndicator.SetActive(isLoading);
        if (statusMessageText != null) statusMessageText.text = message;
    }
}