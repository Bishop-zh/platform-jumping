using UnityEditor;
public static class InterfaceBackdropRevision
{
    public static void Run()
    {
        AssetDatabase.Refresh();
        Rustbound.EditorTools.ModalInterfaceRevision.Apply();
        Level03BackgroundRevision.Run();
    }
}
