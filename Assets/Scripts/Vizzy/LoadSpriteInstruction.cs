using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading.Tasks;
using ModApi.Craft.Program;
using ModApi.Craft.Program.Instructions;
using UnityEngine;

namespace Assets.Scripts.Vizzy {
    [Serializable]
    public class LoadSpriteInstruction : VizzyGLInstructionBase {
        public const String XmlName = "LoadSprite";

        private static ConcurrentDictionary<String, Task<Either<Byte[], String>>> spriteCache =
            new ConcurrentDictionary<String, Task<Either<Byte[], String>>>();

        protected override ProgramInstruction ExecuteAndReturnNextImpl(IThreadContext context) {
            var url = this.GetExpression(0).Evaluate(context).TextValue;

            var spriteDataTask = spriteCache.GetOrAdd(url, this.GetImageData);

            if (spriteDataTask.IsCompleted) {
                var result = spriteDataTask.Result;
                switch (result.Type) {
                    case EitherType.Left:
                        this.DrawingContext.Sprite = result.Left;

                        break;
                    case EitherType.Right:
                        Debug.LogWarning(result.Right);

                        break;
                    case EitherType.Unspecified:
                    default:
                        Debug.LogWarning("Unexpected result return from get sprite image data task.");
                        break;
                }

                return this.Next;
            } else {
                context.BreakExecution(BreakExecutionType.Wait);
                return this;
            }
        }

        private async Task<Either<Byte[], String>> GetImageData(String url) {
            using (var webClient = new WebClient()) {
                try {
                    var response = await webClient.DownloadDataTaskAsync(url);
                    return response;
                } catch (WebException ex) {
                    if (ex.Status == WebExceptionStatus.ProtocolError) {
                        var response = (HttpWebResponse)ex.Response;
                        return $"Unable to fetch sprite image data: HTTP Error {response.StatusCode} - {response.StatusDescription}";
                    } else {
                        return $"Unable to fetch sprite image data: {ex.Message}";
                    }
                }
            }
        }
    }

    public readonly struct Either<TLeft, TRight> {
        private readonly EitherType type;
        private readonly TLeft left;
        private readonly TRight right;

        public EitherType Type => this.type;

        public Boolean IsLeft => this.type == EitherType.Left;
        public Boolean IsRight => this.type == EitherType.Right;

        public TLeft Left {
            get {
                if (this.type != EitherType.Left) {
                    throw new InvalidOperationException("Accessed Left value of Right typed Either.");
                }

                return this.left;
            }
        }

        public TRight Right {
            get {
                if (this.type != EitherType.Right) {
                    throw new InvalidOperationException("Accessed Right value of Left typed Either.");
                }

                return this.right;
            }
        }

        public Either(TLeft left) {
            this.type = EitherType.Left;
            this.left = left;
            this.right = default;
        }

        public Either(TRight right) {
            this.type = EitherType.Right;
            this.right = right;
            this.left = default;
        }

        public static implicit operator Either<TLeft, TRight>(TLeft left) {
            return new Either<TLeft, TRight>(left);
        }

        public static implicit operator Either<TLeft, TRight>(TRight right) {
            return new Either<TLeft, TRight>(right);
        }
    }

    public enum EitherType {
        Unspecified,
        Left,
        Right
    }
}
