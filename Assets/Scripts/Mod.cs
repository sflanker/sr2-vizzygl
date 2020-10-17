using Assets.Scripts.Vizzy.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Assets.Scripts.Craft.Parts.Modifiers;
using Assets.Scripts.Vizzy;
using ModApi.Craft;
using ModApi.Craft.Program;
using ModApi.Craft.Program.Instructions;
using ModApi.Mods;
using ModApi.Ui;
using ModApi.Ui.Events;
using UnityEngine;

namespace Assets.Scripts {
    /// <summary>
    /// A singleton object representing this mod that is instantiated and initialize when the mod is loaded.
    /// </summary>
    public class Mod : GameMod {
        private static readonly Dictionary<string, (Type, Func<ProgramNode>)> ModExpressionAndInstructions;
        private static readonly FieldInfo VizzyToolboxColorsField;
        private static readonly FieldInfo VizzyToolboxStylesField;

        private static VizzyToolbox _vizzyGLToolbox;

        private static VizzyToolbox VizzyGLToolbox {
            get {
                if (_vizzyGLToolbox == null) {
                    var vizzyGLToolboxXml =
                        ModApi.Common.Game.Instance.UserInterface.ResourceDatabase.GetResource<TextAsset>("VizzyGL/Vizzy/VizzyGLToolbox");
                    if (vizzyGLToolboxXml != null) {
                        _vizzyGLToolbox =
                            new VizzyToolbox(XElement.Parse(vizzyGLToolboxXml.text), false);
                    } else {
                        Debug.LogError("VizzyGL: The VizzyGLToolbox Resource Was Not Found.");
                    }
                }

                return _vizzyGLToolbox;
            }
        }

        static Mod() {
            ModExpressionAndInstructions = new Dictionary<string, (Type, Func<ProgramNode>)> {
                { SetContextPropertyInstruction.XmlName, (typeof(SetContextPropertyInstruction), () => new SetContextPropertyInstruction()) },
                { LoadSpriteInstruction.XmlName, (typeof(LoadSpriteInstruction), () => new LoadSpriteInstruction()) },
                { DrawPrimitiveInstruction.XmlName, (typeof(DrawPrimitiveInstruction), () => new DrawPrimitiveInstruction()) },
                { DrawSpriteInstruction.XmlName, (typeof(DrawSpriteInstruction), () => new DrawSpriteInstruction()) },
                { UpdateObjectInstruction.XmlName, (typeof(UpdateObjectInstruction), () => new UpdateObjectInstruction()) },
                { RemoveObjectInstruction.XmlName, (typeof(RemoveObjectInstruction), () => new RemoveObjectInstruction()) },
            };

            VizzyToolboxColorsField =
                typeof(VizzyToolbox).GetField("_colors", BindingFlags.NonPublic | BindingFlags.Instance);
            VizzyToolboxStylesField =
                typeof(VizzyToolbox).GetField("_styles", BindingFlags.NonPublic | BindingFlags.Instance);

            var programNodeCreatorType =
                typeof(ProgramSerializer).GetNestedType("ProgramNodeCreator", BindingFlags.NonPublic);

            var programSerializerTypeNameLookupField =
                typeof(ProgramSerializer).GetField("_typeNameLookup", BindingFlags.NonPublic | BindingFlags.Static);
            var programSerializerXmlNameLookupField =
                typeof(ProgramSerializer).GetField("_xmlNameLookup", BindingFlags.NonPublic | BindingFlags.Static);

            if (programNodeCreatorType != null &&
                programSerializerTypeNameLookupField != null &&
                programSerializerXmlNameLookupField != null) {
                var programNodeCreatorConstructor =
                    programNodeCreatorType.GetConstructor(new[] {
                        typeof(String),
                        typeof(Type),
                        typeof(Func<ProgramNode>)
                    });
                if (programNodeCreatorConstructor != null) {
                    var typeNameLookup = (IDictionary)programSerializerTypeNameLookupField.GetValue(null);
                    var xmlNameLookup = (IDictionary)programSerializerXmlNameLookupField.GetValue(null);

                    foreach (var kvp in ModExpressionAndInstructions) {
                        var xmlName = kvp.Key;
                        var (type, ctor) = kvp.Value;

                        var programNodeCreator =
                            programNodeCreatorConstructor.Invoke(
                                new System.Object[] {
                                    xmlName,
                                    type,
                                    ctor
                                });

                        xmlNameLookup[xmlName] = typeNameLookup[type.Name] = programNodeCreator;
                    }
                } else {
                    Debug.LogError("VizzyGL: Constructor for ProgramNodeCreator not found.");
                }
            } else {
                Debug.LogError(
                    "VizzyGL: Reflection Failed. Unable to find expected internal type ProgramSerializer.ProgramNodeCreator, or one of the expected private fields _typeNameLookup or _xmlNameLookup on ProgramSerializer.");
            }
        }

        /// <summary>
        /// Prevents a default instance of the <see cref="Mod"/> class from being created.
        /// </summary>
        private Mod() {
        }

        /// <summary>
        /// Gets the singleton instance of the mod object.
        /// </summary>
        /// <value>The singleton instance of the mod object.</value>
        public static Mod Instance { get; } = GetModInstance<Mod>();

        public override Boolean IsModRequiredForCraft(CraftData craft) {
            return base.IsModRequiredForCraft(craft) || DoesCraftUseVizzyGL(craft);
        }

        protected override void OnModInitialized() {
            var ui = ModApi.Common.Game.Instance.UserInterface;

            ui.UserInterfaceLoading += UiOnUserInterfaceLoading;

            base.OnModInitialized();
        }

        private static Boolean DoesCraftUseVizzyGL(CraftData craft) {
            Debug.Log($"Checking craft {craft.Name} for VizzyGL usage.");
            if (craft.Assembly != null) {
                foreach (var part in craft.Assembly.Parts) {
                    var flightProgramData = part.GetModifier<FlightProgramData>();

                    if (flightProgramData?.FlightProgramXml != null) {
                        try {
                            var flightProgram = flightProgramData.Script.FlightProgram ??
                                new ProgramSerializer().DeserializeFlightProgram(flightProgramData.FlightProgramXml);
                            if (ContainsVizzyGL(flightProgram.RootInstructions) ||
                                ContainsVizzyGL(flightProgram.RootExpressions) ||
                                ContainsVizzyGL(flightProgram.CustomExpressions) ||
                                ContainsVizzyGL(flightProgram.CustomInstructions)) {
                                Debug.Log($"Flight Program on part {part.Name} contains VizzyGL.");
                                return true;
                            } else {
                                Debug.Log($"Flight Program on part {part.Name} does not contain VizzyGL.");
                            }
                        } catch (Exception ex) {
                            Debug.LogWarning($"Unable to deserialize Flight Program: {flightProgramData.FlightProgramXml}");
                            Debug.LogError(ex);
                        }
                    }
                }
            } else {
                Debug.LogWarning("Unable to check craft for VizzyGL because Assembly is null.");
            }

            return false;
        }

        private static Boolean ContainsVizzyGL(IEnumerable<ProgramInstruction> instructions) {
            var instructionStack = new Stack<ProgramInstruction>(instructions);
            while (instructionStack.Count > 0) {
                var instruction = instructionStack.Pop();
                if (instruction is IVizzyGLProgramNode) {
                    return true;
                }

                if (instruction.Expressions != null && ContainsVizzyGL(instruction.Expressions)) {
                    return true;
                }

                if (instruction.Next != null) {
                    instructionStack.Push(instruction.Next);
                }

                if (instruction.SupportsChildren) {
                    instructionStack.Push(instruction.FirstChild);
                }
            }

            return false;
        }

        private static Boolean ContainsVizzyGL(IEnumerable<ProgramExpression> expressions) {
            var expressionStack = new Stack<ProgramExpression>(expressions);
            while (expressionStack.Count > 0) {
                var expression = expressionStack.Pop();
                if (expression is IVizzyGLProgramNode) {
                    return true;
                } else if (expression.Expressions != null) {
                    foreach (var child in expression.Expressions.Reverse()) {
                        expressionStack.Push(child);
                    }
                }
            }

            return false;
        }

        private void UiOnUserInterfaceLoading(object sender, UserInterfaceLoadingEventArgs e) {
            var vizzyUI = e.XmlLayout.GameObject.GetComponent<VizzyUIController>();
            if (e.UserInterfaceId == UserInterfaceIds.Vizzy) {
                if (VizzyGLToolbox != null) {
                    if (vizzyUI.VizzyUI.Toolbox != null) {
                        MergeToolbox(
                            vizzyUI.VizzyUI.Toolbox,
                            VizzyGLToolbox
                        );
                    } else {
                        Debug.LogError("VizzyGL: The default Vizzy Toolbox isn't loaded yet.");
                    }
                } else {
                    Debug.LogError("VizzyGL: Unable to load VizzyGLToolbox.");
                }
            }
        }

        private void MergeToolbox(
            VizzyToolbox baseToolbox,
            VizzyToolbox extensionToolbox) {
            if (VizzyToolboxColorsField == null) {
                throw new InvalidOperationException($"{nameof(VizzyToolboxColorsField)} is null.");
            } else if (VizzyToolboxStylesField == null) {
                throw new InvalidOperationException($"{nameof(VizzyToolboxStylesField)} is null.");
            }

            var baseColors = (Dictionary<String, Color>)VizzyToolboxColorsField.GetValue(baseToolbox);
            var baseStyles = (Dictionary<String, NodeStyle>)VizzyToolboxStylesField.GetValue(baseToolbox);

            var extensionColors = (Dictionary<String, Color>)VizzyToolboxColorsField.GetValue(extensionToolbox);
            foreach (var color in extensionColors) {
                baseColors[color.Key] = color.Value;
            }

            var extensionStyles = (Dictionary<String, NodeStyle>)VizzyToolboxStylesField.GetValue(extensionToolbox);
            foreach (var style in extensionStyles) {
                baseStyles[style.Key] = style.Value;
            }

            foreach (var category in extensionToolbox.Categories) {
                if (!baseToolbox.Categories.Any(c => String.Equals(c.Name, category.Name, StringComparison.OrdinalIgnoreCase))) {
                    baseToolbox.Categories.Add(category);
                } else {
                    Debug.LogError($"VizzyGL: Duplicate Toolbox Category: '{category.Name}'");
                }
            }
        }
    }
}
