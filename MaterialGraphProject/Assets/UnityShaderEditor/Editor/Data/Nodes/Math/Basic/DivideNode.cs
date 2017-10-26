using System.Reflection;

namespace UnityEngine.MaterialGraph
{
    [Title("Math/Basic/Divide")]
    public class DivideNode : CodeFunctionNode
    {
        public DivideNode()
        {
            name = "Divide";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Divide", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Divide(
            [Slot(0, Binding.None)] DynamicDimensionVector A,
            [Slot(1, Binding.None)] DynamicDimensionVector B,
            [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return @"
{
    Out = A / B;
}
";
        }
    }
}