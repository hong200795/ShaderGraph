using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    [Title("Input/Gradient/Gradient Asset")]
    public class GradientAssetNode : AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction
    {
        [SerializeField]
        private float m_Value;

        public const int OutputSlotId = 0;
        private const string kOutputSlotName = "Out";

        public GradientAssetNode()
        {
            name = "Gradient Asset";
            UpdateNodeAfterDeserialization();
        }

        Gradient m_Gradient = new Gradient();

        [SerializeField]
        Vector4[] m_SerializableColorKeys = { new Vector4(1f, 1f, 1f, 0f), new Vector4(0f, 0f, 0f, 1f), };

        [SerializeField]
        Vector2[] m_SerializableAlphaKeys = { new Vector2(1f, 0f), new Vector2(1f, 1f) };

        [GradientControl("")]
        public Gradient gradient
        {
            get
            {
                return m_Gradient;
            }
            set
            {
                var scope = ModificationScope.Nothing;

                var currentColorKeys = m_Gradient.colorKeys;
                var currentAlphaKeys = m_Gradient.alphaKeys;

                var newColorKeys = value.colorKeys;
                var newAlphaKeys = value.alphaKeys;

                if (currentColorKeys.Length != newColorKeys.Length || currentAlphaKeys.Length != newAlphaKeys.Length)
                {
                    scope = scope < ModificationScope.Graph ? ModificationScope.Graph : scope;
                }
                else
                {
                    for (var i = 0; i < currentColorKeys.Length; i++)
                    {
                        if (currentColorKeys[i].color != newColorKeys[i].color || Mathf.Abs(currentColorKeys[i].time - newColorKeys[i].time) > 1e-9)
                            scope = scope < ModificationScope.Node ? ModificationScope.Node : scope;
                    }

                    for (var i = 0; i < currentAlphaKeys.Length; i++)
                    {
                        if (Mathf.Abs(currentAlphaKeys[i].alpha - newAlphaKeys[i].alpha) > 1e-9 || Mathf.Abs(currentAlphaKeys[i].time - newAlphaKeys[i].time) > 1e-9)
                            scope = scope < ModificationScope.Node ? ModificationScope.Node : scope;
                    }
                }

                if (scope > ModificationScope.Nothing)
                {
                    gradient.SetKeys(newColorKeys, newAlphaKeys);
                    if (onModified != null)
                        onModified(this, scope);
                }
            }
        }

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            m_Gradient = new Gradient();
            var colorKeys = m_SerializableColorKeys.Select(k => new GradientColorKey(new Color(k.x, k.y, k.z, 1f), k.w)).ToArray();
            var alphaKeys = m_SerializableAlphaKeys.Select(k => new GradientAlphaKey(k.x, k.y)).ToArray();
            m_SerializableAlphaKeys = null;
            m_SerializableColorKeys = null;
            m_Gradient.SetKeys(colorKeys, alphaKeys);
        }

        public override void OnBeforeSerialize()
        {
            base.OnBeforeSerialize();
            m_SerializableColorKeys = gradient.colorKeys.Select(k => new Vector4(k.color.r, k.color.g, k.color.b, k.time)).ToArray();
            m_SerializableAlphaKeys = gradient.alphaKeys.Select(k => new Vector2(k.alpha, k.time)).ToArray();
        }

        public override bool hasPreview { get { return false; } }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new GradientMaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output,0));
            RemoveSlotsNameNotMatching(new[] { OutputSlotId });
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            visitor.AddShaderChunk("Gradient " + GetVariableNameForNode() + ";", true);
            visitor.AddShaderChunk(string.Format("Unity_{0} ({0});", GetVariableNameForNode()), true);
        }

        string GetColorKey(int index, Color color, float time)
        {
            return string.Format("g.colors[{0}] = float4({1}, {2}, {3}, {4});", index, color.r, color.g, color.b, time);
        }

        string GetAlphaKey(int index, float alpha, float time)
        {
            return string.Format("g.alphas[{0}] = float2({1}, {2});", index, alpha, time);
        }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            string[] colors = new string[8];
            for(int i = 0; i < colors.Length; i++)
                colors[i] = string.Format("g.colors[{0}] = float4(0, 0, 0, 0);", i.ToString());
            for(int i = 0; i < m_Gradient.colorKeys.Length; i++)
                colors[i] = GetColorKey(i, m_Gradient.colorKeys[i].color, m_Gradient.colorKeys[i].time);

            string[] alphas = new string[8];
            for(int i = 0; i < colors.Length; i++)
                alphas[i] = string.Format("g.alphas[{0}] = float2(0, 0);", i.ToString());
            for(int i = 0; i < m_Gradient.alphaKeys.Length; i++)
                alphas[i] = GetAlphaKey(i, m_Gradient.alphaKeys[i].alpha, m_Gradient.alphaKeys[i].time);

            visitor.AddShaderChunk(string.Format("void Unity_{0} (out Gradient Out)", GetVariableNameForNode()), true);
            visitor.AddShaderChunk("{", true);
            visitor.AddShaderChunk("Gradient g;", true);
            visitor.AddShaderChunk("g.type = 0;", true);
            visitor.AddShaderChunk(string.Format("g.colorsLength = {0};", m_Gradient.colorKeys.Length), true);
            visitor.AddShaderChunk(string.Format("g.alphasLength = {0};", m_Gradient.alphaKeys.Length), true);

            for(int i = 0; i < colors.Length; i++)
                visitor.AddShaderChunk(colors[i], true);

            for(int i = 0; i < alphas.Length; i++)
                visitor.AddShaderChunk(alphas[i], true);
            
            visitor.AddShaderChunk("Out = g;", true);
            visitor.AddShaderChunk("}", true);
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            return GetVariableNameForNode();
        }
    }
}