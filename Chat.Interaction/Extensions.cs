using Chat.Common;
using Chat.Interaction.Xml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Chat.Interaction
{
    public static class Extensions
    {
        public static ErrorInfoType MakeErrorInfo(this Exception ex2, ErrorInfoType infoToFill = null)
        {
            if (ex2 == null)
                return null;

            infoToFill = infoToFill ?? new ErrorInfoType();
            infoToFill.Message = ex2.Message;
            infoToFill.StackTrace = ex2.StackTrace;
            infoToFill.TypeName = ex2.GetType().FullName;
            infoToFill.InnerError = MakeErrorInfo(ex2.InnerException);

            var frames = new StackTrace(ex2, true).GetFrames();
            if (frames != null)
            {
                infoToFill.StackDetails = frames.Select(f => new StackItemInfoType() {
                    MethodSignature = f.GetMethod().GetMethodSignature(),
                    Location = f.GetFileName().IsEmpty() ? null : new SourceLocationInfoType() {
                        Column = f.GetFileColumnNumber(),
                        Line = f.GetFileLineNumber(),
                        FileName = f.GetFileName()
                    }
                }).ToArray();
            }

            return infoToFill;
        }


        public static string FormatErrorInfo(this ErrorInfoType ex)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine($"--- Exception {ex.TypeName} ({(string.IsNullOrWhiteSpace(ex.Message) ? string.Empty : (": " + ex.Message))}) at {{");
                FormatErrorInfoImpl(sb, ex);
                sb.AppendLine("--- } ");
                return sb.ToString();
            }
            catch (Exception ex2)
            {
                System.Diagnostics.Debug.Print(ex2.ToString());
                return ex.ToString();
            }
        }

        private static void FormatErrorInfoImpl(StringBuilder sb, ErrorInfoType ex)
        {

            if (ex.InnerError != null)
            {
                var e = ex.InnerError;
                FormatErrorInfoImpl(sb, e);

                sb.AppendLine($"--- wrapped with {e.TypeName} ({(string.IsNullOrWhiteSpace(e.Message) ? string.Empty : (": " + e.Message))}) at ");
            }

            // D:\Home\Ged\portable-project.ru\runtime\src\Portable.Common\Net\Discovery\DiscoverClient.cs(81,36,81,38): warning CS0168: The variable 'ex' is declared but never used

            if (ex.StackDetails == null || ex.StackDetails.Length == 0)
            {
                sb.AppendLine(ex.StackTrace);
            }
            else
            {
                var fnameLength = ex.StackDetails.Max(f => f.Location?.FileName?.Length ?? 0);

                var lines = ex.StackDetails.Select(f => new {
                    method = f.MethodSignature,
                    prefixString = f.Location == null ? string.Empty : $"{f.Location.FileName}({f.Location.Line},{f.Location.Column}): "
                }).ToList();

                var prefixLength = lines.Max(l => l.prefixString.Length);
                lines.ForEach(f => sb.AppendLine($"{f.prefixString.PadRight(prefixLength, ' ')}{f.method}"));
            }
        }

        public static string GetMethodSignature(this MethodBase method)
        {
            var methodArgs = method.GetParameters().Length > 0 ? "..." : string.Empty;

            return $"{method.DeclaringType?.FullName}::{method.Name}({methodArgs})";
        }
    }
}
