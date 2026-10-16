using UnityEditor;
using UnityEditor.SceneManagement;

public static class GameplayUiEditing
{
    [MenuItem("Rustbound/UI/Edit Gameplay HUD")]
    public static void Hud()=>PrefabStageUtility.OpenPrefab("Assets/Prefabs/UI/GameplayHUD.prefab");
    [MenuItem("Rustbound/UI/Edit Gameplay Pause Window")]
    public static void Pause()=>PrefabStageUtility.OpenPrefab("Assets/Prefabs/UI/GameplayPauseModal.prefab");
}
