/***************************************************************************

based on vssdk sample typing speed meter
https://github.com/microsoft/VSSDK-Extensibility-Samples/blob/master/Typing_Speed_Meter/C%23/CommandFilter.cs

***************************************************************************/

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using EnvDTE90;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

namespace MarvelNames
{

    [Export(typeof(IVsTextViewCreationListener))]
    [TextViewRole(PredefinedTextViewRoles.Editable)]
    [ContentType("text")]
    internal sealed class VsTextViewListener : IVsTextViewCreationListener
    {
        [Import]
        internal IVsEditorAdaptersFactoryService AdapterService = null;

        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            var textView = AdapterService.GetWpfTextView(textViewAdapter);
            if (textView == null)
                return;

            var extension = GetExtension(textView);


            //var adornment = textView.Properties.GetProperty<TypingSpeedMeter>(typeof(TypingSpeedMeter));
            if (IsLanguageSupported(extension, out var regex))
            {
                textView.Properties.GetOrCreateSingletonProperty(
                    () => new TypeCharFilter(textViewAdapter, textView,
                    regex));
            }

        }

        public static Regex csharpRegex, cppRegex, javascriptRegex, javaRegex, pythonRegex;

        bool IsLanguageSupported(string extension, out Regex regex)
        {
            if (csharpExtensions.Contains(extension))
            {
                if (csharpRegex == null)
                    csharpRegex = createRegex(csharpKeywords);
                regex = csharpRegex;
            }
            else if (cppExtensions.Contains(extension))
            {
                if (cppRegex == null)
                    cppRegex = createRegex(cppKeywords);
                regex = cppRegex;
            }
            else if (javascriptExtensions.Contains(extension))
            {
                if (javascriptRegex == null)
                    javascriptRegex = createRegex(javascriptKeywords);
                regex = javascriptRegex;
            }
            else if (javaExtensions.Contains(extension))
            {
                if (javaRegex == null)
                    javaRegex = createRegex(javaKeywords);
                regex = javaRegex;
            }
            else if (pythonExtensions.Contains(extension))
            {
                if (pythonRegex == null)
                    pythonRegex = createRegex(pythonKeywords);
                regex = pythonRegex;
            }
            else
            {
                regex = null;
            }

            return regex != null;

            Regex createRegex(string[] keywords)
            {
                var pattern = String.Format(
                    "([ \\t\\n]|^)({0})(\\*|&|\\?)*(\\[(\\,)*\\])*[ \\t]{1}",
                    string.Join("|", keywords),
                    TypeCharFilter.trailingCharacters ? "" : "$");
                return new Regex(pattern);
            }
        }

        public static string GetExtension(IWpfTextView textView)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            textView.TextBuffer.Properties.TryGetProperty(typeof(IVsTextBuffer), out IVsTextBuffer bufferAdapter);
            var persistFileFormat = bufferAdapter as Microsoft.VisualStudio.Shell.Interop.IPersistFileFormat;

            if (persistFileFormat == null)
            {
                return null;
            }
            persistFileFormat.GetCurFile(out string filePath, out _);
            var extension = System.IO.Path.GetExtension(filePath);
            //return filePath;
            return extension.ToLower();
        }


        // language list from: https://docs.microsoft.com/en-us/visualstudio/ide/adding-visual-studio-editor-support-for-other-languages?view=vs-2022
        /*HashSet<string> both = new HashSet<string>(
            new string[]{
                ".cs", ".csx", // c#
                ".c", ".cc",  ".cpp", ".cxx", ".c++", ".h", ".hh", ".hpp", ".hxx", ".h++", // c/c++
                ".ts", ".tsx", // typescript
                ".js", ".cjs", ".mjs", // javascript
                ".java", ".class", ".jmod", ".jar", // java
                ".py", // python
            });*/

        public static readonly string[] csharpExtensions = new string[]
        {
            ".cs", ".csx", // c#
        };
        public static readonly string[] cppExtensions = new string[]
        {
            ".c", ".cc",  ".cpp", ".cxx", ".c++", ".h", ".hh", ".hpp", ".hxx", ".h++", // c/c++
        };
        public static readonly string[] javascriptExtensions = new string[]
        {
            ".ts", ".tsx", // typescript
            ".js", ".cjs", ".mjs", // javascript
        };
        public static readonly string[] javaExtensions = new string[]
        {
            ".java", ".class", ".jmod", ".jar", // java
        };
        public static readonly string[] pythonExtensions = new string[]
        {
            ".py", // python
        };


        public static readonly string[] csharpKeywords = new string[]
        {
            "bool",
            "byte",
            "char",
            "class",
            "decimal",
            "double",
            "enum",
            "float",
            "int",
            "interface",
            "long",
            "namespace",
            "sbyte",
            "short",
            "string",
            "struct",
            "uint",
            "ulong",
            "ushort",
            "void",
            
            "dynamic",
            "var",
            "record",
        };

        public static readonly string[] cppKeywords = new string[]
        {
            "auto",
            "bool",
            "char",
            "char8_t",
            "char16_t",
            "char32_t",
            "class",
            "double",
            "enum",
            "float",
            "int",
            "long",
            "short",
            "struct",
            "union",
            "void",
            "wchar_t",
        };

        public static readonly string[] javaKeywords = new string[]
        {
            "boolean",
            "byte",
            "char",
            "class",
            "double",
            "enum",
            "float",
            "int",
            "interface",
            "long",
            "package",
            "short",
            "void",
        };

        public static readonly string[] javascriptKeywords = new string[]
        {
            "class",
            "const",
            "function",
            "interface",
            "let",
            "var",
        };

        public static readonly string[] pythonKeywords = new string[]
        {
            "as",
            "class",
            "def",
            "global",
            "nonlocal",
        };

    }


    internal sealed class TypeCharFilter : IOleCommandTarget
    {
        readonly IOleCommandTarget nextCommandHandler;
        readonly ITextView textView;
        //internal int typedChars { get; set; }

        public const bool leadingCharacters = false;
        public const bool trailingCharacters = false;

        readonly Regex regex;

        /// <summary>
        /// Add this filter to the chain of Command Filters
        /// </summary>
        internal TypeCharFilter(IVsTextView adapter, ITextView textView,
            Regex regex)
        {
            this.textView = textView;
            this.regex = regex;
            
            adapter.AddCommandFilter(this, out nextCommandHandler);
        }

        /// <summary>
        /// Get user input and update Typing Speed meter. Also provides public access to
        /// IOleCommandTarget.Exec() function
        /// </summary>
        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            //int hr = VSConstants.S_OK;
            int hr = nextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);

            if (hr != VSConstants.S_OK)
                return hr;

            char typedChar;
            if (TryGetTypedChar(pguidCmdGroup, nCmdID, pvaIn, out typedChar))
            {
                //adornment.UpdateBar(typedChars++);
                NameACandidate(typedChar);
            }

            return hr;
        }

        private void NameACandidate(char typedChar)
        {
            if (typedChar != ' ' &&
                typedChar != '\t')
                return;

            var caretPosition = textView.Caret.Position.BufferPosition;
            var lineRaw = textView.TextBuffer.CurrentSnapshot.GetLineFromPosition(caretPosition);
            var line = lineRaw.Extent;

            // if there are trailing characters
            if (!trailingCharacters && caretPosition != line.End)
                return;


            // check previous char
            //if (!line.Contains(caretPosition - 2) || line.Snapshot[caretPosition - 2] != '/')
            //return;

            //if (!line.Contains(caretPosition - 4) || line.Snapshot[caretPosition - 4] != 'i' ||
            //  line.Snapshot[caretPosition - 3] != 'n' || line.Snapshot[caretPosition - 2] != 't')
            //return;

            /*
            var pattern = String.Format(
                "([ \\t\\n]|^)({0})(\\*|&)*(\\[(\\,)*\\])*[ \\t]{1}",
                string.Join("|", VsTextViewListener.csharpKeywords),
                trailingCharacters ? "" : "$");
            var regex = new Regex(pattern);
            */
            if (!regex.IsMatch(line.GetText()))
                return;
            //line.Snapshot.GetText(0, caretPosition)

            //line.Snapshot.GetText()

            // there are leading characters
            //if (!leadingCharacters && (int)caretPosition - FirstIndexOfNonWhiteSpace(line) != 2)
                //  return;
            

            var name = Namer.GetAName();

            // insert name
            var span = new Span(caretPosition, 0);
            var snapshot = textView.TextBuffer.Replace(span, name);

            // select name
            textView.Selection.Select(new SnapshotSpan(snapshot, span.Start, name.Length), false);
        }

        /// <summary>
        /// Public access to IOleCommandTarget.QueryStatus() function
        /// </summary>
        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            return nextCommandHandler.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }

        /// <summary>
        /// Try to get the keypress value. Returns 0 if attempt fails
        /// </summary>
        /// <param name="typedChar">Outputs the value of the typed char</param>
        /// <returns>Boolean reporting success or failure of operation</returns>
        bool TryGetTypedChar(Guid cmdGroup, uint nCmdID, IntPtr pvaIn, out char typedChar)
        {
            typedChar = char.MinValue;

            if (cmdGroup != VSConstants.VSStd2K || nCmdID != (uint)VSConstants.VSStd2KCmdID.TYPECHAR)
                return false;

            typedChar = (char)(ushort)Marshal.GetObjectForNativeVariant(pvaIn);
            return true;
        }

        public static int FirstIndexOfNonWhiteSpace(SnapshotSpan text)
        {
            var start = (int)text.Start;
            var end = (int)text.End;
            for (var i = start; i < end; i++)
            {
                if (!char.IsWhiteSpace(text.Snapshot[i]))
                {
                    return i;
                }
            }

            return -1;
        }

    }
}
