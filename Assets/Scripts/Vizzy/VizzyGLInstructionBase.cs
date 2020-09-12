using ModApi.Craft.Program;
using ModApi.Craft.Program.Instructions;
using UnityEngine;

namespace Assets.Scripts.Vizzy {
    public abstract class VizzyGLInstructionBase : ProgramInstruction, IVizzyGLProgramNode {
        protected IVizzyGLContext DrawingContext { get; private set; }

        public override ProgramInstruction Execute(IThreadContext context) {
            this.BeforeExecute(context);
            return this.ExecuteAndReturnNextImpl(context);
        }

        protected virtual void BeforeExecute(IThreadContext context) {
            if (this.DrawingContext == null) {
                var partScript = context.Craft.ExecutingPart.PartScript;
                // Hopefully this doesn't risk a deadlock, but we don't want multiple threads creating a drawing context simultaneously
                lock (partScript) {
                    this.DrawingContext = partScript.GetModifierWithInterface<IVizzyGLContext>();

                    if (this.DrawingContext == null) {
                        // Inject a new VizzyGLContext into the part that is running this script.
                        Debug.Log("Initializing new VizzyGL Context for VizzyGLInstruction.");
                        var data = new VizzyGLContextData(partScript.Data);
                        var newContext = (VizzyGLContext)data.CreateScript();

                        // We're haxin, we hope you like haxin too.
                        partScript.Modifiers.Add(newContext);
                        partScript.Data.Modifiers.Add(newContext.Data);

                        this.DrawingContext = newContext;
                    }
                }
            }
        }

        protected virtual ProgramInstruction ExecuteAndReturnNextImpl(IThreadContext context) {
            this.ExecuteImpl(context);
            return this.Next;
        }

        protected virtual void ExecuteImpl(IThreadContext context) {
            //  No-op
        }
    }
}
