using System;
using Rustbound.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Rustbound.EditorTools
{
    public static class ModalInterfaceRevision
    {
        private const string PrefabPath = "Assets/Prefabs/UI/SharedModal.prefab";
        private const string MaterialPath = "Assets/Art/UI/Modal/NeutralLabel.mat";

        [MenuItem("Rustbound/UI/Fix Modal Button States")]
        public static void Apply()
        {
            var material = GetNeutralLabelMaterial();
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var normal = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Modal/ButtonNormal.png");
                var selected = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Modal/ButtonSelected.png");
                foreach (var visual in root.GetComponentsInChildren<ModalButtonVisual>(true))
                    visual.Configure(normal, selected, false);
                foreach (string path in new[] { "Composition/SettingsContent/Return", "Composition/VictoryContent/NextLevel", "Composition/FailureContent/RetryCheckpoint" })
                {
                    var button = root.transform.Find(path);
                    if (button == null) throw new InvalidOperationException("Missing modal button: " + path);
                    button.GetComponent<ModalButtonVisual>().ConfigureLabels(button.Find("Label").GetComponent<Image>(), material);
                }
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
            Debug.Log("MODAL_INTERFACE_REVISION_APPLIED: neutral buttons and original sprite labels preserved; final-level buttons center at runtime.");
        }

        public static Material GetNeutralLabelMaterial()
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Art/UI/Modal/NeutralLabel.shader");
            if (shader == null) throw new InvalidOperationException("Neutral modal label shader was not imported.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(shader) { name = "NeutralLabel" };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            material.shader = shader;
            material.SetColor("_TextColor", new Color(1f, 0.87f, 0.67f, 1f));
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
