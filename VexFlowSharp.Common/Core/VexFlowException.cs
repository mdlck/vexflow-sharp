// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

namespace VexFlowSharp
{
    /// <summary>
    /// Shared exception type for VexFlowSharp.
    /// Maps to VexFlow's RuntimeError(code, message) pattern.
    /// </summary>
    public class VexFlowException : System.Exception
    {
        /// <summary>Short machine-readable error code (e.g., "NoContext", "BadArguments").</summary>
        public string Code { get; }

        /// <summary>
        /// Create a new VexFlowException.
        /// </summary>
        /// <param name="code">Short error code (e.g., "NoContext").</param>
        /// <param name="message">Human-readable description.</param>
        public VexFlowException(string code, string message) : base(message)
        {
            Code = code;
        }
    }
}
