using System;
using ModApi.Craft.Program;

namespace Assets.Scripts.Vizzy {
    [Serializable]
    public class RemoveObjectInstruction  : VizzyGLInstructionBase {
        public const String XmlName = "RemoveObject";

        protected override void ExecuteImpl(IThreadContext context) {
            var name = this.GetExpression(0).Evaluate(context).TextValue;
            if (!String.IsNullOrWhiteSpace(name)) {
                this.DrawingContext.RemoveObject(name);
            } else {
                this.DrawingContext.RemoveAllObjects();
            }
        }
    }
}
