using ModApi.Craft.Program;
using ModApi.Craft.Program.Instructions;
using UnityEngine;

namespace Assets.Scripts.Vizzy {
    public abstract class VizzyGLInstructionBase : ProgramInstruction {
        protected IVizzyGLContext DrawingContext { get; private set; }

        public override ProgramInstruction Execute(IThreadContext context) {
            this.BeforeExecute(context);
            this.ExecuteImpl(context);
            return base.Execute(context);
        }

        protected virtual void BeforeExecute(IThreadContext context) {
            if (this.DrawingContext == null) {
                var partScript = context.Craft.ExecutingPart.PartScript;
                // Hopefully this doesn't risk a deadlock, but we don't want multiple threads creating a drawing context simultaneously
                lock (partScript) {
                    this.DrawingContext = partScript.GetModifierWithInterface<IVizzyGLContext>();

                    if (this.DrawingContext == null) {
                        // Inject a new VizzyGLContext into the part that is running this script.
                        Debug.Log("Initialized new VizzyGL Context");
                        var data = new VizzyGLContextData(partScript.Data);
                        var newContext = (VizzyGLContext)data.CreateScript();
                        newContext.Initialize(data);

                        partScript.Modifiers.Add(newContext);
                        this.DrawingContext = newContext;
                    }
                }
            }
        }

        protected abstract void ExecuteImpl(IThreadContext context);
    }
}
