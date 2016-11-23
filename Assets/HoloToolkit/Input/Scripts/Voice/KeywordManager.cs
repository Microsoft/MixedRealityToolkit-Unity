﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Windows.Speech;
using UnityEngine.VR.WSA.Input;
using System;

namespace HoloToolkit.Unity.InputModule
{
    /// <summary>
    /// KeywordManager allows you to specify keywords and methods in the Unity
    /// Inspector, instead of registering them explicitly in code.
    /// This also includes a setting to either automatically start the
    /// keyword recognizer or allow your code to start it.
    ///
    /// IMPORTANT: Please make sure to add the microphone capability in your app, in Unity under
    /// Edit -> Project Settings -> Player -> Settings for Windows Store -> Publishing Settings -> Capabilities
    /// or in your Visual Studio Package.appxmanifest capabilities.
    /// </summary>
    public partial class KeywordManager : BaseInputSource
    {
        [System.Serializable]
        public struct KeywordAndKeyCode
        {
            [Tooltip("The keyword to recognize.")]
            public string Keyword;
            [Tooltip("The KeyCode to recognize.")]
            public KeyCode KeyCode;
        }

        // This enumeration gives the manager two different ways to handle the recognizer. Both will
        // set up the recognizer and add all keywords. The first causes the recognizer to start
        // immediately. The second allows the recognizer to be manually started at a later time.
        public enum RecognizerStartBehavior { AutoStart, ManualStart };

        [Tooltip("An enumeration to set whether the recognizer should start on or off.")]
        public RecognizerStartBehavior RecognizerStart;

        [Tooltip("An array of string keywords and keys, to be set in the Inspector.")]
        public KeywordAndKeyCode[] KeywordsAndKeys;

        private KeywordRecognizer keywordRecognizer;

        public override SupportedInputEvents SupportedEvents
        {
            get
            {
                return SupportedInputEvents.SourceUpAndDown;
            }
        }

        void Start()
        {
            if (KeywordsAndKeys.Length > 0)
            {
                string[] keywords = KeywordsAndKeys.Select(keywordAndKey => keywordAndKey.Keyword).ToArray();
                keywordRecognizer = new KeywordRecognizer(keywords);
                keywordRecognizer.OnPhraseRecognized += KeywordRecognizer_OnPhraseRecognized;

                if (RecognizerStart == RecognizerStartBehavior.AutoStart)
                {
                    keywordRecognizer.Start();
                }
            }
            else
            {
                Debug.LogError("Must have at least one keyword specified in the Inspector on " + gameObject.name + ".");
            }
        }

        void Update()
        {
            ProcessKeyBindings();
        }

        void OnDestroy()
        {
            if (keywordRecognizer != null)
            {
                StopKeywordRecognizer();
                keywordRecognizer.OnPhraseRecognized -= KeywordRecognizer_OnPhraseRecognized;
                keywordRecognizer.Dispose();
            }
        }

        private void ProcessKeyBindings()
        {
            foreach (var kvp in KeywordsAndKeys)
            {
                if (Input.GetKeyDown(kvp.KeyCode))
                {
                    OnPhraseRecognized(ConfidenceLevel.High, TimeSpan.Zero, DateTime.Now, null, kvp.Keyword);
                    return;
                }
            }
        }

        private void KeywordRecognizer_OnPhraseRecognized(UnityEngine.Windows.Speech.PhraseRecognizedEventArgs args)
        {
            PhraseRecognizedEventArgs raiseArgs = new PhraseRecognizedEventArgs(this, 0, args.confidence, args.phraseDuration, args.phraseStartTime, args.semanticMeanings, args.text);
            RaisePhraseRecognizedEvent(raiseArgs);
        }

        /// <summary>
        /// Make sure the keyword recognizer is off, then start it.
        /// Otherwise, leave it alone because it's already in the desired state.
        /// </summary>
        public void StartKeywordRecognizer()
        {
            if (keywordRecognizer != null && !keywordRecognizer.IsRunning)
            {
                keywordRecognizer.Start();
            }
        }

        /// <summary>
        /// Make sure the keyword recognizer is on, then stop it.
        /// Otherwise, leave it alone because it's already in the desired state.
        /// </summary>
        public void StopKeywordRecognizer()
        {
            if (keywordRecognizer != null && keywordRecognizer.IsRunning)
            {
                keywordRecognizer.Stop();
            }
        }

        private void OnPhraseRecognized(UnityEngine.Windows.Speech.ConfidenceLevel confidence,
            TimeSpan phraseDuration, DateTime phraseStartTime,
            UnityEngine.Windows.Speech.SemanticMeaning[] semanticMeanings,
            string text)
        {
            PhraseRecognizedEventArgs raiseArgs = new PhraseRecognizedEventArgs(this, 0, confidence, phraseDuration, phraseStartTime, semanticMeanings, text);
            RaisePhraseRecognizedEvent(raiseArgs);
        }

        public override bool TryGetPosition(uint sourceId, out Vector3 position)
        {
            position = Vector3.zero;
            return false;
        }

        public override bool TryGetOrientation(uint sourceId, out Quaternion orientation)
        {
            orientation = Quaternion.identity;
            return false;
        }

        public override SupportedInputInfo GetSupportedInputInfo(uint sourceId)
        {
            return SupportedInputInfo.None;
        }
    }
}