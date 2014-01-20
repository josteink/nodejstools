﻿/* ****************************************************************************
 *
 * Copyright (c) Microsoft Corporation. 
 *
 * This source code is subject to terms and conditions of the Apache License, Version 2.0. A 
 * copy of the license can be found in the License.html file at the root of this distribution. If 
 * you cannot locate the Apache License, Version 2.0, please send an email to 
 * vspython@microsoft.com. By using this source code in any fashion, you are agreeing to be bound 
 * by the terms of the Apache License, Version 2.0.
 *
 * You must not remove this notice, or any other, from this software.
 *
 * ***************************************************************************/

using System;
using Microsoft.VisualStudio.Text;

namespace Microsoft.NodejsTools.Jade {
    /// <summary>
    /// Text provider that implements ITextProvider over Visual Studio 
    /// core editor's ITextBuffer or ITextSnapshot 
    /// </summary>
    class TextProvider : ITextProvider, ITextSnapshotProvider {
        static public int BlockLength = 16384;

        string _cachedBlock;
        int _basePosition;
        ITextSnapshot _snapshot;
        bool _partial = false;

        public TextProvider(ITextSnapshot snapshot, bool partial = false) {
            _snapshot = snapshot;
            Length = _snapshot.Length;
            _partial = partial;

            UpdateCachedBlock(0, partial ? BlockLength : snapshot.Length);
        }

        private void UpdateCachedBlock(int position, int length) {
            if (!_partial && _cachedBlock != null)
                return;

            if (_cachedBlock == null || position < _basePosition || (_basePosition + _cachedBlock.Length < position + length)) {
                length = Math.Max(length, BlockLength);
                length = Math.Min(length, _snapshot.Length - position);

                _cachedBlock = _snapshot.GetText(position, length);
                _basePosition = position;
            }
        }

        public int Length { get; private set; }

        public char this[int position] {
            get {
                if (position < 0 || position >= Length)
                    return '\0';

                UpdateCachedBlock(position, 1);
                return _cachedBlock[position - _basePosition];
            }
        }

        public string GetText(int position, int length) {
            UpdateCachedBlock(position, length);
            return _cachedBlock.Substring(position - _basePosition, length);
        }

        public string GetText(ITextRange range) {
            return GetText(range.Start, range.Length);
        }

        public int IndexOf(string text, int startPosition, bool ignoreCase) {
            return IndexOf(text, TextRange.FromBounds(startPosition, this.Length), ignoreCase);
        }

        public int IndexOf(string text, ITextRange range, bool ignoreCase) {
            for (int i = range.Start; i < range.End; i++) {
                bool found = true;
                int k = i;
                int j;

                for (j = 0; j < text.Length && k < range.End; j++, k++) {
                    char ch1 = text[j];
                    char ch2 = this[k];

                    if (ignoreCase) {
                        ch1 = Char.ToLowerInvariant(ch1);
                        ch2 = Char.ToLowerInvariant(ch2);
                    }

                    if (ch1 != ch2) {
                        found = false;
                        break;
                    }
                }

                if (found && j == text.Length) {
                    return i;
                }
            }

            return -1;
        }

        public bool CompareTo(int position, int length, string text, bool ignoreCase) {
            if (text.Length != length)
                return false;

            UpdateCachedBlock(position, Math.Max(length, text.Length));

            return String.Compare(_cachedBlock, position - _basePosition,
                                  text, 0, text.Length,
                                  ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal) == 0;
        }

        public ITextProvider Clone() {
            return new TextProvider(_snapshot, _partial);
        }

        public int Version {
            get { return _snapshot.Version.VersionNumber; }
        }

#pragma warning disable 0067
        public event System.EventHandler<TextChangeEventArgs> OnTextChange;

        #region ITextSnapshotProvider

        public ITextSnapshot Snapshot {
            get { return _snapshot; }
        }

        #endregion
    }
}
