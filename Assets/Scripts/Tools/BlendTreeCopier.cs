using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class BlendTreeCopierWindow : EditorWindow
{
    private AnimatorController sourceController;
    private string originalBlendTreeName;
    private string newBlendTreeName;
    private int layerIndex = 0;

    [MenuItem("Tools/Blend Tree Copier")]
    public static void ShowWindow()
    {
        GetWindow<BlendTreeCopierWindow>("Blend Tree Copier");
    }

    private void OnGUI()
    {
        GUILayout.Label("Blend Tree Copier", EditorStyles.boldLabel);

        sourceController = (AnimatorController)EditorGUILayout.ObjectField("Source Animator Controller", sourceController, typeof(AnimatorController), false);
        layerIndex = EditorGUILayout.IntField("Layer Index", layerIndex);
        originalBlendTreeName = EditorGUILayout.TextField("Original Blend Tree Name", originalBlendTreeName);
        newBlendTreeName = EditorGUILayout.TextField("New Blend Tree Name", newBlendTreeName);

        if (GUILayout.Button("Copy Blend Tree"))
        {
            CopyBlendTree();
        }
    }

    private void CopyBlendTree()
    {
        if (sourceController == null || string.IsNullOrEmpty(originalBlendTreeName) || string.IsNullOrEmpty(newBlendTreeName))
        {
            Debug.LogError("모든 필드를 채워주세요.");
            return;
        }

        var originalBlendTree = FindBlendTree(sourceController, originalBlendTreeName,layerIndex);
        if (originalBlendTree == null)
        {
            Debug.LogError("원본 블렌드 트리를 찾을 수 없습니다.");
            return;
        }

        var newBlendTree = new BlendTree
        {
            name = newBlendTreeName
        };

        CopyBlendTreeSettings(originalBlendTree, newBlendTree);

        var layer = sourceController.layers[layerIndex];
        layer.stateMachine.AddState(newBlendTreeName).motion = newBlendTree;

        Debug.Log("블렌드 트리 복사가 완료되었습니다.");
    }

    private BlendTree FindBlendTree(AnimatorController controller, string blendTreeName, int layerIndex)
    {
        var layer = controller.layers[layerIndex];
        foreach (var state in layer.stateMachine.states)
        {
            if (state.state.motion is BlendTree blendTree && blendTree.name == blendTreeName)
            {
                return blendTree;
            }
        }
        return null;
    }

    private void CopyBlendTreeSettings(BlendTree original, BlendTree copy)
    {
        copy.blendParameter = original.blendParameter;
        copy.blendParameterY = original.blendParameterY;
        copy.blendType = original.blendType;
        copy.useAutomaticThresholds = original.useAutomaticThresholds;

        foreach (var child in original.children)
        {
            if (child.motion is BlendTree childBlendTree)
            {
                var newChildBlendTree = new BlendTree
                {
                    name = childBlendTree.name
                };
                CopyBlendTreeSettings(childBlendTree, newChildBlendTree);

                copy.AddChild(newChildBlendTree);
            }
            else
            {
                ChildMotion newMotion = new();
                newMotion.motion = child.motion;
                newMotion.threshold = child.threshold;
                newMotion.position = child.position;

                if (copy.blendType == BlendTreeType.Simple1D)
                    copy.AddChild(newMotion.motion, newMotion.threshold);
                else if (copy.blendType != BlendTreeType.Direct)
                    copy.AddChild(newMotion.motion, newMotion.position);
            }
        }
    }
}
