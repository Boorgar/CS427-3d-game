using System.IO;
using UnityEditor;
using UnityEngine;

namespace TanksMP
{
    [InitializeOnLoad]
    public class ReviewWindowEditor : EditorWindow
    {
		private static Texture2D reviewWindowImage;
		private static string imagePath = "/EditorFiles/Asset_smallLogo.png";

        [MenuItem("Window/Tanks Multiplayer/Review Asset")]
        static void Init()
        {
            EditorWindow.GetWindowWithRect(typeof(ReviewWindowEditor), new Rect(0, 0, 256, 330), false, "Review Asset");
        }

        void OnGUI()
        {		
			if(reviewWindowImage == null)
			{
				var script = MonoScript.FromScriptableObject(this);
				string path = Path.GetDirectoryName(AssetDatabase.GetAssetPath(script));
				reviewWindowImage = AssetDatabase.LoadAssetAtPath(path + imagePath, typeof(Texture2D)) as Texture2D;
			}
			
			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(30);
			GUILayout.Label(reviewWindowImage);
			EditorGUILayout.EndHorizontal();
			
			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(40);
			EditorGUILayout.LabelField("Review Tanks Multiplayer", EditorStyles.boldLabel, GUILayout.Width(200));
			EditorGUILayout.EndHorizontal();
			
			EditorGUILayout.Space();
            EditorGUILayout.LabelField("Please consider giving me a rating on the");
            EditorGUILayout.LabelField("Unity Asset Store. Your support helps me");
			EditorGUILayout.LabelField("to improve this product. Thank you!");
            EditorGUILayout.Space();

            if (GUILayout.Button("Review now!", GUILayout.Height(40)))
            {
                Help.BrowseURL("https://assetstore.unity.com/packages/slug/69172?aid=1011lGiF&pubref=editor_tanksmp");
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("If you are looking for support, please head");
            EditorGUILayout.LabelField("over to my Unity forum thread instead.");
        }
    }
}