using System;

namespace _Project.Cutscenes
{
    [Serializable]
    public sealed class StoryDialogueLine
    {
        public StoryDialogueLine(string speaker, string emotion, string text)
        {
            Speaker = speaker;
            Emotion = emotion;
            Text = text;
        }

        public string Speaker { get; }
        public string Emotion { get; }
        public string Text { get; }
    }
}
