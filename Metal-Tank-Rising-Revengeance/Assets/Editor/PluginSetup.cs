using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;

namespace TanksMP
{
    public class PluginSetup : EditorWindow
    {
        private static Texture2D[] pluginImages;
        private static string packagesPath;

        private enum Packages
        {
            UnityMLAPI = 0,
            PhotonPUN = 1,
            Mirror = 2
        }


        [MenuItem("Window/Tanks Multiplayer/Network Setup")]
        static void Init()
        {
            packagesPath = "/Packages/";
            EditorWindow window = GetWindowWithRect(typeof(PluginSetup), new Rect(0, 0, 850, 420), false, "Network Setup");

            var script = MonoScript.FromScriptableObject(window);
            string thisPath = AssetDatabase.GetAssetPath(script);
            packagesPath = thisPath.Replace("/PluginSetup.cs", packagesPath);
        }


        void OnGUI()
        {
            if (pluginImages == null)
            {
                var script = MonoScript.FromScriptableObject(this);
                string path = Path.GetDirectoryName(AssetDatabase.GetAssetPath(script)) + "/EditorFiles/";

                int enumLength = System.Enum.GetNames(typeof(Packages)).Length;
                pluginImages = new Texture2D[enumLength];
                for(int i = 0; i < enumLength; i++)
                    pluginImages[i] = AssetDatabase.LoadAssetAtPath(path + ((Packages)i).ToString() + ".png", typeof(Texture2D)) as Texture2D;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Tanks Multiplayer - Network Setup", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Please select the network integration you are going to use this asset with (must be imported beforehand!).");
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Note: For a detailed comparison about features and pricing, please refer to the provider's website.");
            EditorGUILayout.LabelField("If possible, the features of this asset are the same across all multiplayer services.");
            EditorGUILayout.Space(20);

            EditorGUILayout.BeginHorizontal();
            for(int i = 0; i < pluginImages.Length; i++)
            {
                EditorGUILayout.BeginVertical();
                if (GUILayout.Button(pluginImages[i]))
                {
                    Setup(i);
                }

                Packages thisPackage = (Packages)i;
                if (GUILayout.Button("[ ? ]", GUILayout.Width(30)))
                {
                    switch (thisPackage)
                    {
                        case Packages.UnityMLAPI:
                            Application.OpenURL("https://docs-multiplayer.unity3d.com/");
                            break;
                        case Packages.PhotonPUN:
                            Application.OpenURL("https://www.photonengine.com/en/Realtime");
                            break;
                        case Packages.Mirror:
                            Application.OpenURL("https://mirror-networking.com/");
                            break;
                    }
                }

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(20);

            EditorGUILayout.LabelField("Please read the PDF documentation for networking implementation details.");
            EditorGUILayout.LabelField("Support links: Window > Tanks Multiplayer > About.");
        }


        void Setup(int index)
        {
            switch(index)
            {
                case (int)Packages.UnityMLAPI:
                    AssetDatabase.ImportPackage(packagesPath + Packages.UnityMLAPI.ToString() + ".unitypackage", true);
                    break;

                case (int)Packages.PhotonPUN:
                    AssetDatabase.ImportPackage(packagesPath + Packages.PhotonPUN.ToString() + ".unitypackage", true);
                    break;

                case (int)Packages.Mirror:
                    AssetDatabase.ImportPackage(packagesPath + Packages.Mirror.ToString() + ".unitypackage", true);
                    break;
            }
        }
    }
}