using UnityEngine;
using UnityEngine.UIElements;

namespace Popup.UI
{
    public partial class ProfilePopupControl : VisualElement
    {
        // This is a special class that tells UI Builder how to
        // make this control available in the UXML file.
        public new class UxmlFactory : UxmlFactory<ProfilePopupControl> { }

        // --- Class members ---
        private VisualElement m_RootOverlay; // The "profile-popup-overlay" element
        private Button m_CloseButton;
        private Button m_LogOutButton;

        // Path to your UXML file in the "Resources" folder
        private const string UXML_PATH = "Popup/ProfilePopup";

        // --- Constructor ---
        public ProfilePopupControl()
        {
            // 1. Load the VisualTreeAsset (your .uxml file)
            var asset = Resources.Load<VisualTreeAsset>(UXML_PATH);
            if (asset == null)
            {
                Debug.LogError($"ProfilePopupControl: Could not find UXML at '{UXML_PATH}'. Make sure it's in a 'Resources' folder.");
                return;
            }

            // 2. Clone the UXML's content *into this element*
            asset.CloneTree(this);

            // 3. Query for elements (we query 'this' now)
            m_RootOverlay = this.Q("profile-popup-overlay");
            if (m_RootOverlay == null)
            {
                Debug.LogError("ProfilePopupControl: Could not find 'profile-popup-overlay' element. Check the UXML.");
                return;
            }

            // Now query for the buttons *within* this control
            m_CloseButton = this.Q<Button>("CloseButton");
            m_LogOutButton = this.Q<Button>("LogOutButton");

            // 4. Register Callbacks (You can remove these for now if they are causing issues, 
            // but they don't break the visibility)
            m_CloseButton?.RegisterCallback<ClickEvent>(OnCloseClicked);
            m_LogOutButton?.RegisterCallback<ClickEvent>(OnLogOutClicked);

            
            Show();
        }

        // --- Public Methods to Show/Hide ---
        public void Show()
        {
            if (m_RootOverlay != null)
            {
                m_RootOverlay.style.display = DisplayStyle.Flex;
            }
        }

        public void Hide()
        {
            if (m_RootOverlay != null)
            {
                m_RootOverlay.style.display = DisplayStyle.None;
            }
        }

        // Call this from MainMenuController.OnDisable
        public void UnregisterCallbacks()
        {
            m_CloseButton?.UnregisterCallback<ClickEvent>(OnCloseClicked);
            m_LogOutButton?.UnregisterCallback<ClickEvent>(OnLogOutClicked);
        }

        // --- Button Click Handlers ---
        private void OnCloseClicked(ClickEvent evt)
        {
            Debug.Log("Close button clicked");
             Hide(); 
        }

        private void OnLogOutClicked(ClickEvent evt)
        {
            Debug.Log("Log Out button clicked!");
            Hide();
        }
    }
}