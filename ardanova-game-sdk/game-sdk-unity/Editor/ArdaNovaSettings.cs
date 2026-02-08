using UnityEditor;
using UnityEngine;

namespace ArdaNova.Editor
{
    /// <summary>
    /// ScriptableObject for storing ArdaNova API configuration in the Unity Editor.
    /// </summary>
    [CreateAssetMenu(fileName = "ArdaNovaSettings", menuName = "ArdaNova/Settings")]
    public class ArdaNovaSettings : ScriptableObject
    {
        [Header("API Configuration")]
        [Tooltip("Base URL of the ArdaNova API (e.g., https://api.ardanova.io)")]
        public string apiBaseUrl = "http://localhost:5000";

        [Header("Authentication")]
        [Tooltip("API key for server-to-server calls (optional)")]
        public string apiKey;
    }
}
